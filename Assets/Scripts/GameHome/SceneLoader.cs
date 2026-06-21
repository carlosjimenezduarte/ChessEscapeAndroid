using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Back desde GameHome (configurable en Inspector)")]
    [SerializeField] private int guestBackSceneIndex = 10;      // Escena PlayGuest
    [SerializeField] private int registeredBackSceneIndex = 3;  // Escena PlayGame

    public void LoadScene(int sceneIndex)
    {
        Debug.Log("🎵 Sonando antes de cargar escena...");
        SoundManager.Instance.PlaySound(8);
        StartCoroutine(LoadSceneDelay(sceneIndex, 0.5f));
    }

    // Llama este desde el botón Back de GameHome
    public void BackFromGameHome()
    {
        string userType = PlayerPrefs.GetString("userType", "guest"); // "guest", "firebase" o legado "unity"
        int target = (userType == "guest") ? guestBackSceneIndex : registeredBackSceneIndex;

        Debug.Log($"🔙 Back GameHome → {(userType == "guest" ? "Guest" : "Registered")} (scene {target})");
        SoundManager.Instance.PlaySound(8);
        StartCoroutine(LoadSceneDelay(target, 0.5f));
    }

    private IEnumerator LoadSceneDelay(int sceneIndex, float delay)
    {
        Debug.Log($"[SceneLoader] Voy a cargar la escena {sceneIndex} (llamado desde {new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType})");

        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneIndex);
    }

    public void BackGuestToHome()
    {
        Debug.Log("🔙 Invitado → HomeGuest (13)");
        StartCoroutine(LoadSceneDelay(13, 0.5f));
    }

}
