using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantController : MonoBehaviour
{
    [Header("Main Item Pool (overrides config)")]
    [SerializeField] private List<GameObject> mainItemPool = new();

    [Header("Additional Item Pool (overrides config)")]
    [SerializeField] private List<GameObject> additionalItemPool = new();

    private static MerchantConfig _activeConfig;
    private static MerchantConfig ActiveConfig
    {
        get
        {
            if (_activeConfig == null)
                _activeConfig = Resources.Load<MerchantConfig>("MerchantConfig");
            return _activeConfig;
        }
    }

    [Header("Prices")]
    [SerializeField] private int additionalItemPrice = 4;

    [Header("Shop")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinsPerPurchase = 5;
    [SerializeField] private float columnSpacing = 2.0f;
    [SerializeField] private Vector2 gridOffset = new Vector2(0f, -1.5f);

    [Header("Sprites")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] showFrames;
    [SerializeField] private float fps = 8f;

    [Header("Death")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private float skeletonSpawnDelay = 0.5f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float waitMinTime = 1f;
    [SerializeField] private float waitMaxTime = 3f;
    [SerializeField] private float stopPlayerRadius = 3f;

    private Vector3 _origin;
    private Transform _player;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private bool _facingRight = true;
    private bool _isDead;
    private readonly List<GameObject> _spawnedItems = new();
    private Room _room;

    private void Start()
    {
        _origin = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        SpawnItems();
        StartCoroutine(WanderRoutine());

        _room = GetComponentInParent<Room>();
        if (_room != null) _room.OnLeft += HandleRoomLeft;
    }

    private void OnDestroy()
    {
        if (_room != null) _room.OnLeft -= HandleRoomLeft;
    }

    private void HandleRoomLeft()
    {
        if (_isDead) return;
        Die();
    }

    private void Die()
    {
        _isDead = true;

        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();

        StartCoroutine(SpawnSkeletonRoutine());
    }

    private IEnumerator SpawnSkeletonRoutine()
    {
        yield return new WaitForSeconds(skeletonSpawnDelay);

        if (skeletonPrefab != null)
            Instantiate(skeletonPrefab, transform.position, Quaternion.identity, FloorManager.RuntimeRoot);

        Destroy(gameObject);
    }

    private void Update()
    {
        if (_currentFrames == null || _currentFrames.Length == 0) return;
        _frameTimer += Time.deltaTime;
        if (_frameTimer >= 1f / fps)
        {
            _frameTimer = 0f;
            _frameIndex = (_frameIndex + 1) % _currentFrames.Length;
            if (_sr != null) _sr.sprite = _currentFrames[_frameIndex];
        }
    }

    private void SpawnItems()
    {
        int expensivePrice = Random.Range(25, 51);
        int cheapPrice = Random.Range(25, 51);
        int additionalPrice = Random.Range(10, 16);

        // Пул без уже купленных
        var inventory = GameManager.Instance?.PlayerInventory;
        var pool = (mainItemPool != null && mainItemPool.Count > 0)
            ? mainItemPool
            : (ActiveConfig != null && ActiveConfig.mainItemPool != null ? new List<GameObject>(ActiveConfig.mainItemPool) : new List<GameObject>());
        var available = new List<GameObject>();
        foreach (var prefab in pool)
        {
            if (prefab == null) continue;
            if (inventory != null && inventory.Contains(prefab.name)) continue;
            available.Add(prefab);
        }
        Shuffle(available);

        var spawnList = new List<(GameObject prefab, int price)>();
        if (available.Count > 0) spawnList.Add((available[0], expensivePrice));
        if (available.Count > 1) spawnList.Add((available[1], cheapPrice));

        // Дополнительный предмет — случайный из пула
        var additionalPrefab = PickAdditional();
        if (additionalPrefab != null) spawnList.Add((additionalPrefab, additionalPrice));

        // Размещение в ряд по центру
        int count = spawnList.Count;
        float totalWidth = (count - 1) * columnSpacing;
        for (int i = 0; i < count; i++)
        {
            var (prefab, price) = spawnList[i];
            var worldPos = transform.position + new Vector3(
                -totalWidth / 2f + i * columnSpacing + gridOffset.x,
                gridOffset.y,
                0f);

            var go = Instantiate(prefab, worldPos, Quaternion.identity, FloorManager.RuntimeRoot);
            _spawnedItems.Add(go);
            var capturedName = prefab.name;
            SetupShopItem(go, price, capturedName);

            var tag = go.AddComponent<ShopPriceTag>();
            tag.Init(price);
        }
    }

    private GameObject PickAdditional()
    {
        var pool = (additionalItemPool != null && additionalItemPool.Count > 0)
            ? additionalItemPool
            : (ActiveConfig != null && ActiveConfig.additionalItemPool != null ? new List<GameObject>(ActiveConfig.additionalItemPool) : null);
        if (pool == null || pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private void SetupShopItem(GameObject go, int price, string prefabName)
    {
        var attract = go.GetComponent<PickupAttract>();
        if (attract != null) Destroy(attract);

        void MarkPurchased()
        {
            GameManager.Instance?.PlayerInventory.Add(prefabName);
        }

        if (go.TryGetComponent<ProjectilePickup>(out var pp))
        {
            pp.SetPrice(price);
            pp.OnPurchased += MarkPurchased;
            if (price > 0 && coinPrefab != null)
            {
                var ce = go.AddComponent<CoinPayEffect>();
                ce.Init(transform, coinPrefab, coinsPerPurchase);
                pp.OnPurchased += ce.Play;
            }
            return;
        }

        if (!go.TryGetComponent<InteractablePickup>(out var ip))
            ip = go.AddComponent<InteractablePickup>();

        ip.SetPrice(price);
        ip.OnPurchased += MarkPurchased;
        if (price > 0 && coinPrefab != null)
        {
            var ce = go.AddComponent<CoinPayEffect>();
            ce.Init(transform, coinPrefab, coinsPerPurchase);
            ip.OnPurchased += ce.Play;
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (PlayerNearby())
            {
                StopMoving();
                SetFrames(showFrames);
                yield return null;
                continue;
            }

            var target = _origin + (Vector3)(Random.insideUnitCircle * wanderRadius);

            SetFrames(walkFrames);
            float elapsed = 0f;
            while (!PlayerNearby() && Vector2.Distance(transform.position, target) > 0.1f)
            {
                elapsed += Time.deltaTime;
                if (elapsed > 2f) break;

                var dir = ((Vector2)target - (Vector2)transform.position).normalized;
                Flip(dir.x);
                if (_rb != null)
                    _rb.linearVelocity = dir * moveSpeed;
                else
                    transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            StopMoving();
            SetFrames(idleFrames);
            yield return new WaitForSeconds(Random.Range(waitMinTime, waitMaxTime));
        }
    }

    private void StopMoving()
    {
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }

    private void Flip(float dirX)
    {
        if (dirX > 0 && !_facingRight || dirX < 0 && _facingRight)
        {
            _facingRight = !_facingRight;
            var s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    private void SetFrames(Sprite[] frames)
    {
        if (_currentFrames == frames) return;
        _currentFrames = frames;
        _frameIndex = 0;
        _frameTimer = 0f;
    }

    private bool PlayerNearby() =>
        _player != null && Vector2.Distance(transform.position, _player.position) < stopPlayerRadius;
}
