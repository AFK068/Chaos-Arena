using UnityEngine;

public class PlayerProjectileLoadout : MonoBehaviour
{
    [SerializeField] private PlayerShoot playerShoot;
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, -0.35f);
    [SerializeField] private float droppedPickupCooldown = 0.5f;

    private void Awake()
    {
        if (playerShoot == null)
        {
            playerShoot = GetComponent<PlayerShoot>();
        }
    }

    public bool CanSwapTo(ProjectileBase projectilePrefab)
    {
        return playerShoot != null && projectilePrefab != null && playerShoot.ProjectilePrefab != projectilePrefab;
    }

    public void SwapProjectile(ProjectileBase newProjectile)
    {
        if (!CanSwapTo(newProjectile))
        {
            return;
        }

        var oldProjectile = playerShoot.ProjectilePrefab;
        playerShoot.ProjectilePrefab = newProjectile;
        DropProjectilePickup(oldProjectile);
    }

    private void DropProjectilePickup(ProjectileBase projectileToDrop)
    {
        if (projectileToDrop == null)
        {
            return;
        }

        var pickupPrefab = projectileToDrop.PickupPrefab;
        if (pickupPrefab == null)
        {
            return;
        }

        var droppedPickup = Instantiate(pickupPrefab, (Vector2)transform.position + dropOffset, Quaternion.identity);
        droppedPickup.Configure(projectileToDrop);
        droppedPickup.SetPickupCooldown(droppedPickupCooldown);
    }
}
