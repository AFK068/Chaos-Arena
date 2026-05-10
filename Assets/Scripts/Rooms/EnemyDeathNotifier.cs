using UnityEngine;

// RoomSpawner вешает этот компонент на каждого заспавненного врага.
// Room подписывается на OnDied чтобы считать живых в комнате.
// EnemyHealth.Die() вызывает Notify() — это единственный валидный путь смерти,
// чтобы выгрузка сцены не считалась за убийство.
public class EnemyDeathNotifier : MonoBehaviour
{
    public System.Action<EnemyDeathNotifier> OnDied;
    public static System.Action OnAnyEnemyKilled;

    public void Notify()
    {
        OnDied?.Invoke(this);
        OnAnyEnemyKilled?.Invoke();
    }
}
