using UnityEngine;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TMP_Text coinText;

    private int _lastCoins = -1;

    private void Update()
    {
        if (wallet == null) return;
        if (wallet.Coins == _lastCoins) return;

        _lastCoins = wallet.Coins;
        coinText.text = _lastCoins.ToString();
    }
}
