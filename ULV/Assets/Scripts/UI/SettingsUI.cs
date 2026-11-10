using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel de ajustes: volumen de música, volumen de efectos y vibración.</summary>
public class SettingsUI : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle vibrationToggle;

    private void OnEnable()
    {
        AudioManager am = AudioManager.Instance;
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.value = am != null ? am.MusicVolume : PlayerPrefs.GetFloat(AudioManager.MusicVolumeKey, 0.55f);
            musicSlider.onValueChanged.AddListener(v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); });
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.value = am != null ? am.SfxVolume : PlayerPrefs.GetFloat(AudioManager.SfxVolumeKey, 0.9f);
            sfxSlider.onValueChanged.AddListener(v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v); });
        }
        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.RemoveAllListeners();
            vibrationToggle.isOn = PlayerPrefs.GetInt(AudioManager.VibrationKey, 1) == 1;
            vibrationToggle.onValueChanged.AddListener(on => PlayerPrefs.SetInt(AudioManager.VibrationKey, on ? 1 : 0));
        }
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }
}
