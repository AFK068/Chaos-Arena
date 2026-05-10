using UnityEngine;

[System.Serializable]
public struct CoinDropEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float weight;
}

public class CoinDrop : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.45f;
    [SerializeField] private float spawnRadius = 0.3f;

    public float DropChance => dropChance;
    public float SpawnRadius => spawnRadius;

    public void DropImmediate(Vector3 position)
    {
        if (Random.value <= dropChance)
            SpawnCoin(position, spawnRadius);
    }

    private static CoinDropConfig _config;

    public static CoinDropConfig Config
    {
        get
        {
            if (_config == null)
                _config = Resources.Load<CoinDropConfig>("CoinDropConfig");
            return _config;
        }
    }

    public static void SpawnCoin(Vector3 position, float spawnRadius = 0.3f)
    {
        if (Config == null || Config.coinPool == null || Config.coinPool.Length == 0) return;
        var prefab = PickWeighted(Config.coinPool);
        if (prefab == null) return;
        var instance = Object.Instantiate(prefab, position, Quaternion.identity);
        var target = position + (Vector3)(Random.insideUnitCircle.normalized * spawnRadius);
        var drop = instance.GetComponent<ItemDrop>() ?? instance.AddComponent<ItemDrop>();
        drop.Throw(target, 0.4f);
    }

    private static GameObject PickWeighted(CoinDropEntry[] pool)
    {
        float roll = Random.value;
        float cumulative = 0f;
        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry.prefab;
        }
        return pool[pool.Length - 1].prefab;
    }
}
