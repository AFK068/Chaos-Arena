using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HulkZBoss : MonoBehaviour, IDamageable
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float fps = 8f;
    [SerializeField] private float idleFps = 8f;
    [SerializeField] private float walkFps = 8f;
    [SerializeField] private float attackFps = 10f;
    [SerializeField] private float deathFps = 8f;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float chargeSpeed = 18f;
    [SerializeField] private float chargeWindupDuration = 0.5f;
    [SerializeField] private float chargeDuration = 1.2f;
    [SerializeField] private float chargeMaxDistance = 8f;
    [SerializeField] private float chargeInterval = 5f;
    [SerializeField] private float followStopDistance = 0.3f;
    [SerializeField] private bool spriteLooksRightAtPositiveScaleX = true;

    [Header("Traps")]
    [SerializeField] private GameObject[] trapPrefabs;
    [SerializeField] private GameObject trapWarnEffectPrefab;
    [SerializeField] private float trapSpawnInterval = 3.5f;
    [SerializeField] private float trapSpawnRadius = 1.5f;
    [SerializeField] private float trapWarnDelay = 1f;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 0.8f;

    [Header("Death")]
    [SerializeField] private float deathLingerDuration = 3f;

    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private AIPath _aiPath;
    private EnemyAI _enemyAI;
    private AIAnimationController _aiAnimationController;

    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private float _currentFps;
    private float _baseScaleXAbs;
    private bool _isPositiveScaleX;

    private Transform _player;
    private bool _dead;
    private float _nextContactDamageTime;

    private Coroutine _mainRoutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _aiPath = GetComponent<AIPath>();
        _enemyAI = GetComponent<EnemyAI>();
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
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_aiPath != null)
        {
            _aiPath.enabled = true;
            _aiPath.canMove = false;
            _aiPath.constrainInsideGraph = true;
            _aiPath.maxSpeed = wanderSpeed;
            _aiPath.destination = transform.position;
        }
        SetFrames(idleFrames);
        _mainRoutine = StartCoroutine(WanderRoutine());
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
    }

    public bool TryPlayDeathAnimation()
    {
        if (_dead) return false;
        StartCoroutine(DieRoutine());
        return true;
    }

    private void OnCollisionStay2D(Collision2D collision) => TryDealContactDamage(collision.collider);
    private void OnTriggerStay2D(Collider2D other) => TryDealContactDamage(other);

    private IEnumerator WanderRoutine()
    {
        SetFrames(idleFrames);
        float chargeTimer = chargeInterval;
        float trapTimer = 0f;

        while (true)
        {
            if (_player == null) { yield return null; continue; }

            float dist = Vector2.Distance(transform.position, _player.position);

            if (dist > followStopDistance)
            {
                SetFrames(walkFrames);
                MoveTowardsTarget(_player.position, wanderSpeed);
            }
            else
            {
                SetFrames(idleFrames);
                StopVelocity();
            }

            trapTimer += Time.deltaTime;
            if (trapTimer >= trapSpawnInterval)
            {
                trapTimer = 0f;
                SpawnTrap();
            }

            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f)
            {
                StopVelocity();
                _mainRoutine = StartCoroutine(ChargeRoutine());
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ChargeRoutine()
    {
        // разгон — стоп, анимация атаки
        SetFrames(attackFrames);
        StopVelocity();

        var chargeDir = _player != null
            ? ((Vector2)_player.position - (Vector2)transform.position).normalized
            : (_isPositiveScaleX ? Vector2.right : Vector2.left);

        Flip(chargeDir.x);
        yield return new WaitForSeconds(chargeWindupDuration);

        // выключаем A* только на время заряда и задаём velocity каждый фрейм
        if (_aiPath != null) _aiPath.canMove = false;
        SetFrames(walkFrames);

        var chargeStart = transform.position;
        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            if (Vector2.Distance(transform.position, chargeStart) >= chargeMaxDistance) break;
            if (_rb != null) _rb.linearVelocity = chargeDir * chargeSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        if (_aiPath != null) _aiPath.canMove = true;

        _mainRoutine = StartCoroutine(WanderRoutine());
    }

    private IEnumerator DieRoutine()
    {
        _dead = true;
        if (_mainRoutine != null) StopCoroutine(_mainRoutine);
        StopVelocity();

        var shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);
        if (_aiPath != null) _aiPath.enabled = false;
        if (_rb != null) _rb.simulated = false;
        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;

        SetFrames(deathFrames);
        float duration = HasFrames(deathFrames) ? deathFrames.Length / deathFps : 1f;
        yield return new WaitForSeconds(duration);

        if (HasFrames(deathFrames) && _sr != null)
            _sr.sprite = deathFrames[deathFrames.Length - 1];
        _currentFrames = null;
        _currentFps = 0f;

        yield return new WaitForSeconds(deathLingerDuration);
        Destroy(gameObject);
    }

    private void SpawnTrap()
    {
        if (trapPrefabs == null || trapPrefabs.Length == 0 || _player == null) return;
        var prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
        if (prefab == null) return;
        var offset = Random.insideUnitCircle * trapSpawnRadius;
        var pos = _player.position + new Vector3(offset.x, offset.y);
        StartCoroutine(SpawnTrapWithWarn(prefab, pos));
    }

    private IEnumerator SpawnTrapWithWarn(GameObject prefab, Vector3 pos)
    {
        if (trapWarnEffectPrefab != null)
        {
            var effect = Instantiate(trapWarnEffectPrefab, pos + Vector3.down, Quaternion.identity);
            if (effect != null && !effect.TryGetComponent<DestroyAfterSpriteSheetAnimation>(out _))
                effect.AddComponent<DestroyAfterSpriteSheetAnimation>();
        }
        yield return new WaitForSeconds(trapWarnDelay);
        if (!_dead)
            Instantiate(prefab, pos, Quaternion.identity);
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

        if (toTarget.sqrMagnitude <= 0.0001f) { StopVelocity(); return; }
        if (_rb != null) _rb.linearVelocity = toTarget.normalized * speed;
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

    private void SetFrames(Sprite[] frames)
    {
        _currentFps = ResolveFps(frames);
        if (_currentFrames == frames) return;
        _currentFrames = frames;
        _frameIndex = 0;
        _frameTimer = 0f;
        if (_sr != null && frames != null && frames.Length > 0)
            _sr.sprite = frames[0];
    }

    private float ResolveFps(Sprite[] frames)
    {
        if (ReferenceEquals(frames, idleFrames)) return idleFps > 0 ? idleFps : fps;
        if (ReferenceEquals(frames, walkFrames)) return walkFps > 0 ? walkFps : fps;
        if (ReferenceEquals(frames, attackFrames)) return attackFps > 0 ? attackFps : fps;
        if (ReferenceEquals(frames, deathFrames)) return deathFps > 0 ? deathFps : fps;
        return fps;
    }

    private void Flip(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        bool movingRight = dirX > 0f;
        bool targetPositive = movingRight == spriteLooksRightAtPositiveScaleX;
        if (_isPositiveScaleX == targetPositive) return;
        _isPositiveScaleX = targetPositive;
        var s = transform.localScale;
        s.x = targetPositive ? _baseScaleXAbs : -_baseScaleXAbs;
        transform.localScale = s;
    }

    private void TryDealContactDamage(Collider2D other)
    {
        if (_dead || other == null || Time.time < _nextContactDamageTime) return;
        if (!other.TryGetComponent<PlayerHealth>(out var ph)) return;
        ph.TakeDamage(contactDamage);
        _nextContactDamageTime = Time.time + Mathf.Max(0.01f, contactDamageCooldown);
    }

    private static bool HasFrames(Sprite[] arr) => arr != null && arr.Length > 0;
}
