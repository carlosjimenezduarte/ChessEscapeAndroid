using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutController : MonoBehaviour
{
    [Header("Panel de confirmación")]
    public GameObject panelConfirmLogout;

    [Header("Escenas")]
    [Tooltip("Escena a la que volver tras cerrar sesión (ej: 0 = MainMenu)")]
    public int mainMenuBuildIndex = 0;

    public void OnLogoutRequest()
    {
        if (panelConfirmLogout != null)
            panelConfirmLogout.SetActive(true);
    }

    public async void OnConfirmYes()
    {
        try
        {
            await FirebaseWebBridge.SignOutAsync();
            Debug.Log("[Logout] Sesion Firebase cerrada.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Logout] Error cerrando sesion Firebase: " + e.Message);
        }

        SceneManager.LoadScene(mainMenuBuildIndex);
    }

    public void OnConfirmNo()
    {
        if (panelConfirmLogout != null)
            panelConfirmLogout.SetActive(false);
    }
}
