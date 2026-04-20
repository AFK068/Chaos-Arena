using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    private int _coins;

    public int Coins => _coins;

    public void AddCoins(int amount)
    {
        _coins += Mathf.Max(amount, 0);
    }

    public bool TrySpend(int amount)
    {
        if (_coins < amount) return false;
        _coins -= amount;
        return true;
    }
}
