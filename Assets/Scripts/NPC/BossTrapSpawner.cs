using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTrapSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] trapPrefabs;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnRadius = 0.8f;
    [SerializeField] private float armDelay = 0.3f;
    [SerializeField] private float warnDelay = 0.8f;

    private static GameObject _spawnEffect;
    private static bool _effectLoaded;

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

    private Transform _player;
    private readonly List<GameObject> _activeTraps = new();

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
            StartCoroutine(SpawnTrapWithWarn());
        }
    }

    private IEnumerator SpawnTrapWithWarn()
    {
        if (_player == null || trapPrefabs == null || trapPrefabs.Length == 0) yield break;

        var offset = Random.insideUnitCircle * spawnRadius;
        var pos = _player.position + new Vector3(offset.x, offset.y, 0f);

        if (SpawnEffect != null)
        {
            var effect = Instantiate(SpawnEffect, pos, Quaternion.identity);
            if (!effect.TryGetComponent<DestroyAfterSpriteSheetAnimation>(out _))
                effect.AddComponent<DestroyAfterSpriteSheetAnimation>();
        }

        yield return new WaitForSeconds(warnDelay);

        var prefab = trapPrefabs[Random.Range(0, trapPrefabs.Length)];
        if (prefab == null) yield break;

        CleanupDestroyedTraps();
        var trap = Instantiate(prefab, pos, prefab.transform.rotation);

        if (trap.TryGetComponent<HazardZone>(out var hz))
            hz.ArmAfterDelay(armDelay);

        _activeTraps.Add(trap);
    }

    private void CleanupDestroyedTraps()
    {
        for (int i = _activeTraps.Count - 1; i >= 0; i--)
            if (_activeTraps[i] == null) _activeTraps.RemoveAt(i);
    }

    private void OnDestroy()
    {
        foreach (var trap in _activeTraps)
            if (trap != null) Destroy(trap);
    }
}
