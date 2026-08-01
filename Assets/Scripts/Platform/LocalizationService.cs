using System;
using UnityEngine;

namespace ChaosArena.Platform
{
    public sealed class LocalizationService : MonoBehaviour
    {
        private const string ManualLanguagePreferenceKey = "settings.language.manual";

        public static LocalizationService Instance { get; private set; }

        private readonly LocalizationSelectionState _selection = new();
        private string _currentLanguage = LocalizationLanguagePolicy.English;

        public string CurrentLanguage => _currentLanguage;
        public bool UsesManualLanguage => _selection.UsesManualLanguage;

        public event Action<string>? LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var serviceObject = new GameObject(nameof(LocalizationService));
            serviceObject.AddComponent<LocalizationService>();
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

            if (PlayerPrefs.HasKey(ManualLanguagePreferenceKey))
                _selection.SetManualLanguage(PlayerPrefs.GetString(ManualLanguagePreferenceKey));

            YandexPlatformService.LanguageReady += OnPlatformLanguageReady;
            ApplyCurrentLanguage(notify: false);
        }

        private void OnDestroy()
        {
            YandexPlatformService.LanguageReady -= OnPlatformLanguageReady;
            if (Instance == this)
                Instance = null;
        }

        public void UseRussian() => SetManualLanguage(LocalizationLanguagePolicy.Russian);
        public void UseEnglish() => SetManualLanguage(LocalizationLanguagePolicy.English);
        public void UseTurkish() => SetManualLanguage(LocalizationLanguagePolicy.Turkish);

        public void SetManualLanguage(string languageCode)
        {
            _selection.SetManualLanguage(languageCode);
            PlayerPrefs.SetString(ManualLanguagePreferenceKey, _selection.CurrentLanguage);
            PlayerPrefs.Save();
            ApplyCurrentLanguage();
        }

        public void ResetToAuto() => UsePlatformLanguage();

        public void UsePlatformLanguage()
        {
            _selection.UsePlatformLanguage();
            PlayerPrefs.DeleteKey(ManualLanguagePreferenceKey);
            PlayerPrefs.Save();
            ApplyCurrentLanguage();
        }

        public string Get(string key) => LocalizationCatalog.Get(key, CurrentLanguage);
        public string Format(string key, params object[] arguments) => LocalizationCatalog.Format(key, CurrentLanguage, arguments);

        public static string GetText(string key) =>
            Instance != null ? Instance.Get(key) : LocalizationCatalog.Get(key, LocalizationLanguagePolicy.English);

        public static string FormatText(string key, params object[] arguments) =>
            Instance != null
                ? Instance.Format(key, arguments)
                : LocalizationCatalog.Format(key, LocalizationLanguagePolicy.English, arguments);

        private void OnPlatformLanguageReady(string languageCode)
        {
            _selection.SetPlatformLanguage(languageCode);
            ApplyCurrentLanguage();
        }

        private void ApplyCurrentLanguage(bool notify = true)
        {
            var nextLanguage = _selection.CurrentLanguage;
            if (_currentLanguage == nextLanguage)
                return;

            _currentLanguage = nextLanguage;
            if (notify)
                LanguageChanged?.Invoke(_currentLanguage);
        }
    }
}
