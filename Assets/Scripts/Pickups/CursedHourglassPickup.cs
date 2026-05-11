using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursedHourglassPickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<CursedHourglassBuff>() != null) return;
        player.AddComponent<CursedHourglassBuff>();
    }
}

public class CursedHourglassBuff : MonoBehaviour
{
    private const float SlowRadius = 10f;
    private const float SlowMultiplier = 0.3f;
    private const float SlowDuration = 1.5f;

    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _health.OnDamageTaken += OnDamage;
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDamageTaken -= OnDamage;
    }

    private void OnDamage() => StartCoroutine(SlowField());

    private IEnumerator SlowField()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, SlowRadius);
        var enemies = new List<EnemyAI>();
        foreach (var col in cols)
        {
            if (col == null) continue;
            var ai = col.GetComponent<EnemyAI>();
            if (ai != null && !enemies.Contains(ai))
            {
                enemies.Add(ai);
                ai.SetSpeedMultiplier(SlowMultiplier);
            }
        }

        yield return new WaitForSeconds(SlowDuration);

        foreach (var ai in enemies)
        {
            if (ai != null) ai.SetSpeedMultiplier(1f);
        }
    }
}
