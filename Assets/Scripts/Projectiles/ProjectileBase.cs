using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected float speed = 10f;
    [SerializeField] protected float lifeTime = 2f;

    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Launch(Vector2 direction)
    {
        if (rb != null)
            rb.linearVelocity = direction.normalized * speed;
        Destroy(gameObject, lifeTime);
    }
}