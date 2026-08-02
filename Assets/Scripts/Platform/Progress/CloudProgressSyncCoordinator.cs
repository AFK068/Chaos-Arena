using System;
using System.Text;

namespace ChaosArena.Platform
{
    public enum CloudSaveStatus
    {
        LocalOnly,
        Connecting,
        Synced,
        Error
    }

    /// <summary>Small transport boundary: all completions are delivered on Unity's main thread.</summary>
    public interface ICloudProgressTransport
    {
        bool IsAvailable { get; }
        void GetGuest(Action<CloudTransportResult> completed);
        void GetData(Action<CloudTransportResult> completed);
        void SetData(string serializedDocument, bool flush, Action<CloudTransportResult> completed);
    }

    public readonly struct CloudTransportResult
    {
        public CloudTransportResult(bool success, string data = "")
        {
            Success = success;
            Data = data ?? string.Empty;
        }

        public bool Success { get; }
        public string Data { get; }
    }

    /// <summary>
    /// Local-first, single-flight cloud state machine. It deliberately knows no
    /// PlayerPrefs details: the supplied delegates synchronously snapshot and
    /// persist the existing versioned progress document.
    /// </summary>
    public sealed class CloudProgressSyncCoordinator
    {
        public const int MaxPayloadBytes = 200 * 1024;
        private const float ReadRetryInitialSeconds = 1f;
        private const float ReadRetryMaximumSeconds = 30f;
        private const float WriteRetryDelaySeconds = 5f;
        // At most 75 writes per five minutes, leaving headroom beneath the
        // documented 100/5min data API budget for reads and transient retries.
        private const float MinimumWriteSpacingSeconds = 4f;

        private readonly ICloudProgressTransport _transport;
        private readonly Func<string> _snapshotSerialized;
        private readonly Func<string, bool> _mergeAndPersist;
        private readonly Func<float> _now;
        private readonly Random _retryRandom = new Random();
        private readonly Func<float> _retryJitter;

        private bool _initialized;
        private bool _busy;
        private bool _guestReady;
        private bool _initialReadComplete;
        private string _dirtyDocument = string.Empty;
        private string _inFlightDocument = string.Empty;
        private float _nextRetryAt;
        private float _nextReadRetryAt;
        private int _consecutiveReadFailures;
        private ActiveOperation _operation;

        private enum ActiveOperation
        {
            None,
            Guest,
            Read,
            Write
        }

        public CloudProgressSyncCoordinator(
            ICloudProgressTransport transport,
            Func<string> snapshotSerialized,
            Func<string, bool> mergeAndPersist,
            Func<float> now = null,
            Func<float> retryJitter = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _snapshotSerialized = snapshotSerialized ?? throw new ArgumentNullException(nameof(snapshotSerialized));
            _mergeAndPersist = mergeAndPersist ?? throw new ArgumentNullException(nameof(mergeAndPersist));
            _now = now ?? (() => 0f);
            _retryJitter = retryJitter ?? (() => (float)_retryRandom.NextDouble());
        }

        public CloudSaveStatus Status { get; private set; } = CloudSaveStatus.LocalOnly;
        public bool IsBusy => _busy;
        public bool IsDirty => !string.IsNullOrEmpty(_dirtyDocument);

        public event Action<CloudSaveStatus>? StatusChanged;

        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            if (!_transport.IsAvailable)
            {
                SetStatus(CloudSaveStatus.LocalOnly);
                return;
            }

            SetStatus(CloudSaveStatus.Connecting);
            AcquireGuest();
        }

        /// <summary>Called after the synchronous local write from a meaningful run.</summary>
        public void NotifyLocalChanged()
        {
            var snapshot = _snapshotSerialized();
            _dirtyDocument = snapshot;
            TryWriteDirty();
        }

        /// <summary>Bounded retry hook; call from a main-thread Update loop.</summary>
        public void Tick()
        {
            if (_busy || !_initialized || !_transport.IsAvailable)
                return;

            // A write is never permitted until a successful, parseable read has
            // established the remote baseline.  This also coalesces repeated
            // Tick calls while a failed read is waiting for its retry window.
            if (!_guestReady)
            {
                if (_now() >= _nextReadRetryAt)
                    AcquireGuest();
                return;
            }

            if (!_initialReadComplete)
            {
                if (_now() >= _nextReadRetryAt)
                    ReadRemote();
                return;
            }

            if (IsDirty && _now() >= _nextRetryAt)
                TryWriteDirty();
        }

