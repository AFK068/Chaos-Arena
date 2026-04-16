using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PickupAttract : MonoBehaviour
{
    [SerializeField] private float attractSpeed = 5f;
    [SerializeField] private float fadeStartDistance = 0.8f;
    [SerializeField] private float destroyDistance = 0.2f;
    [SerializeField] private UnityEvent onPickedUp;

    private Transform _target;
    private SpriteRenderer[] _renderers;
    private bool _collected;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;
        if (!other.TryGetComponent<PlayerWallet>(out _)) return;

        _collected = true;
        _target = other.transform;

        var floatScript = GetComponent<PickupFloat>();
        if (floatScript != null) floatScript.enabled = false;

        StartCoroutine(AttractRoutine());
    }

    private IEnumerator AttractRoutine()
    {
        yield return null;
        yield return null;

        while (_target != null)
        {
            var dist = Vector2.Distance(transform.position, _target.position);
            var speed = attractSpeed + (1f / Mathf.Max(dist, 0.1f)) * 2f;
            transform.position = Vector2.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);

            if (dist < fadeStartDistance)
            {
                var alpha = dist / fadeStartDistance;
                foreach (var sr in _renderers)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }

            if (dist <= destroyDistance)
            {
                onPickedUp.Invoke();
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }
}
