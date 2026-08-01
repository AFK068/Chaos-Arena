using System.Collections.Generic;
using System;
using ChaosArena.Platform;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public sealed class PressStart2PFontTests
{
    private const string FontResourcePath = "Fonts & Materials/Press Start 2P Font";
    private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Press Start 2P Font.asset";

    [Test]
    public void StaticFontCoversPlayerVisibleEnglishRussianAndTurkishCorpus()
    {
        var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        Assert.That(font, Is.Not.Null);
        Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
        Assert.That(font.fallbackFontAssetTable, Is.Empty);

        var corpus = "$ 0123456789 " +
                     "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя " +
                     "ÇĞİÖŞÜçğıöşü … № ";
        foreach (var key in LocalizationCatalog.Keys)
        {
            corpus += LocalizationCatalog.Get(key, "en");
            corpus += LocalizationCatalog.Get(key, "ru");
            corpus += LocalizationCatalog.Get(key, "tr");
        }

        var missing = new List<string>();
        foreach (var character in corpus)
        {
            if (!font.HasCharacter(character, false, false))
                missing.Add($"U+{(int)character:X4}");
        }

        Assert.That(missing, Is.Empty, string.Join(", ", missing));
    }

    [Test]
    public void CanonicalFontHasAttachedAtlasMaterialAndIsTheTmpDefault()
    {
        var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        Assert.That(font, Is.Not.Null);
        Assert.That(font.material, Is.Not.Null);
        Assert.That(font.atlasTextures, Is.Not.Null.And.Not.Empty);
        Assert.That(font.atlasTextures[0], Is.Not.Null);
        AssertSamePersistentAsset(font.material.GetTexture(ShaderUtilities.ID_MainTex), font.atlasTextures[0],
            "Canonical font material MainTex must be its atlas texture.");
        Assert.That(TMP_Settings.defaultFontAsset, Is.SameAs(font));
    }

    [TestCase("Assets/Scenes/MainMenu.unity")]
    [TestCase("Assets/Scenes/Gameplay.unity")]
    [TestCase("Assets/Scenes/GameOver.unity")]
    public void ReleaseScenesUseTheCanonicalFontAndMaterial(string scenePath)
    {
        var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            AssertTextReferences(scene.GetRootGameObjects(), font, scenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [TestCase("Assets/Assets/UI/Prefabs/Button_Generic.prefab")]
    [TestCase("Assets/Prefabs/PlayerUI/SettingsPanel.prefab")]
    public void ReleasePrefabsUseTheCanonicalFontAndMaterial(string prefabPath)
    {
        var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            AssertTextReferences(new[] { root }, font, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void GeneratorKeepsCanonicalMaterialAndAtlasLocalIdsStableAcrossTwoRuns()
    {
        var before = GetCanonicalSubassetIds();
        var generatorType = Type.GetType("PressStart2PFontGenerator, Assembly-CSharp-Editor");
        Assert.That(generatorType, Is.Not.Null, "Font generator must be compiled into the editor assembly.");
        var generate = generatorType.GetMethod("Generate");
        Assert.That(generate, Is.Not.Null);

        // The optional third-party MCP package can emit an unrelated asynchronous
        // authorization error while Unity imports/saves assets.  It is not a
        // generator result, so do not let that external connection state mask the
        // two deterministic assertions below.
        var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        LogAssert.ignoreFailingMessages = true;
        (string MaterialGuid, long MaterialId, string AtlasGuid, long AtlasId) afterFirstRun;
        (string MaterialGuid, long MaterialId, string AtlasGuid, long AtlasId) afterSecondRun;
        try
        {
            generate.Invoke(null, null);
            afterFirstRun = GetCanonicalSubassetIds();
            generate.Invoke(null, null);
            afterSecondRun = GetCanonicalSubassetIds();
        }
        finally
        {
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
        }

        Assert.That(afterFirstRun, Is.EqualTo(before));
        Assert.That(afterSecondRun, Is.EqualTo(before));

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Assert.That(font.material, Is.Not.Null);
        Assert.That(font.atlasTextures, Is.Not.Null.And.Not.Empty);
        Assert.That(font.atlasTextures[0], Is.Not.Null);
        AssertSamePersistentAsset(font.material.GetTexture(ShaderUtilities.ID_MainTex), font.atlasTextures[0],
            "Generator must preserve the canonical material-to-atlas binding.");
    }

    private static void AssertTextReferences(IEnumerable<GameObject> roots, TMP_FontAsset font, string assetPath)
    {
        var texts = new List<TMP_Text>();
        foreach (var root in roots)
            texts.AddRange(root.GetComponentsInChildren<TMP_Text>(true));

        Assert.That(texts, Is.Not.Empty, $"{assetPath} has no TMP text to validate.");
        foreach (var text in texts)
        {
            Assert.That(text.font, Is.SameAs(font), $"{assetPath}/{text.name} uses a different TMP font.");
            var material = text.fontSharedMaterial;
            Assert.That(material, Is.Not.Null, $"{assetPath}/{text.name} has no TMP material.");
            AssertSamePersistentAsset(material.GetTexture(ShaderUtilities.ID_MainTex), font.atlasTextures[0],
                $"{assetPath}/{text.name} has a stale TMP material/atlas binding.");
        }
    }

    private static (string MaterialGuid, long MaterialId, string AtlasGuid, long AtlasId) GetCanonicalSubassetIds()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Assert.That(font, Is.Not.Null);
        Assert.That(font.material, Is.Not.Null);
        Assert.That(font.atlasTextures, Is.Not.Null.And.Not.Empty);
        Assert.That(font.atlasTextures[0], Is.Not.Null);

        Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font.material, out var materialGuid, out long materialId), Is.True);
        Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font.atlasTextures[0], out var atlasGuid, out long atlasId), Is.True);
        return (materialGuid, materialId, atlasGuid, atlasId);
    }

    private static void AssertSamePersistentAsset(UnityEngine.Object actual, UnityEngine.Object expected, string message)
    {
        Assert.That(actual, Is.Not.Null, message);
        Assert.That(expected, Is.Not.Null, message);
        Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out var actualGuid, out long actualId), Is.True, message);
        Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected, out var expectedGuid, out long expectedId), Is.True, message);
        Assert.That(actualGuid, Is.EqualTo(expectedGuid), message);
        Assert.That(actualId, Is.EqualTo(expectedId), message);
    }
}
