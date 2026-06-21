using UnityEngine;
using UnityEngine.SceneManagement;

/// Se asegura de que, si venimos como invitado, TODO el flujo se mantenga en modo invitado,
/// evitando tocar Authentication/CloudSave y garantizando llaves mínimas.
[DefaultExecutionOrder(-1000)]
public class GuestSessionGuard : MonoBehaviour
{
    private const string USER_TYPE_KEY = "userType";
    private const string SLOT_ID = "guest";
    private const string SLOT_STATE_KEY = SLOT_ID + "_state";

    private static bool _alive;

    void Awake()
    {
        // Singleton persistente
        if (_alive) { Destroy(gameObject); return; }
        _alive = true;
        DontDestroyOnLoad(gameObject);

        // Si ya venimos marcados como invitado, reforzar y sembrar mínimos
        if (IsGuest())
        {
            ForceGuestBasics();
        }

        // Escuchar cambios de escena para reforzar al entrar a 13/10/219/220
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Si estamos en una escena del flujo de invitado, forzar claves mínimas
        if (s.buildIndex == 13 || s.buildIndex == 10 || s.buildIndex == 219 || s.buildIndex == 220)
        {
            SetAsGuest();       // por si venimos de otros flujos
            ForceGuestBasics(); // mínimos
        }
    }

    public static bool IsGuest()
    {
        return PlayerPrefs.GetString(USER_TYPE_KEY, "") == "guest";
    }

    public static void SetAsGuest()
    {
        PlayerPrefs.SetString(USER_TYPE_KEY, "guest");
        PlayerPrefs.SetString("slotActivo", SLOT_ID);
        PlayerPrefs.Save();
    }

    private static void ForceGuestBasics()
    {
        // Estado del slot
        if (!PlayerPrefs.HasKey(SLOT_STATE_KEY))
            PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        if (!PlayerPrefs.HasKey($"{SLOT_ID}_nivelMax"))
            PlayerPrefs.SetInt($"{SLOT_ID}_nivelMax", 1);

        // Ajustes globales razonables
        if (!PlayerPrefs.HasKey("settings_musicVolume")) PlayerPrefs.SetFloat("settings_musicVolume", 0.6f);
        if (!PlayerPrefs.HasKey("settings_sfxVolume"))   PlayerPrefs.SetFloat("settings_sfxVolume", 0.8f);
        if (!PlayerPrefs.HasKey("settings_language"))    PlayerPrefs.SetString("settings_language", "es");
        if (!PlayerPrefs.HasKey("settings_musicOn"))     PlayerPrefs.SetInt("settings_musicOn", 1);
        if (!PlayerPrefs.HasKey("settings_sfxOn"))       PlayerPrefs.SetInt("settings_sfxOn", 1);

        PlayerPrefs.Save();
    }
}
