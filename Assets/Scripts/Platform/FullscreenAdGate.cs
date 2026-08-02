using System;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Serializes fullscreen-ad requests and fail-opens their continuation. The
    /// bridge is intentionally unaware of game state, scenes, and UI lifetime.
    /// </summary>
    public sealed class FullscreenAdGate
    {
        public const float DefaultTimeoutSeconds = 60f;

        private readonly IFullscreenAdBridge _bridge;
        private readonly IUnscaledClock _clock;
        private readonly float _timeoutSeconds;
        private PendingRequest? _pending;
        private ulong _nextRequestId;

        public FullscreenAdGate(
            IFullscreenAdBridge bridge,
            IUnscaledClock clock,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (timeoutSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            _timeoutSeconds = timeoutSeconds;
        }

        public bool IsRequestPending => _pending != null;

        /// <summary>
        /// Starts one request, or immediately continues when ads are unavailable.
        /// A second tap while a request is pending is ignored.
        /// </summary>
        public bool Request(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException(nameof(continuation));
            if (_pending != null)
                return false;

            if (!_bridge.IsAvailable)
            {
                InvokeContinuation(continuation);
                return true;
            }

            var requestId = $"fullscreen-{++_nextRequestId}";
            _pending = new PendingRequest(requestId, _clock.Now + _timeoutSeconds, continuation);

            try
            {
                _bridge.ShowFullscreen(requestId);
            }
            catch (Exception)
            {
                // Browser bridge failures must never block a player from restarting.
                CompletePendingRequest();
            }

            return true;
        }

        /// <summary>
        /// Completes only the currently pending request. Unknown, stale, and
        /// duplicate browser messages are intentionally ignored.
        /// </summary>
        public bool HandleTerminalResponse(string requestId)
        {
            if (_pending == null || string.IsNullOrEmpty(requestId) ||
                !string.Equals(_pending.RequestId, requestId, StringComparison.Ordinal))
                return false;

            CompletePendingRequest();
            return true;
        }

        /// <summary>
        /// Invalidates the current request without invoking its continuation.
        /// Browser terminal messages can still arrive afterwards, but are stale.
        /// </summary>
        public bool CancelPendingRequest()
        {
            if (_pending == null)
                return false;

            _pending = null;
            return true;
        }

        /// <summary>Call from a persistent MonoBehaviour using unscaled time.</summary>
        public void Tick()
        {
            if (_pending != null && _clock.Now >= _pending.Deadline)
                CompletePendingRequest();
        }

        private void CompletePendingRequest()
        {
            var pending = _pending;
            if (pending == null)
                return;

            // Clear before invoking user code so reentrant calls create a fresh
            // request instead of reusing an already terminalized request.
            _pending = null;
            InvokeContinuation(pending.Continuation);
        }

        private static void InvokeContinuation(Action continuation)
        {
            try
            {
                continuation();
            }
            catch (Exception exception)
            {
                // A continuation failure is application-owned; it still cannot
                // resurrect the completed ad request.
                UnityEngine.Debug.LogException(exception);
            }
        }

        private sealed class PendingRequest
        {
            public PendingRequest(string requestId, float deadline, Action continuation)
            {
                RequestId = requestId;
                Deadline = deadline;
                Continuation = continuation;
            }

            public string RequestId { get; }
            public float Deadline { get; }
            public Action Continuation { get; }
        }
    }

    public interface IFullscreenAdBridge
    {
        bool IsAvailable { get; }
        void ShowFullscreen(string requestId);
    }

    public interface IUnscaledClock
    {
        float Now { get; }
    }
}
