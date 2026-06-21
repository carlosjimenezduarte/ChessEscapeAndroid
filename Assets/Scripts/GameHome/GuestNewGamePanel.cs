using UnityEngine;
using UnityEngine.SceneManagement;

public class GuestNewGamePanel : MonoBehaviour
{
    [SerializeField] private int gameHomeBuildIndex = 4;  // GameHome
    [SerializeField] private int homeGuestBuildIndex = 13; // HomeGuest

    private const string SLOT_ID = "guest";

    public void OnNewGame()
    {
        // Trabajar siempre en el slot invitado
        PlayerPrefs.SetString("slotActivo", SLOT_ID);

        // Asegurar nivel inicial (por si no existe)
        if (!PlayerPrefs.HasKey($"{SLOT_ID}_nivelMax"))
            PlayerPrefs.SetInt($"{SLOT_ID}_nivelMax", 1);

        PlayerPrefs.Save();
        SceneManager.LoadScene(gameHomeBuildIndex);
    }

    public void OnBack()
    {
        SceneManager.LoadScene(homeGuestBuildIndex);
    }
}
