using System;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RankSystem : MonoBehaviour
{
    public static RankSystem Instance { get; private set; }

    public enum TriggerType { Parchment, Medal, LegendKey }

    public enum RankId
    {
        R10_Caminante,
        R20_CaballeroVerdad,
        R40_ElegidoEstrellas,
        R50_AlmaVictoriosa,
        R60_VencedorTiempo,
        R70_CaballeroPaz,
        R80_MaestroTableros,
        R100_MaestroSilencio,
        R110_MaestroIndomable,
        R120_GuardianReino,
        R130_GuardianUmbral,
        R150_GuardianLuz,
        R160_AprendizRey,
        R170_ReyEstrellas,
        R180_ReyMisterio,
        R200_ReyLibre,
        R204_ReyCoronado
    }

    [Serializable]
    public struct RankRule
    {
        public int nivelLogico;
        public TriggerType trigger;
        public RankId rank;
        public AchievementId achievement;  // Para Honores/Notifs. Puedes tener los paneles mapeados por ID.
        // Nota: No usamos 'title' para UI. Si quieres, puedes dejarlo fuera.
    }

    // Mapa por nivel lógico + tipo de objeto (en ORDEN ASCENDENTE)
    public RankRule[] rules = new RankRule[]
    {
        new(){nivelLogico=10,  trigger=TriggerType.Parchment, rank=RankId.R10_Caminante,        achievement=AchievementId.Rank_CaminanteMisterio},
        new(){nivelLogico=20,  trigger=TriggerType.Parchment, rank=RankId.R20_CaballeroVerdad,  achievement=AchievementId.Rank_CaballeroVerdad},
        new(){nivelLogico=40,  trigger=TriggerType.Medal,     rank=RankId.R40_ElegidoEstrellas, achievement=AchievementId.Rank_ElegidoEstrellas},
        new(){nivelLogico=50,  trigger=TriggerType.Parchment, rank=RankId.R50_AlmaVictoriosa,   achievement=AchievementId.Rank_AlmaVictoriosa},
        new(){nivelLogico=60,  trigger=TriggerType.Medal,     rank=RankId.R60_VencedorTiempo,   achievement=AchievementId.Rank_VencedorTiempo},
        new(){nivelLogico=70,  trigger=TriggerType.Parchment, rank=RankId.R70_CaballeroPaz,     achievement=AchievementId.Rank_CaballeroPaz},
        new(){nivelLogico=80,  trigger=TriggerType.Parchment, rank=RankId.R80_MaestroTableros,  achievement=AchievementId.Rank_MaestroTableros},
        new(){nivelLogico=100, trigger=TriggerType.Medal,     rank=RankId.R100_MaestroSilencio, achievement=AchievementId.Rank_MaestroSilencio},
        new(){nivelLogico=110, trigger=TriggerType.Parchment, rank=RankId.R110_MaestroIndomable,achievement=AchievementId.Rank_MaestroIndomable},
        new(){nivelLogico=120, trigger=TriggerType.Parchment, rank=RankId.R120_GuardianReino,   achievement=AchievementId.Rank_GuardianReino},
        new(){nivelLogico=130, trigger=TriggerType.Medal,     rank=RankId.R130_GuardianUmbral,  achievement=AchievementId.Rank_GuardianUmbral},
        new(){nivelLogico=150, trigger=TriggerType.Parchment, rank=RankId.R150_GuardianLuz,     achievement=AchievementId.Rank_GuardianLuz},
        new(){nivelLogico=160, trigger=TriggerType.Parchment, rank=RankId.R160_AprendizRey,     achievement=AchievementId.Rank_AprendizRey},
        new(){nivelLogico=170, trigger=TriggerType.Medal,     rank=RankId.R170_ReyEstrellas,    achievement=AchievementId.Rank_ReyEstrellas},
        new(){nivelLogico=180, trigger=TriggerType.Parchment, rank=RankId.R180_ReyMisterio,     achievement=AchievementId.Rank_ReyMisterio},
        new(){nivelLogico=200, trigger=TriggerType.Medal,     rank=RankId.R200_ReyLibre,        achievement=AchievementId.Rank_ReyLibre},
        new(){nivelLogico=204, trigger=TriggerType.LegendKey, rank=RankId.R204_ReyCoronado,     achievement=AchievementId.Rank_ReyCoronado},
    };

    [Header("UI Stadistics")]
    [Tooltip("17 paneles/TMP en orden de la lista rules (10→204). Solo se activará uno.")]
    public GameObject[] rankPanels = new GameObject[17];

    [Tooltip("Panel/TMP 'Sin rango actual'")]
    public GameObject noRankPanel;

    const int BUILD_OFFSET = 14;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Merge de UI y destrucción de duplicado
            Instance.AbsorbUIFrom(this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshUI();
    }

    public void AbsorbUIFrom(RankSystem other)
    {
        if (other == null) return;
        if (other.rankPanels != null && other.rankPanels.Length == rankPanels.Length)
            rankPanels = other.rankPanels;
        if (other.noRankPanel != null)
            noRankPanel = other.noRankPanel;

        RefreshUI();
    }

    // ========= Eventos de recolección (llamados desde Parchment/Medal/LegendKey) =========
    public void ReportParchment() => HandleTrigger(TriggerType.Parchment);
    public void ReportMedal() => HandleTrigger(TriggerType.Medal);
    public void ReportLegendKey() => HandleTrigger(TriggerType.LegendKey);

    private void HandleTrigger(TriggerType t)
    {
        int buildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        // 1) Intentar LevelIdentity (origen de verdad)
        int nivelLogico = LevelIdentity.HasNivelLogico
            ? LevelIdentity.NivelLogico
            : (buildIndex - BUILD_OFFSET); // fallback defensivo

        // 2) (Opcional) mantener PlayerPrefs coherente
        if (PlayerPrefs.GetInt("nivelActivo", -9999) != nivelLogico)
        {
            PlayerPrefs.SetInt("nivelActivo", nivelLogico);
            PlayerPrefs.Save();
        }

        // 3) Buscar la regla
        int idx = System.Array.FindIndex(rules, r => r.nivelLogico == nivelLogico && r.trigger == t);
        if (idx < 0)
        {
            Debug.LogWarning($"[RankSystem] No hay regla para trigger={t} en nivelLogico={nivelLogico} (build={buildIndex}).");
            return;
        }

        string slot = PlayerPrefs.GetString("slotActivo", "slot1");

        // 4) Desbloqueo (idempotente)
        /*AchievementsManager.TryUnlock(slot, rules[idx].achievement, 0, null, null);*/
        var titulo = "Ascenso de Rango";
        var cuerpo = $"Has alcanzado: {rules[idx].rank}";
        AchievementsManager.TryUnlock(slot, rules[idx].achievement, 0, titulo, cuerpo);

        // 5) Guardar máximo rango alcanzado
        int maxIdx = PlayerPrefs.GetInt($"{slot}_rankMaxIndex", -1);
        if (idx > maxIdx)
        {
            PlayerPrefs.SetInt($"{slot}_rankMaxIndex", idx);
            PlayerPrefs.Save();
        }

        // 6) Refrescar UI
        RefreshUI();

        Debug.Log($"[RankSystem] ✅ Trigger {t} aplicado en nivelLogico={nivelLogico} → regla[{idx}]={rules[idx].rank}");
    }



    // ========= UI: enciende SOLO el panel del rango mayor =========
    public void RefreshUI()
    {
        string slot = PlayerPrefs.GetString("slotActivo", "slot1");

        int maxIdx;
        if (PlayerPrefs.HasKey($"{slot}_rankMaxIndex"))
            maxIdx = PlayerPrefs.GetInt($"{slot}_rankMaxIndex");
        else
            maxIdx = ComputeMaxFromAchievements(slot); // devuelve -1 si no hay ninguno

        ShowOnly(maxIdx);
    }

    private void ShowOnly(int idx)
    {
        // Apaga todos
        if (rankPanels != null)
            for (int i = 0; i < rankPanels.Length; i++)
                if (rankPanels[i] != null) rankPanels[i].SetActive(false);

        // Clamp defensivo
        if (idx < 0 || idx >= rankPanels.Length)
        {
            if (noRankPanel != null) noRankPanel.SetActive(true);
            return;
        }

        // Enciende solo el panel del rango
        if (rankPanels[idx] != null) rankPanels[idx].SetActive(true);
        if (noRankPanel != null) noRankPanel.SetActive(false);
    }


    // Si no existe rankMaxIndex (caso legacy), derivarlo de los achievements ya desbloqueados
    private int ComputeMaxFromAchievements(string slot)
    {
        int max = -1;
        for (int i = 0; i < rules.Length; i++)
        {
            if (PlayerPrefs.GetInt($"{slot}_ach_{rules[i].achievement}", 0) == 1)
                max = i;
        }
        if (max >= 0) PlayerPrefs.SetInt($"{slot}_rankMaxIndex", max);
        return max;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RefreshUI(); // vuelve a prender SOLO el panel del rango máximo
    }

}