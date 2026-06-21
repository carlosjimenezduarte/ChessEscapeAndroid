using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class DonationPanelController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button closeButton;       // Botón "X"
    [SerializeField] private Button knowMoreButton;    // Botón "Saber más"
    //[SerializeField] private string donationUrl = "https://chessescape.com";
    [SerializeField] private int nextSceneIndex = -1;  // -1 = siguiente escena automática
    //[SerializeField] private float autoCloseDelay = 10f; // tiempo en segundos

    private bool actionTaken = false;
    private Coroutine autoCloseCoroutine;

    /// <summary>
    /// Llamado desde LevelResultUI cuando el usuario pulsa "Next Level"
    /// </summary>
    public void ShowPanel()
    {
        gameObject.SetActive(true);  // activa el panel en pantalla
        actionTaken = false;

        // Suscribir botones
        closeButton.onClick.AddListener(OnCloseClicked);
        knowMoreButton.onClick.AddListener(OnKnowMoreClicked);

        // Arrancar corutina de cierre automático
       // autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay(autoCloseDelay));
    }

    private void OnDisable()
    {
        // Limpieza de listeners y corutina si el panel se desactiva
        closeButton.onClick.RemoveListener(OnCloseClicked);
        knowMoreButton.onClick.RemoveListener(OnKnowMoreClicked);

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }

    
    public void OnCloseClicked()
    {
        Debug.Log("❌ Usuario cerró panel → siguiente nivel.");
        Continuar();
    }

    public void OnKnowMoreClicked()
    {
        //
    }

    private void Continuar()
    {
        if (actionTaken) return;
        actionTaken = true;

        gameObject.SetActive(false);

        int nextBuildIndex = (nextSceneIndex >= 0)
            ? nextSceneIndex
            : SceneManager.GetActiveScene().buildIndex + 1;

        // No escribimos nivelActivo aquí.
        // Al cargar la escena, LevelIdentity (DefaultExecutionOrder -500)
        // seteará nivelActivo y LevelIdentity.NivelLogico correctamente.
        SceneManager.LoadScene(nextBuildIndex);
    }



}
