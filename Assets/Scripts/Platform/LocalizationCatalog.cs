using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Stable UI/content keys. Prefabs retain their English serialized text as
    /// a safe fallback, while this catalog owns the player-facing translation.
    /// </summary>
    public static class LocalizationCatalog
    {
        // Keep the player-facing title consistent with the selected storefront
        // language and its localized card metadata.
        public const string GameTitle = "brand.game_title";
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
        public const string TutorialDesktop = "tutorial.desktop";
        public const string TutorialMobile = "tutorial.mobile";
        public const string PickupRageNotice = "pickup.rage.notice";
        public const string PickupDashChargeNotice = "pickup.dash_charge.notice";

        public const string BuffFullHeart = "content.buff.full_heart";
        public const string BuffHalfHeart = "content.buff.half_heart";
        public const string BuffMaxHeart = "content.buff.max_heart";
        public const string ProjectileDefault = "content.projectile.default";
        public const string ProjectileFire = "content.projectile.fire";
        public const string ProjectileFrozen = "content.projectile.frozen";
        public const string ProjectilePiercing = "content.projectile.piercing";
        public const string ProjectilePoisonous = "content.projectile.poisonous";
        public const string ItemArmorAmulet = "content.item.armor_amulet";
        public const string ItemCursedHourglass = "content.item.cursed_hourglass";
        public const string ItemDeathMask = "content.item.death_mask";
        public const string ItemTrackersLens = "content.item.trackers_lens";
        public const string ItemHollowVein = "content.item.hollow_vein";
        public const string ItemLeechFang = "content.item.leech_fang";
        public const string ItemQuickHands = "content.item.quick_hands";
        public const string ItemScrollOfSplitting = "content.item.scroll_of_splitting";
        public const string ItemShardOfRage = "content.item.shard_of_rage";
        public const string ItemSwiftLegs = "content.item.swift_legs";
        public const string ItemVoidCarapace = "content.item.void_carapace";
        public const string ItemVoodoo = "content.item.voodoo";
        public const string TraderAlchemist = "content.trader.alchemist";
        public const string TraderChef = "content.trader.chef";
        public const string TraderMerchant = "content.trader.merchant";
        public const string TraderPriest = "content.trader.priest";

        public static readonly IReadOnlyList<string> ProximityContentKeys = new[]
        {
            BuffFullHeart, BuffHalfHeart, BuffMaxHeart,
            ProjectileDefault, ProjectileFire, ProjectileFrozen, ProjectilePiercing, ProjectilePoisonous,
            ItemArmorAmulet, ItemCursedHourglass, ItemDeathMask, ItemTrackersLens, ItemHollowVein,
            ItemLeechFang, ItemQuickHands, ItemScrollOfSplitting, ItemShardOfRage, ItemSwiftLegs,
            ItemVoidCarapace, ItemVoodoo,
            TraderAlchemist, TraderChef, TraderMerchant, TraderPriest
        };

        private readonly struct Entry
        {
            public Entry(string english, string russian, string turkish)
            {
                English = english;
                Russian = russian;
                Turkish = turkish;
            }

            public string English { get; }
            public string Russian { get; }
            public string Turkish { get; }
        }

        private static readonly IReadOnlyDictionary<string, Entry> Entries =
            new Dictionary<string, Entry>(StringComparer.Ordinal)
            {
                // Treat the title as player-facing copy rather than an untranslated
                // proper noun.
                [GameTitle] = new("CHAOS ARENA", "АРЕНА ХАОСА", "KAOS ARENASI"),
                [NewRun] = new("New Run", "Новый забег", "Yeni Koşu"),
                [Settings] = new("Settings", "Настройки", "Ayarlar"),
                [Quit] = new("Quit", "Выйти", "Çıkış"),
                [BestFloor] = new("Best floor", "Лучший этаж", "En iyi kat"),
                [Deaths] = new("Deaths", "Смерти", "Ölümler"),
                [TotalCoins] = new("Total coins", "Всего монет", "Toplam madeni para"),
                [Kills] = new("Kills", "Убийства", "Öldürmeler"),
                [Sounds] = new("Sounds", "Звуки", "Sesler"),
                [Music] = new("Music", "Музыка", "Müzik"),
                [Return] = new("Return", "Назад", "Geri"),
                [SettingsTitle] = new("SETTINGS", "НАСТРОЙКИ", "AYARLAR"),
                [Paused] = new("PAUSED", "ПАУЗА", "DURAKLATILDI"),
                [Continue] = new("Continue", "Продолжить", "Devam et"),
                [GameOver] = new("GAME OVER", "ИГРА ОКОНЧЕНА", "OYUN BİTTİ"),
                [EnemiesSlain] = new("Enemies slain", "Врагов повержено", "Yenilen düşmanlar"),
                [CoinsCollected] = new("Coins collected", "Монет собрано", "Toplanan madeni para"),
                [RunTime] = new("Run time", "Время забега", "Koşu süresi"),
                [FloorReached] = new("Floor reached", "Достигнутый этаж", "Ulaşılan kat"),
                [MainMenu] = new("Main Menu", "Главное меню", "Ana Menü"),
                [FloorFormat] = new("FLOOR {0}", "ЭТАЖ {0}", "KAT {0}"),
                [Item] = new("Item", "Предмет", "Eşya"),
                [MobileDash] = new("Dash", "Рывок", "Atıl"),
                [MobileInteract] = new("Use", "Взаим.", "Kullan"),
                [TutorialDesktop] = new("WASD  MOVE\nARROWS  AIM + SHOOT\nSHIFT  DASH\nE / F  USE", "WASD  ДВИЖЕНИЕ\nСТРЕЛКИ  ПРИЦЕЛ + ВЫСТРЕЛ\nSHIFT  РЫВОК\nE / F  ИСПОЛЬЗОВАТЬ", "WASD  HAREKET\nOKLAR  NİŞAN + ATEŞ\nSHIFT  ATIL\nE / F  KULLAN"),
                [TutorialMobile] = new("LEFT STICK  MOVE\nRIGHT STICK  AIM + FIRE\nDASH  QUICK MOVE\nUSE  INTERACT", "ЛЕВЫЙ СТИК  ДВИЖЕНИЕ\nПРАВЫЙ СТИК  ПРИЦЕЛ + ОГОНЬ\nРЫВОК  БЫСТРОЕ ДВИЖЕНИЕ\nИСП.  ВЗАИМОДЕЙСТВИЕ", "SOL ÇUBUK  HAREKET\nSAĞ ÇUBUK  NİŞAN + ATEŞ\nATIL  HIZLI HAREKET\nKULLAN  ETKİLEŞİM"),
                [PickupRageNotice] = new("RAGE!\nMove speed +50%  |  Fire rate x2\nDash cooldown halved", "ЯРОСТЬ!\nСкорость +50%  |  Темп огня x2\nОткат рывка вдвое меньше", "ÖFKE!\nHız +%50  |  Ateş hızı x2\nAtıl bekleme süresi yarıya iner"),
                [PickupDashChargeNotice] = new("DASH CHARGE RESTORED", "ЗАРЯД РЫВКА ВОССТАНОВЛЕН", "ATIL YÜKÜ YENİLENDİ"),

                [BuffFullHeart] = new("A Full Heart\n\nSomeone left this behind.\nTheir loss, your gain.\n\nRestores one full heart", "Полное сердце\n\nКто-то оставил его здесь.\nЕго потеря — твоя находка.\n\nВосстанавливает одно полное сердце", "Tam Kalp\n\nBiri bunu burada bıraktı.\nOnun kaybı, senin kazancın.\n\nBir tam kalbi yeniler"),
                [BuffHalfHeart] = new("Half a Heart\n\nStill beats.\nJust not as loud.\n\nRestores half a heart", "Половина сердца\n\nВсё ещё бьётся.\nПросто тише.\n\nВосстанавливает половину сердца", "Yarım Kalp\n\nHâlâ atıyor.\nSadece daha sessiz.\n\nYarım kalbi yeniler"),
                [BuffMaxHeart] = new("Room for More\n\nThe body adapts.\nSo does the will to survive.\n\nPermanently increases max health\n(default: 5 hearts)", "Место для большего\n\nТело приспосабливается.\nКак и воля выжить.\n\nНавсегда увеличивает максимум здоровья\n(по умолчанию: 5 сердец)", "Daha Fazlasına Yer\n\nBeden uyum sağlar.\nHayatta kalma isteği de.\n\nAzami sağlığı kalıcı artırır\n(varsayılan: 5 kalp)"),
                [ProjectileDefault] = new("Iron Will\n\nNo element. No trick.\nJust the shot, and whoever is in the way.\n\nStandard projectiles", "Железная воля\n\nБез стихии. Без трюков.\nТолько выстрел и тот, кто встал на пути.\n\nОбычные снаряды", "Demir İrade\n\nElement yok. Hile yok.\nSadece atış ve yolundaki kişi.\n\nStandart mermiler"),
                [ProjectileFire] = new("Ember Flask\n\nThe fire does not ask permission.\nIt simply continues.\n\nProjectiles ignite enemies / burn damage over time", "Колба углей\n\nОгонь не спрашивает разрешения.\nОн просто не останавливается.\n\nСнаряды поджигают врагов / урон горением со временем", "Kor Şişesi\n\nAteş izin istemez.\nSadece sürer.\n\nMermiler düşmanları yakar / zamanla yanma hasarı"),
                [ProjectileFrozen] = new("Shard of Stillness\n\nTime does not freeze.\nOnly the things caught inside it do.\n\nProjectiles chill enemies / slow movement over time", "Осколок покоя\n\nВремя не замирает.\nЗамирает лишь то, что в нём поймано.\n\nСнаряды охлаждают врагов / замедляют со временем", "Durgunluk Parçası\n\nZaman donmaz.\nYalnızca içinde yakalananlar donar.\n\nMermiler düşmanları soğutur / zamanla yavaşlatır"),
                [ProjectilePiercing] = new("Phase Round\n\nThe wall did not stop it.\nNeither did the man behind it.\n\nProjectiles pass through enemies", "Фазовый заряд\n\nСтена его не остановила.\nКак и человек за ней.\n\nСнаряды проходят сквозь врагов", "Faz Mermisi\n\nDuvar onu durdurmadı.\nArkasındaki adam da.\n\nMermiler düşmanların içinden geçer"),
                [ProjectilePoisonous] = new("Widow's Extract\n\nIt does not kill quickly.\nThat is the point.\n\nProjectiles poison enemies / damage over time", "Экстракт вдовы\n\nОн убивает не быстро.\nВ этом весь смысл.\n\nСнаряды отравляют врагов / урон со временем", "Dul Özütü\n\nHızlı öldürmez.\nZaten amaç bu.\n\nMermiler düşmanları zehirler / zamanla hasar"),
                [ItemArmorAmulet] = new("Amulet of the Last Breath\n\nAfter the first wound - silence. The next blow finds only air.\nBut wounds have memory.\n\n+5s Shield on hit", "Амулет последнего вздоха\n\nПосле первой раны — тишина. Следующий удар находит лишь воздух.\nНо раны помнят.\n\nПри попадании: щит на 5 сек.", "Son Nefes Muskası\n\nİlk yaradan sonra sessizlik. Sonraki darbe yalnızca havayı bulur.\nAma yaraların hafızası vardır.\n\nİsabet alınca: 5 sn. kalkan"),
                [ItemCursedHourglass] = new("Cursed Hourglass\n\nTime doesn't stop. It just forgets about them for a moment.\nPain has its own gravity — and they all feel it.\n\nOn hit: slows all nearby enemies to 30% speed for 1.5s", "Проклятые песочные часы\n\nВремя не останавливается. Оно просто ненадолго забывает о них.\nУ боли своя гравитация — и все её чувствуют.\n\nПри попадании: замедляет всех врагов рядом до 30% скорости на 1,5 сек.", "Lanetli Kum Saati\n\nZaman durmaz. Onları bir anlığına unutur.\nAcının kendi çekimi vardır; hepsi hisseder.\n\nİsabet alınca: yakındaki tüm düşmanları 1,5 sn. %30 hıza düşürür"),
                [ItemDeathMask] = new("Death Mask\n\nThey feared what hid beneath.\nNow something wears your face.\n\nOn hit: 20% → Rage 3s\nSpeed +50%  |  Fire rate x2  |  Dash CD /2", "Маска смерти\n\nОни боялись того, что скрывалось под ней.\nТеперь что-то носит твоё лицо.\n\nПри попадании: 20% → ярость на 3 сек.\nСкорость +50%  |  Темп огня x2  |  Откат рывка /2", "Ölüm Maskesi\n\nAltında saklanandan korktular.\nŞimdi bir şey senin yüzünü takıyor.\n\nİsabet alınca: %20 → 3 sn. öfke\nHız +%50  |  Ateş hızı x2  |  Atıl bekleme /2"),
                [ItemTrackersLens] = new("Tracker's Lens\n\nThey say he never missed.\nHe just wore the right glasses.\n\nProjectiles seek the nearest enemy", "Линза следопыта\n\nГоворят, он не промахивался.\nПросто носил правильные очки.\n\nСнаряды ищут ближайшего врага", "İz Sürücünün Merceği\n\nHiç ıskalamadığını söylerler.\nSadece doğru gözlüğü takardı.\n\nMermiler en yakın düşmanı arar"),
                [ItemHollowVein] = new("Hollow Vein\n\nLess to lose. More to run with.\nThe body learns to carry nothing.\n\nMax HP capped at 4. +20% movement speed", "Пустая жила\n\nМеньше терять. Больше бежать.\nТело учится не нести ничего.\n\nМакс. здоровье ограничено 4. Скорость +20%", "Boş Damar\n\nKaybedecek daha az şey, kaçacak daha çok yer.\nBeden hiçbir şey taşımamayı öğrenir.\n\nAzami can 4. Hareket hızı +%20"),
                [ItemLeechFang] = new("Leech Fang\n\nIt doesn't take much. Just enough to keep going.\nSomething small, stolen from the dying.\n\nOn kill: 1% chance to restore 1 HP", "Клык пиявки\n\nМного не нужно. Лишь столько, чтобы идти дальше.\nМалость, украденная у умирающих.\n\nЗа убийство: 1% шанс восстановить 1 HP", "Sülük Dişi\n\nFazlasını almaz. Devam etmeye yetecek kadarını alır.\nÖlmekte olandan çalınmış küçük bir şey.\n\nÖldürünce: 1 HP yenileme şansı %1"),
                [ItemQuickHands] = new("Quick Hands\n\nThe shot was already gone before the thought.\nSome things don't wait to be decided.\n\n+10% attack speed", "Быстрые руки\n\nВыстрел ушёл раньше мысли.\nНекоторые вещи не ждут решения.\n\nСкорость атаки +10%", "Hızlı Eller\n\nDüşünceden önce atış çoktan gitti.\nBazı şeyler kararı beklemez.\n\nSaldırı hızı +%10"),
                [ItemScrollOfSplitting] = new("Scroll of Splitting\n\nOne thought, three arrows.\nThe scroll does not care which one hits.\n\n+2 projectiles per shot / -50% fire rate", "Свиток расщепления\n\nОдна мысль, три стрелы.\nСвитку всё равно, какая попадёт.\n\n+2 снаряда за выстрел / темп огня -50%", "Bölünme Parşömeni\n\nBir düşünce, üç ok.\nParşömen hangisinin vurduğunu umursamaz.\n\nAtış başına +2 mermi / ateş hızı -%50"),
                [ItemShardOfRage] = new("Shard of Rage\n\nEach kill sharpens the hand.\nOne wound — and the shard goes cold.\n\nOn kill: +5% attack speed (max 5 stacks). Resets on hit", "Осколок ярости\n\nКаждое убийство точит руку.\nОдна рана — и осколок стынет.\n\nЗа убийство: скорость атаки +5% (до 5 зарядов). Сбрасывается при попадании", "Öfke Parçası\n\nHer öldürme eli keskinleştirir.\nTek yara ve parça soğur.\n\nÖldürünce: saldırı hızı +%5 (en çok 5 yük). İsabet alınca sıfırlanır"),
                [ItemSwiftLegs] = new("Swift Legs\n\nNot faster than fear. Just faster than what follows it.\n\n+10% movement speed", "Быстрые ноги\n\nНе быстрее страха. Лишь быстрее того, что следует за ним.\n\nСкорость движения +10%", "Çevik Bacaklar\n\nKorkudan hızlı değil. Sadece onu takip edenden hızlı.\n\nHareket hızı +%10"),
                [ItemVoidCarapace] = new("Void Carapace\n\nThe attack finds nothing.\nYou were never there\n\n+15% Evasion", "Панцирь пустоты\n\nАтака ничего не находит.\nТебя там и не было.\n\nУклонение +15%", "Boşluk Kabuğu\n\nSaldırı hiçbir şey bulamaz.\nSen hiç orada değildin.\n\nKaçınma +%15"),
                [ItemVoodoo] = new("Voodoo\n\nA blessing carved from a curse.\n\nMax HP is capped at 4\nProjectile damage is doubled", "Вуду\n\nБлагословение, вырезанное из проклятия.\n\nМакс. здоровье ограничено 4\nУрон снарядов удвоен", "Vudu\n\nLanetin içinden oyulmuş bir lütuf.\n\nAzami can 4 ile sınırlı\nMermi hasarı iki katına çıkar"),
                [TraderAlchemist] = new("Alchemist\n\nThe flask-juggler with burned gloves.\n\nBrews danger,\nsells miracles.", "Алхимик\n\nЖонглёр колбами в обгоревших перчатках.\n\nВарит опасность,\nпродаёт чудеса.", "Simyacı\n\nYanmış eldivenli şişe cambazı.\n\nTehlike demler,\nmucize satar."),
                [TraderChef] = new("Chef\n\nThe blade-smiling cook of the road.\n\nFeeds your guts and your\ncourage.", "Повар\n\nДорожный повар, улыбающийся клинку.\n\nКормит твой желудок\nи отвагу.", "Aşçı\n\nYola gülümseyen bıçaklı aşçı.\n\nKarnını ve\ncesaretini besler."),
                [TraderMerchant] = new("Merchant\n\nThe coin-counting king of bad deals.\n\nIf it shines, he\nalready priced it.", "Торговец\n\nКороль дурных сделок, считающий монеты.\n\nЕсли что-то блестит,\nон уже назначил цену.", "Tüccar\n\nKötü anlaşmaların para sayan kralı.\n\nParlıyorsa,\nfiyatını çoktan koymuştur."),
                [TraderPriest] = new("Priest\n\nThe soft-voiced sinner in holy cloth.\n\nBlessings cost less\nthan absolution.", "Священник\n\nТихоголосый грешник в святом одеянии.\n\nБлагословения стоят дешевле\nотпущения грехов.", "Rahip\n\nKutsal kumaş içindeki yumuşak sesli günahkâr.\n\nLütuflar, bağışlanmadan\ndaha ucuza gelir."),
            };

        public static IEnumerable<string> Keys => Entries.Keys;

        public static bool HasKey(string key) => Entries.ContainsKey(key);

        public static string Get(string key, string? languageCode)
        {
            if (!Entries.TryGetValue(key, out var entry))
                return key;

            return LocalizationLanguagePolicy.NormalizeSupportedLanguage(languageCode) switch
            {
                LocalizationLanguagePolicy.Russian => entry.Russian,
                LocalizationLanguagePolicy.Turkish => entry.Turkish,
                _ => entry.English
            };
        }

        public static string Format(string key, string? languageCode, params object[] arguments) =>
            string.Format(CultureInfo.InvariantCulture, Get(key, languageCode), arguments);
    }
}
