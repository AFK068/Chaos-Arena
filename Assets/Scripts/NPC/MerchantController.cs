using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MerchantSlot
{
    public GameObject itemPrefab;
    public int price;
}

public class MerchantController : MonoBehaviour
{
    [Header("Shop")]
    [SerializeField] private List<MerchantSlot> slots = new();
    [SerializeField] private int itemsPerRow = 3;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinsPerPurchase = 5;
    [SerializeField] private float columnSpacing = 2.0f;
    [SerializeField] private float rowSpacing = 2.0f;
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
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.itemPrefab == null) continue;

            int row = i / itemsPerRow;
            int col = i % itemsPerRow;
            int itemsInThisRow = Mathf.Min(itemsPerRow, slots.Count - row * itemsPerRow);
            float rowWidth = (itemsInThisRow - 1) * columnSpacing;

            var worldPos = transform.position + new Vector3(
                col * columnSpacing - rowWidth / 2f + gridOffset.x,
                -row * rowSpacing + gridOffset.y,
                0f);

            var go = Instantiate(slot.itemPrefab, worldPos, Quaternion.identity);

            SetupShopItem(go, slot.price);

            var tag = go.AddComponent<ShopPriceTag>();
            tag.Init(slot.price);
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

    private void SetupShopItem(GameObject go, int itemPrice)
    {
        // Убираем авто-подбор — в магазине только по F
        var attract = go.GetComponent<PickupAttract>();
        if (attract != null) Destroy(attract);

        if (go.TryGetComponent<ProjectilePickup>(out var pp))
        {
            pp.SetPrice(itemPrice);
            if (itemPrice > 0 && coinPrefab != null)
            {
                var ce = go.AddComponent<CoinPayEffect>();
                ce.Init(transform, coinPrefab, coinsPerPurchase);
                pp.OnPurchased += ce.Play;
            }
            return;
        }

        if (!go.TryGetComponent<InteractablePickup>(out var ip))
            ip = go.AddComponent<InteractablePickup>();

        ip.SetPrice(itemPrice);
        if (itemPrice > 0 && coinPrefab != null)
        {
            var ce = go.AddComponent<CoinPayEffect>();
            ce.Init(transform, coinPrefab, coinsPerPurchase);
            ip.OnPurchased += ce.Play;
        }
    }

    private bool PlayerNearby() =>
        _player != null && Vector2.Distance(transform.position, _player.position) < stopPlayerRadius;
}
