using UnityEngine;
using UnityEngine.UI;

public class Cinematica : MonoBehaviour
{
    [Header("Configuración de música")]
    public AudioSource musicSource;  
    public AudioClip musicClip;      

    [Header("Paneles del slideshow")]
    public GameObject[] panels;      // Tus paneles en orden
    public float panelDuration = 8f; // 8 segundos por panel (ajustable)

    [Header("Botón de cerrar")]
    public Button closeButton;

    private int currentIndex = 0;
    private bool isPlaying = false;

    private void Start()
    {
        // Ocultar todos los paneles al inicio
        foreach (var p in panels) p.SetActive(false);

        // Configurar botón de cerrar
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true); 
            closeButton.onClick.AddListener(CerrarPresentacion);
        }

        // 🔊 Reproducir música
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }

        // Iniciar slideshow automáticamente
        isPlaying = true;
        currentIndex = 0;
        StartCoroutine(ReproducirSlideshow());
    }

    private System.Collections.IEnumerator ReproducirSlideshow()
    {
        while (currentIndex < panels.Length && isPlaying)
        {
            panels[currentIndex].SetActive(true);

            yield return new WaitForSeconds(panelDuration);

            // ❌ No apagar el último panel
            if (currentIndex < panels.Length - 1)
                panels[currentIndex].SetActive(false);

            currentIndex++;
        }
         if (isPlaying) 
        CerrarPresentacion();

        // Cuando termine, el último panel se queda fijo en pantalla
    }

    public void CerrarPresentacion()
    {
        isPlaying = false;

        // Detener música
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        // Ocultar todos los paneles
        foreach (var p in panels) p.SetActive(false);

        // 🔹 Mostrar directamente el escenario del juego (la escena ya está cargada detrás)
        gameObject.SetActive(false); // Ocultar el contenedor de la presentación

        Debug.Log("✅ Presentación cerrada manualmente");
    }
}
