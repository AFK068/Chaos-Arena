using System.Collections.Generic;
using UnityEngine;

public class RoomSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float positionJitter = 0.3f;

    public void Spawn(RoomData data, System.Action<EnemyDeathNotifier> onEnemySpawned)
    {
        if (data == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        if (data.enemyPool != null && data.enemyPool.Length > 0)
            SpawnFromPool(data, onEnemySpawned);
        else if (data.enemies != null && data.enemies.Length > 0)
            SpawnExact(data, onEnemySpawned);
    }

    private void SpawnFromPool(RoomData data, System.Action<EnemyDeathNotifier> onEnemySpawned)
    {
        int min = Mathf.Max(1, data.minCount);
        int max = Mathf.Max(min, data.maxCount);
        int count = Random.Range(min, max + 1);

        var available = new List<Transform>(spawnPoints);

        for (int i = 0; i < count; i++)
        {
            if (available.Count == 0)
                available.AddRange(spawnPoints);

            int pointIdx = Random.Range(0, available.Count);
            Vector3 basePos = available[pointIdx].position;
            available.RemoveAt(pointIdx);

            var prefab = data.enemyPool[Random.Range(0, data.enemyPool.Length)];
            if (prefab == null) continue;

            SpawnAt(prefab, basePos, onEnemySpawned);
        }
    }

    private void SpawnExact(RoomData data, System.Action<EnemyDeathNotifier> onEnemySpawned)
    {
        var available = new List<Transform>(spawnPoints);

        foreach (var config in data.enemies)
        {
            for (int i = 0; i < config.count; i++)
            {
                if (available.Count == 0)
                    available.AddRange(spawnPoints);

                int pointIdx = Random.Range(0, available.Count);
                Vector3 basePos = available[pointIdx].position;
                available.RemoveAt(pointIdx);

                if (config.prefab == null) continue;
                SpawnAt(config.prefab, basePos, onEnemySpawned);
            }
        }
    }

    private void SpawnAt(GameObject prefab, Vector3 basePos, System.Action<EnemyDeathNotifier> onEnemySpawned)
    {
        Vector3 jitter = new Vector3(
            Random.Range(-positionJitter, positionJitter),
            Random.Range(-positionJitter, positionJitter),
            0f
        );

        var enemy = Instantiate(prefab, basePos + jitter, Quaternion.identity);
        var notifier = enemy.AddComponent<EnemyDeathNotifier>();
        onEnemySpawned(notifier);
    }
}
