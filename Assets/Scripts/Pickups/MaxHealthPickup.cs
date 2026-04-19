using UnityEngine;

public class MaxHealthPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private int amount = 2;

    public void OnPickup(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
            health.AddMaxHealth(amount);
    }
}
