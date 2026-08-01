using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private LayerMask interactLayerMask = ~0;
    [SerializeField] private GameObject interactPrompt;

    private void Update()
    {
        if (interactPrompt == null) return;
        bool shouldShow = FindBestInteractable() != null;
        if (interactPrompt.activeSelf != shouldShow)
            interactPrompt.SetActive(shouldShow);

        if (shouldShow)
        {
            var s = interactPrompt.transform.localScale;
            float desiredSign = Mathf.Sign(transform.localScale.x);
            if (Mathf.Sign(s.x) != desiredSign)
            {
                s.x = -s.x;
                interactPrompt.transform.localScale = s;
            }
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        TryInteract();
    }

    public void TryInteract()
    {

        var target = FindBestInteractable();
        if (target != null)
        {
            target.Interact(gameObject);
            return;
        }

        var nearest = FindNearestInteractable();
        if (nearest is MonoBehaviour mb)
            mb.GetComponent<ShopPriceTag>()?.OnCannotAfford();
    }

    private IInteractable FindBestInteractable()
    {
        var hits = interactLayerMask.value == 0
            ? Physics2D.OverlapCircleAll(transform.position, interactRadius)
            : Physics2D.OverlapCircleAll(transform.position, interactRadius, interactLayerMask);

        IInteractable best = null;
        var bestDistanceSqr = float.MaxValue;

        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            var behaviours = hit.GetComponents<MonoBehaviour>();
            for (var j = 0; j < behaviours.Length; j++)
            {
                if (behaviours[j] is not IInteractable candidate)
                {
                    continue;
                }

                if (!candidate.CanInteract(gameObject))
                {
                    continue;
                }

                var distanceSqr = (candidate.GetInteractionPosition() - transform.position).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    best = candidate;
                }
            }
        }

        return best;
    }

    private IInteractable FindNearestInteractable()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactLayerMask);
        IInteractable best = null;
        var bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            foreach (var mb in hit.GetComponents<MonoBehaviour>())
            {
                if (mb is not IInteractable candidate) continue;
                var dist = (candidate.GetInteractionPosition() - transform.position).sqrMagnitude;
                if (dist < bestDist) { bestDist = dist; best = candidate; }
            }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
