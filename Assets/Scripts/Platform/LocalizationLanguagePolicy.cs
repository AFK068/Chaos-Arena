using System;

namespace ChaosArena.Platform
{
    public static class LocalizationLanguagePolicy
    {
        public const string English = "en";
        public const string Russian = "ru";

        public static string FromSdkLanguage(string? sdkLanguage)
        {
            var primaryLanguage = (sdkLanguage ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Split('-', '_')[0];

            return primaryLanguage is "ru" or "be" or "kk" or "uk" or "uz"
                ? Russian
                : English;
        }

        public static string NormalizeSupportedLanguage(string? languageCode) =>
            string.Equals(languageCode, Russian, StringComparison.OrdinalIgnoreCase)
                ? Russian
                : English;
    }
}
