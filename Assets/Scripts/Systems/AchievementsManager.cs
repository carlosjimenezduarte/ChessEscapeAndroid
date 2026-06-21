using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
public enum MasterKeyId
    {
        CrownOfTheKey = 1,
        SoulColumn = 2,
        ToothOfTheKingdom = 3
    }

public enum AchievementId
{
    PawnIsGold,
    Liberador,
    Keys10, Keys50, Keys100, Keys250, Keys500,
    Diamonds10, Diamonds25, Diamonds50, Diamonds100, Diamonds200,
    Crown10, Crown20, Crown30, Crown40, Crown50,
    Chest10, Chest25, Chest50, Chest75, Chest100,
    Bag10, Bag50, Bag100, Bag200, Bag300,
    RealCoin10, RealCoin100, RealCoin250, RealCoin500, RealCoin1000, 
    BlackQueen1, BlackQueen3, BlackQueen5, BlackQueen7, BlackQueen10,
    BlackRook1, BlackRook3, BlackRook5, BlackRook7, BlackRook10,
    BlackBishop1, BlackBishop3, BlackBishop5, BlackBishop7, BlackBishop10,
    BlackKnight1, BlackKnight3, BlackKnight5, BlackKnight7, BlackKnight10,
    RedQueen1, RedQueen15, RedQueen30, RedQueen40, RedQueen50,
    RedRook1, RedRook10, RedRook20, RedRook30, RedRook50,
    RedBishop1, RedBishop5, RedBishop15, RedBishop20, RedBishop30,
    RedKnight1, RedKnight5,RedKnight15, RedKnight20, RedKnight30,
    RedPawn1, RedPawn25, RedPawn50, RedPawn75, RedPawn100,
    FirstTrophy, SecondTrophy, ThirdTrophy, FourthTrophy,
    MasterKey1, MasterKey2, MasterKey3,
    
    Trophy1, Trophy2, Trophy3, Trophy4,
    Score1, Score2, Score3, Score4, Score5,
    Score6, Score7, Score8, Score9, Score10,
    Score11, Score12, Score13, Score14, Score15,
    
  

   
    Rank_CaminanteMisterio,
    Rank_CaballeroVerdad,
    Rank_ElegidoEstrellas,
    Rank_AlmaVictoriosa,
    Rank_VencedorTiempo,
    Rank_CaballeroPaz,
    Rank_MaestroTableros,
    Rank_MaestroSilencio,
    Rank_MaestroIndomable,
    Rank_GuardianReino,
    Rank_GuardianUmbral,
    Rank_GuardianLuz,
    Rank_AprendizRey,
    Rank_ReyEstrellas,
    Rank_ReyMisterio,
    Rank_ReyLibre,
    Rank_ReyCoronado
}

public static class AchievementsManager
{
    // Config
    private const int SCORE_PAWN_IS_GOLD = 1000;

    private const int SCORE_LIBERADOR = 5000;
    private const int OFFSET_NIVELES = 14;
    public static int Liberador_LevelGate = -1;

    // Si quieres limitar el logro a un nivel lógico concreto, pon su id aquí.
    // -1 => válido en cualquier nivel.
    public static int PawnIsGold_LevelGate = -1;

    // --- API pública ---
    public static bool IsUnlocked(string slotId, AchievementId id)
    {
        return PlayerPrefs.GetInt(KeyAch(slotId, id), 0) == 1;
    }

