using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Persistent Player/Data receiver. JavaScript owns the SDK Player instance;
    /// this class only matches one terminal JSON response to one request id.
    /// </summary>
    public sealed class YandexPlayerService : MonoBehaviour, ICloudProgressTransport
    {
        public const float RequestTimeoutSeconds = 15f;
        public static YandexPlayerService? Instance { get; private set; }

        private readonly Dictionary<string, PendingRequest> _pending = new(StringComparer.Ordinal);
        private CloudProgressSyncCoordinator? _sync;
        // Kept as a delegate so deadline behavior remains deterministic in
        // edit-mode coverage without using scaled game time.
        internal Func<float> UnscaledNow { get; set; } = () => Time.unscaledTime;

        public CloudSaveStatus Status => _sync?.Status ?? CloudSaveStatus.LocalOnly;
        public bool IsAvailable => YandexPlatformService.Instance != null && YandexPlatformService.Instance.IsBridgeReady;

        public event Action<CloudSaveStatus>? StatusChanged;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void YandexGames_SetPlayerReceiver(string gameObjectName);

        [DllImport("__Internal")]
        private static extern void YandexGames_PlayerGetGuest(string requestId);

        [DllImport("__Internal")]
        private static extern void YandexGames_PlayerGetData(string requestId);

        [DllImport("__Internal")]
        private static extern void YandexGames_PlayerSetData(string requestId, string serializedDocument);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var serviceObject = new GameObject(nameof(YandexPlayerService));
            serviceObject.AddComponent<YandexPlayerService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            YandexPlatformService.BridgeReady += OnBridgeReady;
        }

        private void Update()
        {
            ExpirePendingRequests();
            ProgressService.Shared.TickCloudSync();
        }

        private void OnDestroy()
        {
            YandexPlatformService.BridgeReady -= OnBridgeReady;
            FailAllPendingRequests();
            if (Instance == this)
                Instance = null;
        }

        public void GetGuest(Action<CloudTransportResult> completed)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Begin(completed, YandexGames_PlayerGetGuest);
#else
            completed(new CloudTransportResult(false));
#endif
        }

        public void GetData(Action<CloudTransportResult> completed)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Begin(completed, YandexGames_PlayerGetData);
#else
            completed(new CloudTransportResult(false));
#endif
        }

        public void SetData(string serializedDocument, bool flush, Action<CloudTransportResult> completed)
        {
            // The current bridge contract always sends flush:true. Keep the
            // parameter on the C# boundary so tests cannot accidentally relax it.
            if (!flush)
            {
                completed(new CloudTransportResult(false));
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            Begin(completed, requestId => YandexGames_PlayerSetData(requestId, serializedDocument));
#else
            completed(new CloudTransportResult(false));
#endif
        }

        // Called via SendMessage. Unknown/late/duplicate ids are ignored because
        // their entry was already removed from the one-shot pending map.
        public void OnYandexGamesPlayerTerminal(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return;

            PlayerBridgeResponse response;
            try { response = JsonUtility.FromJson<PlayerBridgeResponse>(payload); }
            catch (Exception) { return; }

            if (response == null || string.IsNullOrWhiteSpace(response.requestId) ||
                !_pending.TryGetValue(response.requestId, out var pending))
                return;

            // A reply with a known request id but no terminal result is malformed.
            // Remove it and fail open to the coordinator rather than leaving its
            // single-flight state blocked until an unrelated future request.
            _pending.Remove(response.requestId);
            var success = !string.IsNullOrWhiteSpace(response.result) &&
                string.Equals(response.result, "ok", StringComparison.Ordinal);
            pending.Completed(new CloudTransportResult(success, response.data));
        }

        private void OnBridgeReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGames_SetPlayerReceiver(gameObject.name);
#endif
            if (_sync != null)
                return;

            var progress = ProgressService.Shared;
            _sync = new CloudProgressSyncCoordinator(
                this,
                progress.SerializeSnapshot,
                progress.TryMergeSerialized,
                () => Time.realtimeSinceStartup);
            _sync.StatusChanged += status => StatusChanged?.Invoke(status);
            progress.AttachCloudSync(_sync);
        }

        private delegate void PlayerRequest(string requestId);

        private void Begin(Action<CloudTransportResult> completed, PlayerRequest request)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            if (!IsAvailable)
            {
                completed(new CloudTransportResult(false));
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            var requestId = Guid.NewGuid().ToString("N");
            _pending.Add(requestId, new PendingRequest(completed, UnscaledNow() + RequestTimeoutSeconds));
            try
            {
                request(requestId);
            }
            catch (Exception)
            {
                CompletePending(requestId, new CloudTransportResult(false));
            }
#else
            completed(new CloudTransportResult(false));
#endif
        }

        private void ExpirePendingRequests()
        {
            if (_pending.Count == 0)
                return;

            var now = UnscaledNow();
            var expiredRequestIds = new List<string>();
            foreach (var pair in _pending)
            {
                if (now >= pair.Value.Deadline)
                    expiredRequestIds.Add(pair.Key);
            }

            foreach (var requestId in expiredRequestIds)
                CompletePending(requestId, new CloudTransportResult(false));
        }

        private void FailAllPendingRequests()
        {
            if (_pending.Count == 0)
                return;

            var requestIds = new List<string>(_pending.Keys);
            foreach (var requestId in requestIds)
                CompletePending(requestId, new CloudTransportResult(false));
        }

        private void CompletePending(string requestId, CloudTransportResult result)
        {
            if (!_pending.TryGetValue(requestId, out var pending))
                return;

            _pending.Remove(requestId);
            pending.Completed(result);
        }

        private sealed class PendingRequest
        {
            public PendingRequest(Action<CloudTransportResult> completed, float deadline)
            {
                Completed = completed;
                Deadline = deadline;
            }

            public Action<CloudTransportResult> Completed { get; }
            public float Deadline { get; }
        }

        [Serializable]
        private sealed class PlayerBridgeResponse
        {
            public string requestId = string.Empty;
            public string result = string.Empty;
            public string data = string.Empty;
        }
    }
}
