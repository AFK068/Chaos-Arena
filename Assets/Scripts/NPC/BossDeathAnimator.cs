using System.Collections;
using UnityEngine;

public class BossDeathAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private float lingerDuration = 0.5f;

    private bool _dead;

    public bool TryPlayDeathAnimation()
    {
        if (_dead) return false;
        _dead = true;

        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour == this) continue;
            behaviour.StopAllCoroutines();
            behaviour.enabled = false;
        }

        foreach (var animator in GetComponentsInChildren<Animator>())
            animator.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(DeathRoutine());
        return true;
    }

    private IEnumerator DeathRoutine()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null && deathFrames != null && deathFrames.Length > 0)
        {
            float interval = 1f / Mathf.Max(fps, 0.01f);
            foreach (var frame in deathFrames)
            {
                sr.sprite = frame;
                yield return new WaitForSeconds(interval);
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, lingerDuration));
        Destroy(gameObject);
    }
}
