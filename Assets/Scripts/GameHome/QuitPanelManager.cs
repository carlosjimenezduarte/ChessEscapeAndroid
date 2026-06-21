using UnityEngine;

public class QuitPanelManager : MonoBehaviour
{
    [Header("Referencia al panel de QUIT")]
    public GameObject quitPanel;

    private void Start()
    {
        // Asegúrate de que arranque apagado
        if (quitPanel != null) quitPanel.SetActive(false);
    }

    // 🔹 Botón EXIT
    public void OnExitButton()
    {
        if (quitPanel != null) quitPanel.SetActive(true);
        SoundManager.Instance.PlaySound(7);
    }

    // 🔹 Botón NO
    public void OnNoButton()
    {
        if (quitPanel != null) quitPanel.SetActive(false);
    }
}
