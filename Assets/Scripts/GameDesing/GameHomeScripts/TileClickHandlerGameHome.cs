/*using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(LevelTile))]
public class TileClickHandlerGameHome : MonoBehaviour, IPointerClickHandler
{
    [Header("Identificadores del nivel")]
    [Tooltip("Número que corresponde al Build Index en File -> Build Settings")]

    public const int BUILD_OFFSET = 14;
    public int buildIndex;   // 👈 índice real de escena
    public int nivelLogico;  // 👈 número visible (1, 2, 3...)
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (nivelLogico > 0)
            buildIndex = nivelLogico + BUILD_OFFSET;
    }
#endif

    private LevelTile levelTile;

    private void Awake()
    {
        levelTile = GetComponent<LevelTile>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (levelTile.state == LevelTile.TileState.Locked)
        {
            Debug.Log($"⛔ Nivel {nivelLogico} está bloqueado.");
            return;
        }

        // Guardar el nivel lógico activo
        PlayerPrefs.SetInt("nivelActivo", nivelLogico);
        PlayerPrefs.Save();

        Debug.Log($"▶️ Entrando al nivel lógico {nivelLogico} (BuildIndex={buildIndex})");
        SceneManager.LoadScene(buildIndex); // 👈 aquí usamos el índice real de escena
        SoundManager.Instance.PlaySound(0);
    }
}*/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(LevelTile))]
public class TileClickHandlerGameHome : MonoBehaviour, IPointerClickHandler
{
    [Header("Identificadores del nivel")]
    [Tooltip("Número que corresponde al Build Index en File -> Build Settings")]
    public const int BUILD_OFFSET = 14;
    public int buildIndex;   // índice real de escena
    public int nivelLogico;  // número visible (1, 2, 3...)

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (nivelLogico > 0)
            buildIndex = nivelLogico + BUILD_OFFSET;
    }
#endif

    private LevelTile levelTile;

    private void Awake()
    {
        levelTile = GetComponent<LevelTile>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (levelTile.state == LevelTile.TileState.Locked)
        {
            Debug.Log($"⛔ Nivel {nivelLogico} está bloqueado.");
            return;
        }

        // Guardar el nivel lógico activo
        PlayerPrefs.SetInt("nivelActivo", nivelLogico);
        PlayerPrefs.Save();

        Debug.Log($"▶️ Entrando al nivel lógico {nivelLogico} (BuildIndex={buildIndex})");

        // Reproducir sonido y esperar antes de cargar la escena
        SoundManager.Instance.PlaySound(37);
        StartCoroutine(LoadSceneDelay(buildIndex, 0.5f));
    }

    private IEnumerator LoadSceneDelay(int sceneIndex, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneIndex);
    }
}
