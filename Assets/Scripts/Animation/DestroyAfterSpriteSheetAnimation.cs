using System.Collections;
using UnityEngine;

public class DestroyAfterSpriteSheetAnimation : MonoBehaviour
{
    [SerializeField] private float fallbackLifetime = 1f;
    [SerializeField] private float extraDelay = 0f;

    private Coroutine _routine;

    private void OnEnable()
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine()
    {
        float lifetime = Mathf.Max(0f, fallbackLifetime);
        if (TryGetComponent<SpriteSheetAnimator>(out var animator) && animator.AnimationDuration > 0f)
            lifetime = animator.AnimationDuration;

        lifetime += Mathf.Max(0f, extraDelay);
        if (lifetime > 0f)
            yield return new WaitForSeconds(lifetime);

        Destroy(gameObject);
    }
}
