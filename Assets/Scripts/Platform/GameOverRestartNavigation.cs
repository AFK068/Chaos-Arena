namespace ChaosArena.Platform
{
    /// <summary>
    /// Owns the one-shot Game Over restart intent independently of ad and UI
    /// lifetimes. A cancel or scene change invalidates every earlier token.
    /// </summary>
    public sealed class GameOverRestartNavigation
    {
        private ulong _generation;
        private bool _restartPending;

        public bool TryBegin(out ulong token)
        {
            token = 0;
            if (_restartPending)
                return false;

            _restartPending = true;
            token = ++_generation;
            return true;
        }

        public void Cancel()
        {
            _restartPending = false;
            ++_generation;
        }

        public bool TryComplete(ulong token, bool isGameOverScene)
        {
            if (!_restartPending || token != _generation)
                return false;

            if (!isGameOverScene)
            {
                Cancel();
                return false;
            }

            _restartPending = false;
            return true;
        }
    }
}
