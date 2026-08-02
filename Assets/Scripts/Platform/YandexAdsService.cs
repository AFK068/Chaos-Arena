using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Persistent fullscreen-ad receiver. Platform pause/resume remains owned by
    /// <see cref="YandexPlatformService"/>; this service only gates a continuation.
    /// </summary>
    public sealed class YandexAdsService : MonoBehaviour
    {
        public static YandexAdsService? Instance { get; private set; }

        private FullscreenAdGate? _gate;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void YandexGames_SetFullscreenAdReceiver(string gameObjectName);

        [DllImport("__Internal")]
        private static extern void YandexGames_ShowFullscreenAdv(string requestId);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var serviceObject = new GameObject(nameof(YandexAdsService));
            serviceObject.AddComponent<YandexAdsService>();
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
            _gate = new FullscreenAdGate(new WebGlFullscreenAdBridge(), new UnityUnscaledClock());

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGames_SetFullscreenAdReceiver(gameObject.name);
#endif
        }

        private void Update() => _gate?.Tick();

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Requests one fullscreen ad before a game-over restart. The continuation
        /// should target a persistent service (for example GameManager), not scene UI.
        /// </summary>
        public static bool RequestFullscreenBefore(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException(nameof(continuation));

            // This also covers unusually early calls during bootstrap and keeps
            // editor/non-WebGL behaviour fail-open.
            if (Instance == null || Instance._gate == null)
            {
                continuation();
                return true;
            }

            return Instance._gate.Request(continuation);
        }

        /// <summary>Invalidates a pending fullscreen continuation, if any.</summary>
        public static bool CancelPendingFullscreenRequest() =>
            Instance?._gate?.CancelPendingRequest() ?? false;

        // Called by YandexGames.jslib through SendMessage. Its JSON payload carries
        // a request id and a terminal result; result details are intentionally unused.
        public void OnYandexGamesFullscreenAdTerminal(string payload)
        {
            if (_gate == null || string.IsNullOrWhiteSpace(payload))
                return;

            try
            {
                var response = JsonUtility.FromJson<FullscreenAdTerminalResponse>(payload);
                if (response != null)
                    _gate.HandleTerminalResponse(response.requestId);
            }
            catch (ArgumentException)
            {
                // Malformed browser input is equivalent to an unknown response; the
                // timeout remains responsible for fail-open completion.
            }
        }

        [Serializable]
        private sealed class FullscreenAdTerminalResponse
        {
            public string requestId = string.Empty;
            public string result = string.Empty;
        }

        private sealed class UnityUnscaledClock : IUnscaledClock
        {
            public float Now => Time.unscaledTime;
        }

        private sealed class WebGlFullscreenAdBridge : IFullscreenAdBridge
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            public bool IsAvailable => true;

            public void ShowFullscreen(string requestId) => YandexGames_ShowFullscreenAdv(requestId);
#else
            public bool IsAvailable => false;

            public void ShowFullscreen(string requestId) => throw new PlatformNotSupportedException();
#endif
        }
    }
}
