using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Slot1Manager : MonoBehaviour
{
    [Header("Paneles del Slot 1")]
    public GameObject panelPlayFirst;
    public GameObject panelPlay;
    public GameObject panelReset;

    [Header("Texto de progreso global (opcional)")]
    public TMP_Text levelsProgressText; // puedes arrastrar el mismo TMP del GameHome

    // —— Config básica ——
    private const string SLOT_ID = "slot1";
    private const string SLOT_STATE_KEY = SLOT_ID + "_state";
    private const int TOTAL_NIVELES_VISIBLES = 204;
    // Por seguridad, borro “sobrante” hasta 300 para cubrir futuros niveles/tests
    private const int WIPE_MAX_LEVEL = 300;

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
        PlayerPrefs.SetString("slotActivo", SLOT_ID);
        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");

// 🧠 Recordar último slot usado por usuario registrado
    PlayerPrefs.SetString("lastSlotUsed", SLOT_ID);

        PlayerPrefs.Save();
        SceneManager.LoadScene(4); // GameHome
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
        Debug.Log("🧹 Slot1 completamente reiniciado (oficial).");
    }

    public void OnConfirmNo()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        PlayerPrefs.Save();
        UpdateUI();
    }

    

    // =========================================================
    // ===============  RESET / BORRADO OFICIAL  ===============
    // =========================================================
    private static void ResetSlot(string slotId)
    {
        // 1) Progreso por nivel
        for (int lvl = 1; lvl <= WIPE_MAX_LEVEL; lvl++)
        {
            // Estado/score base por nivel
            Del($"{slotId}_level_{lvl}_completed");
            Del($"{slotId}_level_{lvl}_score");
            Del($"{slotId}_level_{lvl}_keys");
            Del($"{slotId}_level_{lvl}_diamonds");
            Del($"{slotId}_level_{lvl}_diamond"); // legacy

            // Premios por nivel
            Del($"{slotId}_level_{lvl}_parchment");
            Del($"{slotId}_level_{lvl}_trophy");
            Del($"{slotId}_level_{lvl}_medal");
            Del($"{slotId}_level_{lvl}_masterKey");

            // Config usadas por GameHome (persistidas por nivel)
            Del($"{slotId}_level_{lvl}_maxKeys");
            Del($"{slotId}_level_{lvl}_maxDiamonds");

            // Mejores por nivel (anti-farmeo) de objetos de score
            Del($"{slotId}_level_{lvl}_bags");
            Del($"{slotId}_level_{lvl}_crowns");
            Del($"{slotId}_level_{lvl}_chests");
            Del($"{slotId}_level_{lvl}_coins");

            // Mejores kills por nivel (anti-farmeo)
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

        // 2) Totales globales del slot
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

        // Flags persistentes de MasterKeys (si los grabaste al recoger en LevelProgress.MasterKey)
        for (int mk = 1; mk <= 3; mk++)
            Del($"{slotId}_mk{mk}_collected");

        // 3) Estadísticas (Stadistics) — objetos de score
        //    Si tienes el enum TipoObjetoScore en el proyecto, borro todos por reflexión:
        foreach (var obj in Enum.GetValues(typeof(TipoObjetoScore)))
        {
            Del($"{slotId}_stats_{obj}");
        }

        // 4) Estadísticas de kills — borro todas por enum
        foreach (var kill in Enum.GetValues(typeof(Stadistics.EnemyKillType)))
        {
            Del($"{slotId}_stats_kill_{kill}");
        }

        // 5) Logros — borro TODOS los AchievementId + sus “dismissed” de notificaciones
        foreach (AchievementId ach in Enum.GetValues(typeof(AchievementId)))
        {
            Del($"{slotId}_ach_{ach}");
            Del($"{slotId}_notif_{ach}_dismissed"); // si usas este patrón de dismiss
        }

        // 6) Notificaciones varias
        Del($"{slotId}_notif_queue"); // cola persistente si la usabas

        // 7) Preferencia de notificaciones (para que este slot vuelva a preguntar)
        Del($"{slotId}_notif_preference");


        PlayerPrefs.Save();
    }

    private static void Del(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }



    
}
