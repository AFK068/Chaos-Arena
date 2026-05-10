using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantController : MonoBehaviour
{
    [Header("Main Item Pool")]
    [SerializeField] private List<GameObject> mainItemPool = new();

    [Header("Additional Item Pool")]
    [SerializeField] private List<GameObject> additionalItemPool = new();

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

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float waitMinTime = 1f;
    [SerializeField] private float waitMaxTime = 3f;
    [SerializeField] private float stopPlayerRadius = 3f;

    private Vector3 _origin;
    private Transform _player;
    private SpriteRenderer _sr;
    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private bool _facingRight = true;

    private void Start()
    {
        _origin = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _sr = GetComponent<SpriteRenderer>();
        SpawnItems();
        StartCoroutine(WanderRoutine());
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
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        var wallet = playerGo?.GetComponent<PlayerWallet>();

        int coins = wallet != null ? wallet.Coins : 10;

        // Цены: дорогой 65%, дешёвый 50%, дополнительный 35%
        int expensivePrice = Mathf.Max(5, Mathf.CeilToInt(coins * 0.65f));
        int cheapPrice     = Mathf.Max(3, Mathf.CeilToInt(coins * 0.50f));
        int additionalPrice = Mathf.Max(2, Mathf.CeilToInt(coins * 0.35f));

        // Пул без уже купленных
        var inventory = GameManager.Instance?.PlayerInventory;
        var available = new List<GameObject>();
        foreach (var prefab in mainItemPool)
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

            var go = Instantiate(prefab, worldPos, Quaternion.identity);
            var capturedName = prefab.name;
            SetupShopItem(go, price, capturedName);

            var tag = go.AddComponent<ShopPriceTag>();
            tag.Init(price);
        }
    }

    private GameObject PickAdditional()
    {
        if (additionalItemPool == null || additionalItemPool.Count == 0) return null;
        return additionalItemPool[Random.Range(0, additionalItemPool.Count)];
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
                SetFrames(showFrames);
                yield return null;
                continue;
            }

            var target = _origin + (Vector3)(Random.insideUnitCircle * wanderRadius);

            SetFrames(walkFrames);
            while (!PlayerNearby() && Vector2.Distance(transform.position, target) > 0.1f)
            {
                var dir = ((Vector2)target - (Vector2)transform.position).normalized;
                Flip(dir.x);
                transform.position = Vector2.MoveTowards(
                    transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            SetFrames(idleFrames);
            yield return new WaitForSeconds(Random.Range(waitMinTime, waitMaxTime));
        }
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
