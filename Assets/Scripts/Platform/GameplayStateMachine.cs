using System;

namespace ChaosArena.Platform
{
    /// <summary>
    /// Keeps local-menu pause and platform pause as separate owners.  The bridge
    /// only receives a state transition after it has successfully initialized.
    /// </summary>
    public sealed class GameplayStateMachine
    {
        private bool _bridgeReady;
        private bool _gameplayReported;

        public bool WantsGameplay { get; private set; }
        public bool IsLocalPauseOpen { get; private set; }
        public bool IsPlatformPaused { get; private set; }

        public bool ShouldRunGameplay => WantsGameplay && !IsLocalPauseOpen && !IsPlatformPaused;
        public bool ShouldPauseAudio => IsLocalPauseOpen || IsPlatformPaused;
        public bool ShouldPauseSimulation => IsLocalPauseOpen || IsPlatformPaused;

        public event Action<bool>? GameplayStateChanged;

        public void SetBridgeReady()
        {
            _bridgeReady = true;
            ReconcileGameplay();
        }

        public void SetGameplayIntent(bool active)
        {
            WantsGameplay = active;
            ReconcileGameplay();
        }

        public void SetLocalPause(bool paused)
        {
            IsLocalPauseOpen = paused;
            ReconcileGameplay();
        }

        public void SetPlatformPause(bool paused)
        {
            IsPlatformPaused = paused;
            ReconcileGameplay();
        }

        private void ReconcileGameplay()
        {
            var shouldRun = ShouldRunGameplay;
            if (!_bridgeReady || shouldRun == _gameplayReported)
                return;

            _gameplayReported = shouldRun;
            GameplayStateChanged?.Invoke(shouldRun);
        }
    }
}
