using System.Collections;
using UnityEngine;

public class GlassesPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float seekAngle = 40f;
    [SerializeField] private float maxRange = 7f;

    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<GlassesBuff>() != null) return;
        var buff = player.AddComponent<GlassesBuff>();
        buff.Init(turnSpeed, seekAngle, maxRange);
    }
}

public class GlassesBuff : MonoBehaviour
{
    private float _turnSpeed;
    private float _seekAngle;
    private float _maxRange;
    private PlayerShoot _shoot;

    public void Init(float turnSpeed, float seekAngle, float maxRange)
    {
        _turnSpeed = turnSpeed;
        _seekAngle = seekAngle;
        _maxRange = maxRange;
    }

    private void Awake()
    {
        _shoot = GetComponent<PlayerShoot>();
        _shoot.OnProjectileFired += OnProjectileFired;
    }

    private void OnDestroy()
    {
        if (_shoot != null) _shoot.OnProjectileFired -= OnProjectileFired;
    }

    private void OnProjectileFired(ProjectileBase projectile)
    {
        var homing = projectile.gameObject.AddComponent<HomingProjectile>();
        homing.Init(_turnSpeed, _seekAngle, _maxRange);
    }
}

public class HomingProjectile : MonoBehaviour
{
    private float _turnSpeed;
    private float _seekAngle;
    private float _maxRange;

    private Rigidbody2D _rb;
    private Transform _target;
    private float _refreshTimer;

    public void Init(float turnSpeed, float seekAngle, float maxRange)
    {
        _turnSpeed = turnSpeed;
        _seekAngle = seekAngle;
        _maxRange = maxRange;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        _refreshTimer -= Time.fixedDeltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 0.2f;
            _target = FindNearest();
        }

        if (_target == null || !_target.gameObject.activeInHierarchy) return;

        var currentDir = _rb.linearVelocity.normalized;
        var wantDir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
        float speed = _rb.linearVelocity.magnitude;

        float angle = Vector2.SignedAngle(currentDir, wantDir);
        float rot = Mathf.Clamp(angle, -_turnSpeed * Time.fixedDeltaTime, _turnSpeed * Time.fixedDeltaTime);
        var newDir = (Vector2)(Quaternion.Euler(0f, 0f, rot) * currentDir);
        _rb.linearVelocity = newDir * speed;

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg);
    }

    private Transform FindNearest()
    {
        var travelDir = _rb.linearVelocity.normalized;
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            var toEnemy = (Vector2)e.transform.position - (Vector2)transform.position;
            float dist = toEnemy.magnitude;
            if (dist > _maxRange) continue;
            if (Vector2.Angle(travelDir, toEnemy) > _seekAngle) continue;
            if (dist < minDist) { minDist = dist; nearest = e.transform; }
        }
        return nearest;
    }
}
