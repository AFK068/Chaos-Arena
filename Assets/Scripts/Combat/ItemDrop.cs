using System.Collections;
using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    public void Throw(Vector3 targetPosition, float duration = 0.4f)
    {
        StartCoroutine(ThrowRoutine(targetPosition, duration));
    }

    private IEnumerator ThrowRoutine(Vector3 target, float duration)
    {
        var start = transform.position;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;

            // Дуга вверх через sin
            var arc = Mathf.Sin(t * Mathf.PI) * 0.5f;
            var pos = Vector3.Lerp(start, target, t);
            pos.y += arc;
            transform.position = pos;

            // Небольшое вращение во время полёта
            transform.rotation = Quaternion.Euler(0f, 0f, t * 360f);

            yield return null;
        }

        transform.position = target;
        transform.rotation = Quaternion.identity;
    }
}
