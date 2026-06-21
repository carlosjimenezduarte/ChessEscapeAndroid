using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class Guest2Manager : MonoBehaviour
{
    [Header("Paneles del Invitado 2")]
    public GameObject panelPlayFirst;   // estado "empty"
    public GameObject panelPlay;        // estado "active"
    public GameObject panelReset;       // estado "reset_pending"

    [Header("Texto de progreso global (opcional)")]
    public TMP_Text levelsProgressText;

    [Header("Escena a cargar al jugar")]
    [Tooltip("BuildIndex al que vas cuando el invitado 2 pulsa Jugar")]
    public int nextSceneBuildIndex = 4;

    // —— Config invitado 2 ——
    private const string SLOT_ID = "guest2"; // 👈 CAMBIO CLAVE
    private const string SLOT_STATE_KEY = SLOT_ID + "_state";
    private const string USER_TYPE_KEY = "userType";

    private const int TOTAL_NIVELES_VISIBLES = 204;
    private const int WIPE_MAX_LEVEL = 300;

    private const string MUSIC_VOL = "settings_musicVolume";
    private const string SFX_VOL = "settings_sfxVolume";
    private const string LANG_KEY = "settings_language";
    private const string MUSIC_ON = "settings_musicOn";
    private const string SFX_ON = "settings_sfxOn";

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        string state = PlayerPrefs.GetString(SLOT_STATE_KEY, "empty");

        if (panelPlayFirst) panelPlayFirst.SetActive(state == "empty");
        if (panelPlay) panelPlay.SetActive(state == "active");
        if (panelReset) panelReset.SetActive(state == "reset_pending");

        int totalScore = PlayerPrefs.GetInt($"{SLOT_ID}_scoreTotal", 0);
        AchievementsManager.ReportScoreProgress(totalScore);
    }

    // —— Acciones UI ——
    public void OnPlay()
    {
        PlayerPrefs.SetString(USER_TYPE_KEY, "guest2"); // 👈 diferencia para saber de dónde viene
        PlayerPrefs.SetString("slotActivo", SLOT_ID);

        if (!PlayerPrefs.HasKey(SLOT_STATE_KEY))
        {
            PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
            SeedGuestDefaults();
        }
        else
        {
            if (PlayerPrefs.GetString(SLOT_STATE_KEY) == "empty")
                PlayerPrefs.SetString(SLOT_STATE_KEY, "active");

            if (!PlayerPrefs.HasKey($"{SLOT_ID}_nivelMax")) 
                PlayerPrefs.SetInt($"{SLOT_ID}_nivelMax", 1);
        }

        if (!PlayerPrefs.HasKey(MUSIC_VOL)) PlayerPrefs.SetFloat(MUSIC_VOL, 0.6f);
        if (!PlayerPrefs.HasKey(SFX_VOL))   PlayerPrefs.SetFloat(SFX_VOL, 0.8f);
        if (!PlayerPrefs.HasKey(LANG_KEY))  PlayerPrefs.SetString(LANG_KEY, "es");
        if (!PlayerPrefs.HasKey(MUSIC_ON))  PlayerPrefs.SetInt(MUSIC_ON, 1);
        if (!PlayerPrefs.HasKey(SFX_ON))    PlayerPrefs.SetInt(SFX_ON, 1);

        PlayerPrefs.Save();
        LoadSceneWithSound(nextSceneBuildIndex);
    }

    public void OnReiniciar()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "reset_pending");
        PlayerPrefs.Save();
        UpdateUI();
    }

    public void OnConfirmYes()
    {
        ResetSlot(SLOT_ID);
        PlayerPrefs.SetString(SLOT_STATE_KEY, "empty");
        PlayerPrefs.Save();

        if (levelsProgressText != null)
            levelsProgressText.text = $"0/{TOTAL_NIVELES_VISIBLES}";

        RankSystem.Instance?.RefreshUI();

        UpdateUI();
        Debug.Log("🧹 Guest2 (invitado 2) completamente reiniciado.");
    }

    public void OnConfirmNo()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        PlayerPrefs.Save();
        UpdateUI();
    }

    private static void SeedGuestDefaults()
    {
        PlayerPrefs.SetInt($"{SLOT_ID}_nivelMax", 1);
        PlayerPrefs.SetInt($"{SLOT_ID}_scoreTotal", 0);
        PlayerPrefs.SetInt($"{SLOT_ID}_keysTotal", 0);
        PlayerPrefs.SetInt($"{SLOT_ID}_diamondsTotal", 0);
        PlayerPrefs.SetInt($"{SLOT_ID}_parchmentsTotal", 0);
        PlayerPrefs.SetInt($"{SLOT_ID}_trophiesTotal", 0);
        PlayerPrefs.SetInt($"{SLOT_ID}_medalsTotal", 0);
        PlayerPrefs.SetInt($"{SLOT_ID}_masterKeysTotal", 0);
        PlayerPrefs.Save();
    }

    public void LoadSceneWithSound(int sceneIndex)
    {
        Debug.Log($"[Guest2Manager] 🎵 Transición a escena {sceneIndex}");
        SoundManager.Instance?.PlaySound(8);
        StartCoroutine(LoadSceneDelay(sceneIndex, 0.5f));
    }

    private System.Collections.IEnumerator LoadSceneDelay(int sceneIndex, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneIndex);
    }

    // RESET completo
    private static void ResetSlot(string slotId)
    {
        for (int lvl = 1; lvl <= WIPE_MAX_LEVEL; lvl++)
        {
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_completed");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_score");
            // ... repite igual que en GuestManager para cada campo ...
        }

        PlayerPrefs.DeleteKey($"{slotId}_nivelMax");
        PlayerPrefs.DeleteKey($"{slotId}_scoreTotal");
        PlayerPrefs.DeleteKey($"{slotId}_keysTotal");
        PlayerPrefs.DeleteKey($"{slotId}_diamondsTotal");
        PlayerPrefs.DeleteKey($"{slotId}_parchmentsTotal");
        PlayerPrefs.DeleteKey($"{slotId}_trophiesTotal");
        PlayerPrefs.DeleteKey($"{slotId}_medalsTotal");
        PlayerPrefs.DeleteKey($"{slotId}_masterKeysTotal");
        PlayerPrefs.DeleteKey($"{slotId}_scoreFromAchievements");
        PlayerPrefs.DeleteKey($"{slotId}_lastRunDebt");
        PlayerPrefs.DeleteKey($"{slotId}_rankMaxIndex");

        PlayerPrefs.Save();
    }
}
