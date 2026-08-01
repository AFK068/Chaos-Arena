using System.Collections.Generic;
using ChaosArena.Platform;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class PressStart2PFontTests
{
    [Test]
    public void StaticFontCoversPlayerVisibleEnglishRussianAndTurkishCorpus()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Press Start 2P Font");
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
}
