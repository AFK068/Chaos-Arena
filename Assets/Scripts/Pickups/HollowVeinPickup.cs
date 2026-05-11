using UnityEngine;

public class HollowVeinPickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<HollowVeinBuff>() != null) return;
        var buff = player.AddComponent<HollowVeinBuff>();
        buff.Apply();
    }
}

public class HollowVeinBuff : MonoBehaviour
{
    public void Apply()
    {
        var health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            int newMax = Mathf.Max(2, Mathf.Min(health.MaxHealth, 4));
            int delta = newMax - health.MaxHealth;
            if (delta != 0) health.AddMaxHealth(delta);
        }

        GetComponent<PlayerMovement>()?.ModifyMoveSpeed(1.2f);
    }
}
