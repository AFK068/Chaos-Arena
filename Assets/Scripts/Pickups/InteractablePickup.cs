using System.Collections;
using UnityEngine;

public class InteractablePickup : MonoBehaviour, IInteractable
{
    [SerializeField] private float attractSpeed = 6f;
    [SerializeField] private float destroyDistance = 0.2f;
    [SerializeField] private float playerShakeDuration = 0.3f;
    [SerializeField] private float playerShakeMagnitude = 0.08f;
    [SerializeField] private int price = 0;

    public void SetPrice(int p) => price = p;

    public event System.Action OnPurchased;

    private bool _interacted;

    public bool CanInteract(GameObject interactor)
    {
        if (_interacted) return false;
        if (price > 0)
        {
            var wallet = interactor.GetComponent<PlayerWallet>();
            return wallet != null && wallet.Coins >= price;
        }
        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (_interacted) return;
        if (price > 0)
        {
            var wallet = interactor.GetComponent<PlayerWallet>();
            if (wallet == null || !wallet.TrySpend(price)) return;
        }
        _interacted = true;
        OnPurchased?.Invoke();

        var floatScript = GetComponent<PickupFloat>();
        if (floatScript != null) floatScript.enabled = false;

        var rareEffect = GetComponent<RareItemEffect>();
        if (rareEffect != null) rareEffect.enabled = false;

        StartCoroutine(FlyAndPickup(interactor));
    }

    public Vector3 GetInteractionPosition() => transform.position;

    private IEnumerator FlyAndPickup(GameObject player)
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();

        while (true)
        {
            var dist = Vector2.Distance(transform.position, player.transform.position);
            var speed = attractSpeed + (1f / Mathf.Max(dist, 0.1f)) * 2f;
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);

            if (dist < 1f)
            {
                var alpha = dist;
                foreach (var sr in renderers)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }

            if (dist <= destroyDistance)
            {
                foreach (var effect in GetComponents<IPickupEffect>())
                    effect.OnPickup(player);

                var animator = player.GetComponentInChildren<Animator>();
                var sprite = animator != null
                    ? animator.GetComponent<SpriteRenderer>()
                    : player.GetComponentInChildren<SpriteRenderer>();
                if (sprite != null)
                    player.GetComponent<MonoBehaviour>()?.StartCoroutine(ShakePlayer(sprite));

                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator ShakePlayer(SpriteRenderer sprite)
    {
        var rb = sprite.GetComponent<Rigidbody2D>();
        float elapsed = 0f;

        while (elapsed < playerShakeDuration)
        {
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            float x = Random.Range(-1f, 1f) * playerShakeMagnitude;
            float y = Random.Range(-1f, 1f) * playerShakeMagnitude;
            var base2d = rb != null ? rb.position : (Vector2)sprite.transform.position;
            sprite.transform.position = new Vector3(base2d.x + x, base2d.y + y, sprite.transform.position.z);
        }
    }
}
