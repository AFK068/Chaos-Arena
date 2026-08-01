using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

        [TestCase("tr")]
        [TestCase("tr-TR")]
        public void TurkishSdkLanguagesMapToTurkish(string sdkLanguage)
        {
            Assert.That(LocalizationLanguagePolicy.FromSdkLanguage(sdkLanguage),
                Is.EqualTo(LocalizationLanguagePolicy.Turkish));
        }

        [TestCase("en")]
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
            state.SetManualLanguage("tr");
            state.SetPlatformLanguage("uz");

            Assert.That(state.CurrentLanguage, Is.EqualTo(LocalizationLanguagePolicy.Turkish));
            Assert.That(state.UsesManualLanguage, Is.True);

            state.UsePlatformLanguage();

            Assert.That(state.CurrentLanguage, Is.EqualTo(LocalizationLanguagePolicy.Russian));
            Assert.That(state.UsesManualLanguage, Is.False);
        }

        [Test]
        public void ManualLanguageCycleIsEnglishRussianTurkish()
        {
            Assert.That(LocalizationLanguagePolicy.NextManualLanguage("en"), Is.EqualTo("ru"));
            Assert.That(LocalizationLanguagePolicy.NextManualLanguage("ru"), Is.EqualTo("tr"));
            Assert.That(LocalizationLanguagePolicy.NextManualLanguage("tr"), Is.EqualTo("en"));
            Assert.That(LocalizationLanguagePolicy.NextManualLanguage("unknown"), Is.EqualTo("ru"));
        }

        [Test]
        public void CatalogIsCompleteForEnglishRussianAndTurkish()
        {
            var keys = LocalizationCatalog.ProximityContentKeys
                .Concat(new[]
                {
                    LocalizationCatalog.NewRun, LocalizationCatalog.Settings, LocalizationCatalog.Quit,
                    LocalizationCatalog.BestFloor, LocalizationCatalog.Deaths, LocalizationCatalog.TotalCoins,
                    LocalizationCatalog.Kills, LocalizationCatalog.Sounds, LocalizationCatalog.Music,
                    LocalizationCatalog.Return, LocalizationCatalog.SettingsTitle, LocalizationCatalog.Paused,
                    LocalizationCatalog.Continue, LocalizationCatalog.GameOver, LocalizationCatalog.EnemiesSlain,
                    LocalizationCatalog.CoinsCollected, LocalizationCatalog.RunTime, LocalizationCatalog.FloorReached,
                    LocalizationCatalog.MainMenu, LocalizationCatalog.FloorFormat, LocalizationCatalog.Item,
                    LocalizationCatalog.MobileDash, LocalizationCatalog.MobileInteract,
                    LocalizationCatalog.TutorialDesktop, LocalizationCatalog.TutorialMobile
                });

            foreach (var key in keys)
            {
                Assert.That(LocalizationCatalog.HasKey(key), Is.True, key);
                foreach (var language in new[] { "en", "ru", "tr" })
                    Assert.That(LocalizationCatalog.Get(key, language), Is.Not.Empty, $"{key} ({language})");
            }
        }

        [Test]
        public void ProximityLabelPrefabsHaveExactlyTheCatalogedStableKeys()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            var keys = new List<string>();

            foreach (var guid in prefabGuids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null)
                    continue;

                foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component.GetType().Name != "ProximityLabel")
                        continue;

                    var key = new SerializedObject(component).FindProperty("contentKey")?.stringValue;
                    Assert.That(key, Is.Not.Null.And.Not.Empty, prefab.name);
                    keys.Add(key);
                }
            }

            Assert.That(keys, Has.Count.EqualTo(24));
            Assert.That(keys.Distinct(), Has.Count.EqualTo(24));
            Assert.That(keys, Is.EquivalentTo(LocalizationCatalog.ProximityContentKeys));
        }

        [Test]
        public void TutorialGateAllowsOnlyFloorOneStartRoomOnce()
        {
            Assert.That(FirstRoomTutorialGate.ShouldShow(1, 0, false), Is.True);
            Assert.That(FirstRoomTutorialGate.ShouldShow(1, 0, true), Is.False);
            Assert.That(FirstRoomTutorialGate.ShouldShow(2, 0, false), Is.False);
            Assert.That(FirstRoomTutorialGate.ShouldShow(1, 1, false), Is.False);
        }

        [Test]
        public void CatalogUsesFallbackAndFormatsLocalizedFloor()
        {
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.NewRun, "tr"), Is.EqualTo("Yeni Koşu"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.Return, "ru"), Is.EqualTo("Назад"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.MobileDash, "tr"), Is.EqualTo("Atıl"));
            Assert.That(LocalizationCatalog.Get("missing.key", "ru"), Is.EqualTo("missing.key"));
            Assert.That(LocalizationCatalog.Format(LocalizationCatalog.FloorFormat, "tr", 7), Is.EqualTo("KAT 7"));
        }
    }
}
