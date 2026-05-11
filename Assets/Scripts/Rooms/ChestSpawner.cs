using UnityEngine;

public class ChestSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] chestPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        if (chestPrefabs == null || chestPrefabs.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        foreach (var point in spawnPoints)
        {
            if (point == null) continue;

            var prefab = chestPrefabs[Random.Range(0, chestPrefabs.Length)];
            if (prefab == null) continue;

            Instantiate(prefab, point.position, Quaternion.identity, transform);
        }
    }
}
