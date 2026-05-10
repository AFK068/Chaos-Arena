using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Room Data")]
public class RoomData : ScriptableObject
{
    public RoomType type;
    public GameObject roomPrefab;

    // Normal, Gauntlet — random pool mode (preferred).
    // Если enemyPool не пуст — спавнер берёт случайное число врагов от minCount до maxCount,
    // случайно выбирая из этого пула. Удобно для процедурного разнообразия.
    public GameObject[] enemyPool;
    public int minCount = 1;
    public int maxCount = 3;

    // Boss / точная композиция — exact mode (fallback).
    // Используется только если enemyPool пуст. Спавнит ровно те prefab'ы и в тех количествах
    // что описаны. Подходит для boss-комнат: [BossPrefab × 1].
    public EnemySpawnConfig[] enemies;

    // Chest
    public int chestCount = 1;

    // Boss
    public GameObject[] bossDropPool;
    public int bossDropCount = 1;
}

[Serializable]
public struct EnemySpawnConfig
{
    public GameObject prefab;
    public int count;
}
