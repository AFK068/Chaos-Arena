using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Minimal, fail-open Yandex Games lifecycle integration. It deliberately
    /// excludes ads, authorization, saves, and translated UI content.
    /// </summary>
    public sealed class YandexPlatformService : MonoBehaviour
    {
        private const string FallbackLanguage = "en";

        public static YandexPlatformService Instance { get; private set; }

        private static event Action<string>? LanguageReadySubscribers;

        /// <summary>
        /// Subscribers receive the already-known value immediately, so services
        /// created after SDK initialization do not miss the startup language.
        /// </summary>
        public static event Action<string> LanguageReady
        {
            add
            {
                LanguageReadySubscribers += value;
                if (Instance != null && Instance.IsBridgeReady)
                    Instance.InvokeLanguageSubscriber(value, Instance.LanguageCode);
            }
            remove => LanguageReadySubscribers -= value;
        }

        public string LanguageCode { get; private set; } = FallbackLanguage;
        public bool IsBridgeReady { get; private set; }

        private readonly GameplayStateMachine _state = new();
        private bool _menuIsInteractive;
        private bool _gameReadyReported;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void YandexGames_Initialize(string gameObjectName);

        [DllImport("__Internal")]
        private static extern void YandexGames_LoadingReady();

        [DllImport("__Internal")]
        private static extern void YandexGames_GameplayStart();

        [DllImport("__Internal")]
        private static extern void YandexGames_GameplayStop();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var serviceObject = new GameObject(nameof(YandexPlatformService));
            serviceObject.AddComponent<YandexPlatformService>();
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
            _state.GameplayStateChanged += ReportGameplayState;

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGames_Initialize(gameObject.name);
#else
            // Editor and non-WebGL builds stay playable without a JavaScript SDK.
            OnYandexGamesInitialized(FallbackLanguage);
#endif
        }

        private void OnDestroy()
        {
            _state.GameplayStateChanged -= ReportGameplayState;
            if (Instance == this)
                Instance = null;
        }

        public static void NotifyMainMenuInteractive() => Instance?.MarkMainMenuInteractive();
        public static void SetGameplayIntent(bool active) => Instance?.SetGameplayIntentInternal(active);
        public static void SetLocalPause(bool paused) => Instance?.SetLocalPauseInternal(paused);

        private void MarkMainMenuInteractive()
        {
            _menuIsInteractive = true;
            ReportGameReadyIfPossible();
        }

        private void SetGameplayIntentInternal(bool active)
        {
            _state.SetGameplayIntent(active);
            ApplyLocalRuntimePause();
        }

        private void SetLocalPauseInternal(bool paused)
        {
            _state.SetLocalPause(paused);
            ApplyLocalRuntimePause();
        }

        // Called by the WebGL bridge via SendMessage after YaGames.init().
        public void OnYandexGamesInitialized(string languageCode)
        {
            LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? FallbackLanguage : languageCode;
            IsBridgeReady = true;
            NotifyLanguageReady();
            _state.SetBridgeReady();
            ApplyLocalRuntimePause();
            ReportGameReadyIfPossible();
        }

        // Called by the WebGL bridge. Failure is intentionally non-fatal.
        public void OnYandexGamesError(string message)
        {
            Debug.LogWarning($"Yandex Games SDK is unavailable; continuing without platform features. {message}");
        }

        // Called by SDK game_api_pause/game_api_resume events.
        public void OnYandexGamesPlatformPause(string _)
        {
            _state.SetPlatformPause(true);
            ApplyLocalRuntimePause();
        }

        public void OnYandexGamesPlatformResume(string _)
        {
            _state.SetPlatformPause(false);
            ApplyLocalRuntimePause();
        }

        private void ReportGameReadyIfPossible()
        {
            if (!_menuIsInteractive || !IsBridgeReady || _gameReadyReported)
                return;

            _gameReadyReported = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGames_LoadingReady();
#endif
        }

        private void ReportGameplayState(bool active)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (active)
                YandexGames_GameplayStart();
            else
                YandexGames_GameplayStop();
#endif
        }

        private void ApplyLocalRuntimePause()
        {
            // Platform resume never reactivates sound or simulation while the
            // local pause owner still holds them.
            AudioListener.pause = _state.ShouldPauseAudio;
            Time.timeScale = _state.ShouldPauseSimulation ? 0f : 1f;
        }

        private void NotifyLanguageReady()
        {
            var subscribers = LanguageReadySubscribers;
            if (subscribers == null)
                return;

            foreach (var callback in subscribers.GetInvocationList())
            {
                if (callback is Action<string> subscriber)
                    InvokeLanguageSubscriber(subscriber, LanguageCode);
            }
        }

        private void InvokeLanguageSubscriber(Action<string> subscriber, string languageCode)
        {
            try
            {
                subscriber(languageCode);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
