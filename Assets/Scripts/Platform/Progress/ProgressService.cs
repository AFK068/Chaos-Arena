using System;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Synchronous local-first facade. A future Yandex transport can submit or
    /// merge serialized documents without changing gameplay callers.
    /// </summary>
    public sealed class ProgressService
    {
        private static ProgressService _shared;

        private readonly ProgressRepository _repository;
        private CloudProgressSyncCoordinator? _cloudSync;

        public ProgressService(ProgressRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public static ProgressService Shared => _shared ??= new ProgressService(
            new ProgressRepository(new PlayerPrefsProgressStorage()));

        public int BestFloor => _repository.BestFloor;
        public int TotalRuns => _repository.TotalRuns;
        public int TotalCoins => _repository.TotalCoins;
        public int TotalKills => _repository.TotalKills;
        public ProgressDocument Snapshot => _repository.Snapshot;
        public CloudSaveStatus CloudStatus => _cloudSync?.Status ?? CloudSaveStatus.LocalOnly;

        public void RecordRun(int floor, int coins, int kills)
        {
            // This call is intentionally first and synchronous. Cloud work is a
            // best-effort hook and can never make a completed run disappear.
            _repository.RecordRun(floor, coins, kills);
            _cloudSync?.NotifyLocalChanged();
        }
        public void Reset() => _repository.Reset();
        public bool TryMergeSerialized(string incomingJson) => _repository.TryMergeSerialized(incomingJson);

        /// <summary>Platform bootstrap attaches at most one nonblocking cloud hook.</summary>
        public void AttachCloudSync(CloudProgressSyncCoordinator cloudSync)
        {
            if (cloudSync == null)
                throw new ArgumentNullException(nameof(cloudSync));
            if (_cloudSync != null)
                return;

            _cloudSync = cloudSync;
            _cloudSync.Initialize();
        }

        public void TickCloudSync() => _cloudSync?.Tick();

        internal string SerializeSnapshot() => ProgressDocumentCodec.Serialize(_repository.Snapshot);

    }
}
