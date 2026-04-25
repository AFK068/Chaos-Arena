using UnityEngine;

public class ScrollPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float spreadAngle = 20f;

    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<ScrollBuff>() != null) return;
        var buff = player.AddComponent<ScrollBuff>();
        buff.Init(spreadAngle);
    }
}

public class ScrollBuff : MonoBehaviour
{
    private float _spreadAngle;
    private PlayerShoot _shoot;
    private bool _spawning;

    public void Init(float spreadAngle) => _spreadAngle = spreadAngle;

    private void Awake()
    {
        _shoot = GetComponent<PlayerShoot>();
        _shoot.OnProjectileFired += OnProjectileFired;
        _shoot.ModifyFireRate(2f);
    }

    private void OnDestroy()
    {
        if (_shoot != null) _shoot.OnProjectileFired -= OnProjectileFired;
    }

    private void OnProjectileFired(ProjectileBase projectile)
    {
        if (_spawning) return;
        _spawning = true;

        var rb = projectile.GetComponent<Rigidbody2D>();
        var dir = rb != null ? rb.linearVelocity.normalized : Vector2.right;
        var pos = projectile.transform.position;

        Spawn(pos, Quaternion.Euler(0f, 0f, _spreadAngle) * dir);
        Spawn(pos, Quaternion.Euler(0f, 0f, -_spreadAngle) * dir);

        _spawning = false;
    }

    private void Spawn(Vector3 pos, Vector2 dir)
    {
        var proj = Instantiate(_shoot.ProjectilePrefab, pos, Quaternion.identity);
        proj.ApplyDamageMultiplier(_shoot.ProjectileDamageMultiplier);
        proj.Launch(dir);
    }
}
