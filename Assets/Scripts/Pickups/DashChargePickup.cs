using UnityEngine;
using ChaosArena.Platform;

public class DashChargePickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        var movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
            return;

        if (movement.RestoreDashCharge())
            PickupNotificationUI.Show(LocalizationCatalog.PickupDashChargeNotice);
    }
}
