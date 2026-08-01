#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChaosArena.Platform;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Reproducibly creates the single static TMP font used by player-visible text.
/// Run from the menu or with -executeMethod PressStart2PFontGenerator.Generate.
/// </summary>
public static class PressStart2PFontGenerator
{
    private const string SourcePath = "Assets/TextMesh Pro/Resources/Fonts & Materials/PressStart2P-Regular.ttf";
    private const string AssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Press Start 2P Font.asset";
    private const int AtlasSize = 2048;
    private const int SamplingPointSize = 32;
    private const int AtlasPadding = 5;

    [MenuItem("Tools/Fonts/Generate Press Start 2P Static Asset")]
    public static void Generate()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        if (sourceFont == null)
            throw new InvalidOperationException($"Source font not imported: {SourcePath}");

        var characters = BuildRequiredCharacters();
        var missingFromSource = FindMissing(sourceFont, characters);
        if (missingFromSource.Count != 0)
            throw new InvalidOperationException($"Press Start 2P source is missing: {FormatCodePoints(missingFromSource)}");

        // Never delete and recreate this asset.  The material and atlas are subassets
        // with local IDs, and every scene/prefab serializes those IDs alongside the
        // font GUID.  Updating the existing dynamic atlas in place keeps them valid.
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                false);
            fontAsset.name = "Press Start 2P Font";
            AssetDatabase.CreateAsset(fontAsset, AssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            foreach (var atlasTexture in fontAsset.atlasTextures)
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }

        if (fontAsset.material == null || fontAsset.atlasTextures == null ||
            fontAsset.atlasTextures.Length == 0 || fontAsset.atlasTextures[0] == null)
        {
            throw new InvalidOperationException(
                $"Existing production font is incomplete at {AssetPath}; refusing to replace its serialized subassets.");
        }

        fontAsset.name = "Press Start 2P Font";
        fontAsset.creationSettings = new FontAssetCreationSettings
        {
            sourceFontFileName = Path.GetFileName(SourcePath),
            sourceFontFileGUID = AssetDatabase.AssetPathToGUID(SourcePath),
            pointSize = SamplingPointSize,
            padding = AtlasPadding,
            paddingMode = 2,
            atlasWidth = AtlasSize,
            atlasHeight = AtlasSize,
            characterSetSelectionMode = 7,
            characterSequence = characters,
            renderMode = (int)GlyphRenderMode.SDFAA
        };
        var missingFromExistingAtlas = characters.Where(character => !fontAsset.HasCharacter(character, false, false)).ToArray();
        if (missingFromExistingAtlas.Length != 0)
        {
            // An existing static asset is only made dynamic when the corpus really
            // expanded.  Toggling it for an unchanged atlas causes TMP to attempt
            // to repack every glyph and makes repeated generation non-idempotent.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.SaveAssets();
            if (!fontAsset.TryAddCharacters(new string(missingFromExistingAtlas), out var missingFromAtlas) ||
                !string.IsNullOrEmpty(missingFromAtlas))
            {
                throw new InvalidOperationException($"Press Start 2P atlas is missing: {FormatCodePoints(missingFromAtlas)}");
            }
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(fontAsset.material);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {AssetPath}: {fontAsset.characterTable.Count} static characters, {AtlasSize}x{AtlasSize} atlas.");
    }

    // Keep this corpus in sync with player-visible text. The separate EditMode test
    // verifies the generated asset and exercises the localization catalog at runtime.
    public static string BuildRequiredCharacters()
    {
        const string ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        const string russianAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        const string turkish = "ÇĞİÖŞÜçğıöşü";
        const string punctuation = "…№";
        var localized = string.Concat(LocalizationCatalog.Keys.SelectMany(key => new[]
        {
            LocalizationCatalog.Get(key, LocalizationLanguagePolicy.English),
            LocalizationCatalog.Get(key, LocalizationLanguagePolicy.Russian),
            LocalizationCatalog.Get(key, LocalizationLanguagePolicy.Turkish)
        }));
        return new string((ascii + russianAlphabet + turkish + punctuation + localized)
            .Where(character => !char.IsControl(character))
            .Distinct()
            .OrderBy(character => character)
            .ToArray());
    }

    private static List<char> FindMissing(Font font, string characters) =>
        characters.Where(character => !font.HasCharacter(character)).ToList();

    private static string FormatCodePoints(IEnumerable<char> characters) =>
        string.Join(", ", characters.Select(character => $"U+{(int)character:X4}"));
}
#endif
