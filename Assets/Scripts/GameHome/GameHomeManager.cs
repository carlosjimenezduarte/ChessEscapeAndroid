/*using UnityEngine;
using System.Collections.Generic;
using TMPro;



public class GameHomeManager : MonoBehaviour
{
    public static GameHomeManager Instance { get; private set; }

    [Header("Lista de casillas (niveles) en este mapa")]
    public List<LevelTile> tiles; // arrastra en orden en el inspector

    [Header("Referencias UI de progreso acumulado")]
    public TMP_Text scoreText;
    public TMP_Text keysText;
    public TMP_Text diamondsText;

    [Header("Progreso de niveles")]
    public TMP_Text levelsProgressText; // 👈 arrastra aquí el TextMeshPro en el inspector
    private const int TOTAL_NIVELES = 204;

    private const int OFFSET_NIVELES = 14;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CargarProgreso();

        // 👇 Intentar refrescar la casilla final (si existe en este mapa)
        //var final = FindFirstObjectByType<FinalDoorTile>();
        //if (final != null) final.RefreshVisual();

    }

    /// <summary>
    /// Regla unificada de validación de Objeto Clave.
    /// - Niveles normales: 3 llaves + todos los diamantes.
    /// - Niveles especiales (1 llave + 2 diamantes): basta esa combinación.
    /// - O bien, pergamino / trofeo / medalla / masterKey.
    /// </summary>
    private bool EsNivelCompletadoConObjetoClave(
        int keys, int diamondsCollected, int maxKeys, int maxDiamonds,
        bool parchment, bool trophy, bool medal, bool masterKey)
    {
        if (maxKeys == 1 && maxDiamonds == 2)
        {
            // Caso especial: 1 llave + 2 diamantes
            return (keys >= 1 && diamondsCollected >= 2) ||
                   parchment || trophy || medal || masterKey;
        }

        // Caso general: 3 llaves + todos los diamantes
        return (keys >= 3 && diamondsCollected >= maxDiamonds) ||
               parchment || trophy || medal || masterKey;
    }

    // Método público para LevelResultUI
    public bool ValidarObjetoClave(
        int keys, int diamondsCollected, int maxKeys, int maxDiamonds,
        bool parchment, bool trophy, bool medal, bool masterKey)
    {
        return EsNivelCompletadoConObjetoClave(
            keys, diamondsCollected, maxKeys, maxDiamonds,
            parchment, trophy, medal, masterKey
        );
    }
    public void CargarProgreso()
    {
        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int nivelMax = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);

        // 🔹 Determinar mapa actual según el progreso
        int mapaIndex = ObtenerMapaPorNivel(nivelMax);
        FindFirstObjectByType<MapNavigator>()?.IrAlMapa(mapaIndex);

        // 👇 Resto de tu código original de CargarProgreso
        int totalScore = PlayerPrefs.GetInt(slotActivo + "_scoreTotal", 0);
        int totalKeys = PlayerPrefs.GetInt(slotActivo + "_keysTotal", 0);
        int totalDiamonds = PlayerPrefs.GetInt(slotActivo + "_diamondsTotal", 0);

        int lastRunDebt = PlayerPrefs.GetInt($"{slotActivo}_lastRunDebt", 0);

        if (scoreText != null)
        {
            if (lastRunDebt < 0)
                scoreText.text = $"{totalScore}  <color=#FF5555>({lastRunDebt})</color>";
            else
                scoreText.text = totalScore.ToString();
        }

        if (keysText != null) keysText.text = totalKeys.ToString();
        if (diamondsText != null) diamondsText.text = totalDiamonds.ToString();

        int nivelesCompletados = 0;

        for (int i = 0; i < tiles.Count; i++)
        {
            var handler = tiles[i].GetComponent<TileClickHandlerGameHome>();
            if (handler == null) continue;

            int completed = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_completed", 0);
            int keys = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_keys", 0);
            int diamondsCollected = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_diamonds", 0);

            int maxKeys = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_maxKeys", 3);
            int maxDiamonds = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_maxDiamonds", 1);

            bool parchment = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_parchment", 0) == 1;
            bool trophy = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_trophy", 0) == 1;
            bool medal = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_medal", 0) == 1;
            bool masterKey = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_masterKey", 0) == 1;

            bool obtuvoObjetoClave = EsNivelCompletadoConObjetoClave(
                keys, diamondsCollected, maxKeys, maxDiamonds,
                parchment, trophy, medal, masterKey
            );

            if (completed == 1)
            {
                tiles[i].SetState(LevelTile.TileState.Completed, obtuvoObjetoClave);
                nivelesCompletados++;
            }
            else if (handler.nivelLogico == nivelMax)
            {
                tiles[i].SetState(LevelTile.TileState.Unlocked);
            }
            else
            {
                tiles[i].SetState(LevelTile.TileState.Locked);
            }
        }

        if (levelsProgressText != null)
            levelsProgressText.text = $"{nivelesCompletados}/{TOTAL_NIVELES}";
    }





    public void MarcarNivelCompletado(int nivelLogico, bool obtuvoObjetoClave)
    {
        Debug.Log($"[GameHomeManager] ➡️ MarcarNivelCompletado -> nivel={nivelLogico}, ObjetoClave={obtuvoObjetoClave}");

        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int nivelMaxAntes = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);

        LevelTile tile = tiles.Find(t =>
        {
            var handler = t.GetComponent<TileClickHandlerGameHome>();
            return handler != null && handler.nivelLogico == nivelLogico;
        });

        if (tile == null)
        {
            Debug.LogWarning($"⚠️ No se encontró casilla para Nivel lógico={nivelLogico}");
            return;
        }

        int keys = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_keys", 0);
        int diamondsCollected = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_diamonds", 0);

        int maxKeys = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxKeys", 3);
        int maxDiamonds = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxDiamonds", 1);

        bool parchment = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_parchment", 0) == 1;
        bool trophy = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_trophy", 0) == 1;
        bool medal = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_medal", 0) == 1;
        bool masterKey = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_masterKey", 0) == 1;

        bool logroPerfecto = EsNivelCompletadoConObjetoClave(
            keys, diamondsCollected, maxKeys, maxDiamonds,
            parchment, trophy, medal, masterKey
        );

        tile.SetState(LevelTile.TileState.Completed, logroPerfecto);

        int currentIndex = tiles.IndexOf(tile);
        if (currentIndex >= 0 && currentIndex + 1 < tiles.Count)
        {
            tiles[currentIndex + 1].SetState(LevelTile.TileState.Unlocked);
            Debug.Log($"   • Nivel lógico {nivelLogico + 1} desbloqueado.");
        }

        PlayerPrefs.SetInt(slotActivo + "_level_" + nivelLogico + "_completed", 1);
        if (logroPerfecto)
            PlayerPrefs.SetInt(slotActivo + "_level_" + nivelLogico + "_diamond", 1);

        if (nivelLogico + 1 > nivelMaxAntes)
            PlayerPrefs.SetInt(slotActivo + "_nivelMax", nivelLogico + 1);

        PlayerPrefs.Save();

        // 🔄 Refrescar contador visual
        CargarProgreso();

        // ✅ Solo una vez la variable
        int nivelMaxDespues = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);
        Debug.Log($"[GameHomeManager] ✅ Progreso guardado -> nivel={nivelLogico}, Perfecto={logroPerfecto}, nivelMax={nivelMaxDespues}");

        int mapaIndex = ObtenerMapaPorNivel(nivelMaxDespues);
        FindFirstObjectByType<MapNavigator>()?.IrAlMapa(mapaIndex);
    }

    public void RefrescarTilesActuales()
    {
        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int nivelMax = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);

        int nivelesCompletados = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            var handler = tiles[i].GetComponent<TileClickHandlerGameHome>();
            if (handler == null) continue;

            int completed = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_completed", 0);
            int keys = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_keys", 0);
            int diamondsCollected = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_diamonds", 0);
            int maxKeys = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_maxKeys", 3);
            int maxDiamonds = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_maxDiamonds", 1);
            bool parchment = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_parchment", 0) == 1;
            bool trophy = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_trophy", 0) == 1;
            bool medal = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_medal", 0) == 1;
            bool masterKey = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_masterKey", 0) == 1;

            bool objetoClave = EsNivelCompletadoConObjetoClave(
                keys, diamondsCollected, maxKeys, maxDiamonds, parchment, trophy, medal, masterKey);

            if (completed == 1)
            {
                tiles[i].SetState(LevelTile.TileState.Completed, objetoClave);
                nivelesCompletados++;
            }
            else if (handler.nivelLogico == nivelMax)
            {
                tiles[i].SetState(LevelTile.TileState.Unlocked);
            }
            else
            {
                tiles[i].SetState(LevelTile.TileState.Locked);
            }
        }

        if (levelsProgressText != null)
            levelsProgressText.text = $"{nivelesCompletados}/{TOTAL_NIVELES}";
    }




    private int ObtenerMapaPorNivel(int nivelLogico)
    {
        if (nivelLogico <= 64) return 0;   // Mapa 1
        if (nivelLogico <= 113) return 1;  // Mapa 2
        if (nivelLogico <= 149) return 2;  // Mapa 3
        if (nivelLogico <= 174) return 3;  // Mapa 4
        if (nivelLogico <= 190) return 4;  // Mapa 5
        if (nivelLogico <= 199) return 5;  // Mapa 6
        if (nivelLogico <= 203) return 6;  // Mapa 7
        return 7;                          // Mapa 8 (nivel 204)
    }

    private int CalcularCompletadosGlobal(string slot)
    {
        int count = 0;
        for (int lvl = 1; lvl <= TOTAL_NIVELES; lvl++)
        {
            if (PlayerPrefs.GetInt($"{slot}_level_{lvl}_completed", 0) == 1)
                count++;
        }
        return count;
    }



}

*/

