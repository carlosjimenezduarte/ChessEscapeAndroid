using UnityEngine;
using UnityEngine.SceneManagement;

public class GuestEntryButton : MonoBehaviour
{
    [SerializeField] private int homeGuestBuildIndex = 13; // Escena 13 = HomeGuest

    private const string SLOT_ID = "guest";
    private const string USER_TYPE_KEY = "userType";

    public void OnContinueAsGuest()
    {
        // Tipo de usuario
        PlayerPrefs.SetString(USER_TYPE_KEY, "guest");

        // Marcar slot activo = guest
        PlayerPrefs.SetString("slotActivo", SLOT_ID);

        PlayerPrefs.Save();

        // Ir a HomeGuest, donde el GuestManager ya tomará el control
        SceneManager.LoadScene(homeGuestBuildIndex);
    }
    
}
