using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                    LocalizationCatalog.MainMenu, LocalizationCatalog.GameTitle, LocalizationCatalog.FloorFormat, LocalizationCatalog.Item,
                    LocalizationCatalog.MobileDash, LocalizationCatalog.MobileInteract,
                    LocalizationCatalog.TutorialDesktop, LocalizationCatalog.TutorialMobile,
                    LocalizationCatalog.PickupRageNotice, LocalizationCatalog.PickupDashChargeNotice
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

            Assert.That(keys.Count, Is.EqualTo(24));
            Assert.That(keys.Distinct().Count(), Is.EqualTo(24));
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
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.GameTitle, "ru"), Is.EqualTo("АРЕНА ХАОСА"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.GameTitle, "tr"), Is.EqualTo("KAOS ARENASI"));
            Assert.That(LocalizationCatalog.Get(LocalizationCatalog.FloorReached, "ru"), Is.EqualTo("Достигнутый этаж"));
            Assert.That(LocalizationCatalog.Get("missing.key", "ru"), Is.EqualTo("missing.key"));
            Assert.That(LocalizationCatalog.Format(LocalizationCatalog.FloorFormat, "tr", 7), Is.EqualTo("KAT 7"));
        }

        [TestCase("Assets/Scenes/MainMenu.unity")]
        [TestCase("Assets/Scenes/Gameplay.unity")]
        [TestCase("Assets/Scenes/GameOver.unity")]
        public void ReleaseSceneStaticTextHasLocalizationOwnership(string scenePath)
        {
            var existingScene = SceneManager.GetSceneByPath(scenePath);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                AssertStaticTextHasLocalizationOwnership(existingScene, scenePath);
                return;
            }

            var sceneSetup = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            var hadLoadedScenes = sceneSetup.Any(entry => entry.isLoaded);
            var dirtyScenes = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .Where(scene => scene.IsValid() && scene.isLoaded && scene.isDirty)
                .Select(scene => string.IsNullOrEmpty(scene.path) ? scene.name : scene.path)
                .ToArray();
            if (dirtyScenes.Length > 0)
            {
                Assert.Ignore(
                    $"Cannot safely open '{scenePath}' while dirty scenes are present: {string.Join(", ", dirtyScenes)}. " +
                    "Save them before running the release-scene localization audit.");
            }

            try
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                AssertStaticTextHasLocalizationOwnership(scene, scenePath);
            }
            finally
            {
                if (hadLoadedScenes)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                }
                else
                {
                    // RestoreSceneManagerSetup rejects a setup with no loaded scene.
                    // Replacing the audited scene with an empty one cleans it up while
                    // keeping the editor in a valid state for the next test.
                    UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                        UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                        UnityEditor.SceneManagement.NewSceneMode.Single);
                }
            }
        }

        private static void AssertStaticTextHasLocalizationOwnership(Scene scene, string scenePath)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var text in roots.SelectMany(root => root.GetComponentsInChildren<TMPro.TMP_Text>(true)))
            {
                if (IsDynamicOrInputText(text))
                    continue;

                var localizedText = text.GetComponent("LocalizedText");
                Assert.That(localizedText, Is.Not.Null,
                    $"{scenePath}/{GetHierarchyPath(text.transform)} must use LocalizedText or be explicitly dynamic.");
                var key = new SerializedObject(localizedText).FindProperty("key")?.stringValue;
                Assert.That(LocalizationCatalog.HasKey(key), Is.True,
                    $"{scenePath}/{GetHierarchyPath(text.transform)} has an unknown localization key '{key}'.");
            }
        }

        private static bool IsDynamicOrInputText(TMPro.TMP_Text text)
        {
            var serializedText = text.text?.Trim();
            if (string.IsNullOrEmpty(serializedText) || serializedText.All(char.IsDigit))
                return true;

            // These values are set at runtime; keyboard letters are input glyphs,
            // not language copy. Their surrounding instruction text is localized.
            if (serializedText.Length == 1 && char.IsLetterOrDigit(serializedText[0]))
                return true;

            return IsSerializedRuntimeText(text);
        }

        private static bool IsSerializedRuntimeText(TMPro.TMP_Text text)
        {
            foreach (var component in text.GetComponentsInParent<Component>(true))
            {
                if (component == null || component.GetType().Name is not
                        ("LanguageToggleUI" or "CoinDisplay" or "MainMenuStats" or "GameOverUI"))
                    continue;

                var serializedComponent = new SerializedObject(component);
                var property = serializedComponent.GetIterator();
                if (!property.NextVisible(true))
                    continue;

                do
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == text)
                        return true;
                }
                while (property.NextVisible(false));
            }

            return false;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }
    }
}
