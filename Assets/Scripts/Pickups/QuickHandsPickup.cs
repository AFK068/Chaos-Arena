using UnityEngine;

public class QuickHandsPickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<QuickHandsBuff>() != null) return;
        player.AddComponent<QuickHandsBuff>();
        player.GetComponent<PlayerShoot>()?.ModifyFireRate(1f / 1.1f);
    }
}

public class QuickHandsBuff : MonoBehaviour { }
