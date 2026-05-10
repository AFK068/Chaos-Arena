using UnityEngine;

public class TraderSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] traderPrefabs;
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        if (traderPrefabs == null || traderPrefabs.Length == 0) return;

        var prefab = traderPrefabs[Random.Range(0, traderPrefabs.Length)];
        if (prefab == null) return;

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Instantiate(prefab, pos, Quaternion.identity, transform);
    }
}
