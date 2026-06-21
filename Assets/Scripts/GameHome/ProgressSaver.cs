using UnityEngine;

public class ProgressSaver : MonoBehaviour
{
    public enum EspecialTipo { Parchment, Trophy, Medal, MasterKey }
    // 🔹 Método central para progreso de niveles
    private static void GuardarResultado(
    string slotId, int levelId, int score, int keys,
    int diamonds, bool parchment, bool trophy, bool medal, bool masterKey)
    {
        string levelCompletedKey = $"{slotId}_level_{levelId}_completed";
        string scoreKey = $"{slotId}_level_{levelId}_score";
        string keysKey = $"{slotId}_level_{levelId}_keys";
        string diamondsKey = $"{slotId}_level_{levelId}_diamonds";

        // 🔹 Mejores por nivel (anti-farmeo)
        string bagsKey = $"{slotId}_level_{levelId}_bags";
        string crownsKey = $"{slotId}_level_{levelId}_crowns";
        string chestsKey = $"{slotId}_level_{levelId}_chests";
        string coinsKey = $"{slotId}_level_{levelId}_coins";

        // 🔹 NUEVO: kills de peón enemigo por nivel (anti-farmeo)
        string pawnKillsKey = $"{slotId}_level_{levelId}_killsPawn";

        // Previos
        int prevCompleted = PlayerPrefs.GetInt(levelCompletedKey, 0);
        int prevScoreBest = PlayerPrefs.GetInt(scoreKey, 0);
        int prevKeys = PlayerPrefs.GetInt(keysKey,   0);
        int prevDiamonds = PlayerPrefs.GetInt(diamondsKey, 0);
        int prevBags = PlayerPrefs.GetInt(bagsKey, 0);
        int prevCrowns = PlayerPrefs.GetInt(crownsKey, 0);
        int prevChests = PlayerPrefs.GetInt(chestsKey, 0);
        int prevCoins = PlayerPrefs.GetInt(coinsKey, 0);
        int prevPawnKills = PlayerPrefs.GetInt(pawnKillsKey, 0); // 👈

        // Intento actual (runtime)
        int runKeys = Mathf.Clamp(keys, 0, LevelProgress.Instance != null ? LevelProgress.Instance.maxKeys : 3);
        int runDiamonds = Mathf.Clamp(diamonds, 0, LevelProgress.Instance != null ? LevelProgress.Instance.maxDiamonds : 1);
        int runBags = (LevelProgress.Instance != null) ? LevelProgress.Instance.bagsCollected : 0;
        int runCrowns = (LevelProgress.Instance != null) ? LevelProgress.Instance.crownsCollected : 0;
        int runChests = (LevelProgress.Instance != null) ? LevelProgress.Instance.chestsCollected : 0;
        int runCoins = (LevelProgress.Instance != null) ? LevelProgress.Instance.coinsCollected : 0;
        int runPawnKills = (LevelProgress.Instance != null) ? LevelProgress.Instance.enemyPawnsKilled : 0; // 👈

        // Max por nivel (anti-farmeo)
        int finalKeys = Mathf.Max(prevKeys, runKeys);
        int finalDiamonds = Mathf.Max(prevDiamonds, runDiamonds);
        int finalBags = Mathf.Max(prevBags, runBags);
        int finalCrowns = Mathf.Max(prevCrowns, runCrowns);
        int finalChests = Mathf.Max(prevChests, runChests);
        int finalCoins = Mathf.Max(prevCoins, runCoins);
        int finalPawnKills = Mathf.Max(prevPawnKills, runPawnKills); // 👈

        // Deltas
        int deltaKeys = finalKeys - prevKeys;
        int deltaDiamonds = finalDiamonds - prevDiamonds;
        int deltaBags = finalBags - prevBags;
        int deltaCrowns = finalCrowns - prevCrowns;
        int deltaChests = finalChests - prevChests;
        int deltaCoins = finalCoins - prevCoins;
        int deltaPawnKills = finalPawnKills - prevPawnKills; // 👈

        Debug.Log($"[PS] Keys Δ{deltaKeys} | Diamonds Δ{deltaDiamonds} | Bags Δ{deltaBags} | Crowns Δ{deltaCrowns} | Chests Δ{deltaChests} | Coins Δ{deltaCoins} | PawnKills Δ{deltaPawnKills}");

        // 🏁 Si no mejora nada y el score tampoco mejora, salimos
        if (prevCompleted == 1 &&
            deltaKeys == 0 && deltaDiamonds == 0 &&
            deltaBags == 0 && deltaCrowns == 0 && deltaChests == 0 && deltaCoins == 0 &&
            deltaPawnKills == 0 && // 👈 considera kills
            score <= prevScoreBest)
        {
            Debug.Log($"⚠️ Nivel {levelId} ya completado en {slotId}. No hay mejora.");
            return;
        }

        // ✅ Guardar mejor score por nivel y sumar al Total solo el DELTA
        int newBestScore = Mathf.Max(prevScoreBest, score);
        int scoreDelta = newBestScore - prevScoreBest;

        PlayerPrefs.SetInt(levelCompletedKey, 1);
        PlayerPrefs.SetInt(scoreKey, newBestScore);
        PlayerPrefs.SetInt(keysKey, finalKeys);
        PlayerPrefs.SetInt(diamondsKey, finalDiamonds);

        // Guardar mejores por nivel de objetos de score
        PlayerPrefs.SetInt(bagsKey, finalBags);
        PlayerPrefs.SetInt(crownsKey, finalCrowns);
        PlayerPrefs.SetInt(chestsKey, finalChests);
        PlayerPrefs.SetInt(coinsKey, finalCoins);

        // 👇 NUEVO: mejores kills por nivel
        PlayerPrefs.SetInt(pawnKillsKey, finalPawnKills);

        // Awards por nivel
        PlayerPrefs.SetInt($"{slotId}_level_{levelId}_parchment", parchment ? 1 : 0);
        PlayerPrefs.SetInt($"{slotId}_level_{levelId}_trophy", trophy ? 1 : 0);
        PlayerPrefs.SetInt($"{slotId}_level_{levelId}_medal", medal ? 1 : 0);
        PlayerPrefs.SetInt($"{slotId}_level_{levelId}_masterKey", masterKey ? 1 : 0);

        // Config de nivel (para GameHome)
        if (LevelProgress.Instance != null)
        {
            PlayerPrefs.SetInt($"{slotId}_level_{levelId}_maxKeys", LevelProgress.Instance.maxKeys);
            PlayerPrefs.SetInt($"{slotId}_level_{levelId}_maxDiamonds", LevelProgress.Instance.maxDiamonds);
        }

        // 🧮 Acumulados globales
        if (scoreDelta > 0)
        {
            int totalScore = PlayerPrefs.GetInt($"{slotId}_scoreTotal", 0) + scoreDelta;
            PlayerPrefs.SetInt($"{slotId}_scoreTotal", totalScore);
            Debug.Log($"[PS] ScoreTotal +{scoreDelta} -> {totalScore}");
        }

        if (deltaKeys > 0 || deltaDiamonds > 0 || parchment || trophy || medal || masterKey)
            SumarAcumulados(slotId, deltaKeys, deltaDiamonds, parchment, trophy, medal, masterKey);

        // Stadistics objetos de score (Δ)
        if (deltaBags > 0) Stadistics.RegistrarObjeto(slotId, TipoObjetoScore.Bag, deltaBags);
        if (deltaBags > 0)
        {
            int totalBags = Stadistics.ObtenerConteo(slotId, TipoObjetoScore.Bag);
            AchievementsManager.ReportBagsProgress(totalBags);
        }

        if (deltaChests > 0) Stadistics.RegistrarObjeto(slotId, TipoObjetoScore.Chest, deltaChests);
        if (deltaChests > 0)
        {
            Stadistics.RegistrarObjeto(slotId, TipoObjetoScore.Chest, deltaChests);

            // 🔫 Pistola de cuerda para Cofre
            int totalChests = Stadistics.ObtenerConteo(slotId, TipoObjetoScore.Chest);
            AchievementsManager.ReportChestsProgress(totalChests);
        }

        if (deltaCrowns > 0) Stadistics.RegistrarObjeto(slotId, TipoObjetoScore.Crown, deltaCrowns);
        if (deltaCrowns > 0)
        {
            Stadistics.RegistrarObjeto(slotId, TipoObjetoScore.Crown, deltaCrowns);

            // 🔫 Pistola de cuerda para Corona
            int totalCrowns = Stadistics.ObtenerConteo(slotId, TipoObjetoScore.Crown);
            AchievementsManager.ReportCrownsProgress(totalCrowns);
        }



        if (deltaCoins > 0) Stadistics.RegistrarObjeto(slotId, TipoObjetoScore.RealCoin, deltaCoins);
        if (deltaCoins > 0)
        {
        int totalRealCoins = Stadistics.ObtenerConteo(slotId, TipoObjetoScore.RealCoin);
        AchievementsManager.ReportRealCoinProgress(totalRealCoins);
        }

        // 👇 NUEVO: estadística global de kills de peón (Δ) — sin depender de Stadistics
        /*if (deltaPawnKills > 0)
        {
            Stadistics.RegistrarKill(slotId, Stadistics.EnemyKillType.PawnRed, deltaPawnKills);
        }*/
        // --- Deuda del último intento (solo para mostrar en GameHome; NO afecta acumulado global) ---
        int lastRunDebt = Mathf.Min(score, 0); // si score > 0 => 0; si score < 0 => queda negativo
        PlayerPrefs.SetInt($"{slotId}_lastRunDebt", lastRunDebt);
        Debug.Log($"[PS] Snapshot lastRunDebt = {lastRunDebt}");

        PlayerPrefs.Save();
        Debug.Log($"✅ Guardado nivel {levelId}: Keys={finalKeys}, Diamonds={finalDiamonds}, BestScore={newBestScore}, PawnKillsBest={finalPawnKills}");

        // Sincronizar en la nube (solo si hay sesion Firebase)
        try
        {
            if (FirebaseWebBridge.IsSignedIn || PlayerPrefs.GetString("userType", "guest") == "firebase")
            {
                CloudSlotSync.PushSlotSafe(slotId);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ProgressSaver] Error al disparar sync en la nube: " + ex.Message);
        }

    }



