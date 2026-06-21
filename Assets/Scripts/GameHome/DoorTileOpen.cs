using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class DoorTileOpen : MonoBehaviour, IPointerClickHandler
{
    [Header("Config publicación (provisional)")]
    [Tooltip("Offset de build que usas en todo el proyecto (p. ej. 14).")]
    public const int BUILD_OFFSET = 14;

    [Tooltip("Nivel lógico requerido para abrir la puerta (203).")]
    public int nivelRequisito = 203;

    [Tooltip("Nivel lógico de destino (204).")]
    public int nivelDestino = 204;

    [Tooltip("BuildIndex real del destino. Si es -1, se calcula como nivelDestino + BUILD_OFFSET.")]
    public int buildIndexDestino = -1;

    [Header("Iconos de la casilla")]
    public GameObject doorOpenIcon;   // “Door” abierta
    public GameObject doorClosedIcon; // “CloseDoor” cerrada

    [Header("Panel opcional cuando aún no puede entrar")]
    public GameObject panelDoorClosed;
    public TMP_Text panelMessage;

    private string slotId;

    void Awake()
    {
        slotId = PlayerPrefs.GetString("slotActivo", "slot1");
        if (buildIndexDestino < 0)
            buildIndexDestino = nivelDestino + BUILD_OFFSET;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshVisual();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        bool eligible = EsElegible();

        if (doorOpenIcon)   doorOpenIcon.SetActive(eligible);
        if (doorClosedIcon) doorClosedIcon.SetActive(!eligible);

        if (panelDoorClosed) panelDoorClosed.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (EsElegible())
        {
            PlayerPrefs.SetInt("nivelActivo", nivelDestino);
            PlayerPrefs.Save();
            SceneManager.LoadScene(buildIndexDestino);
        }
        else
        {
            if (panelDoorClosed) panelDoorClosed.SetActive(true);
            if (panelMessage)
                panelMessage.text = $"Para entrar aquí, primero completa la sala {nivelRequisito}.";
        }
    }

    /// <summary>
    /// Elegible si:
    /// 1) slot_level_203_completed == 1  (vía ProgressSaver), o
    /// 2) nivelMax >= 204 (fallback robusto).
    /// </summary>
    private bool EsElegible()
    {
        // Evidencia directa: nivel 203 completado
        int completed203 = PlayerPrefs.GetInt($"{slotId}_level_{nivelRequisito}_completed", 0);

        // Fallback por progresión global
        int nivelMax = PlayerPrefs.GetInt($"{slotId}_nivelMax", 1);

        return (completed203 == 1) || (nivelMax >= (nivelDestino));
    }

    // Botón [X] para cerrar panel
    public void ClosePanel()
    {
        if (panelDoorClosed) panelDoorClosed.SetActive(false);
    }
}
