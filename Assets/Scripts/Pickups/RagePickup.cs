using UnityEngine;
using ChaosArena.Platform;

public class RagePickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float duration = 8f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float fireRateMultiplier = 2f;
    [SerializeField] private float dashCooldownMultiplier = 2f;
    [SerializeField] private GameObject effectPrefab;

    public void OnPickup(GameObject player)
    {
        var applied = false;

        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.ApplyRageBuff(speedMultiplier, dashCooldownMultiplier, duration);
            applied = true;
        }

        var shooting = player.GetComponent<PlayerShoot>();
        if (shooting != null)
        {
            shooting.ApplyFireRateBuff(fireRateMultiplier, duration);
            applied = true;
        }

        RageEffect.Spawn(effectPrefab, player.transform, duration);
        if (applied)
            PickupNotificationUI.Show(LocalizationCatalog.PickupRageNotice);
    }
}