    private static void SumarAcumulados(
        string slotId, int keysToAdd, int diamondsToAdd,
        bool parchment, bool trophy, bool medal, bool masterKey)
    {
        if (keysToAdd > 0)
        {
            int totalKeys = PlayerPrefs.GetInt($"{slotId}_keysTotal", 0) + keysToAdd;
            PlayerPrefs.SetInt($"{slotId}_keysTotal", totalKeys);

            // 👇 Pistola de cuerda: dispara todos los hitos con el total actualizado
            AchievementsManager.ReportKeysProgress(totalKeys);
            

        }

        if (diamondsToAdd > 0)
        {
           int totalDiamonds = PlayerPrefs.GetInt($"{slotId}_diamondsTotal", 0) + diamondsToAdd;
        PlayerPrefs.SetInt($"{slotId}_diamondsTotal", totalDiamonds);
        AchievementsManager.ReportDiamondsProgress(totalDiamonds);
            
        }

        if (parchment)
        {
            int total = PlayerPrefs.GetInt($"{slotId}_parchmentsTotal", 0) + 1;
            PlayerPrefs.SetInt($"{slotId}_parchmentsTotal", total);
        }
        if (trophy)
        {
            int total = PlayerPrefs.GetInt($"{slotId}_trophiesTotal", 0) + 1;
            PlayerPrefs.SetInt($"{slotId}_trophiesTotal", total);
        }
        if (medal)
        {
            int total = PlayerPrefs.GetInt($"{slotId}_medalsTotal", 0) + 1;
            PlayerPrefs.SetInt($"{slotId}_medalsTotal", total);
        }
        if (masterKey)
        {
            int total = PlayerPrefs.GetInt($"{slotId}_masterKeysTotal", 0) + 1;
            PlayerPrefs.SetInt($"{slotId}_masterKeysTotal", total);
        }
        FindFirstObjectByType<Stadistics>()?.RefrescarUI();
    }


