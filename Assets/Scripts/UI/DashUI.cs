using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [SerializeField] private Sprite fullChargeSprite;
    [SerializeField] private Sprite emptyChargeSprite;
    [SerializeField] private Transform chargesContainer;

    private PlayerMovement _playerMovement;
    private Image[] _chargeImages;

    private void Start()
    {
        _playerMovement = FindFirstObjectByType<PlayerMovement>();
        _chargeImages = new Image[0];
    }

    private void Update()
    {
        var cooldowns = _playerMovement.ChargesCooldownNormalized;

        if (cooldowns.Length != _chargeImages.Length)
            RebuildImages(cooldowns.Length);

        // Левый пустой = активно восстанавливается = мерцает
        int blinkIndex = -1;
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] < 1f) { blinkIndex = i; break; }
        }

        int count = Mathf.Min(_chargeImages.Length, cooldowns.Length);
        for (int i = 0; i < count; i++)
        {
            bool ready = cooldowns[i] >= 1f;
            _chargeImages[i].sprite = ready ? fullChargeSprite : emptyChargeSprite;

            float alpha;
            if (ready) alpha = 1f;
            else if (i == blinkIndex) alpha = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
            else alpha = 1f;

            var c = _chargeImages[i].color;
            c.a = alpha;
            _chargeImages[i].color = c;
        }
    }

    private void RebuildImages(int count)
    {
        foreach (var img in _chargeImages)
            Destroy(img.gameObject);

        _chargeImages = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("DashCharge_" + i, typeof(Image));
            go.transform.SetParent(chargesContainer, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(24f, 24f);
            _chargeImages[i] = go.GetComponent<Image>();
            _chargeImages[i].sprite = fullChargeSprite;
        }
    }
}
