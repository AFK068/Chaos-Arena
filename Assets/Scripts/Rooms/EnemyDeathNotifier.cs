using UnityEngine;

// RoomSpawner вешает этот компонент на каждого заспавненного врага.
// Room подписывается на OnDied чтобы считать убийства.
public class EnemyDeathNotifier : MonoBehaviour
{
    public System.Action OnDied;

    private void OnDestroy()
    {
        OnDied?.Invoke();
    }
}
