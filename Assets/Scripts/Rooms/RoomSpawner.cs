using System.Collections.Generic;
using UnityEngine;

public class RoomSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float positionJitter = 0.3f;

    public void Spawn(RoomData data, System.Action<EnemyDeathNotifier> onEnemySpawned)
    {
        if (data == null || data.enemies == null || data.enemies.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        var available = new List<Transform>(spawnPoints);

        foreach (var config in data.enemies)
        {
            for (int i = 0; i < config.count; i++)
            {
                if (available.Count == 0)
                    available.AddRange(spawnPoints);

                int idx = Random.Range(0, available.Count);
                Vector3 basePos = available[idx].position;
                available.RemoveAt(idx);

                Vector3 jitter = new Vector3(
                    Random.Range(-positionJitter, positionJitter),
                    Random.Range(-positionJitter, positionJitter),
                    0f
                );

                var enemy = Instantiate(config.prefab, basePos + jitter, Quaternion.identity);
                var notifier = enemy.AddComponent<EnemyDeathNotifier>();
                onEnemySpawned(notifier);
            }
        }
    }
}
