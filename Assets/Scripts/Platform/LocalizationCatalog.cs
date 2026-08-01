using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChaosArena.Platform
{
    public static class LocalizationCatalog
    {
        public const string NewRun = "menu.new_run";
        public const string Settings = "menu.settings";
        public const string Quit = "menu.quit";
        public const string BestFloor = "stats.best_floor";
        public const string Deaths = "stats.deaths";
        public const string TotalCoins = "stats.total_coins";
        public const string Kills = "stats.kills";
        public const string Sounds = "settings.sounds";
        public const string Music = "settings.music";
        public const string Return = "settings.return";
        public const string SettingsTitle = "settings.title";
        public const string Paused = "pause.title";
        public const string Continue = "pause.continue";
        public const string GameOver = "game_over.title";
        public const string EnemiesSlain = "game_over.enemies_slain";
        public const string CoinsCollected = "game_over.coins_collected";
        public const string RunTime = "game_over.run_time";
        public const string FloorReached = "game_over.floor_reached";
        public const string MainMenu = "game_over.main_menu";
        public const string FloorFormat = "transition.floor";
        public const string Item = "common.item";
        public const string MobileDash = "mobile.dash";
        public const string MobileInteract = "mobile.interact";

        private readonly struct Entry
        {
            public Entry(string english, string russian)
            {
                English = english;
                Russian = russian;
            }

            public string English { get; }
            public string Russian { get; }
        }

        private static readonly IReadOnlyDictionary<string, Entry> Entries =
            new Dictionary<string, Entry>(StringComparer.Ordinal)
            {
                [NewRun] = new("New Run", "Новый забег"),
                [Settings] = new("Settings", "Настройки"),
                [Quit] = new("Quit", "Выйти"),
                [BestFloor] = new("Best floor", "Лучший этаж"),
                [Deaths] = new("Deaths", "Смерти"),
                [TotalCoins] = new("Total coins", "Всего монет"),
                [Kills] = new("Kills", "Убийства"),
                [Sounds] = new("Sounds", "Звуки"),
                [Music] = new("Music", "Музыка"),
                [Return] = new("Return", "Назад"),
                [SettingsTitle] = new("SETTINGS", "НАСТРОЙКИ"),
                [Paused] = new("PAUSED", "ПАУЗА"),
                [Continue] = new("Continue", "Продолжить"),
                [GameOver] = new("GAME OVER", "ИГРА ОКОНЧЕНА"),
                [EnemiesSlain] = new("Enemies slain", "Врагов повержено"),
                [CoinsCollected] = new("Coins collected", "Монет собрано"),
                [RunTime] = new("Run time", "Время забега"),
                [FloorReached] = new("Floor reached", "Достигнутый этаж"),
                [MainMenu] = new("Main Menu", "Главное меню"),
                [FloorFormat] = new("FLOOR {0}", "ЭТАЖ {0}"),
                [Item] = new("Item", "Предмет"),
                [MobileDash] = new("Dash", "Рывок"),
                [MobileInteract] = new("Use", "Взаим."),
            };

        public static string Get(string key, string? languageCode)
        {
            if (!Entries.TryGetValue(key, out var entry))
                return key;

            return LocalizationLanguagePolicy.NormalizeSupportedLanguage(languageCode)
                == LocalizationLanguagePolicy.Russian
                ? entry.Russian
                : entry.English;
        }

        public static string Format(string key, string? languageCode, params object[] arguments) =>
            string.Format(CultureInfo.InvariantCulture, Get(key, languageCode), arguments);
    }
}
