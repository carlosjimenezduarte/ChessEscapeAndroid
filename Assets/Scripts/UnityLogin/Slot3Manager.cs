using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Slot3Manager : MonoBehaviour
{
    [Header("Paneles del Slot 3")]
    public GameObject panelPlayFirst;
    public GameObject panelPlay;
    public GameObject panelReset;

    [Header("Texto de progreso global (opcional)")]
    public TMP_Text levelsProgressText;

    private const string SLOT_ID = "slot3";                // 👉 Todo queda bajo prefijo slot3_
    private const string SLOT_STATE_KEY = SLOT_ID + "_state";
    private const int TOTAL_NIVELES_VISIBLES = 204;
    private const int WIPE_MAX_LEVEL = 300;

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        string state = PlayerPrefs.GetString(SLOT_STATE_KEY, "empty");

        if (panelPlayFirst) panelPlayFirst.SetActive(state == "empty");
        if (panelPlay)      panelPlay.SetActive(state == "active");
        if (panelReset)     panelReset.SetActive(state == "reset_pending");

        // Reporte de score SOLO del slot3
        int totalScore = PlayerPrefs.GetInt($"{SLOT_ID}_scoreTotal", 0);
        AchievementsManager.ReportScoreProgress(totalScore);
    }

    public void OnPlay()
    {
        // Marcamos este slot como activo (global por diseño, para que el resto del juego lea de aquí)
        PlayerPrefs.SetString("slotActivo", SLOT_ID);

        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        PlayerPrefs.Save();

        SceneManager.LoadScene(4); // Ir a GameHome
    }

    public void OnReiniciar()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "reset_pending");
        PlayerPrefs.Save();
        UpdateUI();
    }

    public void OnConfirmYes()
    {
        ResetSlot3(); // 🔄 Solo borra claves con prefijo slot3_

        PlayerPrefs.SetString(SLOT_STATE_KEY, "empty");
        PlayerPrefs.Save();

        if (levelsProgressText != null)
            levelsProgressText.text = $"0/{TOTAL_NIVELES_VISIBLES}";

        RankSystem.Instance?.RefreshUI();  // 👈 añadido como en slot1 y slot2

        UpdateUI();
        Debug.Log("🧹 Slot3 completamente reiniciado (oficial).");
    }

    public void OnConfirmNo()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        PlayerPrefs.Save();
        UpdateUI();
    }

    // ======= Reset completo del SLOT 3 (aislado) =======
    private static void ResetSlot3()
    {
        const string slotId = "slot3";

        for (int lvl = 1; lvl <= WIPE_MAX_LEVEL; lvl++)
        {
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_completed");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_score");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_keys");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_diamonds");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_diamond");

            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_parchment");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_trophy");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_medal");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_masterKey");

            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_maxKeys");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_maxDiamonds");

            // Mejores por nivel (score-objetos)
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_bags");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_crowns");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_chests");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_coins");

            // Mejores kills por nivel
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsPawn");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsKnight");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsBishop");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsRook");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsQueen");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsRookBlack");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsQueenBlack");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsBishopBlack");
            PlayerPrefs.DeleteKey($"{slotId}_level_{lvl}_killsKnightBlack");
        }

        // Totales y snapshots SOLO del slot3
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
        PlayerPrefs.DeleteKey($"{slotId}_rankMaxIndex");   // 👈 añadido

        // Flags de master keys recogidas
        for (int mk = 1; mk <= 3; mk++)
            PlayerPrefs.DeleteKey($"{slotId}_mk{mk}_collected");

        // Estadísticas (score-objetos)
        foreach (var obj in Enum.GetValues(typeof(TipoObjetoScore)))
            PlayerPrefs.DeleteKey($"{slotId}_stats_{obj}");

        // Estadísticas (kills)
        foreach (var kill in Enum.GetValues(typeof(Stadistics.EnemyKillType)))
            PlayerPrefs.DeleteKey($"{slotId}_stats_kill_{kill}");

        // Achievements y dismiss de notificaciones — SOLO slot3
        foreach (AchievementId ach in Enum.GetValues(typeof(AchievementId)))
        {
            PlayerPrefs.DeleteKey($"{slotId}_ach_{ach}");
            PlayerPrefs.DeleteKey($"{slotId}_notif_{ach}_dismissed");
        }

        // Cola de notificaciones persistentes (si la usas)
        PlayerPrefs.DeleteKey($"{slotId}_notif_queue");

        PlayerPrefs.DeleteKey($"{slotId}_notif_preference");

        PlayerPrefs.Save();
    }
}
