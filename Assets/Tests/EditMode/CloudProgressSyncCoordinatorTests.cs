using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ChaosArena.Platform.Tests
{
    public sealed class CloudProgressSyncCoordinatorTests
    {
        [Test]
        public void GuestStartup_ReadsMergesAndFlushesAggregateBaseline()
        {
            var storage = new MemoryStorage();
            var repository = new ProgressRepository(storage, () => "local");
            repository.RecordRun(3, 2, 1);
            var transport = new FakeTransport();
            var sync = new CloudProgressSyncCoordinator(
                transport,
                () => ProgressDocumentCodec.Serialize(repository.Snapshot),
                repository.TryMergeSerialized);

            sync.Initialize();
            Assert.That(transport.GetGuestCalls, Is.EqualTo(1));
            Assert.That(transport.GetDataCalls, Is.EqualTo(0));
            Assert.That(transport.SetCalls, Is.EqualTo(0));

            transport.CompleteGuest(new CloudTransportResult(true));
            transport.CompleteRead(new CloudTransportResult(true,
                DocumentJson(8, Counter("local", 1, 9, 0), Counter("other", 4, 5, 6))));

            Assert.That(repository.BestFloor, Is.EqualTo(8));
            Assert.That(repository.TotalRuns, Is.EqualTo(5));
            Assert.That(repository.TotalCoins, Is.EqualTo(14));
            Assert.That(transport.SetCalls, Is.EqualTo(1));
            Assert.That(transport.SetFlushes[0], Is.True);
            Assert.That(transport.SetDocuments[0], Does.Contain("\"other\""));
        }

        [Test]
        public void LocalChangeDuringInitialRead_RefreshesMergedWrite()
        {
            var storage = new MemoryStorage();
            var repository = new ProgressRepository(storage, () => "local");
            var transport = new FakeTransport();
            var sync = new CloudProgressSyncCoordinator(
                transport,
                () => ProgressDocumentCodec.Serialize(repository.Snapshot),
                repository.TryMergeSerialized);

            sync.Initialize();
            transport.CompleteGuest(new CloudTransportResult(true));
            repository.RecordRun(3, 2, 1);
            sync.NotifyLocalChanged();
            transport.CompleteRead(new CloudTransportResult(true,
                DocumentJson(9, Counter("remote", 7, 40, 5))));

            Assert.That(transport.SetCalls, Is.EqualTo(1));
            Assert.That(ProgressDocumentCodec.TryDeserialize(transport.SetDocuments[0], out var sent), Is.True);
            Assert.That(sent.bestFloor, Is.EqualTo(9));
            Assert.That(sent.devices, Has.Length.EqualTo(2));
            Assert.That(sent.devices[0].deviceId, Is.EqualTo("local"));
            Assert.That(sent.devices[1].deviceId, Is.EqualTo("remote"));
        }

        [Test]
        public void FailedOrInvalidInitialRead_KeepsWritesGatedUntilSuccessfulRetry()
        {
            var transport = new FakeTransport();
            var now = 0f;
            var document = DocumentJson(2, Counter("device", 1, 1, 1));
            var sync = new CloudProgressSyncCoordinator(transport, () => document, _ => false, () => now, () => 0f);

            sync.Initialize();
            sync.NotifyLocalChanged();
            transport.CompleteGuest(new CloudTransportResult(true));
            transport.CompleteRead(new CloudTransportResult(false));

            Assert.That(sync.IsDirty, Is.True);
            Assert.That(transport.SetCalls, Is.EqualTo(0));
            now = 1.1f;
            sync.Tick();
            Assert.That(transport.GetDataCalls, Is.EqualTo(2));

            transport.CompleteRead(new CloudTransportResult(true, "{\"schemaVersion\":999}"));
            Assert.That(transport.SetCalls, Is.EqualTo(0));
            now = 3.3f;
            sync.Tick();
            transport.CompleteRead(new CloudTransportResult(true));
            Assert.That(transport.SetCalls, Is.EqualTo(1));
        }

        [Test]
        public void GuestAcquisitionFailure_RetriesWithoutLosingLocalProgress()
        {
            var transport = new FakeTransport();
            var now = 0f;
            var sync = new CloudProgressSyncCoordinator(
                transport,
                () => DocumentJson(2, Counter("device", 1, 1, 1)),
                _ => true,
                () => now,
                () => 0f);

            sync.Initialize();
            sync.NotifyLocalChanged();
            transport.CompleteGuest(new CloudTransportResult(false));

            Assert.That(transport.GetDataCalls, Is.EqualTo(0));
            Assert.That(transport.SetCalls, Is.EqualTo(0));
            now = 1.1f;
            sync.Tick();
            Assert.That(transport.GetGuestCalls, Is.EqualTo(2));
        }

        [Test]
        public void RemoteFailure_PreservesDirtyLocalDocumentForBoundedRetry()
        {
            var transport = new FakeTransport();
            var now = 0f;
            var sync = NewSync(transport, () => DocumentJson(2, Counter("device", 1, 1, 1)), () => now);
            InitializeGuest(sync, transport);

            sync.NotifyLocalChanged();
            transport.CompleteWrite(new CloudTransportResult(false));

            Assert.That(sync.IsDirty, Is.True);
            now = 4.9f;
            sync.Tick();
            Assert.That(transport.SetCalls, Is.EqualTo(1));
            now = 5f;
            sync.Tick();
            Assert.That(transport.SetCalls, Is.EqualTo(2));
        }

        [Test]
        public void WritesAreCoalescedAndDuplicateCallbacksCannotStartParallelCalls()
        {
            var transport = new FakeTransport();
            var now = 0f;
            var sync = NewSync(transport, () => DocumentJson(2, Counter("device", 1, 1, 1)), () => now);
            InitializeGuest(sync, transport);

            Assert.That(transport.SetCalls, Is.EqualTo(1)); // startup flush
            transport.CompleteWrite(new CloudTransportResult(true));
            now = 4f;
            sync.NotifyLocalChanged();
            sync.NotifyLocalChanged();
            Assert.That(transport.SetCalls, Is.EqualTo(2));
            transport.CompleteWrite(new CloudTransportResult(true));
            transport.CompleteWrite(new CloudTransportResult(true)); // duplicate terminal
            Assert.That(transport.SetCalls, Is.EqualTo(2));
        }

        [Test]
        public void OversizedPayload_IsNeverSent()
        {
            var transport = new FakeTransport();
            var hugeDocument = new string('x', CloudProgressSyncCoordinator.MaxPayloadBytes + 1);
            var sync = NewSync(transport, () => hugeDocument);
            InitializeGuest(sync, transport);

            Assert.That(transport.SetCalls, Is.EqualTo(0));
            Assert.That(sync.IsDirty, Is.True);
            Assert.That(sync.Status, Is.EqualTo(CloudSaveStatus.Error));
        }

        [Test]
        public void UnavailableTransport_IsEditorSafeLocalOnly()
        {
            var transport = new FakeTransport { IsAvailable = false };
            var sync = NewSync(transport, () => DocumentJson(0));

            sync.Initialize();
            sync.NotifyLocalChanged();

            Assert.That(sync.Status, Is.EqualTo(CloudSaveStatus.LocalOnly));
            Assert.That(transport.GetGuestCalls, Is.EqualTo(0));
            Assert.That(transport.SetCalls, Is.EqualTo(0));
        }

        private static CloudProgressSyncCoordinator NewSync(
            FakeTransport transport, Func<string> snapshot, Func<float> now = null) =>
            new(transport, snapshot, _ => true, now);

        private static void InitializeGuest(CloudProgressSyncCoordinator sync, FakeTransport transport)
        {
            sync.Initialize();
            transport.CompleteGuest(new CloudTransportResult(true));
            transport.CompleteRead(new CloudTransportResult(true));
        }

        private static string DocumentJson(int floor, params ProgressDeviceCounters[] devices) =>
            ProgressDocumentCodec.Serialize(new ProgressDocument { bestFloor = floor, devices = devices });

        private static ProgressDeviceCounters Counter(string id, int runs, int coins, int kills) => new()
        {
            deviceId = id,
            totalRuns = runs,
            totalCoins = coins,
            totalKills = kills
        };

        private sealed class FakeTransport : ICloudProgressTransport
        {
            private readonly List<Action<CloudTransportResult>> _guest = new();
            private readonly List<Action<CloudTransportResult>> _read = new();
            private readonly List<Action<CloudTransportResult>> _write = new();

            public bool IsAvailable { get; set; } = true;
            public int GetGuestCalls => _guest.Count;
            public int GetDataCalls => _read.Count;
            public int SetCalls => _write.Count;
            public List<string> SetDocuments { get; } = new();
            public List<bool> SetFlushes { get; } = new();

            public void GetGuest(Action<CloudTransportResult> completed) => _guest.Add(completed);
            public void GetData(Action<CloudTransportResult> completed) => _read.Add(completed);
            public void SetData(string serializedDocument, bool flush, Action<CloudTransportResult> completed)
            {
                SetDocuments.Add(serializedDocument);
                SetFlushes.Add(flush);
                _write.Add(completed);
            }

            public void CompleteGuest(CloudTransportResult result) => _guest[_guest.Count - 1](result);
            public void CompleteRead(CloudTransportResult result) => _read[_read.Count - 1](result);
            public void CompleteWrite(CloudTransportResult result) => _write[_write.Count - 1](result);
        }

        private sealed class MemoryStorage : IProgressStorage
        {
            private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);
            public int SaveCalls { get; private set; }
            public bool HasKey(string key) => _strings.ContainsKey(key);
            public string GetString(string key, string defaultValue) => _strings.TryGetValue(key, out var value) ? value : defaultValue;
            public int GetInt(string key, int defaultValue) => defaultValue;
            public void SetString(string key, string value) => _strings[key] = value;
            public void Save() => SaveCalls++;
        }
    }
}
