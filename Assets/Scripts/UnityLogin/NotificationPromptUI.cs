using UnityEngine;
using TMPro;

public class NotificationPromptUI : MonoBehaviour
{
    [Header("Raíz del panel (Canvas o panel contenedor)")]
    public GameObject panelRoot;

    [Header("Texto opcional (por si quieres cambiarlo vía script)")]
    public TMP_Text questionText;

    private string currentSlotId;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false); // oculto por defecto
    }

    private void Start()
    {
        // Texto por defecto (puedes dejarlo vacío y escribirlo en el Inspector si prefieres)
        if (questionText != null && string.IsNullOrEmpty(questionText.text))
        {
            questionText.text = "¿Te gustaría recibir recordatorios diarios para jugar ChessEscape?";
        }

        ShowIfNeededForCurrentSlot();
    }

    private void ShowIfNeededForCurrentSlot()
    {
        currentSlotId = PlayerPrefs.GetString("slotActivo", "slot1");
        string key = $"{currentSlotId}_notif_preference"; // 0 = no decidido, 1 = sí, 2 = no

        int pref = PlayerPrefs.GetInt(key, 0);

        // Solo mostramos el panel si aún no decidió (0) o si nunca se ha grabado nada
        if (pref == 0)
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }
    }

    // === Botón "Sí" ===
    public void OnClickYes()
    {
        if (string.IsNullOrEmpty(currentSlotId))
            currentSlotId = PlayerPrefs.GetString("slotActivo", "slot1");

        string key = $"{currentSlotId}_notif_preference";

        PlayerPrefs.SetInt(key, 1); // aceptó en este slot
        PlayerPrefs.Save();

        // Programar notificaciones globales (si aún no estaban programadas)
        DailyNotificationsManager.Instance?.ScheduleForNext30DaysIfNeeded();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        Debug.Log($"[NotifUI] Usuario ACEPTÓ notificaciones en {currentSlotId}.");
    }

    // === Botón "No" ===
    public void OnClickNo()
    {
        if (string.IsNullOrEmpty(currentSlotId))
            currentSlotId = PlayerPrefs.GetString("slotActivo", "slot1");

        string key = $"{currentSlotId}_notif_preference";

        PlayerPrefs.SetInt(key, 2); // rechazó en este slot
        PlayerPrefs.Save();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        Debug.Log($"[NotifUI] Usuario RECHAZÓ notificaciones en {currentSlotId}.");
    }

    // === Botón "Decidir más tarde" ===
    public void OnClickLater()
    {
        // No guardamos nada; la preferencia queda en 0
        // y por tanto el panel volverá a aparecer la próxima vez.

        if (panelRoot != null)
            panelRoot.SetActive(false);

        Debug.Log("[NotifUI] Usuario eligió DECIDIR MÁS TARDE. Se volverá a preguntar.");
    }
}
