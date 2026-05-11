using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Merchant Config", fileName = "MerchantConfig")]
public class MerchantConfig : ScriptableObject
{
    public GameObject[] mainItemPool;
    public GameObject[] additionalItemPool;
}
