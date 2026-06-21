using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityPlayerLogin : MonoBehaviour
{
    [Header("Escena a cargar tras login")]
    public int escenaDespuesDeLogin = 1;

    private bool firebaseInitialized;

    private async void Start()
    {
        try
        {
            FirebaseWebUser user = await FirebaseWebBridge.EnsureInitializedAsync();
            firebaseInitialized = true;

            if (user != null && user.signedIn)
            {
                SceneManager.LoadScene(escenaDespuesDeLogin);
            }
        }
        catch (Exception e)
        {
            firebaseInitialized = false;
            Debug.LogError("[Login] Firebase init fallo: " + e.Message);
        }
    }

    // Mantiene el nombre usado por los botones existentes en las escenas.
    public async void StartPlayerAccountsSignInAsync()
    {
        await StartFirebaseGoogleSignInAsync();
    }

    public async Task StartFirebaseGoogleSignInAsync()
    {
        if (!FirebaseWebBridge.IsFirebaseRuntimeSupported)
        {
            Debug.LogWarning("[Login] Firebase Google Sign-In requiere una build WebGL servida desde navegador. En el Editor no se puede validar este flujo.");
            return;
        }

        if (!firebaseInitialized)
        {
            try
            {
                await FirebaseWebBridge.EnsureInitializedAsync();
                firebaseInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Login] Firebase no esta listo: " + e.Message);
                return;
            }
        }

        try
        {
            FirebaseWebUser user = await FirebaseWebBridge.SignInWithGoogleAsync();
            if (user == null || !user.signedIn)
            {
                Debug.LogWarning("[Login] Google Sign-In no devolvio usuario autenticado.");
                return;
            }

            PlayerPrefs.SetString("userType", "firebase");
            PlayerPrefs.SetString("playerId", user.uid);
            PlayerPrefs.Save();

            SceneManager.LoadScene(escenaDespuesDeLogin);
        }
        catch (Exception ex)
        {
            Debug.LogError("[Login] Error en Google Sign-In: " + ex.Message);
        }
    }
}
