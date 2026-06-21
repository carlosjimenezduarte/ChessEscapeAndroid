using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Slot2Manager : MonoBehaviour
{
    [Header("Paneles del Slot 2")]
    public GameObject panelPlayFirst;
    public GameObject panelPlay;
    public GameObject panelReset;

    [Header("Texto de progreso global (opcional)")]
    public TMP_Text levelsProgressText;

    private const string SLOT_ID = "slot2";                // 👉 Todo queda bajo prefijo slot2_
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

        // Reporte de score SOLO del slot2 (no toca slot1/slot3)
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
        ResetSlot2(); // 🔄 Solo borra claves con prefijo slot2_

        PlayerPrefs.SetString(SLOT_STATE_KEY, "empty");
        PlayerPrefs.Save();

        if (levelsProgressText != null)
            levelsProgressText.text = $"0/{TOTAL_NIVELES_VISIBLES}";

        RankSystem.Instance?.RefreshUI();  // 👈 añadido

        UpdateUI();
        Debug.Log("🧹 Slot2 completamente reiniciado (oficial).");
    }


    public void OnConfirmNo()
    {
        PlayerPrefs.SetString(SLOT_STATE_KEY, "active");
        PlayerPrefs.Save();
        UpdateUI();
    }

    // ======= Reset completo del SLOT 2 (aislado) =======
    private static void ResetSlot2()
    {
        const string slotId = "slot2";

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

            // Mejores por nivel (score-objetos)
            Del($"{slotId}_level_{lvl}_bags");
            Del($"{slotId}_level_{lvl}_crowns");
            Del($"{slotId}_level_{lvl}_chests");
            Del($"{slotId}_level_{lvl}_coins");

            // Mejores kills por nivel
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

        // Totales y snapshots SOLO del slot2
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
        Del($"{slotId}_rankMaxIndex");   // 👈 añadido
        
        // Flags de master keys recogidas
        for (int mk = 1; mk <= 3; mk++)
            Del($"{slotId}_mk{mk}_collected");

        // Estadísticas (score-objetos)
        foreach (var obj in Enum.GetValues(typeof(TipoObjetoScore)))
            Del($"{slotId}_stats_{obj}");

        // Estadísticas (kills)
        foreach (var kill in Enum.GetValues(typeof(Stadistics.EnemyKillType)))
            Del($"{slotId}_stats_kill_{kill}");

        // Achievements y dismiss de notificaciones — SOLO slot2
        foreach (AchievementId ach in Enum.GetValues(typeof(AchievementId)))
        {
            Del($"{slotId}_ach_{ach}");
            Del($"{slotId}_notif_{ach}_dismissed");
        }

        // Cola de notificaciones persistentes (si la usas)
        Del($"{slotId}_notif_queue");

        // Preferencia de notificaciones para slot2
        Del($"{slotId}_notif_preference");

        PlayerPrefs.Save();
    }

    private static void Del(string key) => PlayerPrefs.DeleteKey(key);
}
