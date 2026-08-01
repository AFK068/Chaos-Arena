using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class LocalizationFoundationTests
    {
        [TestCase("ru")]
        [TestCase("be")]
        [TestCase("kk")]
        [TestCase("uk")]
        [TestCase("uz")]
        [TestCase("ru-RU")]
        public void SupportedSdkLanguagesMapToRussian(string sdkLanguage)
        {
            Assert.That(LocalizationLanguagePolicy.FromSdkLanguage(sdkLanguage),
                Is.EqualTo(LocalizationLanguagePolicy.Russian));
        }

        [TestCase("en")]
        [TestCase("tr")]
        [TestCase("")]
        [TestCase(null)]
        public void OtherSdkLanguagesFallbackToEnglish(string? sdkLanguage)
        {
            Assert.That(LocalizationLanguagePolicy.FromSdkLanguage(sdkLanguage),
                Is.EqualTo(LocalizationLanguagePolicy.English));
        }

        [Test]
        public void ManualLanguageWinsUntilResetToAuto()
        {
            var state = new LocalizationSelectionState();
            state.SetPlatformLanguage("ru");
            state.SetManualLanguage("en");
            state.SetPlatformLanguage("uz");

            Assert.That(state.CurrentLanguage, Is.EqualTo(LocalizationLanguagePolicy.English));
            Assert.That(state.UsesManualLanguage, Is.True);

            state.UsePlatformLanguage();

            Assert.That(state.CurrentLanguage, Is.EqualTo(LocalizationLanguagePolicy.Russian));
            Assert.That(state.UsesManualLanguage, Is.False);
        }

        [Test]
        public void CatalogUsesEnglishFallbackAndExposesMissingKeys()
        {
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.NewRun, "tr"), Is.EqualTo("New Run"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.Return, "ru"), Is.EqualTo("Назад"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.MobileDash, "ru"), Is.EqualTo("Рывок"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.MobileInteract, "en"), Is.EqualTo("Use"));
            Assert.That(LocalizationCatalog.Get("missing.key", "ru"), Is.EqualTo("missing.key"));
            Assert.That(LocalizationCatalog.Format(LocalizationCatalog.FloorFormat, "ru", 7), Is.EqualTo("ЭТАЖ 7"));
        }
    }
}
