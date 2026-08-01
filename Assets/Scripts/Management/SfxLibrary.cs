using System;
using UnityEngine;

public enum SfxCue
{
    Shoot,
    Dash,
    PlayerHit,
    PlayerDeath,
    EnemyHit,
    EnemyDeath,
    Coin,
    Chest,
    UiClick,
}

[Serializable]
public class SfxSlot
{
    public SfxCue cue;
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float minInterval = 0.05f;
    [Range(0.1f, 3f)] public float minPitch = 0.95f;
    [Range(0.1f, 3f)] public float maxPitch = 1.05f;
}

[CreateAssetMenu(menuName = "Audio/SFX Library")]
public class SfxLibrary : ScriptableObject
{
    public SfxSlot[] slots;

    public SfxSlot Find(SfxCue cue)
    {
        if (slots == null) return null;
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot != null && slot.cue == cue) return slot;
        }
        return null;
    }
}

public static class SfxPlaybackRules
{
    public static int PickVariantIndex(int clipCount, int previousIndex, float randomValue)
    {
        if (clipCount <= 0) return -1;
        if (clipCount == 1) return 0;

        var candidate = Mathf.Clamp((int)(Mathf.Clamp01(randomValue) * clipCount), 0, clipCount - 1);
        if (previousIndex >= 0 && candidate >= previousIndex) candidate = (candidate + 1) % clipCount;
        return candidate;
    }

    public static bool CanPlay(float now, float lastPlayTime, float minimumInterval) =>
        now - lastPlayTime >= Mathf.Max(0f, minimumInterval);
}