    /// <summary>Intento de desbloqueo: aplica gating, “solo una vez”, suma score, dispara notificación, refresca Honors.</summary>
    public static bool TryUnlock(string slotId, AchievementId id, int scoreReward = 0, string notifTitle = null, string notifBody = null)
    {
        if (IsUnlocked(slotId, id))
        {
            Debug.Log($"[Ach] {id} ya estaba desbloqueado para {slotId}.");
            return false;
        }

        PlayerPrefs.SetInt(KeyAch(slotId, id), 1);

        // Score de logro (si aplica)
        if (scoreReward != 0)
        {
            int totalScore = PlayerPrefs.GetInt($"{slotId}_scoreTotal", 0) + scoreReward;
            PlayerPrefs.SetInt($"{slotId}_scoreTotal", totalScore);
            // (Opcional) tracking específico de score por logros
            int achScore = PlayerPrefs.GetInt($"{slotId}_scoreFromAchievements", 0) + scoreReward;
            PlayerPrefs.SetInt($"{slotId}_scoreFromAchievements", achScore);
        }

        PlayerPrefs.Save();

        // Notificación persistente (queda hasta que el usuario la cierre)
        if (!string.IsNullOrEmpty(notifTitle))
        {
            NotificationCenter.Instance?.OnAchievementUnlocked(
                slotId,
                id,
                notifTitle,
                notifBody ?? string.Empty
            );
        }


        // Aviso opcional para paneles de Honors que “escuchen”
        OnAchievementUnlocked?.Invoke(slotId, id);

        Debug.Log($"[Ach] ✅ Desbloqueado {id} para {slotId}. Score+={scoreReward}");
        return true;
    }

    // --- Logro específico: El Peón vale Oro ---
    public static void ReportPawnCoronation()
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");

        // Gate por nivel, si lo quieres activo
        if (PawnIsGold_LevelGate >= 0)
        {
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nivelLogico = sceneIndex - OFFSET_NIVELES;
            if (nivelLogico != PawnIsGold_LevelGate)
            {
                Debug.Log($"[Ach] Coronación detectada pero nivel {nivelLogico} != gate {PawnIsGold_LevelGate}. No desbloquea.");
                return;
            }
        }

