using System;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Synchronous local-first facade for gameplay callers.
    /// </summary>
    public sealed class ProgressService
    {
        private static ProgressService _shared;

        private readonly ProgressRepository _repository;

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

        public void RecordRun(int floor, int coins, int kills)
        {
            _repository.RecordRun(floor, coins, kills);
        }
        public void Reset() => _repository.Reset();
        public bool TryMergeSerialized(string incomingJson) => _repository.TryMergeSerialized(incomingJson);

    }
}
