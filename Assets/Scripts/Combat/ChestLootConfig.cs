using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Chest Loot Config", fileName = "ChestLootConfig")]
public class ChestLootConfig : ScriptableObject
{
    public GameObject[] guaranteedItems;
    public GameObject[] itemPool;
}
