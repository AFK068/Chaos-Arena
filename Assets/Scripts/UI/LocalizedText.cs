using ChaosArena.Platform;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private TMP_Text targetText;

    public string Key => key;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    public void Refresh()
    {
        if (targetText != null && !string.IsNullOrWhiteSpace(key))
            targetText.text = LocalizationService.GetText(key);
    }

    private void OnLanguageChanged(string _) => Refresh();
}
