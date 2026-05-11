using UnityEngine;

public class SwiftLegsPickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<SwiftLegsBuff>() != null) return;
        player.AddComponent<SwiftLegsBuff>();
        player.GetComponent<PlayerMovement>()?.ModifyMoveSpeed(1.1f);
    }
}

public class SwiftLegsBuff : MonoBehaviour { }
