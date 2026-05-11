using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum MusicCategory { None, Menu, Gameplay, Boss }

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SfxVolumeParam = "SfxVolume";
    private const float DefaultVolume = 0.7f;
    private const float CrossfadeDuration = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioManager");
        Instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    private AudioMixer _mixer;
    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private MusicConfig _musicConfig;

    private Coroutine _musicRoutine;
    private MusicCategory _currentCategory = MusicCategory.None;
    private int _lastClipIndex = -1;

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _mixer = Resources.Load<AudioMixer>("Audio/MainMixer");
        if (_mixer == null)
        {
            Debug.LogError("AudioManager: не найден AudioMixer по пути Resources/Audio/MainMixer");
            return;
        }

        var musicGroups = _mixer.FindMatchingGroups("Music");
        var sfxGroups = _mixer.FindMatchingGroups("SFX");
        var musicGroup = musicGroups.Length > 0 ? musicGroups[0] : null;
        var sfxGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = false;
        _musicSource.playOnAwake = false;
        _musicSource.outputAudioMixerGroup = musicGroup;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.outputAudioMixerGroup = sfxGroup;

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);
        ApplyMusicVolume();
        ApplySfxVolume();

        _musicConfig = Resources.Load<MusicConfig>("Audio/MusicConfig");
        if (_musicConfig == null)
            Debug.LogWarning("AudioManager: не найден MusicConfig по пути Resources/Audio/MusicConfig");
    }

    public void PlayMenuMusic() => SwitchCategory(MusicCategory.Menu);
    public void PlayGameplayMusic() => SwitchCategory(MusicCategory.Gameplay);
    public void PlayBossMusic() => SwitchCategory(MusicCategory.Boss);

    private void SwitchCategory(MusicCategory cat)
    {
        if (cat == _currentCategory) return;
        _currentCategory = cat;
        _lastClipIndex = -1;

        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(PlaylistRoutine(cat));
    }

    private IEnumerator PlaylistRoutine(MusicCategory cat)
    {
        var entry = GetPlaylist(cat);
        if (entry?.clips == null || entry.clips.Length == 0) yield break;

        bool needsCrossfade = _musicSource.isPlaying;

        while (true)
        {
            var clip = PickRandomClip(entry.clips);
            if (clip == null) yield break;

            if (needsCrossfade)
            {
                yield return CrossfadeTo(clip, entry.loop);
                needsCrossfade = false;
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.loop = entry.loop;
                _musicSource.volume = 1f;
                _musicSource.Play();
            }

            if (entry.loop) yield break;

            yield return new WaitForSecondsRealtime(clip.length);
        }
    }

    private MusicPlaylist GetPlaylist(MusicCategory cat)
    {
        if (_musicConfig == null) return null;
        return cat switch
        {
            MusicCategory.Menu => _musicConfig.menu,
            MusicCategory.Gameplay => _musicConfig.gameplay,
            MusicCategory.Boss => _musicConfig.boss,
            _ => null,
        };
    }

    private AudioClip PickRandomClip(AudioClip[] playlist)
    {
        if (playlist.Length == 0) return null;
        if (playlist.Length == 1) { _lastClipIndex = 0; return playlist[0]; }
        int idx;
        do { idx = Random.Range(0, playlist.Length); } while (idx == _lastClipIndex);
        _lastClipIndex = idx;
        return playlist[idx];
    }

    private IEnumerator CrossfadeTo(AudioClip newClip, bool loop)
    {
        float half = Mathf.Max(0.01f, CrossfadeDuration * 0.5f);
        float startVol = _musicSource.volume;
        float t = 0f;
        while (t < half && _musicSource.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, 0f, t / half);
            yield return null;
        }
        _musicSource.clip = newClip;
        _musicSource.loop = loop;
        _musicSource.volume = 0f;
        _musicSource.Play();
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(0f, 1f, t / half);
            yield return null;
        }
        _musicSource.volume = 1f;
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        ApplySfxVolume();
    }

    private void ApplyMusicVolume()
    {
        if (_mixer != null) _mixer.SetFloat(MusicVolumeParam, LinearToDb(MusicVolume));
    }

    private void ApplySfxVolume()
    {
        if (_mixer != null) _mixer.SetFloat(SfxVolumeParam, LinearToDb(SfxVolume));
    }

    private static float LinearToDb(float linear) =>
        linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, volume);
    }
}
