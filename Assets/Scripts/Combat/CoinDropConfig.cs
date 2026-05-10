using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Coin Drop Config", fileName = "CoinDropConfig")]
public class CoinDropConfig : ScriptableObject
{
    public CoinDropEntry[] coinPool = new CoinDropEntry[]
    {
        new CoinDropEntry { weight = 0.50f },
        new CoinDropEntry { weight = 0.25f },
        new CoinDropEntry { weight = 0.14f },
        new CoinDropEntry { weight = 0.07f },
        new CoinDropEntry { weight = 0.04f },
    };
}
