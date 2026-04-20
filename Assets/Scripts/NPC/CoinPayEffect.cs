using System.Collections;
using UnityEngine;

public class CoinPayEffect : MonoBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinCount = 5;
    [SerializeField] private float scatterRadius = 0.4f;
    [SerializeField] private float flySpeed = 5f;

    private Transform _merchantTransform;

    public void Init(Transform merchant, GameObject prefab, int count)
    {
        _merchantTransform = merchant;
        coinPrefab = prefab;
        coinCount = count;
    }

    public void Play()
    {
        var runner = new GameObject("_CoinPayRunner");
        var mb = runner.AddComponent<CoinPayRunner>();
        mb.Run(transform.position, _merchantTransform, coinPrefab, coinCount, scatterRadius, flySpeed);
    }
}

public class CoinPayRunner : MonoBehaviour
{
    public void Run(Vector3 spawnPos, Transform target, GameObject coinPrefab, int count, float radius, float speed)
    {
        StartCoroutine(SpawnCoins(spawnPos, target, coinPrefab, count, radius, speed));
    }

    private IEnumerator SpawnCoins(Vector3 spawnPos, Transform target, GameObject coinPrefab, int count, float radius, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            var offset = Random.insideUnitCircle * radius;
            var pos = spawnPos + new Vector3(offset.x, offset.y, 0f);
            var coin = Instantiate(coinPrefab, pos, Quaternion.identity);

            var coinScript = coin.GetComponent<Coin>();
            if (coinScript != null) coinScript.enabled = false;
            foreach (var col in coin.GetComponentsInChildren<Collider2D>())
                col.enabled = false;

            StartCoroutine(FlyToTarget(coin, target, speed));
            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    private IEnumerator FlyToTarget(GameObject coin, Transform target, float speed)
    {
        var renderers = coin.GetComponentsInChildren<SpriteRenderer>();
        yield return new WaitForSeconds(Random.Range(0f, 0.15f));

        while (coin != null && target != null)
        {
            var dist = Vector2.Distance(coin.transform.position, target.position);

            if (dist <= 0.2f) { Destroy(coin); yield break; }

            var s = speed + (1f / Mathf.Max(dist, 0.1f)) * 2f;
            coin.transform.position = Vector2.MoveTowards(coin.transform.position, target.position, s * Time.deltaTime);

            if (dist < 1f)
            {
                var alpha = dist;
                foreach (var sr in renderers)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }

            yield return null;
        }

        if (coin != null) Destroy(coin);
    }
}
