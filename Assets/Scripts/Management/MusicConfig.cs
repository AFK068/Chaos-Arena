using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Music Config")]
public class MusicConfig : ScriptableObject
{
    public MusicPlaylist menu;
    public MusicPlaylist gameplay;
    public MusicPlaylist boss;
}

[System.Serializable]
public class MusicPlaylist
{
    public AudioClip[] clips;
    public bool loop;
}
