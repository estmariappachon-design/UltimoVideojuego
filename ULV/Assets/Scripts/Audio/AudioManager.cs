using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Música de fondo y efectos de sonido. Está en el prefab Assets/Prefabs/AudioManager:
/// arrastra ahí tus sonidos (.wav / .mp3 / .ogg) y se usan en todo el juego.
/// Hay una copia en cada escena; la primera que se carga se queda y las demás se borran solas.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum Sfx
    {
        Drop, Perfect, Cut, Miss, WrongFlavor, OrderDone, OrderFailed,
        Click, Wind, Win, Lose, Tick, Whoosh,
        FlyBuzz, Shoo, Fan, Tremor, Stabilize, LightsOff, Flour, Alert,
        DecoyAvoided, OrderChange, StarPop, Count, NewRecord
    }

    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    [Tooltip("Para sonidos que suben de tono (combo de perfectos)")]
    public AudioSource pitchedSource;

    [Header("Música")]
    public AudioClip menuMusic;
    public AudioClip defaultLevelMusic;
    public AudioClip winJingle;
    public AudioClip loseJingle;

    [Header("Efectos")]
    public AudioClip drop;
    public AudioClip perfect;
    public AudioClip cut;
    public AudioClip miss;
    public AudioClip wrongFlavor;
    public AudioClip orderDone;
    public AudioClip orderFailed;
    public AudioClip click;
    public AudioClip wind;
    public AudioClip tick;
    public AudioClip whoosh;

    [Header("Retos y pantallas de resultado")]
    public AudioClip flyBuzz;
    public AudioClip shoo;
    public AudioClip fan;
    public AudioClip tremor;
    public AudioClip stabilize;
    public AudioClip lightsOff;
    public AudioClip flour;
    public AudioClip alert;
    public AudioClip decoyAvoided;
    public AudioClip orderChange;
    public AudioClip starPop;
    public AudioClip count;
    public AudioClip newRecord;

    [Header("Opciones")]
    public bool clickOnAllButtons = true;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.55f;
    [Range(0f, 1f)] public float defaultSfxVolume = 0.9f;

    public const string MusicVolumeKey = "musicVol";
    public const string SfxVolumeKey = "sfxVol";
    public const string VibrationKey = "vibrar";

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (pitchedSource == null) pitchedSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = sfxSource.playOnAwake = pitchedSource.playOnAwake = false;

        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
        musicSource.volume = MusicVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!clickOnAllButtons) return;
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.gameObject.scene == scene) b.onClick.AddListener(() => Play(Sfx.Click));
    }

    // ------------------------------------------------------------------ API
    public static void Play(Sfx sfx)
    {
        if (Instance == null) return;
        AudioClip c = Instance.ClipFor(sfx);
        if (c != null) Instance.sfxSource.PlayOneShot(c, Instance.SfxVolume);
    }

    /// <summary>Reproduce con otro tono (1 = normal, 1.2 = más agudo).</summary>
    public static void PlayPitched(Sfx sfx, float pitch)
    {
        if (Instance == null) return;
        AudioClip c = Instance.ClipFor(sfx);
        if (c == null) return;
        Instance.pitchedSource.pitch = pitch;
        Instance.pitchedSource.PlayOneShot(c, Instance.SfxVolume);
    }

    public static void PlayMusic(AudioClip clip)
    {
        if (Instance == null) return;
        AudioSource src = Instance.musicSource;
        if (clip == null) { src.Stop(); return; }
        if (src.clip == clip && src.isPlaying) return;
        src.clip = clip;
        src.Play();
    }

    public static void PlayJingle(bool win)
    {
        if (Instance == null) return;
        Instance.musicSource.Stop();
        AudioClip c = win ? Instance.winJingle : Instance.loseJingle;
        if (c != null) Instance.sfxSource.PlayOneShot(c, Instance.SfxVolume);
    }

    public void SetMusicVolume(float v)
    {
        MusicVolume = v;
        musicSource.volume = v;
        PlayerPrefs.SetFloat(MusicVolumeKey, v);
    }

    public void SetSfxVolume(float v)
    {
        SfxVolume = v;
        PlayerPrefs.SetFloat(SfxVolumeKey, v);
    }

    /// <summary>Vibración corta en el celular (si está activada en Ajustes).</summary>
    public static void Vibrate()
    {
        if (PlayerPrefs.GetInt(VibrationKey, 1) != 1) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    private AudioClip ClipFor(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Drop: return drop;
            case Sfx.Perfect: return perfect;
            case Sfx.Cut: return cut;
            case Sfx.Miss: return miss;
            case Sfx.WrongFlavor: return wrongFlavor;
            case Sfx.OrderDone: return orderDone;
            case Sfx.OrderFailed: return orderFailed;
            case Sfx.Click: return click;
            case Sfx.Wind: return wind;
            case Sfx.Win: return winJingle;
            case Sfx.Lose: return loseJingle;
            case Sfx.Tick: return tick;
            case Sfx.Whoosh: return whoosh;
            case Sfx.FlyBuzz: return flyBuzz;
            case Sfx.Shoo: return shoo;
            case Sfx.Fan: return fan;
            case Sfx.Tremor: return tremor;
            case Sfx.Stabilize: return stabilize;
            case Sfx.LightsOff: return lightsOff;
            case Sfx.Flour: return flour;
            case Sfx.Alert: return alert;
            case Sfx.DecoyAvoided: return decoyAvoided;
            case Sfx.OrderChange: return orderChange;
            case Sfx.StarPop: return starPop;
            case Sfx.Count: return count;
            case Sfx.NewRecord: return newRecord;
        }
        return null;
    }
}
