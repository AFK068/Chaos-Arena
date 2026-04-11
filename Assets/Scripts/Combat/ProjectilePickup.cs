using UnityEngine;

public class ProjectilePickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ProjectileBase projectilePrefab;
    [SerializeField] private SpriteRenderer pickupRenderer;
    [SerializeField] private float pickupCooldown = 0f;

    private float _spawnTime;

    public ProjectileBase ProjectilePrefab => projectilePrefab;

    public void Configure(ProjectileBase newProjectilePrefab)
    {
        projectilePrefab = newProjectilePrefab;
        RefreshVisual();
    }

    public void SetPickupCooldown(float cooldown)
    {
        pickupCooldown = Mathf.Max(cooldown, 0f);
        _spawnTime = Time.time;
    }

    public bool CanInteract(GameObject interactor)
    {
        if (projectilePrefab == null || interactor == null)
        {
            return false;
        }

        if (Time.time < _spawnTime + pickupCooldown)
        {
            return false;
        }

        if (!interactor.TryGetComponent<PlayerProjectileLoadout>(out var loadout))
        {
            return false;
        }

        return loadout.CanSwapTo(projectilePrefab);
    }

    public void Interact(GameObject interactor)
    {
        if (!interactor.TryGetComponent<PlayerProjectileLoadout>(out var loadout))
        {
            return;
        }

        loadout.SwapProjectile(projectilePrefab);
        Destroy(gameObject);
    }

    public Vector3 GetInteractionPosition()
    {
        return transform.position;
    }

    private void Awake()
    {
        _spawnTime = Time.time;

        if (pickupRenderer == null)
        {
            pickupRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (pickupRenderer == null || projectilePrefab == null)
        {
            return;
        }

        var projectileSprite = projectilePrefab.GetComponentInChildren<SpriteRenderer>();
        if (projectileSprite != null)
        {
            pickupRenderer.sprite = projectileSprite.sprite;
            pickupRenderer.color = Color.white;
        }
    }
}
