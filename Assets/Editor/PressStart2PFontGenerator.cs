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

        // TMP can allocate a replacement material and atlas when a previously static
        // font is switched to Dynamic.  Those replacements are not automatically
        // attached to the .asset file.  Saving them as references creates a font that
        // works in the current editor session but renders every glyph as a rectangle
        // after a restart.  Keep exactly one persistent material and atlas, copy any
        // dynamic updates back into them, then explicitly restore every link.
        var (canonicalMaterial, canonicalAtlas) = GetCanonicalSubassets(fontAsset);

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

            CopyDynamicAtlasIntoCanonicalSubassets(fontAsset, canonicalMaterial, canonicalAtlas);
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        RestoreCanonicalLinks(fontAsset, canonicalMaterial, canonicalAtlas);
        RemoveDuplicateSubassets(fontAsset, canonicalMaterial, canonicalAtlas);
        EditorUtility.SetDirty(fontAsset);
        EditorUtility.SetDirty(canonicalMaterial);
        EditorUtility.SetDirty(canonicalAtlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

        // Verify the on-disk form, not just the in-memory objects.  This is the
        // condition that matters after a clean editor restart and in player builds.
        var reloaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        var (reloadedMaterial, reloadedAtlas) = GetCanonicalSubassets(reloaded);
        if (reloaded.material != reloadedMaterial || reloaded.atlasTextures.Length != 1 ||
            reloaded.atlasTextures[0] != reloadedAtlas ||
            reloadedMaterial.GetTexture(ShaderUtilities.ID_MainTex) != reloadedAtlas)
        {
            throw new InvalidOperationException($"{AssetPath} did not persist a coherent TMP material/atlas binding.");
        }

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

    private static (Material Material, Texture2D Atlas) GetCanonicalSubassets(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
            throw new InvalidOperationException($"TMP font is missing at {AssetPath}.");

        var subassets = AssetDatabase.LoadAllAssetsAtPath(AssetPath);
        var material = fontAsset.material;
        var atlas = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0
            ? fontAsset.atlasTextures[0]
            : null;

        // A corrupted YAML reference loads as null after a restart.  Recover from
        // the actual attached objects, preferring the material that already owns an
        // attached atlas texture.
        if (material == null || !AssetDatabase.Contains(material))
        {
            material = subassets.OfType<Material>()
                .FirstOrDefault(candidate => candidate.GetTexture(ShaderUtilities.ID_MainTex) is Texture2D attachedAtlas &&
                                             AssetDatabase.Contains(attachedAtlas))
                ?? subassets.OfType<Material>().FirstOrDefault();
        }

        if (atlas == null || !AssetDatabase.Contains(atlas))
        {
            atlas = material?.GetTexture(ShaderUtilities.ID_MainTex) as Texture2D;
            if (atlas == null || !AssetDatabase.Contains(atlas))
                atlas = subassets.OfType<Texture2D>().FirstOrDefault();
        }

        if (material == null || atlas == null)
            throw new InvalidOperationException($"{AssetPath} has no persistent TMP material and atlas subassets.");

        return (material, atlas);
    }

    private static void CopyDynamicAtlasIntoCanonicalSubassets(
        TMP_FontAsset fontAsset,
        Material canonicalMaterial,
        Texture2D canonicalAtlas)
    {
        var dynamicMaterial = fontAsset.material;
        var dynamicAtlas = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0
            ? fontAsset.atlasTextures[0]
            : null;
        if (dynamicMaterial == null || dynamicAtlas == null)
            throw new InvalidOperationException("TMP replaced the canonical subassets with an incomplete dynamic atlas.");

        if (dynamicAtlas != canonicalAtlas)
        {
            if (dynamicAtlas.width != canonicalAtlas.width || dynamicAtlas.height != canonicalAtlas.height ||
                dynamicAtlas.graphicsFormat != canonicalAtlas.graphicsFormat)
            {
                throw new InvalidOperationException("TMP generated an incompatible dynamic atlas; refusing to serialize a broken binding.");
            }

            Graphics.CopyTexture(dynamicAtlas, canonicalAtlas);
        }

        if (dynamicMaterial != canonicalMaterial)
            canonicalMaterial.CopyPropertiesFromMaterial(dynamicMaterial);

        RestoreCanonicalLinks(fontAsset, canonicalMaterial, canonicalAtlas);
    }

    private static void RestoreCanonicalLinks(TMP_FontAsset fontAsset, Material canonicalMaterial, Texture2D canonicalAtlas)
    {
        canonicalMaterial.SetTexture(ShaderUtilities.ID_MainTex, canonicalAtlas);
        fontAsset.material = canonicalMaterial;
        fontAsset.atlasTextures = new[] { canonicalAtlas };
    }

    private static void RemoveDuplicateSubassets(TMP_FontAsset fontAsset, Material canonicalMaterial, Texture2D canonicalAtlas)
    {
        foreach (var subasset in AssetDatabase.LoadAllAssetsAtPath(AssetPath))
        {
            if (subasset == fontAsset || subasset == canonicalMaterial || subasset == canonicalAtlas ||
                (subasset is not Material && subasset is not Texture2D))
                continue;

            AssetDatabase.RemoveObjectFromAsset(subasset);
            UnityEngine.Object.DestroyImmediate(subasset, true);
        }
    }
}
#endif
