using System;
using ChaosArena.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class MobileTouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform _baseTransform;
    private RectTransform _knobTransform;

    public event Action<Vector2> ValueChanged;

    public void Initialize(RectTransform knobTransform)
    {
        _baseTransform = (RectTransform)transform;
        _knobTransform = knobTransform;
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateValue(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateValue(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_knobTransform != null)
            _knobTransform.anchoredPosition = Vector2.zero;
        ValueChanged?.Invoke(Vector2.zero);
    }

    private void UpdateValue(PointerEventData eventData)
    {
        if (_baseTransform == null || _knobTransform == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _baseTransform, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        var radius = Mathf.Max(1f, Mathf.Min(_baseTransform.rect.width, _baseTransform.rect.height) * 0.5f);
        var value = MobileControlMath.NormalizeStick(localPoint.x / radius, localPoint.y / radius);
        var direction = new Vector2(value.X, value.Y);
        _knobTransform.anchoredPosition = direction * (radius * 0.55f);
        ValueChanged?.Invoke(direction);
    }
}
