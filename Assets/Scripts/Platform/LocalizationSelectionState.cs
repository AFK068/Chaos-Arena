namespace ChaosArena.Platform
{
    /// <summary>
    /// Pure selection state: platform language drives auto mode, while a
    /// persisted manual choice remains authoritative until explicitly reset.
    /// </summary>
    public sealed class LocalizationSelectionState
    {
        public string PlatformLanguage { get; private set; } = LocalizationLanguagePolicy.English;
        public string? ManualLanguage { get; private set; }

        public string CurrentLanguage => ManualLanguage ?? PlatformLanguage;
        public bool UsesManualLanguage => ManualLanguage != null;

        public void SetPlatformLanguage(string? sdkLanguage) =>
            PlatformLanguage = LocalizationLanguagePolicy.FromSdkLanguage(sdkLanguage);

        public void SetManualLanguage(string? languageCode) =>
            ManualLanguage = LocalizationLanguagePolicy.NormalizeSupportedLanguage(languageCode);

        public void UsePlatformLanguage() => ManualLanguage = null;
    }
}
