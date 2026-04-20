using UnityEngine;

public class DodgePickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float dodgeAmount = 0.15f;

    public void OnPickup(GameObject player)
    {
        player.GetComponent<PlayerHealth>()?.AddDodgeChance(dodgeAmount);
    }
}
