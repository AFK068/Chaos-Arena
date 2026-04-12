using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    private int _coins;

    public int Coins => _coins;

    public void AddCoins(int amount)
    {
        _coins += Mathf.Max(amount, 0);
    }
}