        // Desbloquear (solo una vez)
        TryUnlock(
            slotId,
            AchievementId.PawnIsGold,
            SCORE_PAWN_IS_GOLD,
            "Logro desbloqueado: El Peón vale Oro",
            $"+{SCORE_PAWN_IS_GOLD} puntos por coronar al Peón."
        );
    }

    public static void ReportLiberador()
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");

        // (Opcional) Gateo por nivel — actualmente desactivado:
        if (Liberador_LevelGate >= 0)
        {
            int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            const int OFFSET_NIVELES = 8;
            int nivelLogico = sceneIndex - OFFSET_NIVELES;
            if (nivelLogico != Liberador_LevelGate)
            {
                Debug.Log($"[Ach] Liberador detectado pero nivel {nivelLogico} != gate {Liberador_LevelGate}. No desbloquea.");
                return;
            }
        }

        TryUnlock(
            slotId,
            AchievementId.Liberador,
            SCORE_LIBERADOR,
            "Logro desbloqueado: Liberador",
            $"+{SCORE_LIBERADOR} puntos por liberar a todas las fichas aliadas en un mapa."
        );
    }

    // --- Evento para UIs (Honors) que quieran reaccionar al vuelo ---
    public static event Action<string, AchievementId> OnAchievementUnlocked;

    // --- Helpers ---
    private static string KeyAch(string slotId, AchievementId id) => $"{slotId}_ach_{id}";

    // ----- Hitos de llaves -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] KEY_MILESTONES =
    {
    (AchievementId.Keys10,   10,   0, "Logro desbloqueado: 10 llaves",   "+0 puntos por alcanzar 10 llaves."),
    (AchievementId.Keys50,   50,   0, "Logro desbloqueado: 50 llaves",   "+0 puntos por alcanzar 50 llaves."),
    (AchievementId.Keys100,  100,  0, "Logro desbloqueado: 100 llaves",  "+0 puntos por alcanzar 100 llaves."),
    (AchievementId.Keys250,  250,  0, "Logro desbloqueado: 250 llaves",  "+0 puntos por alcanzar 250 llaves."),
    (AchievementId.Keys500,  500,  0, "Logro desbloqueado: 500 llaves",  "+0 puntos por alcanzar 500 llaves."),
    };


    // ----- Verificador único: llámalo pasándole el total global de llaves -----
    public static void ReportKeysProgress(int totalKeys)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in KEY_MILESTONES)
        {
            if (totalKeys >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
            }
        }

    }

    public static void ReportTrophyPickupForCurrentLevel()
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");

        // Detectar nivel lógico (usa nivelActivo si existe; si no, cae al índice de escena - OFFSET_NIVELES)
        int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nivelLogico = PlayerPrefs.GetInt("nivelActivo", sceneIndex - OFFSET_NIVELES);

        switch (nivelLogico)
        {
            case 30: // Trofeo Verde = +25.000
                TryUnlock(
                    slotId,
                    AchievementId.FirstTrophy,
                    0,
                    "Felicidades: obtuviste el primer Trofeo",
                    "Trofeo Verde conseguido."
                );
                TryUnlock(
                    slotId,
                    AchievementId.Trophy1,
                    25000,
                    "Felicidades: +25.000 Score",
                    "Bonificación por el Trofeo Verde."
                );
                break;

            case 90: // Trofeo Azul = +50.000
                TryUnlock(
                    slotId,
                    AchievementId.SecondTrophy,
                    0,
                    "Felicidades: obtuviste el segundo Trofeo",
                    "Trofeo Azul conseguido."
                );
                TryUnlock(
                    slotId,
                    AchievementId.Trophy2,
                    50000,
                    "Felicidades: +50.000 Score",
                    "Bonificación por el Trofeo Azul."
                );
                break;

            case 140: // Trofeo Rojo = +100.000
                TryUnlock(
                    slotId,
                    AchievementId.ThirdTrophy,
                    0,
                    "Felicidades: obtuviste el tercer Trofeo",
                    "Trofeo Rojo conseguido."
                );
                TryUnlock(
                    slotId,
                    AchievementId.Trophy3,
                    100000,
                    "Felicidades: +100.000 Score",
                    "Bonificación por el Trofeo Rojo."
                );
                break;

            case 190: // Trofeo Dorado = +200.000
                TryUnlock(
                    slotId,
                    AchievementId.FourthTrophy,
                    0,
                    "Felicidades: obtuviste el cuarto Trofeo",
                    "Trofeo Dorado conseguido."
                );
                TryUnlock(
                    slotId,
                    AchievementId.Trophy4,
                    200000,
                    "Felicidades: +200.000 Score",
                    "Bonificación por el Trofeo Dorado."
                );
                break;

            default:
                Debug.Log($"[Ach] TrophyPickup en nivel lógico {nivelLogico} sin configuración de bono.");
                break;
        }
    }

    // ----- Hitos de diamantes -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] DIAMOND_MILESTONES =
    {
    (AchievementId.Diamonds10,   10,   0, "Logro desbloqueado: 10 diamantes",   "+0 puntos por alcanzar 10 diamantes."),
    (AchievementId.Diamonds25,   25,   0, "Logro desbloqueado: 25 diamantes",   "+0 puntos por alcanzar 25 diamantes."),
    (AchievementId.Diamonds50,   50,   0, "Logro desbloqueado: 50 diamantes",   "+0 puntos por alcanzar 50 diamantes."),
    (AchievementId.Diamonds100, 100,   0, "Logro desbloqueado: 100 diamantes",  "+0 puntos por alcanzar 100 diamantes."),
    (AchievementId.Diamonds200, 200,   0, "Logro desbloqueado: 200 diamantes",  "+0 puntos por alcanzar 200 diamantes."),
    };


    // ----- Verificador único: llámalo pasándole el total global de diamantes -----
    public static void ReportDiamondsProgress(int totalDiamonds)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in DIAMOND_MILESTONES)
        {
            if (totalDiamonds >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
            }
        }

    }

    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] REALCOIN_MILESTONES =
{
    (AchievementId.RealCoin10,    10,    0, "Logro desbloqueado: 10 Monedas Reales",    "+0 puntos por alcanzar 10 monedas."),
    (AchievementId.RealCoin100,   100,   0, "Logro desbloqueado: 100 Monedas Reales",   "+0 puntos por alcanzar 100 monedas."),
    (AchievementId.RealCoin250,   250,   0, "Logro desbloqueado: 250 Monedas Reales",   "+0 puntos por alcanzar 250 monedas."),
    (AchievementId.RealCoin500,   500,   0, "Logro desbloqueado: 500 Monedas Reales",   "+0 puntos por alcanzar 500 monedas."),
    (AchievementId.RealCoin1000,  1000,  0, "Logro desbloqueado: 1000 Monedas Reales",  "+0 puntos por alcanzar 1000 monedas."),
};

    // ----- Verificador único: pásale el total global de Monedas Reales -----
    public static void ReportRealCoinProgress(int totalRealCoins)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in REALCOIN_MILESTONES)
        {
            if (totalRealCoins >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
            }
        }
    }

    // ----- Hitos de Bolsa (Bag) -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] BAG_MILESTONES =
    {
        (AchievementId.Bag10,   10,   0, "Logro desbloqueado: 10 Bolsas",   "+0 puntos por alcanzar 10 bolsas."),
        (AchievementId.Bag50,   50,   0, "Logro desbloqueado: 50 Bolsas",   "+0 puntos por alcanzar 50 bolsas."),
        (AchievementId.Bag100,  100,  0, "Logro desbloqueado: 100 Bolsas",  "+0 puntos por alcanzar 100 bolsas."),
        (AchievementId.Bag200,  200,  0, "Logro desbloqueado: 200 Bolsas",  "+0 puntos por alcanzar 200 bolsas."),
        (AchievementId.Bag300,  300,  0, "Logro desbloqueado: 300 Bolsas",  "+0 puntos por alcanzar 300 bolsas."),
    };

    // ----- Verificador único: pásale el total global de Bolsas -----
    public static void ReportBagsProgress(int totalBags)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in BAG_MILESTONES)
        {
            if (totalBags >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
            }
        }

    }

    // ----- Hitos de Cofre (Chest) -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] CHEST_MILESTONES =
{
    (AchievementId.Chest10,   10,   0, "Logro desbloqueado: 10 Cofres",   "+0 puntos por alcanzar 10 cofres."),
    (AchievementId.Chest25,   25,   0, "Logro desbloqueado: 25 Cofres",   "+0 puntos por alcanzar 25 cofres."),
    (AchievementId.Chest50,   50,   0, "Logro desbloqueado: 50 Cofres",   "+0 puntos por alcanzar 50 cofres."),
    (AchievementId.Chest75,   75,   0, "Logro desbloqueado: 75 Cofres",   "+0 puntos por alcanzar 75 cofres."),
    (AchievementId.Chest100, 100,   0, "Logro desbloqueado: 100 Cofres",  "+0 puntos por alcanzar 100 cofres."),
};
    // ----- Verificador único: pásale el total global de Cofres -----
    public static void ReportChestsProgress(int totalChests)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in CHEST_MILESTONES)
        {
            if (totalChests >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
            }
        }
    }
    // ----- Hitos de Corona (Crown) -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] CROWN_MILESTONES =
    {
    (AchievementId.Crown10,  10,  0, "Logro desbloqueado: 10 Coronas",  "+0 puntos por alcanzar 10 coronas."),
    (AchievementId.Crown20,  20,  0, "Logro desbloqueado: 20 Coronas",  "+0 puntos por alcanzar 20 coronas."),
    (AchievementId.Crown30,  30,  0, "Logro desbloqueado: 30 Coronas",  "+0 puntos por alcanzar 30 coronas."),
    (AchievementId.Crown40,  40,  0, "Logro desbloqueado: 40 Coronas",  "+0 puntos por alcanzar 40 coronas."),
    (AchievementId.Crown50,  50,  0, "Logro desbloqueado: 50 Coronas",  "+0 puntos por alcanzar 50 coronas."),
};

    // ----- Verificador único: pásale el total global de Coronas -----
    public static void ReportCrownsProgress(int totalCrowns)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in CROWN_MILESTONES)
        {
            if (totalCrowns >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
            }
        }
    }


    // ----- Hitos de kills: Peón Rojo -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] RED_PAWN_MILESTONES =
    {
    (AchievementId.RedPawn1,   1,   0, "Logro: 1 Peón Rojo eliminado",    "+0 puntos por 1 kill."),
    (AchievementId.RedPawn25,  25,  0, "Logro: 25 Peones Rojos eliminados", "+0 puntos por 25 kills."),
    (AchievementId.RedPawn50,  50,  0, "Logro: 50 Peones Rojos eliminados", "+0 puntos por 50 kills."),
    (AchievementId.RedPawn75,  75,  0, "Logro: 75 Peones Rojos eliminados", "+0 puntos por 75 kills."),
    (AchievementId.RedPawn100, 100, 0, "Logro: 100 Peones Rojos eliminados", "+0 puntos por 100 kills."),
};

    // ----- Verificador único: pásale el total global de kills de Peón Rojo -----
    public static void ReportRedPawnKillsProgress(int totalKills)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in RED_PAWN_MILESTONES)
            if (totalKills >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ----- Hitos de kills: Caballo Rojo -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] RED_KNIGHT_MILESTONES =
    {
        (AchievementId.RedKnight1,   1,   0, "Logro: 1 Caballo Rojo eliminado",    "+0 puntos por 1 kill."),
        (AchievementId.RedKnight5,   5,   0, "Logro: 5 Caballos Rojos eliminados", "+0 puntos por 5 kills."),
        (AchievementId.RedKnight15, 15,   0, "Logro: 15 Caballos Rojos eliminados","+0 puntos por 15 kills."),
        (AchievementId.RedKnight20, 20,   0, "Logro: 20 Caballos Rojos eliminados","+0 puntos por 20 kills."),
        (AchievementId.RedKnight30, 30,   0, "Logro: 30 Caballos Rojos eliminados","+0 puntos por 30 kills."),
    };

    // ----- Verificador único: pásale el total global de kills de Caballo Rojo -----
    public static void ReportRedKnightKillsProgress(int totalKills)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in RED_KNIGHT_MILESTONES)
            if (totalKills >= m.threshold && !IsUnlocked(slotId, m.id))

                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ----- Hitos de kills: Alfil Rojo -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] RED_BISHOP_MILESTONES =
    {
    (AchievementId.RedBishop1,   1,   0, "Logro: 1 Alfil Rojo eliminado",     "+0 puntos por 1 kill."),
    (AchievementId.RedBishop5,   5,   0, "Logro: 5 Alfiles Rojos eliminados", "+0 puntos por 5 kills."),
    (AchievementId.RedBishop15, 15,   0, "Logro: 15 Alfiles Rojos eliminados","+0 puntos por 15 kills."),
    (AchievementId.RedBishop20, 20,   0, "Logro: 20 Alfiles Rojos eliminados","+0 puntos por 20 kills."),
    (AchievementId.RedBishop30, 30,   0, "Logro: 30 Alfiles Rojos eliminados","+0 puntos por 30 kills."),
    };


    // ----- Verificador único: pásale el total global de kills de Alfil Rojo -----
    public static void ReportRedBishopKillsProgress(int totalKills)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in RED_BISHOP_MILESTONES)
            if (totalKills >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ----- Hitos de kills: Torre Roja -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] RED_ROOK_MILESTONES =
    {
    (AchievementId.RedRook1,   1,   0, "Logro: 1 Torre Roja eliminada",    "+0 puntos por 1 kill."),
    (AchievementId.RedRook10, 10,   0, "Logro: 10 Torres Rojas eliminadas","+0 puntos por 10 kills."),
    (AchievementId.RedRook20, 20,   0, "Logro: 20 Torres Rojas eliminadas","+0 puntos por 20 kills."),
    (AchievementId.RedRook30, 30,   0, "Logro: 30 Torres Rojas eliminadas","+0 puntos por 30 kills."),
    (AchievementId.RedRook50, 50,   0, "Logro: 50 Torres Rojas eliminadas","+0 puntos por 50 kills."),
};


    public static void ReportRedRookKillsProgress(int totalKills)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in RED_ROOK_MILESTONES)
            if (totalKills >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ----- Hitos de kills: Reina Roja -----
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] RED_QUEEN_MILESTONES =
    {
    (AchievementId.RedQueen1,   1,   0, "Logro: 1 Reina Roja eliminada",     "+0 puntos por 1 kill."),
    (AchievementId.RedQueen15, 15,   0, "Logro: 15 Reinas Rojas eliminadas", "+0 puntos por 15 kills."),
    (AchievementId.RedQueen30, 30,   0, "Logro: 30 Reinas Rojas eliminadas", "+0 puntos por 30 kills."),
    (AchievementId.RedQueen40, 40,   0, "Logro: 40 Reinas Rojas eliminadas", "+0 puntos por 40 kills."),
    (AchievementId.RedQueen50, 50,   0, "Logro: 50 Reinas Rojas eliminadas", "+0 puntos por 50 kills."),
};

    public static void ReportRedQueenKillsProgress(int totalKills)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in RED_QUEEN_MILESTONES)
            if (totalKills >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ===== KILLS NEGROS: TORRE =====
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] BLACK_ROOK_MILESTONES =
    {
    (AchievementId.BlackRook1,  1, 0, "Logro: 1 Torre Negra eliminada",   "+0 puntos por 1 kill."),
    (AchievementId.BlackRook3,  3, 0, "Logro: 3 Torres Negras eliminadas","+0 puntos por 3 kills."),
    (AchievementId.BlackRook5,  5, 0, "Logro: 5 Torres Negras eliminadas","+0 puntos por 5 kills."),
    (AchievementId.BlackRook7,  7, 0, "Logro: 7 Torres Negras eliminadas","+0 puntos por 7 kills."),
    (AchievementId.BlackRook10,10, 0, "Logro: 10 Torres Negras eliminadas","+0 puntos por 10 kills."),
};
    public static void ReportBlackRookKillsProgress(int total)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in BLACK_ROOK_MILESTONES)
            if (total >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ===== KILLS NEGROS: ALFIL =====
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] BLACK_BISHOP_MILESTONES =
    {
    (AchievementId.BlackBishop1,  1, 0, "Logro: 1 Alfil Negro eliminado",   "+0 puntos por 1 kill."),
    (AchievementId.BlackBishop3,  3, 0, "Logro: 3 Alfiles Negros eliminados","+0 puntos por 3 kills."),
    (AchievementId.BlackBishop5,  5, 0, "Logro: 5 Alfiles Negros eliminados","+0 puntos por 5 kills."),
    (AchievementId.BlackBishop7,  7, 0, "Logro: 7 Alfiles Negros eliminados","+0 puntos por 7 kills."),
    (AchievementId.BlackBishop10,10,0, "Logro: 10 Alfiles Negros eliminados","+0 puntos por 10 kills."),
};
    public static void ReportBlackBishopKillsProgress(int total)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in BLACK_BISHOP_MILESTONES)
            if (total >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ===== KILLS NEGROS: CABALLO =====
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] BLACK_KNIGHT_MILESTONES =
    {
    (AchievementId.BlackKnight1,  1, 0, "Logro: 1 Caballo Negro eliminado",   "+0 puntos por 1 kill."),
    (AchievementId.BlackKnight3,  3, 0, "Logro: 3 Caballos Negros eliminados","+0 puntos por 3 kills."),
    (AchievementId.BlackKnight5,  5, 0, "Logro: 5 Caballos Negros eliminados","+0 puntos por 5 kills."),
    (AchievementId.BlackKnight7,  7, 0, "Logro: 7 Caballos Negros eliminados","+0 puntos por 7 kills."),
    (AchievementId.BlackKnight10,10,0, "Logro: 10 Caballos Negros eliminados","+0 puntos por 10 kills."),
};
    public static void ReportBlackKnightKillsProgress(int total)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in BLACK_KNIGHT_MILESTONES)
            if (total >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);
    }

    // ===== KILLS NEGROS: REINA =====
    static readonly (AchievementId id, int threshold, int reward, string title, string body)[] BLACK_QUEEN_MILESTONES =
    {
    (AchievementId.BlackQueen1,  1, 0, "Logro: 1 Reina Negra eliminada",   "+0 puntos por 1 kill."),
    (AchievementId.BlackQueen3,  3, 0, "Logro: 3 Reinas Negras eliminadas","+0 puntos por 3 kills."),
    (AchievementId.BlackQueen5,  5, 0, "Logro: 5 Reinas Negras eliminadas","+0 puntos por 5 kills."),
    (AchievementId.BlackQueen7,  7, 0, "Logro: 7 Reinas Negras eliminadas","+0 puntos por 7 kills."),
    (AchievementId.BlackQueen10,10,0, "Logro: 10 Reinas Negras eliminadas","+0 puntos por 10 kills."),
};
    public static void ReportBlackQueenKillsProgress(int total)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in BLACK_QUEEN_MILESTONES)
            if (total >= m.threshold && !IsUnlocked(slotId, m.id))
                TryUnlock(slotId, m.id, m.reward, m.title, m.body);

    }

    // ----- Hitos de Score -----
    static readonly (AchievementId id, int threshold, string title, string body)[] SCORE_MILESTONES =
    {

    (AchievementId.Score1,   10000,   "Logro desbloqueado: 10.000 puntos de Score",  "Has alcanzado 10.000 puntos de Score."),
    (AchievementId.Score2,   20000,   "Logro desbloqueado: 20.000 puntos de Score",  "Has alcanzado 20.000 puntos de Score."),
    (AchievementId.Score3,   30000,   "Logro desbloqueado: 30.000 puntos de Score",  "Has alcanzado 30.000 puntos de Score."),
    (AchievementId.Score4,   40000,   "Logro desbloqueado: 40.000 puntos de Score",  "Has alcanzado 40.000 puntos de Score."),
    (AchievementId.Score5,   50000,   "Logro desbloqueado: 50.000 puntos de Score",  "Has alcanzado 50.000 puntos de Score."),
    (AchievementId.Score6,   60000,   "Logro desbloqueado: 60.000 puntos de Score",  "Has alcanzado 60.000 puntos de Score."),
    (AchievementId.Score7,   70000,   "Logro desbloqueado: 70.000 puntos de Score",  "Has alcanzado 70.000 puntos de Score."),
    (AchievementId.Score8,   80000,   "Logro desbloqueado: 80.000 puntos de Score",  "Has alcanzado 80.000 puntos de Score."),
    (AchievementId.Score9,   90000,   "Logro desbloqueado: 90.000 puntos de Score",  "Has alcanzado 90.000 puntos de Score."),
    (AchievementId.Score10, 100000,   "Logro desbloqueado: 100.000 puntos de Score", "Has alcanzado 100.000 puntos de Score."),
    (AchievementId.Score11, 120000,   "Logro desbloqueado: 120.000 puntos de Score", "Has alcanzado 120.000 puntos de Score."),
    (AchievementId.Score12, 140000,   "Logro desbloqueado: 140.000 puntos de Score", "Has alcanzado 140.000 puntos de Score."),
    (AchievementId.Score13, 160000,   "Logro desbloqueado: 160.000 puntos de Score", "Has alcanzado 160.000 puntos de Score."),
    (AchievementId.Score14, 180000,   "Logro desbloqueado: 180.000 puntos de Score", "Has alcanzado 180.000 puntos de Score."),
    (AchievementId.Score15, 200000,   "Logro desbloqueado: 200.000 puntos de Score", "Has alcanzado 200.000 puntos de Score."),
};

    public static void ReportScoreProgress(int totalScore)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        foreach (var m in SCORE_MILESTONES)
        {
            if (totalScore >= m.threshold && !IsUnlocked(slotId, m.id))
            {
                TryUnlock(slotId, m.id, 0, m.title, m.body);
            }
        }
    }
    
    public static void ReportMasterKey(MasterKeyId which)
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");

        switch (which)
        {
            case MasterKeyId.CrownOfTheKey:
                TryUnlock(
                    slotId,
                    AchievementId.MasterKey1,
                    0,
                    "Logro desbloqueado: MasterKey I",
                    "Has obtenido la Corona de la Llave."
                );
                break;

            case MasterKeyId.SoulColumn:
                TryUnlock(
                    slotId,
                    AchievementId.MasterKey2,
                    0,
                    "Logro desbloqueado: MasterKey II",
                    "Has obtenido la Columna del Alma."
                );
                break;

            case MasterKeyId.ToothOfTheKingdom:
                TryUnlock(
                    slotId,
                    AchievementId.MasterKey3,
                    0,
                    "Logro desbloqueado: MasterKey III",
                    "Has obtenido el Diente del Reino."
                );
                break;
        }
    }








}
