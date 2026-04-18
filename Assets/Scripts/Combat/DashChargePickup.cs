using UnityEngine;

public class DashChargePickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        var movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.RestoreDashCharge();
    }
}
