using UnityEngine;
using UnityEngine.SceneManagement;

public static class RankSystemUtil
{
    public static readonly RankSystem.RankRule[] Rules = new RankSystem.RankRule[]
    {
        new(){nivelLogico=10,  trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R10_Caminante,        achievement=AchievementId.Rank_CaminanteMisterio},
        new(){nivelLogico=20,  trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R20_CaballeroVerdad,  achievement=AchievementId.Rank_CaballeroVerdad},
        new(){nivelLogico=40,  trigger=RankSystem.TriggerType.Medal,     rank=RankSystem.RankId.R40_ElegidoEstrellas, achievement=AchievementId.Rank_ElegidoEstrellas},
        new(){nivelLogico=50,  trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R50_AlmaVictoriosa,   achievement=AchievementId.Rank_AlmaVictoriosa},
        new(){nivelLogico=60,  trigger=RankSystem.TriggerType.Medal,     rank=RankSystem.RankId.R60_VencedorTiempo,   achievement=AchievementId.Rank_VencedorTiempo},
        new(){nivelLogico=70,  trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R70_CaballeroPaz,     achievement=AchievementId.Rank_CaballeroPaz},
        new(){nivelLogico=80,  trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R80_MaestroTableros,  achievement=AchievementId.Rank_MaestroTableros},
        new(){nivelLogico=100, trigger=RankSystem.TriggerType.Medal,     rank=RankSystem.RankId.R100_MaestroSilencio, achievement=AchievementId.Rank_MaestroSilencio},
        new(){nivelLogico=110, trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R110_MaestroIndomable,achievement=AchievementId.Rank_MaestroIndomable},
        new(){nivelLogico=120, trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R120_GuardianReino,   achievement=AchievementId.Rank_GuardianReino},
        new(){nivelLogico=130, trigger=RankSystem.TriggerType.Medal,     rank=RankSystem.RankId.R130_GuardianUmbral,  achievement=AchievementId.Rank_GuardianUmbral},
        new(){nivelLogico=150, trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R150_GuardianLuz,     achievement=AchievementId.Rank_GuardianLuz},
        new(){nivelLogico=160, trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R160_AprendizRey,     achievement=AchievementId.Rank_AprendizRey},
        new(){nivelLogico=170, trigger=RankSystem.TriggerType.Medal,     rank=RankSystem.RankId.R170_ReyEstrellas,    achievement=AchievementId.Rank_ReyEstrellas},
        new(){nivelLogico=180, trigger=RankSystem.TriggerType.Parchment, rank=RankSystem.RankId.R180_ReyMisterio,     achievement=AchievementId.Rank_ReyMisterio},
        new(){nivelLogico=200, trigger=RankSystem.TriggerType.Medal,     rank=RankSystem.RankId.R200_ReyLibre,        achievement=AchievementId.Rank_ReyLibre},
        new(){nivelLogico=204, trigger=RankSystem.TriggerType.LegendKey, rank=RankSystem.RankId.R204_ReyCoronado,     achievement=AchievementId.Rank_ReyCoronado},
    };

    const int BUILD_OFFSET = 14;

    public static void TriggerHeadless(RankSystem.TriggerType t)
    {
        int nivelLogico = LevelIdentity.HasNivelLogico
            ? LevelIdentity.NivelLogico
            : (SceneManager.GetActiveScene().buildIndex - BUILD_OFFSET);

        int idx = System.Array.FindIndex(Rules, r => r.nivelLogico == nivelLogico && r.trigger == t);
        if (idx < 0)
        {
            Debug.LogWarning($"[RankSystemUtil] No rule for trigger={t} @ nivelLogico={nivelLogico}");
            return;
        }

        string slot = PlayerPrefs.GetString("slotActivo", "slot1");

        // Notificación linda
        string titulo = "Ascenso de Rango";
        string cuerpo  = $"Has alcanzado: {Rules[idx].rank}";

        // Desbloqueo idempotente + notificación
        AchievementsManager.TryUnlock(slot, Rules[idx].achievement, 0, titulo, cuerpo);

        // Persistir max index para que la UI funcione cuando cargue Stadistics
        int maxIdx = PlayerPrefs.GetInt($"{slot}_rankMaxIndex", -1);
        if (idx > maxIdx)
        {
            PlayerPrefs.SetInt($"{slot}_rankMaxIndex", idx);
            PlayerPrefs.Save();
        }

        Debug.Log($"[RankSystemUtil] ✅ Ascenso {Rules[idx].rank} por {t} en nivel {nivelLogico}");
    }
}
