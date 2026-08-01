using System;

namespace ChaosArena.Platform
{
    public static class LocalizationLanguagePolicy
    {
        public const string English = "en";
        public const string Russian = "ru";
        public const string Turkish = "tr";

        public static string FromSdkLanguage(string? sdkLanguage)
        {
            var primaryLanguage = (sdkLanguage ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Split('-', '_')[0];

            return primaryLanguage switch
            {
                "tr" => Turkish,
                "ru" or "be" or "kk" or "uk" or "uz" => Russian,
                _ => English
            };
        }

        public static string NormalizeSupportedLanguage(string? languageCode)
        {
            if (string.Equals(languageCode, Russian, StringComparison.OrdinalIgnoreCase))
                return Russian;
            if (string.Equals(languageCode, Turkish, StringComparison.OrdinalIgnoreCase))
                return Turkish;
            return English;
        }

        public static string NextManualLanguage(string? currentLanguage) =>
            NormalizeSupportedLanguage(currentLanguage) switch
            {
                English => Russian,
                Russian => Turkish,
                _ => English
            };
    }
}
