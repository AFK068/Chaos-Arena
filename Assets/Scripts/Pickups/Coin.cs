using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float attractSpeed = 5f;
    [SerializeField] private float fadeStartDistance = 0.8f;
    [SerializeField] private float destroyDistance = 0.2f;

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
        StartCoroutine(AttractRoutine(other.GetComponent<PlayerWallet>()));
    }

    private IEnumerator AttractRoutine(PlayerWallet wallet)
    {
        while (_target != null)
        {
            var dist = Vector2.Distance(transform.position, _target.position);

            if (dist <= destroyDistance)
            {
                wallet.AddCoins(value);
                Destroy(gameObject);
                yield break;
            }

            var speed = attractSpeed + (1f / Mathf.Max(dist, 0.1f)) * 2f;
            transform.position = Vector2.MoveTowards(transform.position, _target.position, speed * Time.deltaTime);

            if (dist < fadeStartDistance)
            {
                var alpha = dist / fadeStartDistance;
                foreach (var sr in _renderers)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }

            yield return null;
        }
    }
}