using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameHomeManager : MonoBehaviour
{
    public static GameHomeManager Instance { get; private set; }

    [Header("Lista de casillas (niveles) en este mapa")]
    public List<LevelTile> tiles; // arrastra en orden en el inspector

    [Header("Referencias UI de progreso acumulado")]
    public TMP_Text scoreText;
    public TMP_Text keysText;
    public TMP_Text diamondsText;

    [Header("Progreso de niveles")]
    public TMP_Text levelsProgressText; // 👈 arrastra aquí el TextMeshPro en el inspector

    private const int TOTAL_NIVELES = 204;
    private const int OFFSET_NIVELES = 14;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CargarProgreso();
        // var final = FindFirstObjectByType<FinalDoorTile>();
        // if (final != null) final.RefreshVisual();
    }

    /// <summary>
    /// Regla unificada de validación de Objeto Clave.
    /// - Niveles normales: 3 llaves + todos los diamantes.
    /// - Niveles especiales (1 llave + 2 diamantes): basta esa combinación.
    /// - O bien, pergamino / trofeo / medalla / masterKey.
    /// </summary>
    private bool EsNivelCompletadoConObjetoClave(
        int keys, int diamondsCollected, int maxKeys, int maxDiamonds,
        bool parchment, bool trophy, bool medal, bool masterKey)
    {
        if (maxKeys == 1 && maxDiamonds == 2)
        {
            // Caso especial: 1 llave + 2 diamantes
            return (keys >= 1 && diamondsCollected >= 2) ||
                   parchment || trophy || medal || masterKey;
        }

        // Caso general: 3 llaves + todos los diamantes
        return (keys >= 3 && diamondsCollected >= maxDiamonds) ||
               parchment || trophy || medal || masterKey;
    }

    // Método público para LevelResultUI
    public bool ValidarObjetoClave(
        int keys, int diamondsCollected, int maxKeys, int maxDiamonds,
        bool parchment, bool trophy, bool medal, bool masterKey)
    {
        return EsNivelCompletadoConObjetoClave(
            keys, diamondsCollected, maxKeys, maxDiamonds,
            parchment, trophy, medal, masterKey
        );
    }

    public void CargarProgreso()
    {
        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int nivelMax = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);

        // 🔹 Determinar mapa actual según el progreso
        int mapaIndex = ObtenerMapaPorNivel(nivelMax);
        FindFirstObjectByType<MapNavigator>()?.IrAlMapa(mapaIndex);

        // 🔹 Totales visibles
        int totalScore = PlayerPrefs.GetInt(slotActivo + "_scoreTotal", 0);
        int totalKeys = PlayerPrefs.GetInt(slotActivo + "_keysTotal", 0);
        int totalDiamonds = PlayerPrefs.GetInt(slotActivo + "_diamondsTotal", 0);
        int lastRunDebt = PlayerPrefs.GetInt($"{slotActivo}_lastRunDebt", 0);

        if (scoreText != null)
        {
            if (lastRunDebt < 0)
                scoreText.text = $"{totalScore}  <color=#FF5555>({lastRunDebt})</color>";
            else
                scoreText.text = totalScore.ToString();
        }

        if (keysText != null) keysText.text = totalKeys.ToString();
        if (diamondsText != null) diamondsText.text = totalDiamonds.ToString();

        // 🔹 Pintado de tiles del mapa actual (se mantiene igual)
        int nivelesCompletadosMapa = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            var handler = tiles[i].GetComponent<TileClickHandlerGameHome>();
            if (handler == null) continue;

            int completed = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_completed", 0);
            int keys = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_keys", 0);
            int diamondsCollected = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_diamonds", 0);

            int maxKeys = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_maxKeys", 3);
            int maxDiamonds = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_maxDiamonds", 1);

            bool parchment = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_parchment", 0) == 1;
            bool trophy = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_trophy", 0) == 1;
            bool medal = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_medal", 0) == 1;
            bool masterKey = PlayerPrefs.GetInt(slotActivo + "_level_" + handler.nivelLogico + "_masterKey", 0) == 1;

            bool obtuvoObjetoClave = EsNivelCompletadoConObjetoClave(
                keys, diamondsCollected, maxKeys, maxDiamonds,
                parchment, trophy, medal, masterKey
            );

            if (completed == 1)
            {
                tiles[i].SetState(LevelTile.TileState.Completed, obtuvoObjetoClave);
                nivelesCompletadosMapa++;
            }
            else if (handler.nivelLogico == nivelMax)
            {
                tiles[i].SetState(LevelTile.TileState.Unlocked);
            }
            else
            {
                tiles[i].SetState(LevelTile.TileState.Locked);
            }
        }

        // 🔥 HOTFIX: el numerador del texto usa el conteo GLOBAL (1→204) por PlayerPrefs
        int completadosGlobal = CalcularCompletadosGlobal(slotActivo);
        if (levelsProgressText != null)
            levelsProgressText.text = $"{completadosGlobal}/{TOTAL_NIVELES}";
    }

    public void MarcarNivelCompletado(int nivelLogico, bool obtuvoObjetoClave)
    {
        Debug.Log($"[GameHomeManager] ➡️ MarcarNivelCompletado -> nivel={nivelLogico}, ObjetoClave={obtuvoObjetoClave}");

        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int nivelMaxAntes = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);

        LevelTile tile = tiles.Find(t =>
        {
            var handler = t.GetComponent<TileClickHandlerGameHome>();
            return handler != null && handler.nivelLogico == nivelLogico;
        });

        if (tile == null)
        {
            Debug.LogWarning($"⚠️ No se encontró casilla para Nivel lógico={nivelLogico}");
            // Aunque no esté en este mapa, persistimos y refrescamos el contador global
            PlayerPrefs.SetInt($"{slotActivo}_level_{nivelLogico}_completed", 1);
            PlayerPrefs.Save();
            CargarProgreso();
            return;
        }

        int keys = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_keys", 0);
        int diamondsCollected = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_diamonds", 0);
        int maxKeys = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxKeys", 3);
        int maxDiamonds = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_maxDiamonds", 1);
        bool parchment = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_parchment", 0) == 1;
        bool trophy = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_trophy", 0) == 1;
        bool medal = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_medal", 0) == 1;
        bool masterKey = PlayerPrefs.GetInt(slotActivo + "_level_" + nivelLogico + "_masterKey", 0) == 1;

        bool logroPerfecto = EsNivelCompletadoConObjetoClave(
            keys, diamondsCollected, maxKeys, maxDiamonds,
            parchment, trophy, medal, masterKey
        );

        tile.SetState(LevelTile.TileState.Completed, logroPerfecto);

        int currentIndex = tiles.IndexOf(tile);
        if (currentIndex >= 0 && currentIndex + 1 < tiles.Count)
        {
            tiles[currentIndex + 1].SetState(LevelTile.TileState.Unlocked);
            Debug.Log($"   • Nivel lógico {nivelLogico + 1} desbloqueado.");
        }

        PlayerPrefs.SetInt(slotActivo + "_level_" + nivelLogico + "_completed", 1);
        if (logroPerfecto)
            PlayerPrefs.SetInt(slotActivo + "_level_" + nivelLogico + "_diamond", 1);

        if (nivelLogico + 1 > nivelMaxAntes)
            PlayerPrefs.SetInt(slotActivo + "_nivelMax", nivelLogico + 1);

        PlayerPrefs.Save();

        // 🔄 Refrescar UI y mover mapa
        CargarProgreso();
        int nivelMaxDespues = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);
        int mapaIndex = ObtenerMapaPorNivel(nivelMaxDespues);
        FindFirstObjectByType<MapNavigator>()?.IrAlMapa(mapaIndex);
    }

    public void RefrescarTilesActuales()
    {
        string slotActivo = PlayerPrefs.GetString("slotActivo", "slot1");
        int nivelMax = PlayerPrefs.GetInt(slotActivo + "_nivelMax", 1);

        int nivelesCompletados = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            var handler = tiles[i].GetComponent<TileClickHandlerGameHome>();
            if (handler == null) continue;

            int completed = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_completed", 0);
            int keys = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_keys", 0);
            int diamondsCollected = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_diamonds", 0);
            int maxKeys = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_maxKeys", 3);
            int maxDiamonds = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_maxDiamonds", 1);
            bool parchment = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_parchment", 0) == 1;
            bool trophy = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_trophy", 0) == 1;
            bool medal = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_medal", 0) == 1;
            bool masterKey = PlayerPrefs.GetInt($"{slotActivo}_level_{handler.nivelLogico}_masterKey", 0) == 1;

            bool objetoClave = EsNivelCompletadoConObjetoClave(
                keys, diamondsCollected, maxKeys, maxDiamonds, parchment, trophy, medal, masterKey);

            if (completed == 1)
            {
                tiles[i].SetState(LevelTile.TileState.Completed, objetoClave);
                nivelesCompletados++;
            }
            else if (handler.nivelLogico == nivelMax)
            {
                tiles[i].SetState(LevelTile.TileState.Unlocked);
            }
            else
            {
                tiles[i].SetState(LevelTile.TileState.Locked);
            }
        }

        // 🔥 Mantener el texto con CONTEO GLOBAL
        int completadosGlobal = CalcularCompletadosGlobal(slotActivo);
        if (levelsProgressText != null)
            levelsProgressText.text = $"{completadosGlobal}/{TOTAL_NIVELES}";
    }

    private int ObtenerMapaPorNivel(int nivelLogico)
    {
        if (nivelLogico <= 64) return 0;   // Mapa 1
        if (nivelLogico <= 113) return 1;  // Mapa 2
        if (nivelLogico <= 149) return 2;  // Mapa 3
        if (nivelLogico <= 174) return 3;  // Mapa 4
        if (nivelLogico <= 190) return 4;  // Mapa 5
        if (nivelLogico <= 199) return 5;  // Mapa 6
        if (nivelLogico <= 203) return 6;  // Mapa 7
        return 7;                          // Mapa 8 (nivel 204)
    }

    // ========= HOTFIX helper =========
    private int CalcularCompletadosGlobal(string slot)
    {
        int count = 0;
        for (int lvl = 1; lvl <= TOTAL_NIVELES; lvl++)
        {
            if (PlayerPrefs.GetInt($"{slot}_level_{lvl}_completed", 0) == 1)
                count++;
        }
        return count;
    }
}

