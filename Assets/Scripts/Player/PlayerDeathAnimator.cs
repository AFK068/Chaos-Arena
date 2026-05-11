using System.Collections;
using UnityEngine;

public class PlayerDeathAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float fps = 3f;

    private void Awake()
    {
        GetComponent<PlayerHealth>().OnPlayerDied += Play;
    }

    private void Play()
    {
        var anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null || deathFrames == null || deathFrames.Length == 0) yield break;

        float interval = 1f / Mathf.Max(fps, 0.01f);
        foreach (var frame in deathFrames)
        {
            sr.sprite = frame;
            yield return new WaitForSeconds(interval);
        }
    }
}
