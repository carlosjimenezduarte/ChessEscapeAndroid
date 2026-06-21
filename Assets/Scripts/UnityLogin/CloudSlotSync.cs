using System;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class LevelCloudData
{
    public int completed;
    public int score;
    public int keys;
    public int diamonds;

    public int parchment;
    public int trophy;
    public int medal;
    public int masterKey;

    public int maxKeys;
    public int maxDiamonds;

    public int bags;
    public int crowns;
    public int chests;
    public int coins;

    public int killsPawn;
    public int killsKnight;
    public int killsBishop;
    public int killsRook;
    public int killsQueen;
    public int killsRookBlack;
    public int killsQueenBlack;
    public int killsBishopBlack;
    public int killsKnightBlack;
}

[Serializable]
public class SlotCloudData
{
    public string slotId;
    public int schemaVersion;

    // Totales globales
    public int nivelMax;
    public int scoreTotal;
    public int keysTotal;
    public int diamondsTotal;
    public int parchmentsTotal;
    public int trophiesTotal;
    public int medalsTotal;
    public int masterKeysTotal;
    public int scoreFromAchievements;
    public int lastRunDebt;
    public int rankMaxIndex;

    // Flags MasterKey globales
    public int[] mkCollected;

    // Por ahora no subimos Stadistics globales para simplificar;
    // se pueden añadir después.

    // Datos por nivel (índice = nivel lógico; usaremos 1..MAX_LEVELS)
    public LevelCloudData[] levels;
}

public static class CloudSlotSync
{
    private const int MAX_LEVELS = 204;     // Ajusta si más adelante usas >204 niveles
    private const int SCHEMA_VERSION = 1;

    // =============================
    //  API PÚBLICA
    // =============================

    /// <summary>
    /// Llamada segura desde código no-async (ProgressSaver etc.).
    /// </summary>
    public static async void PushSlotSafe(string slotId)
    {
        try
        {
            await PushSlotAsync(slotId);
        }
        catch (Exception e)
        {
            Debug.LogError("[CloudSlotSync] Error en PushSlotSafe: " + e.Message);
        }
    }

    /// <summary>
    /// Sube el snapshot completo de un slot (PlayerPrefs -> Firestore).
    /// </summary>
    public static async Task PushSlotAsync(string slotId)
    {
        if (!await PuedeUsarCloudSaveAsync())
        {
            Debug.Log("[CloudSlotSync] No hay autenticacion Firebase, no se sube " + slotId);
            return;
        }

        SlotCloudData data = BuildSlotDataFromPlayerPrefs(slotId);
        string json = JsonUtility.ToJson(data);

        await FirebaseWebBridge.SaveSlotAsync(slotId, json);
        Debug.Log($"[CloudSlotSync] Subido progreso de {slotId} ({json.Length} bytes).");
    }

    /// <summary>
    /// Descarga un slot (Firestore -> PlayerPrefs).
    /// </summary>
    public static async Task PullSlotAsync(string slotId)
    {
        if (!await PuedeUsarCloudSaveAsync())
        {
            Debug.Log("[CloudSlotSync] No hay autenticacion Firebase, no se descarga " + slotId);
            return;
        }

        FirebaseWebSlotResult result = await FirebaseWebBridge.LoadSlotAsync(slotId);
        if (result == null || !result.exists)
        {
            Debug.Log("[CloudSlotSync] No hay datos remotos para " + slotId);
            return;
        }

        string json = result.json;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[CloudSlotSync] JSON vacío para " + slotId);
            return;
        }

        SlotCloudData data = JsonUtility.FromJson<SlotCloudData>(json);
        if (data == null)
        {
            Debug.LogError("[CloudSlotSync] No se pudo deserializar SlotCloudData para " + slotId);
            return;
        }

