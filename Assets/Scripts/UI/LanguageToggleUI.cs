using ChaosArena.Platform;
using TMPro;
using UnityEngine;

public sealed class LanguageToggleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;

        RefreshLabel();
    }

    private void OnDisable()
    {
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    public void ToggleLanguage()
    {
        if (LocalizationService.Instance == null)
            return;

        if (LocalizationService.Instance.CurrentLanguage == LocalizationLanguagePolicy.Russian)
            LocalizationService.Instance.UseEnglish();
        else
            LocalizationService.Instance.UseRussian();
    }

    public void UseRussian() => LocalizationService.Instance?.UseRussian();
    public void UseEnglish() => LocalizationService.Instance?.UseEnglish();
    public void UsePlatformLanguage() => LocalizationService.Instance?.UsePlatformLanguage();
    public void ResetToAuto() => LocalizationService.Instance?.ResetToAuto();

    private void OnLanguageChanged(string _) => RefreshLabel();

    private void RefreshLabel()
    {
        if (label == null)
            return;

        label.text = LocalizationService.Instance?.CurrentLanguage == LocalizationLanguagePolicy.Russian
            ? "RU"
            : "EN";
    }
}
