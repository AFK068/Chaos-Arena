using UnityEngine;

public class HealthPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private int healAmount = 1;

    public void OnPickup(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
            health.Heal(healAmount);
    }
}
