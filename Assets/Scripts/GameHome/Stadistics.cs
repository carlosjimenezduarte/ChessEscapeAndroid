using UnityEngine;
using TMPro;

public class Stadistics : MonoBehaviour
{
    [Header("Referencias UI (recolectables)")]
    public TMP_Text realCoinText;
    public TMP_Text bagText;
    public TMP_Text chestText;
    public TMP_Text crownText;

    [Header("Especiales")]
    public TMP_Text parchmentText;
    public TMP_Text trophyText;
    public TMP_Text medalText;

    [Header("Metas de especiales (editable)")]
    public int parchmentsGoal = 10;
    public int trophiesGoal   = 4;
    public int medalsGoal     = 6;

    // ----- KILLS ENEMIGOS -----
    public enum EnemyKillType
    {
        PawnRed, KnightRed, BishopRed, RookRed, QueenRed,
        KnightBlack, BishopBlack, RookBlack, QueenBlack
    }

    [System.Serializable]
    public struct KillStatUI
    {
        public EnemyKillType tipo;
        public TMP_Text text;
        public int goal;
    }

    [Header("Kills de Enemigos (arrastrar 9 TMP_Text)")]
    public KillStatUI[] enemyKillStats;

    // ------------------ API recolectables (score) ------------------
    public static void RegistrarObjeto(string slotId, TipoObjetoScore tipo, int cantidad)
    {
        string key = slotId + "_stats_" + tipo.ToString();
        int previo = PlayerPrefs.GetInt(key, 0);
        int nuevo = previo + cantidad;
        PlayerPrefs.SetInt(key, nuevo);
        PlayerPrefs.Save();
        Debug.Log($"📊 Estadística {tipo}: {previo} -> {nuevo}");
    }

    public static int ObtenerConteo(string slotId, TipoObjetoScore tipo)
    {
        return PlayerPrefs.GetInt(slotId + "_stats_" + tipo.ToString(), 0);
    }

    // ------------------ API kills ------------------
    public static void RegistrarKill(string slotId, EnemyKillType tipo, int cantidad)
    {
        RegistrarKill(slotId, tipo.ToString(), cantidad);
    }

    public static int ObtenerKills(string slotId, EnemyKillType tipo)
    {
        return ObtenerKills(slotId, tipo.ToString());
    }

    public static void RegistrarKill(string slotId, string tipo, int cantidad)
    {
        string key = $"{slotId}_stats_kill_{tipo}";
        int previo = PlayerPrefs.GetInt(key, 0);
        int nuevo = previo + cantidad;
        PlayerPrefs.SetInt(key, nuevo);
        PlayerPrefs.Save();
        Debug.Log($"📊 Kill {tipo}: {previo} -> {nuevo} (+{cantidad})");
    }

    public static int ObtenerKills(string slotId, string tipo)
    {
        return PlayerPrefs.GetInt($"{slotId}_stats_kill_{tipo}", 0);
    }

    // ------------------ UI ------------------
    private void Start()
    {
        RefrescarUI();
    }

    public void RefrescarUI()
    {
        string slot = PlayerPrefs.GetString("slotActivo", "slot1");

        // Recolectables de score (con metas fijas de ejemplo)
        if (realCoinText != null) realCoinText.text = $"{ObtenerConteo(slot, TipoObjetoScore.RealCoin)}/1000";
        if (bagText != null)      bagText.text      = $"{ObtenerConteo(slot, TipoObjetoScore.Bag)}/300";
        if (chestText != null)    chestText.text    = $"{ObtenerConteo(slot, TipoObjetoScore.Chest)}/100";
        if (crownText != null)    crownText.text    = $"{ObtenerConteo(slot, TipoObjetoScore.Crown)}/50";

        // ✅ Especiales: leer totales globales *reales*
        int parchTotal = PlayerPrefs.GetInt($"{slot}_parchmentsTotal", 0);
        int trophTotal = PlayerPrefs.GetInt($"{slot}_trophiesTotal", 0);
        int medalTotal = PlayerPrefs.GetInt($"{slot}_medalsTotal", 0);

        if (parchmentText != null) parchmentText.text = $"{parchTotal}/{parchmentsGoal}";
        if (trophyText != null)    trophyText.text    = $"{trophTotal}/{trophiesGoal}";
        if (medalText != null)     medalText.text     = $"{medalTotal}/{medalsGoal}";

        // Kills enemigos
        if (enemyKillStats != null)
        {
            for (int i = 0; i < enemyKillStats.Length; i++)
            {
                var ui = enemyKillStats[i];
                if (ui.text == null) continue;
                int count = ObtenerKills(slot, ui.tipo);
                ui.text.text = ui.goal > 0 ? $"{count}/{ui.goal}" : $"{count}";
            }
        }
    }
}
