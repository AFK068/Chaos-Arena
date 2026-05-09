using UnityEngine;

[CreateAssetMenu(menuName = "Chaos Arena/Room Data Pool")]
public class RoomDataPool : ScriptableObject
{
    public RoomData startRoom;
    public RoomData[] normalRooms;
    public RoomData[] challengeRooms;
    public RoomData[] gauntletRooms;
    public RoomData[] chestRooms;
    public RoomData[] shopRooms;
    public RoomData[] secretRooms;
    public RoomData[] bossRooms;
    public RoomData[] emptyRooms;

    public RoomData GetRandom(RoomType type)
    {
        return type switch
        {
            RoomType.Start     => startRoom,
            RoomType.Normal    => PickRandom(normalRooms),
            RoomType.Challenge => PickRandom(challengeRooms),
            RoomType.Gauntlet  => PickRandom(gauntletRooms),
            RoomType.Chest     => PickRandom(chestRooms),
            RoomType.Shop      => PickRandom(shopRooms),
            RoomType.Secret    => PickRandom(secretRooms),
            RoomType.Boss      => PickRandom(bossRooms),
            RoomType.Empty     => PickRandom(emptyRooms),
            _                  => null
        };
    }

    public bool HasContent(RoomType type)
    {
        return type switch
        {
            RoomType.Start     => startRoom != null,
            RoomType.Normal    => HasAny(normalRooms),
            RoomType.Challenge => HasAny(challengeRooms),
            RoomType.Gauntlet  => HasAny(gauntletRooms),
            RoomType.Chest     => HasAny(chestRooms),
            RoomType.Shop      => HasAny(shopRooms),
            RoomType.Secret    => HasAny(secretRooms),
            RoomType.Boss      => HasAny(bossRooms),
            RoomType.Empty     => HasAny(emptyRooms),
            _                  => false
        };
    }

    private static bool HasAny(RoomData[] pool) => pool != null && pool.Length > 0;

    private static RoomData PickRandom(RoomData[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning($"RoomDataPool: пул пустой!");
            return null;
        }
        return pool[Random.Range(0, pool.Length)];
    }
}
