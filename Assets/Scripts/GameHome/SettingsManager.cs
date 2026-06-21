using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Botones Música")]
    public Button musicOnButton;
    public Button musicOffButton;

    [Header("Botones Sonido")]
    public Button soundOnButton;
    public Button soundOffButton;

    [Header("Color de activación/desactivación")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    private void Start()
    {
        // Inicializar estados guardados
        UpdateMusicUI(MusicManager.Instance != null && MusicManager.Instance.IsMusicOn());
        UpdateSoundUI(SoundManager.Instance != null && SoundManager.Instance.IsSoundOn());

        // Listeners botones música
        musicOnButton.onClick.AddListener(() =>
        {
            MusicManager.Instance?.SetMusicState(true);
            UpdateMusicUI(true);
            SoundManager.Instance?.PlaySound(36); // 🔊 sonido especial al activar
        });

        musicOffButton.onClick.AddListener(() =>
        {
            MusicManager.Instance?.SetMusicState(false);
            UpdateMusicUI(false);
        });

        // Listeners botones sonido
        soundOnButton.onClick.AddListener(() =>
        {
            SoundManager.Instance?.SetSoundState(true);
            UpdateSoundUI(true);
            SoundManager.Instance?.PlaySound(8); // 🔊 sonido especial al activar
        });

        soundOffButton.onClick.AddListener(() =>
        {
            SoundManager.Instance?.SetSoundState(false);
            UpdateSoundUI(false);
        });
    }

    private void UpdateMusicUI(bool musicOn)
    {
        musicOnButton.image.color = musicOn ? activeColor : inactiveColor;
        musicOffButton.image.color = musicOn ? inactiveColor : activeColor;
    }

    private void UpdateSoundUI(bool soundOn)
    {
        soundOnButton.image.color = soundOn ? activeColor : inactiveColor;
        soundOffButton.image.color = soundOn ? inactiveColor : activeColor;
    }
}
