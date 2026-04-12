using System.Collections;
using UnityEngine;

public class HazardZone : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 0.8f;

    private Coroutine _damageCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out var playerHealth))
            return;

        _damageCoroutine = StartCoroutine(DamageRoutine(playerHealth));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out _))
            return;

        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }
    }

    private IEnumerator DamageRoutine(PlayerHealth playerHealth)
    {
        while (true)
        {
            playerHealth.TakeDamage(damage);
            yield return new WaitForSeconds(damageCooldown);
        }
    }
}
