using UnityEngine;

public class RagePickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float duration = 8f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float fireRateMultiplier = 2f;
    [SerializeField] private float dashCooldownMultiplier = 2f;
    [SerializeField] private GameObject effectPrefab;

    public void OnPickup(GameObject player)
    {
        player.GetComponent<PlayerMovement>()?.ApplyRageBuff(speedMultiplier, dashCooldownMultiplier, duration);
        player.GetComponent<PlayerShoot>()?.ApplyFireRateBuff(fireRateMultiplier, duration);
        RageEffect.Spawn(effectPrefab, player.transform, duration);
    }
}
