using System.Collections;
using UnityEngine;
using Pathfinding;

public class ExperimentZ10Boss : MonoBehaviour, IDamageable
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] moveFrames;
    [SerializeField] private Sprite[] attack1Frames;
    [SerializeField] private Sprite[] attack2Frames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float moveFps = 8f;
    [SerializeField] private float attack1Fps = 10f;
    [SerializeField] private float attack2Fps = 10f;
    [SerializeField] private float deathFps = 8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float meleeRange = 3f;
    [SerializeField] private float meleeSpeed = 6f;
    [SerializeField] private float followStopDistance = 0.3f;
    [SerializeField] private bool spriteLooksRightAtPositiveScaleX = true;

    [Header("Dash Attack")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.8f;
    [SerializeField] private float dashMaxDistance = 7f;
    [SerializeField] private float dashTriggerRange = 6f;
    [SerializeField] private float dashInterval = 6f;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float contactDamageCooldown = 0.8f;
    [SerializeField] private float contactTouchDistance = 0.1f;
    [SerializeField] private float postMeleeHitCooldown = 3f;
    [SerializeField] private float meleeAttemptCooldown = 1.2f;

    [Header("Death")]
    [SerializeField] private float deathLingerDuration = 3f;

    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private AIPath _aiPath;
    private EnemyAI _enemyAI;
    private AIAnimationController _aiAnimationController;
    private Collider2D _selfCollider;

    private Sprite[] _currentFrames;
    private int _frameIndex;
    private float _frameTimer;
    private float _currentFps;
    private float _baseScaleXAbs;
    private bool _isPositiveScaleX;

    private Transform _player;
    private Collider2D _playerCollider;
    private bool _dead;
    private float _nextDamageTime;
    private float _nextMeleeActionTime;
    private float _nextMeleeAttemptTime;
    private bool _attack1Locked;
    private float _attack1LockUntil;
    private Coroutine _mainRoutine;

    private void Awake()
    {
        _sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _aiPath = GetComponent<AIPath>();
        _enemyAI = GetComponent<EnemyAI>();
        _aiAnimationController = GetComponent<AIAnimationController>();
        _selfCollider = GetComponent<Collider2D>();

        if (_enemyAI != null) _enemyAI.enabled = false;
        if (_aiAnimationController != null) _aiAnimationController.enabled = false;
        var animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = false;

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
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
        _playerCollider = _player != null ? _player.GetComponent<Collider2D>() : null;
        Debug.Log($"[ExpZ10] Start — player={_player != null}, rb={_rb != null}, aiPath={_aiPath != null}");

        if (_aiPath != null)
        {
            _aiPath.enabled = true;
            _aiPath.canMove = false;
            _aiPath.constrainInsideGraph = true;
            _aiPath.maxSpeed = moveSpeed;
            _aiPath.destination = transform.position;
        }

        SetFrames(moveFrames);
        _mainRoutine = StartCoroutine(MainRoutine());
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

    private IEnumerator MainRoutine()
    {
        float dashTimer = 0f;
        int _tick = 0;

        while (true)
        {
            if (_player == null) { yield return null; continue; }
            if (_playerCollider == null) _playerCollider = _player.GetComponent<Collider2D>();

            float dist = GetCombatDistanceToPlayer();
            dashTimer -= Time.deltaTime;

            _tick++;
            if (_tick % 60 == 0)
                Debug.Log($"[ExpZ10] tick — pos={transform.position} playerPos={_player.position} dist={dist:F1} canMove={_aiPath?.canMove} dest={_aiPath?.destination}");

            // Attack1 — после старта всегда доигрывается до конца
            if (_attack1Locked)
            {
                ProcessAttack1State(dist);
                if (Time.time >= _attack1LockUntil)
                    _attack1Locked = false;
                yield return null;
                continue;
            }

            // После успешного ближнего удара даём окно, в которое нет ближней атаки и прыжка
            if (Time.time < _nextMeleeActionTime)
            {
                SetFrames(moveFrames);
                MoveTowardsTarget(GetPlayerAimPosition(), moveSpeed);
                yield return null;
                continue;
            }

            // Attack1 — вход в ближнюю зону запускает "замах" фиксированной длины
            if (dist <= meleeRange)
            {
                if (Time.time >= _nextMeleeAttemptTime)
                {
                    StartAttack1Lock();
                    ProcessAttack1State(dist);
                }
                else
                {
                    SetFrames(moveFrames);
                    if (IsPlayerInDamageContact(dist))
                        StopVelocity();
                    else
                        MoveTowardsTarget(GetPlayerAimPosition(), moveSpeed);
                }
                yield return null;
                continue;
            }

            // Attack2 — если игрок чуть дальше ближней зоны, прыжок с перезарядкой
            float validDashRange = Mathf.Max(meleeRange + 0.1f, dashTriggerRange);
            if (dist <= validDashRange && dashTimer <= 0f)
            {
                dashTimer = dashInterval;
                StopVelocity();
                yield return StartCoroutine(DashAttackRoutine());
                continue;
            }

            SetFrames(moveFrames);
            MoveTowardsTarget(GetPlayerAimPosition(), moveSpeed);

            yield return null;
        }
    }

    // Attack2: трансформация (полная анимация), потом рывок — всё до конца, потом цикл
    private IEnumerator DashAttackRoutine()
    {
        SetFrames(attack2Frames);
        if (_aiPath != null) _aiPath.canMove = false;

        float transformDuration = HasFrames(attack2Frames)
            ? attack2Frames.Length / (attack2Fps > 0 ? attack2Fps : 8f)
            : 1f;
        yield return new WaitForSeconds(transformDuration);

        var dashDir = _player != null
            ? ((Vector2)_player.position - (Vector2)transform.position).normalized
            : (_isPositiveScaleX ? Vector2.right : Vector2.left);
        Flip(dashDir.x);

        var dashStart = transform.position;
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            if (Vector2.Distance(transform.position, dashStart) >= dashMaxDistance) break;
            if (_rb != null) _rb.linearVelocity = dashDir * dashSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        if (_aiPath != null) _aiPath.canMove = true;
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
        float duration = HasFrames(deathFrames) ? deathFrames.Length / (deathFps > 0 ? deathFps : 8f) : 1f;
        yield return new WaitForSeconds(duration);

        if (HasFrames(deathFrames) && _sr != null)
            _sr.sprite = deathFrames[deathFrames.Length - 1];
        _currentFrames = null;
        _currentFps = 0f;

        yield return new WaitForSeconds(deathLingerDuration);
        Destroy(gameObject);
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
        if (ReferenceEquals(frames, moveFrames)) return moveFps;
        if (ReferenceEquals(frames, attack1Frames)) return attack1Fps;
        if (ReferenceEquals(frames, attack2Frames)) return attack2Fps;
        if (ReferenceEquals(frames, deathFrames)) return deathFps;
        return 8f;
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
        if (_dead || other == null || Time.time < _nextDamageTime || Time.time < _nextMeleeActionTime) return;
        if (!other.TryGetComponent<PlayerHealth>(out var ph)) return;
        ph.TakeDamage(contactDamage);
        _nextDamageTime = Time.time + Mathf.Max(0.01f, contactDamageCooldown);
        _nextMeleeActionTime = Time.time + Mathf.Max(0f, postMeleeHitCooldown);
    }

    private void StartAttack1Lock()
    {
        _attack1Locked = true;
        _attack1LockUntil = Time.time + GetAttack1Duration();
        _nextMeleeAttemptTime = Time.time + Mathf.Max(0f, meleeAttemptCooldown);
    }

    private void ProcessAttack1State(float distToPlayer)
    {
        SetFrames(attack1Frames);
        TryDealTouchDamageInAttack1(distToPlayer);

        bool inDamageContact = IsPlayerInDamageContact(distToPlayer);
        if (!inDamageContact)
            MoveTowardsTarget(GetPlayerAimPosition(), meleeSpeed);
        else
            StopVelocity();
    }

    private float GetAttack1Duration()
    {
        if (HasFrames(attack1Frames))
            return attack1Frames.Length / (attack1Fps > 0f ? attack1Fps : 8f);
        return 0.2f;
    }

    private float GetCombatDistanceToPlayer()
    {
        if (_player == null) return float.PositiveInfinity;
        if (_selfCollider != null && _playerCollider != null)
        {
            Vector2 bossPoint = _selfCollider.ClosestPoint(_playerCollider.bounds.center);
            Vector2 playerPoint = _playerCollider.ClosestPoint(_selfCollider.bounds.center);
            return Vector2.Distance(bossPoint, playerPoint);
        }

        return Vector2.Distance(transform.position, _player.position);
    }

    private Vector3 GetPlayerAimPosition()
    {
        if (_playerCollider != null)
            return _playerCollider.bounds.center;
        return _player != null ? _player.position : transform.position;
    }

    private void TryDealTouchDamageInAttack1(float distToPlayer)
    {
        if (_dead || Time.time < _nextDamageTime || Time.time < _nextMeleeActionTime) return;
        if (!IsPlayerInDamageContact(distToPlayer)) return;
        if (_player == null || !_player.TryGetComponent<PlayerHealth>(out var ph)) return;

        ph.TakeDamage(contactDamage);
        _nextDamageTime = Time.time + Mathf.Max(0.01f, contactDamageCooldown);
        _nextMeleeActionTime = Time.time + Mathf.Max(0f, postMeleeHitCooldown);
    }

    private bool IsPlayerInDamageContact(float distToPlayer)
    {
        if (_selfCollider != null && _playerCollider != null && _selfCollider.IsTouching(_playerCollider))
            return true;

        return distToPlayer <= Mathf.Max(0f, contactTouchDistance);
    }

    private static bool HasFrames(Sprite[] arr) => arr != null && arr.Length > 0;
}