        private void HandleGuest(CloudTransportResult result)
        {
            if (_operation != ActiveOperation.Guest)
                return;

            _busy = false;
            _operation = ActiveOperation.None;
            if (!result.Success)
            {
                ScheduleReadRetry();
                SetStatus(CloudSaveStatus.Error);
                return;
            }

            _guestReady = true;
            ReadRemote();
        }

        private void AcquireGuest()
        {
            if (_busy)
                return;

            _busy = true;
            _operation = ActiveOperation.Guest;
            _transport.GetGuest(HandleGuest);
        }

        private void ReadRemote()
        {
            if (_busy)
                return;

            // A successful, parseable read establishes the only baseline that
            // permits writes, so a failed guest read can never overwrite data.
            _initialReadComplete = false;
            _busy = true;
            _operation = ActiveOperation.Read;
            _transport.GetData(HandleRemoteRead);
        }

        private void HandleRemoteRead(CloudTransportResult result)
        {
            if (_operation != ActiveOperation.Read)
                return;

            _busy = false;
            _operation = ActiveOperation.None;
            if (!result.Success)
            {
                ScheduleReadRetry();
                SetStatus(CloudSaveStatus.Error);
                return;
            }

            // Empty data is a valid baseline for a first cloud save.  A nonempty
            // value must parse and merge successfully; treating invalid/future
            // data as an empty document could overwrite another device's state.
            if (!string.IsNullOrWhiteSpace(result.Data) && !_mergeAndPersist(result.Data))
            {
                ScheduleReadRetry();
                SetStatus(CloudSaveStatus.Error);
                return;
            }

            _initialReadComplete = true;
            _consecutiveReadFailures = 0;
            _nextReadRetryAt = 0f;

            // Always flush the merged aggregate baseline. A local mutation may
            // have happened while the read was in flight, so snapshot only
            // after merging to include both remote and local progress.
            _dirtyDocument = _snapshotSerialized();
            TryWriteDirty();
        }

        private void TryWriteDirty()
        {
            if (_busy || !_initialReadComplete || string.IsNullOrEmpty(_dirtyDocument) ||
                !_transport.IsAvailable || _now() < _nextRetryAt)
                return;

            if (Encoding.UTF8.GetByteCount(_dirtyDocument) > MaxPayloadBytes)
            {
                // A document can only become writeable again after a new local
                // snapshot replaces it; do not hammer the API with an invalid payload.
                _nextRetryAt = float.PositiveInfinity;
                SetStatus(CloudSaveStatus.Error);
                return;
            }

            _inFlightDocument = _dirtyDocument;
            _busy = true;
            _operation = ActiveOperation.Write;
            _transport.SetData(_inFlightDocument, flush: true, HandleRemoteWrite);
        }

        private void HandleRemoteWrite(CloudTransportResult result)
        {
            if (_operation != ActiveOperation.Write)
                return;

            _busy = false;
            _operation = ActiveOperation.None;
            if (!result.Success)
            {
                _nextRetryAt = _now() + WriteRetryDelaySeconds;
                SetStatus(CloudSaveStatus.Error);
                return;
            }

            if (string.Equals(_dirtyDocument, _inFlightDocument, StringComparison.Ordinal))
                _dirtyDocument = string.Empty;

            _nextRetryAt = _now() + MinimumWriteSpacingSeconds;
            SetStatus(CloudSaveStatus.Synced);
            // A run that completed during this request is coalesced into one next write.
            TryWriteDirty();
        }

        private void ScheduleReadRetry()
        {
            _consecutiveReadFailures = Math.Min(_consecutiveReadFailures + 1, 6);
            var exponentialDelay = Math.Min(
                ReadRetryMaximumSeconds,
                ReadRetryInitialSeconds * (1 << (_consecutiveReadFailures - 1)));
            // Bounded jitter avoids synchronized client retries and is injected
            // in tests when an exact retry deadline matters.
            var jitter = 0.85f + Math.Max(0f, Math.Min(1f, _retryJitter())) * 0.30f;
            _nextReadRetryAt = _now() + Math.Min(ReadRetryMaximumSeconds, exponentialDelay * jitter);
        }

        private void SetStatus(CloudSaveStatus status)
        {
            if (Status == status)
                return;

            Status = status;
            StatusChanged?.Invoke(status);
        }
    }
}
