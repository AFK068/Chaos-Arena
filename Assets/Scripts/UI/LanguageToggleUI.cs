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

        LocalizationService.Instance.SetManualLanguage(
            LocalizationLanguagePolicy.NextManualLanguage(LocalizationService.Instance.CurrentLanguage));
    }

    public void UseRussian() => LocalizationService.Instance?.UseRussian();
    public void UseEnglish() => LocalizationService.Instance?.UseEnglish();
    public void UseTurkish() => LocalizationService.Instance?.UseTurkish();
    public void UsePlatformLanguage() => LocalizationService.Instance?.UsePlatformLanguage();
    public void ResetToAuto() => LocalizationService.Instance?.ResetToAuto();

    private void OnLanguageChanged(string _) => RefreshLabel();

    private void RefreshLabel()
    {
        if (label == null)
            return;

        label.text = LocalizationService.Instance?.CurrentLanguage switch
        {
            LocalizationLanguagePolicy.Russian => "RU",
            LocalizationLanguagePolicy.Turkish => "TR",
            _ => "EN"
        };
    }
}
