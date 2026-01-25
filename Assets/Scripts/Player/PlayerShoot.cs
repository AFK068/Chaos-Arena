using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private Transform shootPoint;
    [SerializeField] private ProjectileBase projectilePrefab;
    [SerializeField] private float fireRate = 0.3f;

    public ProjectileBase ProjectilePrefab
    {
        get => projectilePrefab;
        set => projectilePrefab = value;
    }

    private Vector2 shootDir = Vector2.zero;
    private float fireTimer = 0f;

    public void OnShoot(InputAction.CallbackContext context)
    {
        shootDir = context.ReadValue<Vector2>();
    }

    void Update()
    {
        fireTimer += Time.deltaTime;
        if (shootDir != Vector2.zero && fireTimer >= fireRate)
        {
            Shoot(shootDir);
            fireTimer = 0f;
        }
    }

    private void Shoot(Vector2 direction)
    {
        ProjectileBase projectile = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        projectile.Launch(direction);
    }
}