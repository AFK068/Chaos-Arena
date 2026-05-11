using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FloorExitPortal : MonoBehaviour, IInteractable
{
    private bool _used;

    public bool CanInteract(GameObject interactor) => !_used;

    public void Interact(GameObject interactor)
    {
        if (_used) return;
        _used = true;

        if (FloorTransitionFX.Instance == null)
            new GameObject("FloorTransitionFX").AddComponent<FloorTransitionFX>();

        FloorTransitionFX.Instance.Play(
            FloorManager.Instance.CurrentFloor + 1,
            FloorManager.Instance.GoToNextFloor);
    }

    public Vector3 GetInteractionPosition() => transform.position;
}
