using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Collider2D wallCollider;
    [SerializeField] private Collider2D passTrigger;

    [HideInInspector] public Direction direction;
    [HideInInspector] public int targetNodeId = -1;

    public void SetLocked(bool locked)
    {
        wallCollider.enabled = locked;
        passTrigger.enabled = !locked;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        FloorManager.Instance.TransitionToRoom(targetNodeId, direction);
    }
}
