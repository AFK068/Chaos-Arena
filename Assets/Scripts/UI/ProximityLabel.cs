using UnityEngine;

public class ProximityLabel : MonoBehaviour
{
    [SerializeField] private string labelText = "Item";
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.6f, 0f);

    public string LabelText => labelText;
    public Vector3 Offset => offset;

    private void Awake()
    {
        if (LabelUI.Instance == null)
        {
            var go = new GameObject("LabelUI");
            go.AddComponent<LabelUI>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerWallet>(out _)) return;
        LabelUI.Instance.Register(this, other.transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerWallet>(out _)) return;
        LabelUI.Instance.Unregister(this);
    }
}