    // 🔹 Métodos especializados
    public static void GuardarNivelNormal(string slotId, int levelId, int score, int keys, int diamonds)
    {
        GuardarResultado(slotId, levelId, score, keys, diamonds, false, false, false, false);
    }

    public static void GuardarNivelPergamino(string slotId, int levelId, int score, bool parchment)
    {
        GuardarResultado(slotId, levelId, score, 0, 0, parchment, false, false, false);
    }

    public static void GuardarNivelTrofeo(string slotId, int levelId, int score, bool trophy)
    {
        GuardarResultado(slotId, levelId, score, 0, 0, false, trophy, false, false);
    }

    public static void GuardarNivelMedalla(string slotId, int levelId, int score, bool medal)
    {
        GuardarResultado(slotId, levelId, score, 0, 0, false, false, medal, false);
    }

    public static void GuardarNivelMasterKey(string slotId, int levelId, int score, bool masterKey)
    {
        GuardarResultado(slotId, levelId, score, 0, 0, false, false, false, masterKey);
    }

    // 🔹 Totales acumulados
    private static void SumarAcumulados(
        string slotId, int score, int keysToAdd,
        int diamondsToAdd, bool parchment, bool trophy, bool medal, bool masterKey)
    {
        int totalScore = PlayerPrefs.GetInt(slotId + "_scoreTotal", 0) + score;
        PlayerPrefs.SetInt(slotId + "_scoreTotal", totalScore);

        if (keysToAdd > 0)
        {
            int totalKeys = PlayerPrefs.GetInt(slotId + "_keysTotal", 0) + keysToAdd;
            PlayerPrefs.SetInt(slotId + "_keysTotal", totalKeys);
        }

        if (diamondsToAdd > 0)
        {
            int totalDiamonds = PlayerPrefs.GetInt(slotId + "_diamondsTotal", 0) + diamondsToAdd;
            PlayerPrefs.SetInt(slotId + "_diamondsTotal", totalDiamonds);
        }

        if (parchment)
        {
            int totalParchments = PlayerPrefs.GetInt(slotId + "_parchmentsTotal", 0) + 1;
            PlayerPrefs.SetInt(slotId + "_parchmentsTotal", totalParchments);
        }

        if (trophy)
        {
            int totalTrophies = PlayerPrefs.GetInt(slotId + "_trophiesTotal", 0) + 1;
            PlayerPrefs.SetInt(slotId + "_trophiesTotal", totalTrophies);
        }

        if (medal)
        {
            int totalMedals = PlayerPrefs.GetInt(slotId + "_medalsTotal", 0) + 1;
            PlayerPrefs.SetInt(slotId + "_medalsTotal", totalMedals);
        }

        if (masterKey)
        {
            int totalMasterKeys = PlayerPrefs.GetInt(slotId + "_masterKeysTotal", 0) + 1;
            PlayerPrefs.SetInt(slotId + "_masterKeysTotal", totalMasterKeys);
        }
    }

    // 🔹 Registrar recolectables globales (para logros futuros básicos)
    public static void RegistrarObjetoRecolectado(string slotId, string tipo)
    {
        string key = slotId + "_" + tipo + "sTotal";
        int prev = PlayerPrefs.GetInt(key, 0);
        PlayerPrefs.SetInt(key, prev + 1);
        PlayerPrefs.Save();

        Debug.Log($"[ProgressSaver] ➕ Registrado {tipo} -> total ahora {prev + 1}");
    }

    // 🔹 Registrar ScoreObjects (Bag, Chest, Crown, RealCoin) sin farmeo
    public static bool RegistrarScoreObject(string slotId, int levelId, TipoObjetoScore tipo)
    {
        string key = $"{slotId}_level_{levelId}_{tipo}";

        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            Debug.Log($"⚠️ {tipo} ya registrado en nivel {levelId}. No suma otra vez en Stadistics.");
            return false; // Ya estaba registrado
        }

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        Stadistics.RegistrarObjeto(slotId, tipo, 1);
        Debug.Log($"[ProgressSaver] 📊 Nuevo registro de {tipo} en nivel {levelId}");
        return true;
    }
  

}
