using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class ProgressFoundationTests
    {
        [Test]
        public void Merge_IsCommutativeAssociativeAndIdempotent()
        {
            var a = Document(4, Counter("phone", 2, 10, 1));
            var b = Document(7, Counter("desktop", 3, 1, 8), Counter("phone", 1, 15, 0));
            var c = Document(6, Counter("tablet", 4, 4, 4), Counter("phone", 8, 9, 2));

            Assert.That(MergeJson(a, b), Is.EqualTo(MergeJson(b, a)));
            Assert.That(MergeJson(Merge(a, b), c), Is.EqualTo(MergeJson(a, Merge(b, c))));
            Assert.That(MergeJson(a, a), Is.EqualTo(ProgressDocumentCodec.Serialize(a)));
        }

        [Test]
        public void Merge_UsesMaximumForBestFloorAndEachDeviceCounter()
        {
            var merged = Merge(
                Document(3, Counter("one", 2, 30, 4)),
                Document(9, Counter("one", 5, 20, 6), Counter("two", 1, 2, 3)));

            Assert.That(merged.bestFloor, Is.EqualTo(9));
            Assert.That(merged.devices, Has.Length.EqualTo(2));
            AssertCounter(merged.devices[0], "one", 5, 30, 6);
            AssertCounter(merged.devices[1], "two", 1, 2, 3);
        }

        [Test]
        public void Codec_SortsDevicesDeterministically()
        {
            var document = Document(1, Counter("z", 1, 1, 1), Counter("a", 2, 2, 2));

            var json = ProgressDocumentCodec.Serialize(document);
            Assert.That(ProgressDocumentCodec.TryDeserialize(json, out var parsed), Is.True);

            Assert.That(parsed.devices[0].deviceId, Is.EqualTo("a"));
            Assert.That(parsed.devices[1].deviceId, Is.EqualTo("z"));
            Assert.That(json.IndexOf("\"a\"", StringComparison.Ordinal),
                Is.LessThan(json.IndexOf("\"z\"", StringComparison.Ordinal)));
        }

        [TestCase("not json")]
        [TestCase("{\"schemaVersion\":2,\"bestFloor\":1,\"devices\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"bestFloor\":-1,\"devices\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"bestFloor\":1,\"devices\":[{\"deviceId\":\"\",\"totalRuns\":0,\"totalCoins\":0,\"totalKills\":0}]}")]
        [TestCase("{\"schemaVersion\":1,\"bestFloor\":1,\"devices\":[{\"deviceId\":\"a\",\"totalRuns\":0,\"totalCoins\":0,\"totalKills\":0},{\"deviceId\":\"a\",\"totalRuns\":0,\"totalCoins\":0,\"totalKills\":0}]}")]
        public void Codec_RejectsMalformedUnknownOrInvalidDocuments(string json)
        {
            Assert.That(ProgressDocumentCodec.TryDeserialize(json, out _), Is.False);
        }

        [Test]
        public void LegacyImport_UsesZeroForMissingAndNegativeKeys()
        {
            var storage = new MemoryProgressStorage();
            storage.SetInt(ProgressRepository.LegacyBestFloorKey, 12);
            storage.SetInt(ProgressRepository.LegacyTotalCoinsKey, -4);

            var migrated = ProgressRepository.ImportLegacy(storage, "device-a");

            Assert.That(migrated.bestFloor, Is.EqualTo(12));
            AssertCounter(migrated.devices[0], "device-a", 0, 0, 0);
        }

        [Test]
        public void Repository_MigratesLegacyOnlyOnceAndPreservesLegacyKeys()
        {
            var storage = new MemoryProgressStorage();
            storage.SetInt(ProgressRepository.LegacyBestFloorKey, 4);
            storage.SetInt(ProgressRepository.LegacyTotalRunsKey, 2);
            storage.SetInt(ProgressRepository.LegacyTotalCoinsKey, 10);
            storage.SetInt(ProgressRepository.LegacyTotalKillsKey, 3);

            var first = new ProgressRepository(storage, () => "device-a");
            Assert.That(first.BestFloor, Is.EqualTo(4));
            Assert.That(first.TotalRuns, Is.EqualTo(2));
            Assert.That(storage.GetInt(ProgressRepository.LegacyTotalRunsKey, -1), Is.EqualTo(2));

            storage.SetInt(ProgressRepository.LegacyTotalRunsKey, 99);
            var second = new ProgressRepository(storage, () => "different-device");
            Assert.That(second.DeviceId, Is.EqualTo("device-a"));
            Assert.That(second.TotalRuns, Is.EqualTo(2));
        }

        [Test]
        public void Repository_PersistsLocallyAndRecordsRunsSynchronously()
        {
            var storage = new MemoryProgressStorage();
            var repository = new ProgressRepository(storage, () => "device-a");

            repository.RecordRun(5, 7, 9);
            repository.RecordRun(3, 2, 1);

            Assert.That(repository.BestFloor, Is.EqualTo(5));
            Assert.That(repository.TotalRuns, Is.EqualTo(2));
            Assert.That(repository.TotalCoins, Is.EqualTo(9));
            Assert.That(repository.TotalKills, Is.EqualTo(10));
            Assert.That(storage.HasKey(ProgressRepository.DocumentKey), Is.True);
            Assert.That(storage.SaveCalls, Is.GreaterThan(0));

            var reloaded = new ProgressRepository(storage, () => "unused");
            Assert.That(reloaded.DeviceId, Is.EqualTo("device-a"));
            Assert.That(reloaded.TotalRuns, Is.EqualTo(2));
            Assert.That(reloaded.TotalCoins, Is.EqualTo(9));
            Assert.That(reloaded.TotalKills, Is.EqualTo(10));
        }

        [Test]
        public void Service_ExposesSynchronousPlayerStatsCompatibleGetters()
        {
            var service = new ProgressService(
                new ProgressRepository(new MemoryProgressStorage(), () => "device-a"));

            service.RecordRun(6, 4, 2);

            Assert.That(service.BestFloor, Is.EqualTo(6));
            Assert.That(service.TotalRuns, Is.EqualTo(1));
            Assert.That(service.TotalCoins, Is.EqualTo(4));
            Assert.That(service.TotalKills, Is.EqualTo(2));
        }

        [Test]
        public void Repository_RejectsBadIncomingDocumentWithoutReplacingGoodLocalProgress()
        {
            var storage = new MemoryProgressStorage();
            var repository = new ProgressRepository(storage, () => "device-a");
            repository.RecordRun(5, 7, 9);
            var before = storage.GetString(ProgressRepository.DocumentKey, string.Empty);

            Assert.That(repository.TryMergeSerialized("{\"schemaVersion\":999}"), Is.False);

            Assert.That(storage.GetString(ProgressRepository.DocumentKey, string.Empty), Is.EqualTo(before));
            Assert.That(repository.BestFloor, Is.EqualTo(5));
            Assert.That(repository.TotalRuns, Is.EqualTo(1));
        }

        [TestCase("not json")]
        [TestCase("{\"schemaVersion\":2,\"bestFloor\":77,\"devices\":[]}")]
        public void Repository_QuarantinesCorruptOrFutureRawDocumentBeforeRecordRun(string rawDocument)
        {
            var storage = new MemoryProgressStorage();
            storage.SetString(ProgressRepository.DocumentKey, rawDocument);

            var repository = new ProgressRepository(storage, () => "device-a");
            repository.RecordRun(4, 5, 6);

            Assert.That(repository.LoadState,
                Is.EqualTo(ProgressRepository.LocalDocumentLoadState.CorruptOrUnsupported));
            Assert.That(repository.HasQuarantinedLocalDocument, Is.True);
            Assert.That(storage.GetString(ProgressRepository.InvalidDocumentBackupKey, string.Empty),
                Is.EqualTo(rawDocument));
            Assert.That(storage.GetString(ProgressRepository.DocumentKey, string.Empty),
                Is.Not.EqualTo(rawDocument));
            Assert.That(repository.TotalRuns, Is.EqualTo(1));
        }

        [Test]
        public void Repository_ResetPersistsZeroDocumentSoLegacyValuesDoNotReturn()
        {
            var storage = new MemoryProgressStorage();
            storage.SetInt(ProgressRepository.LegacyTotalRunsKey, 8);
            var repository = new ProgressRepository(storage, () => "device-a");

            repository.Reset();
            var reloaded = new ProgressRepository(storage, () => "unused");

            Assert.That(reloaded.TotalRuns, Is.EqualTo(0));
            Assert.That(storage.GetInt(ProgressRepository.LegacyTotalRunsKey, -1), Is.EqualTo(8));
        }

        [Test]
        public void RecordRun_RejectsNegativeValues()
        {
            var repository = new ProgressRepository(new MemoryProgressStorage(), () => "device-a");

            Assert.That(() => repository.RecordRun(-1, 0, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => repository.RecordRun(0, -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => repository.RecordRun(0, 0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static ProgressDocument Document(int bestFloor, params ProgressDeviceCounters[] devices) => new()
        {
            bestFloor = bestFloor,
            devices = devices
        };

        private static ProgressDeviceCounters Counter(string deviceId, int runs, int coins, int kills) => new()
        {
            deviceId = deviceId,
            totalRuns = runs,
            totalCoins = coins,
            totalKills = kills
        };

        private static ProgressDocument Merge(ProgressDocument left, ProgressDocument right)
        {
            Assert.That(ProgressDocumentMerger.TryMerge(left, right, out var merged), Is.True);
            return merged;
        }

        private static string MergeJson(ProgressDocument left, ProgressDocument right) =>
            ProgressDocumentCodec.Serialize(Merge(left, right));

        private static void AssertCounter(ProgressDeviceCounters actual, string id, int runs, int coins, int kills)
        {
            Assert.That(actual.deviceId, Is.EqualTo(id));
            Assert.That(actual.totalRuns, Is.EqualTo(runs));
            Assert.That(actual.totalCoins, Is.EqualTo(coins));
            Assert.That(actual.totalKills, Is.EqualTo(kills));
        }

        private sealed class MemoryProgressStorage : IProgressStorage
        {
            private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _ints = new(StringComparer.Ordinal);

            public int SaveCalls { get; private set; }

            public bool HasKey(string key) => _strings.ContainsKey(key) || _ints.ContainsKey(key);
            public string GetString(string key, string defaultValue) =>
                _strings.TryGetValue(key, out var value) ? value : defaultValue;
            public int GetInt(string key, int defaultValue) =>
                _ints.TryGetValue(key, out var value) ? value : defaultValue;
            public void SetString(string key, string value) => _strings[key] = value;
            public void SetInt(string key, int value) => _ints[key] = value;
            public void Save() => SaveCalls++;
        }
    }
}
