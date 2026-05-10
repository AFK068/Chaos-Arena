using UnityEngine;

public class BossChestDrop : MonoBehaviour
{
    private static GameObject[] _chestPrefabs;

    private static GameObject[] ChestPrefabs
    {
        get
        {
            if (_chestPrefabs == null)
                _chestPrefabs = Resources.LoadAll<GameObject>("Chests");
            return _chestPrefabs;
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (ChestPrefabs == null || ChestPrefabs.Length == 0) return;
        var prefab = ChestPrefabs[Random.Range(0, ChestPrefabs.Length)];
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
