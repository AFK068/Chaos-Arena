using ChaosArena.Platform;
using UnityEngine;

/// <summary>
/// Runtime-only touch aim source. It does not own projectile firing: it merely
/// supplies a direction to PlayerShoot while landscape touch gameplay is live.
/// </summary>
public sealed class MobileAutoAimController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float targetRange = 12f;
    [SerializeField, Range(0.10f, 0.15f)] private float scanInterval = 0.12f;
    [SerializeField, Range(0.5f, 0.99f)] private float switchDistanceRatio = 0.85f;

    private PlayerShoot _shoot;
    private Camera _camera;
    private EnemyHealth _target;
    private float _nextScanTime;
    private int _wallsMask;
    private bool _gameplayActive;

    private void Awake()
    {
        _shoot = GetComponent<PlayerShoot>();
        _wallsMask = LayerMask.GetMask("Walls");
    }

    private void OnDisable() => Clear();

    public void SetGameplayActive(bool active)
    {
        _gameplayActive = active;
        if (!active)
            Clear();
        else
            _nextScanTime = 0f;
    }

    private void Update()
    {
        if (!_gameplayActive)
            return;

        if (!IsEligible(_target))
            Clear();

        if (Time.unscaledTime < _nextScanTime)
            return;

        _nextScanTime = Time.unscaledTime + scanInterval;
        _target = FindBestTarget();
        ApplyTargetDirection();
    }

    private EnemyHealth FindBestTarget()
    {
        var camera = GetCamera();
        if (camera == null)
            return null;

        var playerPosition = transform.position;
        var rangeSquared = targetRange * targetRange;
        var currentDistanceSquared = IsEligible(_target)
            ? ((Vector2)(_target.transform.position - playerPosition)).sqrMagnitude
            : -1f;
        EnemyHealth best = _target;
        var bestDistanceSquared = currentDistanceSquared;

        foreach (var candidate in EnemyHealth.ActiveEnemies)
        {
            if (!IsEligible(candidate) || !IsInViewport(candidate.transform.position, camera))
                continue;

            var offset = (Vector2)(candidate.transform.position - playerPosition);
            var candidateDistanceSquared = offset.sqrMagnitude;
            if (candidateDistanceSquared > rangeSquared || IsWallBlocking(playerPosition, candidate.transform.position))
                continue;

            if (best == null || MobileControlMath.ShouldSwitchAutoAimTarget(bestDistanceSquared, candidateDistanceSquared,
                    switchDistanceRatio))
            {
                best = candidate;
                bestDistanceSquared = candidateDistanceSquared;
            }
        }

        return best;
    }

    private bool IsEligible(EnemyHealth candidate)
    {
        if (candidate == null || !candidate.isActiveAndEnabled || !candidate.IsAlive)
            return false;

        var camera = GetCamera();
        if (camera == null || !IsInViewport(candidate.transform.position, camera))
            return false;

        var fromPlayer = (Vector2)(candidate.transform.position - transform.position);
        return fromPlayer.sqrMagnitude <= targetRange * targetRange && !IsWallBlocking(transform.position, candidate.transform.position);
    }

    private Camera GetCamera()
    {
        if (_camera == null || !_camera.isActiveAndEnabled)
            _camera = Camera.main;
        return _camera;
    }

    private static bool IsInViewport(Vector3 worldPosition, Camera camera)
    {
        var viewport = camera.WorldToViewportPoint(worldPosition);
        return viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    private bool IsWallBlocking(Vector3 from, Vector3 to)
    {
        return _wallsMask != 0 && Physics2D.Linecast(from, to, _wallsMask).collider != null;
    }

    private void ApplyTargetDirection()
    {
        if (_target == null)
        {
            _shoot?.SetMobileAutoAimDirection(Vector2.zero);
            return;
        }

        var direction = (Vector2)(_target.transform.position - transform.position);
        _shoot?.SetMobileAutoAimDirection(direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero);
    }

    private void Clear()
    {
        _target = null;
        _shoot?.SetMobileAutoAimDirection(Vector2.zero);
    }
}
