using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Collider2D wallCollider;
    [SerializeField] private Collider2D passTrigger;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    [HideInInspector] public Direction direction;
    [HideInInspector] public int targetNodeId = -1;

    private void Awake()
    {
        SetLocked(true);
    }

    public void SetLocked(bool locked)
    {
        wallCollider.enabled = locked;
        passTrigger.enabled = !locked;

        if (spriteRenderer != null)
            spriteRenderer.sprite = locked ? closedSprite : openSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        FloorManager.Instance.TransitionToRoom(targetNodeId, direction);
    }
}
