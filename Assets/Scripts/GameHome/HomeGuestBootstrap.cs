using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class HomeGuestBootstrap : MonoBehaviour
{
    private const string SLOT_ID = "guest";
    private const string USER_TYPE_KEY = "userType";

    void Awake()
    {
        // Entramos a HomeGuest => somos invitado por definición
        PlayerPrefs.SetString(USER_TYPE_KEY, "guest");
        PlayerPrefs.SetString("slotActivo", SLOT_ID);

        if (!PlayerPrefs.HasKey("guest_state"))
        {
            PlayerPrefs.SetString("guest_state", "active");
            if (!PlayerPrefs.HasKey($"{SLOT_ID}_nivelMax"))
                PlayerPrefs.SetInt($"{SLOT_ID}_nivelMax", 1);
        }

        PlayerPrefs.Save();
    }
}
