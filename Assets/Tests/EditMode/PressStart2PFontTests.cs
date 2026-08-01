using System.Collections.Generic;
using ChaosArena.Platform;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class PressStart2PFontTests
{
    private static readonly string[] LocalizationKeys =
    {
        LocalizationCatalog.NewRun, LocalizationCatalog.Settings, LocalizationCatalog.Quit,
        LocalizationCatalog.BestFloor, LocalizationCatalog.Deaths, LocalizationCatalog.TotalCoins,
        LocalizationCatalog.Kills, LocalizationCatalog.Sounds, LocalizationCatalog.Music,
        LocalizationCatalog.Return, LocalizationCatalog.SettingsTitle, LocalizationCatalog.Paused,
        LocalizationCatalog.Continue, LocalizationCatalog.GameOver, LocalizationCatalog.EnemiesSlain,
        LocalizationCatalog.CoinsCollected, LocalizationCatalog.RunTime, LocalizationCatalog.FloorReached,
        LocalizationCatalog.MainMenu, LocalizationCatalog.FloorFormat, LocalizationCatalog.Item,
        LocalizationCatalog.MobileDash, LocalizationCatalog.MobileInteract
    };

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
        foreach (var key in LocalizationKeys)
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
