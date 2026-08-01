using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CoinDenomination
{
    public GameObject prefab;
    public int value;
}

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private Sprite[] disappearFrames;
    [SerializeField] private Color disappearColor = Color.white;
    [SerializeField] private float openFps = 12f;
    [SerializeField] private float destroyDelay = 3f;

    [Header("Loot - Items")]
    [SerializeField] private GameObject[] guaranteedItems;
    [SerializeField] private GameObject[] itemPool;
    [SerializeField] [Range(0f, 1f)] private float itemDropChance = 0.4f;

    [Header("Loot - Coins")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int minCoins = 10;
    [SerializeField] private int maxCoins = 15;

    [Header("Drop Physics")]
    [SerializeField] private float dropRadius = 1f;
    [SerializeField] private float dropDuration = 0.4f;

    private SpriteRenderer _renderer;
    private bool _opened;
    private float _baseBottomY;
    private Collider2D _collider;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        if (_renderer != null && _renderer.sprite != null)
        {
            var scale = _renderer.transform.lossyScale.y;
            _baseBottomY = _renderer.transform.position.y + _renderer.sprite.bounds.min.y * scale;
        }
    }

    public bool CanInteract(GameObject interactor) => !_opened;
    public void Interact(GameObject interactor)
    {
        if (_opened) return;
        _opened = true;
        AudioManager.Instance?.PlaySfx(SfxCue.Chest);
        StartCoroutine(OpenRoutine());
    }

    public Vector3 GetInteractionPosition() => transform.position;

    private void SpawnDrops()
    {
        var drops = new List<GameObject>();
        var parent = FloorManager.RuntimeRoot;

        var guaranteed = (guaranteedItems != null && guaranteedItems.Length > 0) ? guaranteedItems : LootConfig?.guaranteedItems;
        if (guaranteed != null)
            foreach (var item in guaranteed)
                if (item != null)
                    drops.Add(Instantiate(item, transform.position, Quaternion.identity, parent));

        if (Random.value <= itemDropChance)
        {
            var item = PickItem();
            if (item != null)
                drops.Add(Instantiate(item, transform.position, Quaternion.identity, parent));
        }

        drops.AddRange(GenerateCoins());

        if (drops.Count == 0) return;

        float startAngle = Random.Range(0f, 360f / drops.Count);
        for (int i = 0; i < drops.Count; i++)
        {
            float angle = startAngle + i * (360f / drops.Count);
            float rad = angle * Mathf.Deg2Rad;
            var target = transform.position + new Vector3(
                Mathf.Cos(rad) * dropRadius,
                Mathf.Sin(rad) * dropRadius, 0f);

            var drop = drops[i].GetComponent<ItemDrop>() ?? drops[i].AddComponent<ItemDrop>();
            drop.Throw(target, dropDuration);
        }
    }

    private static ChestLootConfig _lootConfig;
    private static ChestLootConfig LootConfig
    {
        get
        {
            if (_lootConfig == null)
                _lootConfig = Resources.Load<ChestLootConfig>("ChestLootConfig");
            return _lootConfig;
        }
    }

    private GameObject PickItem()
    {
        var pool = (itemPool != null && itemPool.Length > 0) ? itemPool : LootConfig?.itemPool;
        if (pool == null || pool.Length == 0) return null;
        var inventory = GameManager.Instance?.PlayerInventory;
        var available = new List<GameObject>();
        foreach (var prefab in pool)
        {
            if (prefab == null) continue;
            if (inventory != null && inventory.Contains(prefab.name)) continue;
            available.Add(prefab);
        }
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    private List<GameObject> GenerateCoins()
    {
        var result = new List<GameObject>();
        if (coinPrefab == null) return result;
        var parent = FloorManager.RuntimeRoot;
        int count = Random.Range(minCoins, maxCoins + 1);
        for (int i = 0; i < count; i++)
            result.Add(Instantiate(coinPrefab, transform.position, Quaternion.identity, parent));
        return result;
    }

    private IEnumerator OpenRoutine()
    {
        if (openFrames != null && openFrames.Length > 0 && _renderer != null)
        {
            var interval = 1f / Mathf.Max(openFps, 1f);
            for (var i = 0; i < openFrames.Length; i++)
            {
                var frame = openFrames[i];
                _renderer.sprite = frame;
                var scale = _renderer.transform.lossyScale.y;
                var pos = _renderer.transform.position;
                pos.y = _baseBottomY - frame.bounds.min.y * scale;
                _renderer.transform.position = pos;
                yield return new WaitForSeconds(interval);
            }
        }

        if (_collider != null)
            _collider.enabled = false;

        yield return new WaitForSeconds(destroyDelay);

        SpawnDrops();

        var shadow = transform.Find("Shadow");
        if (shadow != null)
            Destroy(shadow.gameObject);

        if (disappearFrames != null && disappearFrames.Length > 0 && _renderer != null)
        {
            _renderer.color = disappearColor;
            var interval = 1f / Mathf.Max(openFps, 1f);
            foreach (var frame in disappearFrames)
            {
                _renderer.sprite = frame;
                yield return new WaitForSeconds(interval);
            }
        }

        Destroy(gameObject);
    }
}
