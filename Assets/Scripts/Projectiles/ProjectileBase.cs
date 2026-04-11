using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected float speed = 10f;
    [SerializeField] protected float lifeTime = 2f;
    [SerializeField] protected int damage = 1;
    [SerializeField] protected DebuffType debuffType = DebuffType.None;
    [SerializeField] protected float debuffDuration = 0f;
    [SerializeField] protected float debuffPower = 0f;
    [SerializeField] private ProjectilePickup pickupPrefab;
    [SerializeField] private bool rotateToDirection = false;
    [SerializeField] private float spriteForwardAngleOffset = 0f;

    protected Rigidbody2D rb;
    private bool _hasHit;

    public ProjectilePickup PickupPrefab => pickupPrefab;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        var hitCollider = GetComponent<CircleCollider2D>();
        if (hitCollider == null)
        {
            hitCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        hitCollider.isTrigger = true;
    }

    public virtual void Launch(Vector2 direction)
    {
        var normalizedDirection = direction.normalized;

        if (rotateToDirection && normalizedDirection.sqrMagnitude > 0.0001f)
        {
            var angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteForwardAngleOffset);
        }

        if (rb != null)
            rb.linearVelocity = normalizedDirection * speed;
        Destroy(gameObject, lifeTime);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyHit(other);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyHit(collision.collider);
    }

    private void TryApplyHit(Collider2D other)
    {
        if (_hasHit || other == null)
        {
            return;
        }

        if (!other.TryGetComponent<IDamageable>(out var damageable))
        {
            return;
        }

        _hasHit = true;
        var hitData = new HitData(damage, debuffType, debuffDuration, debuffPower);
        damageable.ApplyHit(hitData);
        Destroy(gameObject);
    }
}
