using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CacodaemonBoss : MonoBehaviour, IDamageable
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] tiredFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float fps = 8f;
    [SerializeField] private float idleFps = 8f;
    [SerializeField] private float runFps = 8f;
    [SerializeField] private float tiredFps = 8f;
    [SerializeField] private float deathFps = 8f;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float followStartDistance = 0.6f;
    [SerializeField] private float followStopDistance = 0.25f;
    [SerializeField] private bool spriteLooksRightAtPositiveScaleX = true;

    [Header("Phases")]
    [SerializeField] private int enrageDamageThreshold = 10;
    [SerializeField] private float enrageDuration = 10f;
    [SerializeField] private float tiredDuration = 8f;
    [SerializeField] private float deathLingerDuration = 5f;

    [Header("Traps")]
    [SerializeField] private GameObject[] trapPrefabs;
    [SerializeField] private float trapSpawnInterval = 1f;
    [SerializeField] private float trapSpawnRadius = 1.2f;
    [SerializeField] private float spawnedTrapArmDelay = 0.3f;

    [Header("Summons")]
    [SerializeField] private GameObject[] commonMobPrefabs;
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float summonRadius = 2.5f;
    [SerializeField] private float summonInterval = 3f;
    [SerializeField] private float tiredSummonInterval = 1f;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 0.8f;

    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private EnemyAI _enemyAI;
    private AIPath _aiPath;
    private AIAnimationController _aiAnimationController;
    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private float _currentFps;
    private float _baseScaleXAbs;
    private bool _isPositiveScaleX;

    private Transform _player;
    private Vector3 _origin;
    private int _totalDamage;
    private bool _dead;
    private bool _isFollowingPlayer;
    private float _nextContactDamageTime;
    private readonly List<GameObject> _activeTraps = new();

    private enum Phase { Wander, Enrage, Tired }
    private Phase _phase = Phase.Wander;
    private Coroutine _mainRoutine;
    private Coroutine _summonRoutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _enemyAI = GetComponent<EnemyAI>();
        _aiPath = GetComponent<AIPath>();
        _aiAnimationController = GetComponent<AIAnimationController>();

        if (_enemyAI != null) _enemyAI.enabled = false;
        if (_aiAnimationController != null) _aiAnimationController.enabled = false;
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.mass = Mathf.Max(_rb.mass, 1000f);
            _rb.linearDamping = Mathf.Max(_rb.linearDamping, 5f);
        }

        _baseScaleXAbs = Mathf.Abs(transform.localScale.x);
        if (_baseScaleXAbs < 0.0001f) _baseScaleXAbs = 1f;
        _isPositiveScaleX = transform.localScale.x >= 0f;
    }

    private void Start()
    {
        _origin = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_aiPath != null)
        {
            _aiPath.enabled = true;
            _aiPath.canMove = true;
            _aiPath.constrainInsideGraph = true;
            _aiPath.maxSpeed = wanderSpeed;
            _aiPath.destination = transform.position;
        }
        SetFrames(idleFrames);
        _mainRoutine = StartCoroutine(WanderRoutine());
        _summonRoutine = StartCoroutine(SummonRoutine());
    }

    private void Update()
    {
        if (_currentFrames == null || _currentFrames.Length == 0 || _currentFps <= 0f) return;
        _frameTimer += Time.deltaTime;

        if (_frameTimer >= 1f / _currentFps)
        {
            _frameTimer -= 1f / _currentFps;
            _frameIndex = (_frameIndex + 1) % _currentFrames.Length;
            if (_sr != null) _sr.sprite = _currentFrames[_frameIndex];
        }
    }

    public void ApplyHit(HitData hitData)
    {
        if (_dead) return;
        _totalDamage += hitData.Damage;

        if (_phase == Phase.Wander && _totalDamage >= enrageDamageThreshold)
            TriggerEnrage();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealContactDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealContactDamage(other);
    }

    public bool TryPlayDeathAnimation()
    {
        if (_dead) return false;
        StartCoroutine(DieRoutine());
        return true;
    }

    private void TriggerEnrage()
    {
        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        ClearActiveTraps();
        StopVelocity();
        _phase = Phase.Enrage;
        _mainRoutine = StartCoroutine(EnrageRoutine());
    }

    private IEnumerator WanderRoutine()
    {
        _phase = Phase.Wander;
        SetFrames(idleFrames);

        while (true)
        {
            if (_player == null)
            {
                StopVelocity();
                yield return null;
                continue;
            }

            var toPlayer = (Vector2)_player.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;
            float startDistance = Mathf.Max(followStartDistance, followStopDistance);
            float stopDistance = Mathf.Max(0f, followStopDistance);

            if (_isFollowingPlayer)
            {
                if (distance <= stopDistance)
                {
                    _isFollowingPlayer = false;
                    StopVelocity();
                }
                else
                {
                    MoveTowardsTarget(_player.position, wanderSpeed);
                }
            }
            else
            {
                if (distance >= startDistance)
                {
                    _isFollowingPlayer = true;
                    MoveTowardsTarget(_player.position, wanderSpeed);
                }
                else
                {
                    StopVelocity();
                }
            }
            yield return null;
        }
    }

    private IEnumerator EnrageRoutine()
    {
        SetFrames(HasFrames(runFrames) ? runFrames : idleFrames);
        float elapsed = 0f;

        while (elapsed < enrageDuration)
        {
            if (_player != null)
            {
                MoveTowardsTarget(_player.position, chaseSpeed);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopVelocity();
        _mainRoutine = StartCoroutine(TiredRoutine());
    }

    private IEnumerator TiredRoutine()
    {
        _phase = Phase.Tired;
        SetFrames(HasFrames(tiredFrames) ? tiredFrames : idleFrames);
        _totalDamage = 0;
        StopVelocity();

        float elapsed = 0f;
        float trapTimer = 0f;

        while (elapsed < tiredDuration)
        {
            trapTimer += Time.deltaTime;
            CleanupDestroyedTraps();
            if (trapTimer >= trapSpawnInterval &&
                trapPrefabs != null &&
                trapPrefabs.Length > 0 &&
                _player != null)
            {
                trapTimer = 0f;
                SpawnTrap();
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        ClearActiveTraps();
        _mainRoutine = StartCoroutine(WanderRoutine());
    }

    private IEnumerator SummonRoutine()
    {
        while (!_dead)
        {
            float interval = _phase == Phase.Tired ? tiredSummonInterval : summonInterval;
            yield return new WaitForSeconds(Mathf.Max(0.1f, interval));
            if (_dead) yield break;
            SpawnSummonedMob(transform.position);
        }
    }

    private void SpawnTrap()
    {
        if (_player == null || trapPrefabs == null || trapPrefabs.Length == 0) return;
        CleanupDestroyedTraps();
        var prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
        if (prefab == null) return;
        var offset = Random.insideUnitCircle * trapSpawnRadius;
        var pos = _player.position + new Vector3(offset.x, offset.y, 0f);
        var trap = Instantiate(prefab, pos, Quaternion.identity);
        if (trap == null) return;

        if (trap.TryGetComponent<HazardZone>(out var hazardZone))
            hazardZone.ArmAfterDelay(spawnedTrapArmDelay);

        _activeTraps.Add(trap);
    }

    private void CleanupDestroyedTraps()
    {
        for (int i = _activeTraps.Count - 1; i >= 0; i--)
        {
            if (_activeTraps[i] == null)
                _activeTraps.RemoveAt(i);
        }
    }

    private void ClearActiveTraps()
    {
        for (int i = _activeTraps.Count - 1; i >= 0; i--)
        {
            var trap = _activeTraps[i];
            if (trap != null)
                Destroy(trap);
        }
        _activeTraps.Clear();
    }

    private void SpawnSummonedMob(Vector3 center)
    {
        if (commonMobPrefabs == null || commonMobPrefabs.Length == 0)
            return;

        if (!TryGetRandomGraphPoint(center, out var spawnPos))
            return;

        if (spawnEffectPrefab != null)
        {
            var effect = Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity);
            if (effect != null && !effect.TryGetComponent<DestroyAfterSpriteSheetAnimation>(out _))
                effect.AddComponent<DestroyAfterSpriteSheetAnimation>();
        }

        var prefab = commonMobPrefabs[Random.Range(0, commonMobPrefabs.Length)];
        if (prefab != null)
            Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private bool TryGetRandomGraphPoint(Vector3 center, out Vector3 spawnPos)
    {
        spawnPos = center;
        if (AstarPath.active == null)
            return false;

        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0.1f, summonRadius);
            Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);
            var nearest = AstarPath.active.GetNearest(candidate).node;
            if (nearest != null && nearest.Walkable)
            {
                spawnPos = (Vector3)nearest.position;
                return true;
            }
        }

        var fallback = AstarPath.active.GetNearest(center).node;
        if (fallback != null && fallback.Walkable)
        {
            spawnPos = (Vector3)fallback.position;
            return true;
        }

        return false;
    }

    private IEnumerator DieRoutine()
    {
        _dead = true;
        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        if (_summonRoutine != null) StopCoroutine(_summonRoutine);
        ClearActiveTraps();
        StopVelocity();
        if (_aiPath != null) _aiPath.enabled = false;
        if (_rb != null) _rb.simulated = false;
        var colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
        SetFrames(HasFrames(deathFrames) ? deathFrames : idleFrames);

        float deathAnimFps = Mathf.Max(ResolveFpsForFrames(HasFrames(deathFrames) ? deathFrames : idleFrames), 0.01f);
        float duration = HasFrames(deathFrames) ? deathFrames.Length / deathAnimFps : 1f;
        yield return new WaitForSeconds(duration);
        if (HasFrames(deathFrames) && _sr != null)
            _sr.sprite = deathFrames[deathFrames.Length - 1];
        _currentFrames = null;
        _currentFps = 0f;
        yield return new WaitForSeconds(Mathf.Max(0f, deathLingerDuration));
        Destroy(gameObject);
    }

    private void SetFrames(Sprite[] frames)
    {
        _currentFps = ResolveFpsForFrames(frames);
        if (_currentFrames == frames) return;
        _currentFrames = frames;
        _frameIndex = 0;
        _frameTimer = 0f;
        if (_sr != null && frames != null && frames.Length > 0)
            _sr.sprite = frames[0];
    }

    private float ResolveFpsForFrames(Sprite[] frames)
    {
        float stateFps = fps;
        if (ReferenceEquals(frames, idleFrames)) stateFps = idleFps;
        else if (ReferenceEquals(frames, runFrames)) stateFps = runFps;
        else if (ReferenceEquals(frames, tiredFrames)) stateFps = tiredFps;
        else if (ReferenceEquals(frames, deathFrames)) stateFps = deathFps;

        if (stateFps > 0f) return stateFps;
        return fps > 0f ? fps : 0f;
    }

    private void Flip(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        bool movingRight = dirX > 0f;
        bool targetPositiveScale = movingRight == spriteLooksRightAtPositiveScaleX;
        if (_isPositiveScaleX == targetPositiveScale) return;

        _isPositiveScaleX = targetPositiveScale;
        var s = transform.localScale;
        s.x = targetPositiveScale ? _baseScaleXAbs : -_baseScaleXAbs;
        transform.localScale = s;
    }

    private void SetVelocity(Vector2 v)
    {
        if (_rb != null)
            _rb.linearVelocity = v;
        else
            transform.position += (Vector3)(v * Time.deltaTime);
    }

    private void StopVelocity()
    {
        if (_aiPath != null && _aiPath.enabled)
        {
            _aiPath.destination = transform.position;
            _aiPath.canMove = false;
        }

        if (_rb != null) _rb.linearVelocity = Vector2.zero;
    }

    private void MoveTowardsTarget(Vector3 target, float speed)
    {
        var toTarget = (Vector2)target - (Vector2)transform.position;
        Flip(toTarget.x);

        if (_aiPath != null && _aiPath.enabled)
        {
            _aiPath.canMove = true;
            _aiPath.maxSpeed = Mathf.Max(0f, speed);
            _aiPath.destination = target;
            return;
        }

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            StopVelocity();
            return;
        }

        SetVelocity(toTarget.normalized * speed);
    }

    private void TryDealContactDamage(Collider2D other)
    {
        if (_dead || other == null || Time.time < _nextContactDamageTime)
            return;

        if (!other.TryGetComponent<PlayerHealth>(out var playerHealth))
            return;

        playerHealth.TakeDamage(contactDamage);
        _nextContactDamageTime = Time.time + Mathf.Max(0.01f, contactDamageCooldown);
    }

    private static bool HasFrames(Sprite[] arr) => arr != null && arr.Length > 0;

#if UNITY_EDITOR
    private const string CommonPrefabsFolder = "Assets/Prefabs/NPC/Common";
    private const string SpawnEffectPath = "Assets/Prefabs/Effects/SpawnEffect.prefab";

    private void OnValidate()
    {
        if ((commonMobPrefabs == null || commonMobPrefabs.Length == 0) && !Application.isPlaying)
            commonMobPrefabs = LoadPrefabsFromFolder(CommonPrefabsFolder);

        if (spawnEffectPrefab == null && !Application.isPlaying)
            spawnEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnEffectPath);
    }

    private static GameObject[] LoadPrefabsFromFolder(string folder)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        var prefabs = new List<GameObject>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        return prefabs.ToArray();
    }
#endif
}
