using System.Collections;
using UnityEngine;

public class BossChestDrop : MonoBehaviour
{
    private static GameObject[] _chestPrefabs;
    private static GameObject _spawnEffect;
    private static bool _effectLoaded;

    private static GameObject[] ChestPrefabs
    {
        get
        {
            if (_chestPrefabs == null)
                _chestPrefabs = Resources.LoadAll<GameObject>("Chests");
            return _chestPrefabs;
        }
    }

    private static GameObject SpawnEffect
    {
        get
        {
            if (!_effectLoaded)
            {
                _spawnEffect = Resources.Load<GameObject>("Effects/SpawnEffect");
                _effectLoaded = true;
            }
            return _spawnEffect;
        }
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (ChestPrefabs == null || ChestPrefabs.Length == 0) return;

        var chestPrefab = ChestPrefabs[Random.Range(0, ChestPrefabs.Length)];
        var pos = transform.position;

        var helper = new GameObject("ChestSpawnHelper");
        helper.AddComponent<ChestSpawnHelper>().Init(pos, chestPrefab, SpawnEffect);
    }
}

public class ChestSpawnHelper : MonoBehaviour
{
    public void Init(Vector3 pos, GameObject chestPrefab, GameObject effectPrefab)
    {
        StartCoroutine(Routine(pos, chestPrefab, effectPrefab));
    }

    private IEnumerator Routine(Vector3 pos, GameObject chestPrefab, GameObject effectPrefab)
    {
        float delay = 0f;

        if (effectPrefab != null)
        {
            var effect = Instantiate(effectPrefab, pos, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 2f;

            var anim = effect.GetComponent<SpriteSheetAnimator>();
            if (anim != null) delay = anim.AnimationDuration;
            else delay = 0.5f;

            if (!effect.TryGetComponent<DestroyAfterSpriteSheetAnimation>(out _))
                effect.AddComponent<DestroyAfterSpriteSheetAnimation>();
        }

        yield return new WaitForSeconds(delay);

        Instantiate(chestPrefab, pos, Quaternion.identity);
        Destroy(gameObject);
    }
}
