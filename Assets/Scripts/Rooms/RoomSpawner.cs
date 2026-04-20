using System.Collections.Generic;
using UnityEngine;

public class RoomSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public void Spawn(RoomData data, System.Action<EnemyDeathNotifier> onEnemySpawned)
    {
        if (data == null || data.enemies == null || data.enemies.Length == 0) return;

        var available = new List<Transform>(spawnPoints);

        foreach (var config in data.enemies)
        {
            int toSpawn = Mathf.Min(config.count, available.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                int idx = Random.Range(0, available.Count);
                Vector3 pos = available[idx].position;
                available.RemoveAt(idx);

                var enemy = Instantiate(config.prefab, pos, Quaternion.identity);
                var notifier = enemy.AddComponent<EnemyDeathNotifier>();
                onEnemySpawned(notifier);
            }
        }
    }
}
