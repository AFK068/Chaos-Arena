using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Coin Drop Config", fileName = "CoinDropConfig")]
public class CoinDropConfig : ScriptableObject
{
    [Range(0f, 1f)] public float dropChance = 0.45f;
    public CoinDropEntry[] coinPool = new CoinDropEntry[]
    {
        new CoinDropEntry { weight = 0.50f },
        new CoinDropEntry { weight = 0.25f },
        new CoinDropEntry { weight = 0.14f },
        new CoinDropEntry { weight = 0.07f },
        new CoinDropEntry { weight = 0.04f },
    };
}
