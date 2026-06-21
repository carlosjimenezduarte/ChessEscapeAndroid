// 🔊 SoundManager.cs
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Clips de sonido (0 = movimiento aliado, 1 = ataque aliado, etc.)")]
    public AudioClip[] clips;

    private AudioSource audioSource;
    private bool soundOn = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
        }

        // Cargar estado guardado
        soundOn = PlayerPrefs.GetInt("soundOn", 1) == 1;
    }

    public void PlaySound(int index, float volume = 0.7f)
    {
        if (!soundOn) return;
        if (clips.Length > 0 && index >= 0 && index < clips.Length)
            audioSource.PlayOneShot(clips[index], volume);
    }

    public void SetSoundState(bool enabled)
    {
        soundOn = enabled;
        PlayerPrefs.SetInt("soundOn", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsSoundOn() => soundOn;
}
