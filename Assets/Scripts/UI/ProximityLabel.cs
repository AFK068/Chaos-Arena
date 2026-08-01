using UnityEngine;
using ChaosArena.Platform;

public class ProximityLabel : MonoBehaviour
{
    [Tooltip("Stable localization key. The serialized English labelText remains the fallback for old/missing content.")]
    [SerializeField] private string contentKey;
    [SerializeField] private string labelText = "Item";
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.6f, 0f);

    public string ContentKey => contentKey;
    public string LabelText =>
        string.IsNullOrWhiteSpace(contentKey) || !LocalizationCatalog.HasKey(contentKey)
            ? labelText
            : LocalizationService.GetText(contentKey);
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
