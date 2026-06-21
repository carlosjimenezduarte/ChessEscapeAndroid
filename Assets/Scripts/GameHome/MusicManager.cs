// 🎵 MusicManager.cs
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Canción de fondo de este nivel")]
    public AudioClip musicaNivel;

    private AudioSource audioSource;
    private bool musicOn = true;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        // Cargar estado guardado
        musicOn = PlayerPrefs.GetInt("musicOn", 1) == 1;
    }

    public void PlayMusic(float volume = 0.4f)
    {
        if (!musicOn || musicaNivel == null) return;

        if (audioSource.clip == musicaNivel && audioSource.isPlaying) return;

        audioSource.clip = musicaNivel;
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public void SetMusicState(bool enabled)
    {
        musicOn = enabled;
        PlayerPrefs.SetInt("musicOn", enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (!enabled) StopMusic();
        else PlayMusic();
    }

    public bool IsMusicOn() => musicOn;
}
