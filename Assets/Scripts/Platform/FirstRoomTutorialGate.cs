namespace ChaosArena.Platform
{
    /// <summary>
    /// The floor generator reserves node 0 for the start room. Keeping this
    /// predicate free of Unity state makes the tutorial's run/floor contract
    /// independently testable.
    /// </summary>
    public static class FirstRoomTutorialGate
    {
        public static bool ShouldShow(int currentFloor, int roomNodeId, bool alreadyShownForFloor) =>
            currentFloor == 1 && roomNodeId == 0 && !alreadyShownForFloor;
    }
}
