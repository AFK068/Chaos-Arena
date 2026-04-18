using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Room Data")]
public class RoomData : ScriptableObject
{
    public RoomType type;
    public GameObject roomPrefab;

    // Normal, Boss, Gauntlet
    public EnemySpawnConfig[] enemies;

    // Chest
    public int chestCount = 1;
}

[Serializable]
public struct EnemySpawnConfig
{
    public GameObject prefab;
    public int count;
}
