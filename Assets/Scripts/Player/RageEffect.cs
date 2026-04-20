using UnityEngine;

public class RageEffect : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 5;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float scale = 1.7f;

    public static void Spawn(GameObject prefab, Transform player, float duration)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, player);
        var effect = go.GetComponent<RageEffect>();
        if (effect != null)
        {
            go.transform.localPosition = effect.offset;
            go.transform.localScale = Vector3.one * effect.scale;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingLayerName = effect.sortingLayer;
                sr.sortingOrder = effect.sortingOrder;
            }
        }
        else
        {
            go.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        }
        Destroy(go, duration);
    }
}