        ApplySlotDataToPlayerPrefs(data);
        Debug.Log("[CloudSlotSync] Descargado y aplicado progreso de " + slotId);
    }

    /// <summary>
    /// Descarga los 3 slots estándar (slot1, slot2, slot3).
    /// </summary>
    public static async Task PullAllSlotsAsync()
    {
        await PullSlotAsync("slot1");
        await PullSlotAsync("slot2");
        await PullSlotAsync("slot3");
        PlayerPrefs.Save();
    }

    // =============================
    //  PRIVADOS
    // =============================

    private static async Task<bool> PuedeUsarCloudSaveAsync()
    {
        await FirebaseWebBridge.EnsureInitializedAsync();

        if (!FirebaseWebBridge.IsSignedIn)
            await FirebaseWebBridge.RefreshCurrentUserAsync();

        return FirebaseWebBridge.IsSignedIn;
    }

    private static SlotCloudData BuildSlotDataFromPlayerPrefs(string slotId)
    {
        var data = new SlotCloudData
        {
            slotId = slotId,
            schemaVersion = SCHEMA_VERSION,
            nivelMax        = PlayerPrefs.GetInt($"{slotId}_nivelMax", 1),
            scoreTotal      = PlayerPrefs.GetInt($"{slotId}_scoreTotal", 0),
            keysTotal       = PlayerPrefs.GetInt($"{slotId}_keysTotal", 0),
            diamondsTotal   = PlayerPrefs.GetInt($"{slotId}_diamondsTotal", 0),
            parchmentsTotal = PlayerPrefs.GetInt($"{slotId}_parchmentsTotal", 0),
            trophiesTotal   = PlayerPrefs.GetInt($"{slotId}_trophiesTotal", 0),
            medalsTotal     = PlayerPrefs.GetInt($"{slotId}_medalsTotal", 0),
            masterKeysTotal = PlayerPrefs.GetInt($"{slotId}_masterKeysTotal", 0),
            scoreFromAchievements = PlayerPrefs.GetInt($"{slotId}_scoreFromAchievements", 0),
            lastRunDebt     = PlayerPrefs.GetInt($"{slotId}_lastRunDebt", 0),
            rankMaxIndex    = PlayerPrefs.GetInt($"{slotId}_rankMaxIndex", 0),
            mkCollected     = new int[3],
            levels          = new LevelCloudData[MAX_LEVELS + 1] // índice 1..MAX_LEVELS
        };

        for (int mk = 1; mk <= 3; mk++)
        {
            data.mkCollected[mk - 1] = PlayerPrefs.GetInt($"{slotId}_mk{mk}_collected", 0);
        }

        for (int lvl = 1; lvl <= MAX_LEVELS; lvl++)
        {
            var lvlData = new LevelCloudData
            {
                completed   = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_completed", 0),
                score       = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_score", 0),
                keys        = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_keys", 0),
                diamonds    = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_diamonds", 0),

                parchment   = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_parchment", 0),
                trophy      = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_trophy", 0),
                medal       = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_medal", 0),
                masterKey   = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_masterKey", 0),

                maxKeys     = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_maxKeys", 0),
                maxDiamonds = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_maxDiamonds", 0),

                bags        = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_bags", 0),
                crowns      = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_crowns", 0),
                chests      = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_chests", 0),
                coins       = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_coins", 0),

                killsPawn       = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsPawn", 0),
                killsKnight     = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsKnight", 0),
                killsBishop     = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsBishop", 0),
                killsRook       = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsRook", 0),
                killsQueen      = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsQueen", 0),
                killsRookBlack  = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsRookBlack", 0),
                killsQueenBlack = PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsQueenBlack", 0),
                killsBishopBlack= PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsBishopBlack", 0),
                killsKnightBlack= PlayerPrefs.GetInt($"{slotId}_level_{lvl}_killsKnightBlack", 0)
            };

            data.levels[lvl] = lvlData;
        }

        return data;
    }

    private static void ApplySlotDataToPlayerPrefs(SlotCloudData data)
    {
        if (data == null) return;
        string slotId = data.slotId;

        PlayerPrefs.SetInt($"{slotId}_nivelMax",        data.nivelMax);
        PlayerPrefs.SetInt($"{slotId}_scoreTotal",      data.scoreTotal);
        PlayerPrefs.SetInt($"{slotId}_keysTotal",       data.keysTotal);
        PlayerPrefs.SetInt($"{slotId}_diamondsTotal",   data.diamondsTotal);
        PlayerPrefs.SetInt($"{slotId}_parchmentsTotal", data.parchmentsTotal);
        PlayerPrefs.SetInt($"{slotId}_trophiesTotal",   data.trophiesTotal);
        PlayerPrefs.SetInt($"{slotId}_medalsTotal",     data.medalsTotal);
        PlayerPrefs.SetInt($"{slotId}_masterKeysTotal", data.masterKeysTotal);
        PlayerPrefs.SetInt($"{slotId}_scoreFromAchievements", data.scoreFromAchievements);
        PlayerPrefs.SetInt($"{slotId}_lastRunDebt",     data.lastRunDebt);
        PlayerPrefs.SetInt($"{slotId}_rankMaxIndex",    data.rankMaxIndex);

        if (data.mkCollected != null && data.mkCollected.Length >= 3)
        {
            for (int mk = 1; mk <= 3; mk++)
                PlayerPrefs.SetInt($"{slotId}_mk{mk}_collected", data.mkCollected[mk - 1]);
        }

        if (data.levels != null)
        {
            // empiezas en 1 porque el índice 0 lo dejamos sin uso
            for (int lvl = 1; lvl < data.levels.Length; lvl++)
            {
                LevelCloudData lvlData = data.levels[lvl];
                if (lvlData == null) continue;

                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_completed",   lvlData.completed);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_score",       lvlData.score);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_keys",        lvlData.keys);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_diamonds",    lvlData.diamonds);

                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_parchment",   lvlData.parchment);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_trophy",      lvlData.trophy);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_medal",       lvlData.medal);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_masterKey",   lvlData.masterKey);

                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_maxKeys",     lvlData.maxKeys);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_maxDiamonds", lvlData.maxDiamonds);

                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_bags",        lvlData.bags);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_crowns",      lvlData.crowns);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_chests",      lvlData.chests);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_coins",       lvlData.coins);

                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsPawn",       lvlData.killsPawn);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsKnight",     lvlData.killsKnight);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsBishop",     lvlData.killsBishop);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsRook",       lvlData.killsRook);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsQueen",      lvlData.killsQueen);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsRookBlack",  lvlData.killsRookBlack);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsQueenBlack", lvlData.killsQueenBlack);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsBishopBlack",lvlData.killsBishopBlack);
                PlayerPrefs.SetInt($"{slotId}_level_{lvl}_killsKnightBlack",lvlData.killsKnightBlack);
            }
        }

        PlayerPrefs.Save();
    }
}
