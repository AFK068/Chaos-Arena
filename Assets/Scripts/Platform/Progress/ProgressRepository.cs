using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosArena.Platform
{
    public interface IProgressStorage
    {
        bool HasKey(string key);
        string GetString(string key, string defaultValue);
        int GetInt(string key, int defaultValue);
        void SetString(string key, string value);
        void Save();
    }

    public sealed class PlayerPrefsProgressStorage : IProgressStorage
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// Owns the versioned local document and the one-time legacy PlayerPrefs import.
    /// Audio and language preferences deliberately remain outside this repository.
    /// </summary>
    public sealed class ProgressRepository
    {
        public const string DocumentKey = "chaos_arena.progress.v1";
        // Kept only as a local diagnostic/recovery copy. It contains the same
        // aggregate counters as the document, never account/player identity.
        public const string InvalidDocumentBackupKey = "chaos_arena.progress.v1.invalid_backup";
        public const string DeviceIdKey = "chaos_arena.progress.device_id";
        public const string LegacyBestFloorKey = "stats.best_floor";
        public const string LegacyTotalRunsKey = "stats.total_runs";
        public const string LegacyTotalCoinsKey = "stats.total_coins";
        public const string LegacyTotalKillsKey = "stats.total_kills";

        private readonly IProgressStorage _storage;
        private readonly string _deviceId;
        private ProgressDocument _document;
        private LocalDocumentLoadState _loadState;

        public enum LocalDocumentLoadState
        {
            Valid,
            MigratedLegacy,
            CorruptOrUnsupported
        }

        public ProgressRepository(IProgressStorage storage, Func<string> deviceIdFactory = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _deviceId = LoadOrCreateDeviceId(deviceIdFactory ?? CreateDeviceId);
            _document = LoadOrMigrate();
        }

        public string DeviceId => _deviceId;
        public ProgressDocument Snapshot => Clone(_document);
        public int BestFloor => _document.bestFloor;
        public int TotalRuns => GetTotal(counter => counter.totalRuns);
        public int TotalCoins => GetTotal(counter => counter.totalCoins);
        public int TotalKills => GetTotal(counter => counter.totalKills);
        /// <summary>Signals that the original raw value was quarantined before any new save.</summary>
        public LocalDocumentLoadState LoadState => _loadState;
        public bool HasQuarantinedLocalDocument => _loadState == LocalDocumentLoadState.CorruptOrUnsupported;

        public void RecordRun(int floor, int coins, int kills)
        {
            if (floor < 0)
                throw new ArgumentOutOfRangeException(nameof(floor));
            if (coins < 0)
                throw new ArgumentOutOfRangeException(nameof(coins));
            if (kills < 0)
                throw new ArgumentOutOfRangeException(nameof(kills));

            var device = GetOrCreateDeviceCounters();
            _document.bestFloor = Math.Max(_document.bestFloor, floor);
            device.totalRuns = SaturatingAdd(device.totalRuns, 1);
            device.totalCoins = SaturatingAdd(device.totalCoins, coins);
            device.totalKills = SaturatingAdd(device.totalKills, kills);
            Persist();
        }

        /// <summary>
        /// Used by the existing developer reset action. An empty persisted v1
        /// document prevents a future startup from re-importing legacy keys.
        /// </summary>
        public void Reset()
        {
            _document = ProgressDocument.Empty();
            Persist();
        }

        public bool TryMergeSerialized(string incomingJson)
        {
            if (!ProgressDocumentCodec.TryDeserialize(incomingJson, out var incoming) ||
                !ProgressDocumentMerger.TryMerge(_document, incoming, out var merged))
                return false;

            _document = merged;
            Persist();
            return true;
        }

        public static ProgressDocument ImportLegacy(IProgressStorage storage, string deviceId)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));
            if (!ProgressDocumentValidation.IsValidDeviceId(deviceId))
                throw new ArgumentException("A non-empty device id is required.", nameof(deviceId));

            return new ProgressDocument
            {
                bestFloor = ReadLegacyNonNegative(storage, LegacyBestFloorKey),
                devices = new[]
                {
                    new ProgressDeviceCounters
                    {
                        deviceId = deviceId,
                        totalRuns = ReadLegacyNonNegative(storage, LegacyTotalRunsKey),
                        totalCoins = ReadLegacyNonNegative(storage, LegacyTotalCoinsKey),
                        totalKills = ReadLegacyNonNegative(storage, LegacyTotalKillsKey)
                    }
                }
            };
        }

        private ProgressDocument LoadOrMigrate()
        {
            if (_storage.HasKey(DocumentKey))
            {
                var serialized = _storage.GetString(DocumentKey, string.Empty);
                if (ProgressDocumentCodec.TryDeserialize(serialized, out var existing))
                {
                    _loadState = LocalDocumentLoadState.Valid;
                    return existing;
                }

                // Preserve the exact raw document before RecordRun, merge, or
                // reset can replace it. Keep the first failure deterministically
                // so later writes cannot erase the only recovery evidence.
                if (!_storage.HasKey(InvalidDocumentBackupKey))
                {
                    _storage.SetString(InvalidDocumentBackupKey, serialized);
                    _storage.Save();
                }

                _loadState = LocalDocumentLoadState.CorruptOrUnsupported;
                return ProgressDocument.Empty();
            }

            var migrated = ImportLegacy(_storage, _deviceId);
            _loadState = LocalDocumentLoadState.MigratedLegacy;
            _document = migrated;
            Persist();
            return migrated;
        }

        private string LoadOrCreateDeviceId(Func<string> deviceIdFactory)
        {
            if (_storage.HasKey(DeviceIdKey))
            {
                var existing = _storage.GetString(DeviceIdKey, string.Empty);
                if (ProgressDocumentValidation.IsValidDeviceId(existing))
                    return existing;
            }

            var generated = deviceIdFactory();
            if (!ProgressDocumentValidation.IsValidDeviceId(generated))
                throw new InvalidOperationException("The progress device id factory returned an invalid id.");

            _storage.SetString(DeviceIdKey, generated);
            _storage.Save();
            return generated;
        }

        private ProgressDeviceCounters GetOrCreateDeviceCounters()
        {
            foreach (var device in _document.devices)
            {
                if (string.Equals(device.deviceId, _deviceId, StringComparison.Ordinal))
                    return device;
            }

            var created = new ProgressDeviceCounters { deviceId = _deviceId };
            var devices = new List<ProgressDeviceCounters>(_document.devices) { created };
            _document.devices = devices.ToArray();
            return created;
        }

        private int GetTotal(Func<ProgressDeviceCounters, int> selector)
        {
            var total = 0;
            foreach (var device in _document.devices)
                total = SaturatingAdd(total, selector(device));
            return total;
        }

        private void Persist()
        {
            _document = Clone(_document);
            _storage.SetString(DocumentKey, ProgressDocumentCodec.Serialize(_document));
            _storage.Save();
        }

        private static ProgressDocument Clone(ProgressDocument source)
        {
            if (!ProgressDocumentValidation.TryNormalize(source, out var clone))
                throw new InvalidOperationException("The local progress document became invalid.");
            return clone;
        }

        private static int ReadLegacyNonNegative(IProgressStorage storage, string key) =>
            Math.Max(0, storage.GetInt(key, 0));

        private static int SaturatingAdd(int left, int right) =>
            left > int.MaxValue - right ? int.MaxValue : left + right;

        private static string CreateDeviceId() => Guid.NewGuid().ToString("N");
    }
}
