using UnityEngine;

public class DeathMaskPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float rageDuration = 3f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float fireRateMultiplier = 2f;
    [SerializeField] private float dashCooldownMultiplier = 2f;
    [SerializeField][Range(0f, 1f)] private float triggerChance = 0.2f;
    [SerializeField] private GameObject effectPrefab;

    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<DeathMaskBuff>() != null) return;

        var buff = player.AddComponent<DeathMaskBuff>();
        buff.Init(rageDuration, speedMultiplier, fireRateMultiplier, dashCooldownMultiplier, triggerChance, effectPrefab);
    }
}
