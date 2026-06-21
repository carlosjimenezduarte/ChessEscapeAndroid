using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GuestManager : MonoBehaviour
{
    [Header("Paneles del Invitado")]
    public GameObject panelPlayFirst;   // estado "empty"
    public GameObject panelPlay;        // estado "active"
    public GameObject panelReset;       // estado "reset_pending"

    [Header("Texto de progreso global (opcional)")]
    public TMP_Text levelsProgressText; // puedes arrastrar el mismo TMP del GameHome si quieres

    [Header("Escena a cargar al jugar")]
    [Tooltip("BuildIndex al que vas cuando el invitado pulsa Jugar (p. ej. 4 = GameHome, o 13 = HomeGuest si lo prefieres)")]
    public int nextSceneBuildIndex = 4;

    // —— Config invitado ——
    private const string SLOT_ID = "guest";
    private const string SLOT_STATE_KEY = SLOT_ID + "_state";
    private const string USER_TYPE_KEY = "userType";

    private const int TOTAL_NIVELES_VISIBLES = 204;
    private const int WIPE_MAX_LEVEL = 300; // por seguridad

    // Ajustes globales sugeridos (si no existen)
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

        // (Opcional) reportar score a milestones (con 0 no desbloquea nada)
        int totalScore = PlayerPrefs.GetInt($"{SLOT_ID}_scoreTotal", 0);
        AchievementsManager.ReportScoreProgress(totalScore);
    }

    // —— Acciones UI ——
    public void OnPlay()
    {
        PlayerPrefs.SetString(USER_TYPE_KEY, "guest");
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
        LoadSceneWithSound(nextSceneBuildIndex); // ← usamos nuestro nuevo método
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
        Debug.Log("🧹 Guest (invitado) completamente reiniciado.");
    }

    public void OnConfirmNo()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        PlayerPrefs.Save();
        UpdateUI();
    }

    // —— Inicialización mínima del invitado (primera vez) ——
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

    // —— NUEVO: Cargar escenas con sonido ——
    public void LoadSceneWithSound(int sceneIndex)
    {
        Debug.Log($"[GuestManager] 🎵 Transición a escena {sceneIndex}");
        SoundManager.Instance?.PlaySound(8); // usa tu ID de sonido de transición
        StartCoroutine(LoadSceneDelay(sceneIndex, 0.5f));
    }

    private System.Collections.IEnumerator LoadSceneDelay(int sceneIndex, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneIndex);
    }

    // =========================================================
    // ===============  RESET / BORRADO OFICIAL  ===============
    // =========================================================
    private static void ResetSlot(string slotId)
    {
        for (int lvl = 1; lvl <= WIPE_MAX_LEVEL; lvl++)
        {
            Del($"{slotId}_level_{lvl}_completed");
            Del($"{slotId}_level_{lvl}_score");
            Del($"{slotId}_level_{lvl}_keys");
            Del($"{slotId}_level_{lvl}_diamonds");
            Del($"{slotId}_level_{lvl}_diamond"); 
            Del($"{slotId}_level_{lvl}_parchment");
            Del($"{slotId}_level_{lvl}_trophy");
            Del($"{slotId}_level_{lvl}_medal");
            Del($"{slotId}_level_{lvl}_masterKey");
            Del($"{slotId}_level_{lvl}_maxKeys");
            Del($"{slotId}_level_{lvl}_maxDiamonds");
            Del($"{slotId}_level_{lvl}_bags");
            Del($"{slotId}_level_{lvl}_crowns");
            Del($"{slotId}_level_{lvl}_chests");
            Del($"{slotId}_level_{lvl}_coins");
            Del($"{slotId}_level_{lvl}_killsPawn");
            Del($"{slotId}_level_{lvl}_killsKnight");
            Del($"{slotId}_level_{lvl}_killsBishop");
            Del($"{slotId}_level_{lvl}_killsRook");
            Del($"{slotId}_level_{lvl}_killsQueen");
            Del($"{slotId}_level_{lvl}_killsRookBlack");
            Del($"{slotId}_level_{lvl}_killsQueenBlack");
            Del($"{slotId}_level_{lvl}_killsBishopBlack");
            Del($"{slotId}_level_{lvl}_killsKnightBlack");
        }

        Del($"{slotId}_nivelMax");
        Del($"{slotId}_scoreTotal");
        Del($"{slotId}_keysTotal");
        Del($"{slotId}_diamondsTotal");
        Del($"{slotId}_parchmentsTotal");
        Del($"{slotId}_trophiesTotal");
        Del($"{slotId}_medalsTotal");
        Del($"{slotId}_masterKeysTotal");
        Del($"{slotId}_scoreFromAchievements");
        Del($"{slotId}_lastRunDebt");
        Del($"{slotId}_rankMaxIndex");

        for (int mk = 1; mk <= 3; mk++) Del($"{slotId}_mk{mk}_collected");

        foreach (var obj in Enum.GetValues(typeof(TipoObjetoScore))) Del($"{slotId}_stats_{obj}");
        foreach (var kill in Enum.GetValues(typeof(Stadistics.EnemyKillType))) Del($"{slotId}_stats_kill_{kill}");
        foreach (AchievementId ach in Enum.GetValues(typeof(AchievementId)))
        {
            Del($"{slotId}_ach_{ach}");
            Del($"{slotId}_notif_{ach}_dismissed");
        }

        Del($"{slotId}_notif_queue");
        PlayerPrefs.Save();
    }

    private static void Del(string key) => PlayerPrefs.DeleteKey(key);
}
