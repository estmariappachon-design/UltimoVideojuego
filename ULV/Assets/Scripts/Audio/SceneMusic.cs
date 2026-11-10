using UnityEngine;

/// <summary>
/// Pon este componente en cualquier escena para elegir su música de fondo.
/// Si "music" está vacío usa la música de menú del AudioManager.
/// (En los niveles la música se elige en LevelSetup.)
/// </summary>
public class SceneMusic : MonoBehaviour
{
    public AudioClip music;

    private void Start()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.PlayMusic(music != null ? music : AudioManager.Instance.menuMusic);
    }
}
