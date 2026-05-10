using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    private static GameObject _prefab;
    private static bool _loaded;

    private static GameObject Prefab
    {
        get
        {
            if (!_loaded)
            {
                _prefab = Resources.Load<GameObject>("Effects/DeathEffect");
                _loaded = true;
            }
            return _prefab;
        }
    }

    public void Play(Vector3 position)
    {
        if (Prefab == null) return;
        Instantiate(Prefab, position, Quaternion.identity);
    }
}
