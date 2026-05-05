using UnityEngine;

public class RunStats
{
    public int Floor;
    public int Coins;
    public int Kills;
    public float StartTime;

    private float _finalDuration = -1f;

    public float Duration => _finalDuration >= 0f ? _finalDuration : Time.time - StartTime;

    public void Reset()
    {
        Floor = 1;
        Coins = 0;
        Kills = 0;
        StartTime = Time.time;
        _finalDuration = -1f;
    }

    public void StopTimer()
    {
        _finalDuration = Time.time - StartTime;
    }
}
