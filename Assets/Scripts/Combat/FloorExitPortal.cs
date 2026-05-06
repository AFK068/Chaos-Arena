using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FloorExitPortal : MonoBehaviour
{
    private bool _used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (!other.CompareTag("Player")) return;

        _used = true;
        FloorManager.Instance.GoToNextFloor();
    }
}
