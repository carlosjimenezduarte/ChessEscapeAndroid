using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelProgress : MonoBehaviour
{
    public static LevelProgress Instance { get; private set; }

    [Header("Identificadores")]
    public int levelIdInspector = -1; // opcional por inspector
    private int _levelId;
    private string _slotId;

    private void Start()
    {
        _slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        _levelId = (levelIdInspector > 0) ? levelIdInspector : SceneManager.GetActiveScene().buildIndex;
    }

    [Header("Tipo de nivel")]
    public bool esNivelMasterKey = false;
    public bool esNivelPergamino = false;
    public bool esNivelTrofeo = false;
    public bool esNivelMedalla = false;
    public int bagsCollected = 0;
    public int crownsCollected = 0;
    public int chestsCollected = 0;
    public int coinsCollected = 0;

    [Header("Fichas Enemigas")]
    public int enemyPawnsKilled = 0;
    public int enemyKnightsKilled = 0;   // 👈 NUEVO
    public int enemyBishopsKilled = 0;   // 👈 NUEVO
    public int enemyRooksKilled = 0;    // 👈 NUEVO
    public int enemyQueensKilled = 0;   // 👈 NUEVO
    public int enemyRooksBlackKilled = 0;   // 👈 NUEVO
    public int enemyQueensBlackKilled = 0;  // 👈 NUEVO

    public int enemyBishopsBlackKilled = 0;  // NUEVO
    public int enemyKnightsBlackKilled = 0;  // NUEVO


    [Header("Config de nivel")]
    public int maxKeys = 3;        // por defecto 3 llaves
    public int maxDiamonds = 1;    // por defecto 1 diamante

    [Header("Estado del nivel (runtime, no editable)")]
    [HideInInspector] public int keysCollected = 0;
    [HideInInspector] public int diamondsCollected = 0;
    [HideInInspector] public bool hasParchment = false;
    [HideInInspector] public bool hasTrophy = false;
    [HideInInspector] public bool hasMedal = false;
    [HideInInspector] public bool hasMasterKey3 = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🔹 Métodos de recolección
    public void Key()
    {
        int before = keysCollected;
        keysCollected = Mathf.Clamp(keysCollected + 1, 0, maxKeys);
        Debug.Log($"🗝️ Llaves recogidas: {keysCollected}/{maxKeys} (antes {before})");
    }

    public void Diamond()
    {
        int before = diamondsCollected;
        diamondsCollected = Mathf.Clamp(diamondsCollected + 1, 0, maxDiamonds);
        Debug.Log($"💎 Diamantes recogidos: {diamondsCollected}/{maxDiamonds} (antes {before})");
    }

    // 🔹 Penalizaciones
    public void Padlock()
    {
        int before = keysCollected;
        keysCollected = Mathf.Clamp(keysCollected - 1, 0, maxKeys);
        Debug.Log($"🔒 Candado -> Llaves ahora: {keysCollected}/{maxKeys} (antes {before})");
    }

    public void Talisman()
    {
        int before = diamondsCollected;
        diamondsCollected = Mathf.Clamp(diamondsCollected - 1, 0, maxDiamonds);
        Debug.Log($"🔮 Talismán -> Diamantes ahora: {diamondsCollected}/{maxDiamonds} (antes {before})");
    }

    // 🔹 Objetos especiales
    public void Parchment()
    {
        if (!hasParchment)
        {
            hasParchment = true;
            Debug.Log($"📜 ¡Pergamino obtenido!");
        }
    }

    public void Trophy()
    {
        if (!hasTrophy)
        {
            hasTrophy = true;
            Debug.Log($"🏆 ¡Trofeo obtenido!");
        }
    }

    public void Medal()
    {
        if (!hasMedal)
        {
            hasMedal = true;
            Debug.Log($"🎖️ ¡Medalla obtenida!");
        }
    }

    public void MasterKey()
    {
        if (!hasMasterKey3)
        {
            hasMasterKey3 = true;
            Debug.Log($"🗝️ Fragmento de llave recogido.");
        }
    }
    public void AddBag()
    {
        bagsCollected++;
        Debug.Log($"💰 Bolsa recogida. Total en este nivel: {bagsCollected}");
    }

    public void AddCrown()
    {
        crownsCollected++;
        Debug.Log($"👑 Corona recogida. Total en este nivel: {crownsCollected}");
    }

    public void AddChest()
    {
        chestsCollected++;
        Debug.Log($"📦 Cofre recogido. Total en este nivel: {chestsCollected}");
    }

    public void AddCoin()
    {
        coinsCollected++;
        Debug.Log($"🪙 Moneda real recogida. Total en este nivel: {coinsCollected}");
    }


    // 🔹 Reset al empezar/reintentar nivel
    public void ResetProgress()
    {
        keysCollected = 0;
        diamondsCollected = 0;
        hasParchment = false;
        hasTrophy = false;
        hasMedal = false;
        hasMasterKey3 = false;

        // ✅ Reiniciar contadores de score-objetos por intento
        bagsCollected = 0;
        crownsCollected = 0;
        chestsCollected = 0;
        coinsCollected = 0;

        enemyPawnsKilled = 0;
        enemyKnightsKilled = 0;  // 👈 NUEVO
        enemyBishopsKilled = 0;  // 👈 NUEVO
        enemyRooksKilled = 0;   // 👈
        enemyQueensKilled = 0;  // 👈
        enemyRooksBlackKilled = 0;   // 👈
        enemyQueensBlackKilled = 0;  // 👈
        enemyBishopsBlackKilled = 0;  // NUEVO
        enemyKnightsBlackKilled = 0;  // NUEVO




        Debug.Log("🔄 Progreso del nivel reiniciado.");
    }

    public void AddEnemyPawnKill()
    {
        enemyPawnsKilled++;
        Debug.Log($"☠️ Peón enemigo eliminado. Total en este nivel: {enemyPawnsKilled}");

        // Anti-farmeo inmediato: solo sumar al global si superaste tu mejor marca de este nivel
        string perLevelKey = $"{_slotId}_level_{_levelId}_killsPawn";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyPawnsKilled > prevBest)
        {
            int delta = enemyPawnsKilled - prevBest;              // lo nuevo que SÍ cuenta
            PlayerPrefs.SetInt(perLevelKey, enemyPawnsKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.PawnRed, delta);
            Debug.Log($"[LP] ΔPawnRed={delta} (best={enemyPawnsKilled}) aplicado al global.");

            FindFirstObjectByType<Stadistics>()?.RefrescarUI();   // refresco visible si hay UI cargada
            // 🔫 Pistola de cuerda: total global actualizado -> chequear milestones
            int totalPawnRed = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.PawnRed);
            AchievementsManager.ReportRedPawnKillsProgress(totalPawnRed);
    
        }


    }

    public void AddEnemyKnightKill() // 👈 NUEVO
    {
        enemyKnightsKilled++;
        Debug.Log($"☠️ Caballo enemigo eliminado. Total en este nivel: {enemyKnightsKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsKnight";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyKnightsKilled > prevBest)
        {
            int delta = enemyKnightsKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyKnightsKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.KnightRed, delta);
            Debug.Log($"[LP] ΔKnightRed={delta} (best={enemyKnightsKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            // 🔫 Pistola de cuerda: total global actualizado -> chequear milestones
            int totalKnightRed = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.KnightRed);
            AchievementsManager.ReportRedKnightKillsProgress(totalKnightRed);
        }
    }

    public void AddEnemyBishopKill() // 👈 NUEVO
    {
        enemyBishopsKilled++;
        Debug.Log($"☠️ Alfil enemigo eliminado. Total en este nivel: {enemyBishopsKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsBishop";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyBishopsKilled > prevBest)
        {
            int delta = enemyBishopsKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyBishopsKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.BishopRed, delta);
            Debug.Log($"[LP] ΔBishopRed={delta} (best={enemyBishopsKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            // 🔫 Pistola de cuerda: total global actualizado -> chequear milestones
            int totalBishopRed = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.BishopRed);
            AchievementsManager.ReportRedBishopKillsProgress(totalBishopRed);

        }
    }
    public void AddEnemyRookKill()   // 👈 NUEVO
    {
        enemyRooksKilled++;
        Debug.Log($"☠️ Torre enemiga eliminada. Total en este nivel: {enemyRooksKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsRook";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyRooksKilled > prevBest)
        {
            int delta = enemyRooksKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyRooksKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.RookRed, delta);
            Debug.Log($"[LP] ΔRookRed={delta} (best={enemyRooksKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            // 🔫 Pistola de cuerda: total global actualizado -> chequear milestones
            int totalRookRed = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.RookRed);
            AchievementsManager.ReportRedRookKillsProgress(totalRookRed);
        }
    }


    public void AddEnemyQueenKill()  // 👈 NUEVO
    {
        enemyQueensKilled++;
        Debug.Log($"☠️ Reina enemiga eliminada. Total en este nivel: {enemyQueensKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsQueen";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyQueensKilled > prevBest)
        {
            int delta = enemyQueensKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyQueensKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.QueenRed, delta);
            Debug.Log($"[LP] ΔQueenRed={delta} (best={enemyQueensKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            // 🔫 Pistola de cuerda: total global actualizado -> chequear milestones
            int totalQueenRed = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.QueenRed);
            AchievementsManager.ReportRedQueenKillsProgress(totalQueenRed);

        }
    }

    public void AddEnemyRookBlackKill()
    {
        enemyRooksBlackKilled++;
        Debug.Log($"☠️ Torre NEGRA eliminada. Total en este nivel: {enemyRooksBlackKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsRookBlack";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyRooksBlackKilled > prevBest)
        {
            int delta = enemyRooksBlackKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyRooksBlackKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.RookBlack, delta);
            Debug.Log($"[LP] ΔRookBlack={delta} (best={enemyRooksBlackKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            int totalRookBlack = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.RookBlack);
            AchievementsManager.ReportBlackRookKillsProgress(totalRookBlack);

        }
    }


    public void AddEnemyQueenBlackKill()
    {
        enemyQueensBlackKilled++;
        Debug.Log($"☠️ Reina NEGRA eliminada. Total en este nivel: {enemyQueensBlackKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsQueenBlack";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyQueensBlackKilled > prevBest)
        {
            int delta = enemyQueensBlackKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyQueensBlackKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.QueenBlack, delta);
            Debug.Log($"[LP] ΔQueenBlack={delta} (best={enemyQueensBlackKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            int totalQueenBlack = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.QueenBlack);
            AchievementsManager.ReportBlackQueenKillsProgress(totalQueenBlack);

        }
    }

    public void AddEnemyBishopBlackKill()
    {
        enemyBishopsBlackKilled++;
        Debug.Log($"☠️ Alfil NEGRO eliminado. Total en este nivel: {enemyBishopsBlackKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsBishopBlack";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyBishopsBlackKilled > prevBest)
        {
            int delta = enemyBishopsBlackKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyBishopsBlackKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.BishopBlack, delta);
            Debug.Log($"[LP] ΔBishopBlack={delta} (best={enemyBishopsBlackKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
            int totalBishopBlack = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.BishopBlack);
            AchievementsManager.ReportBlackBishopKillsProgress(totalBishopBlack);
            int totalKnightBlack = Stadistics.ObtenerKills(_slotId, Stadistics.EnemyKillType.KnightBlack);
            AchievementsManager.ReportBlackKnightKillsProgress(totalKnightBlack);

        }
    }


    public void AddEnemyKnightBlackKill()
    {
        enemyKnightsBlackKilled++;
        Debug.Log($"☠️ Caballo NEGRO eliminado. Total en este nivel: {enemyKnightsBlackKilled}");

        string perLevelKey = $"{_slotId}_level_{_levelId}_killsKnightBlack";
        int prevBest = PlayerPrefs.GetInt(perLevelKey, 0);

        if (enemyKnightsBlackKilled > prevBest)
        {
            int delta = enemyKnightsBlackKilled - prevBest;
            PlayerPrefs.SetInt(perLevelKey, enemyKnightsBlackKilled);
            PlayerPrefs.Save();

            Stadistics.RegistrarKill(_slotId, Stadistics.EnemyKillType.KnightBlack, delta);
            Debug.Log($"[LP] ΔKnightBlack={delta} (best={enemyKnightsBlackKilled}) aplicado al global.");
            FindFirstObjectByType<Stadistics>()?.RefrescarUI();
        }
    }



public void MasterKey(MasterKeyId which)
{
    // Flag opcional en memoria local (si quieres reflejar algo en UI del nivel)
    hasMasterKey3 = true; // o crea flags por cada MK si lo necesitas

    // Marca persistente opcional (útil si luego quieres leerlo en menús)
    string slotId = PlayerPrefs.GetString("slotActivo", "slot1");
    PlayerPrefs.SetInt($"{slotId}_mk{(int)which}_collected", 1);
    PlayerPrefs.Save();

    // Dispara el logro correspondiente
    AchievementsManager.ReportMasterKey(which);
}


}
