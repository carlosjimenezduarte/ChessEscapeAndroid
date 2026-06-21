/*using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SlideshowController : MonoBehaviour
{
    [Header("Configuración de música")]
    public AudioSource musicSource;  // 🎶 Arrastrar aquí el AudioSource del MusicPanel
    public AudioClip musicClip;      // 🎵 Arrastrar aquí el .wav desde Assets

    [Header("Paneles del slideshow")]
    public GameObject[] panels;      // Tus 40 paneles en orden
    public float panelDuration = 5f; // 5 segundos cada uno

    [Header("Botón de cerrar")]
    public Button closeButton;

    private int currentIndex = 0;
    private bool isPlaying = false;

    private void Start()
    {
        // Ocultar todos los paneles al inicio
        foreach (var p in panels) p.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false); // 🔒 botón oculto al inicio

        if (closeButton != null)
            closeButton.onClick.AddListener(CerrarPresentacion);
    }

    public void IniciarPresentacion()
    {
        if (isPlaying) return;

        isPlaying = true;
        currentIndex = 0;

        // 🔊 Reproducir música
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
        
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        // Iniciar slideshow
        StartCoroutine(ReproducirSlideshow());
    }

    private System.Collections.IEnumerator ReproducirSlideshow()
    {
        while (currentIndex < panels.Length && isPlaying)
        {
            panels[currentIndex].SetActive(true);

            yield return new WaitForSeconds(panelDuration);

            // ❌ No apagar el último panel (logo final)
            if (currentIndex < panels.Length - 1)
                panels[currentIndex].SetActive(false);

            currentIndex++;
        }

        // Termina automáticamente
        CerrarPresentacion();
    }

    private void CerrarPresentacion()
    {
        isPlaying = false;

        // Detener música
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        // Ocultar todos los paneles excepto el último
        for (int i = 0; i < panels.Length - 1; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(false);
        }

        // 🔹 Mantener el último panel visible (logo + link)
        if (panels.Length > 0 && panels[panels.Length - 1] != null)
            panels[panels.Length - 1].SetActive(true);

        // 🔒 Ocultar botón de cerrar al salir
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        // Cambiar a GameHome sin "parpadeo del Jardín"
        SceneManager.LoadScene(4);

        Debug.Log("✅ Presentación cerrada en último panel (logo visible).");
    }
}
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SlideshowController : MonoBehaviour
{
    [Header("Configuración de música")]
    public AudioSource musicSource;  // 🎶 Arrastra aquí el AudioSource del MusicPanel
    public AudioClip musicClip;      // 🎵 Arrastra aquí el .wav desde Assets

    [Header("Paneles del slideshow")]
    public GameObject[] panels;      // Tus paneles en orden
    public float panelDuration = 5f; // 5 segundos cada uno

    [Header("Botón de cerrar")]
    public Button closeButton;

    [Header("Final settings")]
    [Tooltip("Nivel lógico final a acreditar")]
    public int finalNivelLogico = 204;
    [Tooltip("BuildIndex de GameHome (tu escena 4)")]
    public int gameHomeBuildIndex = 4;

    private int currentIndex = 0;
    private bool isPlaying = false;
    private bool isClosing = false;  // evita dobles cierres

    private void Start()
    {
        // Ocultar todos los paneles al inicio
        if (panels != null)
            foreach (var p in panels) if (p) p.SetActive(false);

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false); // 🔒 oculto al inicio
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CerrarPresentacion);
        }
    }

    public void IniciarPresentacion()
    {
        if (isPlaying || isClosing) return;

        isPlaying = true;
        currentIndex = 0;

        // 🔊 Reproducir música
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        // Iniciar slideshow
        StartCoroutine(ReproducirSlideshow());
        MusicManager.Instance.StopMusic();
    }

    private System.Collections.IEnumerator ReproducirSlideshow()
    {
        while (isPlaying && !isClosing && currentIndex < panels.Length)
        {
            if (panels[currentIndex]) panels[currentIndex].SetActive(true);

            yield return new WaitForSeconds(panelDuration);

            // ❌ No apagar el último panel (logo final)
            if (currentIndex < panels.Length - 1 && panels[currentIndex])
                panels[currentIndex].SetActive(false);

            currentIndex++;
        }

        // Termina automáticamente
        CerrarPresentacion();
    }

    private void CerrarPresentacion()
    {
        if (isClosing) return; // guardia
        isClosing = true;
        isPlaying = false;

        StopAllCoroutines();

        // Detener música
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        // Ocultar todos los paneles excepto el último
        if (panels != null && panels.Length > 0)
        {
            for (int i = 0; i < panels.Length - 1; i++)
                if (panels[i]) panels[i].SetActive(false);

            // 🔹 Mantener el último panel visible (logo + link)
            if (panels[panels.Length - 1])
                panels[panels.Length - 1].SetActive(true);
        }

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        // ✅ Escribir progreso 204/204 ANTES de salir a GameHome
        MarcarFinal204();

        // Pequeñísima espera (seguridad) y luego ir a GameHome
        StartCoroutine(LoadHomeNextFrame());
    }

    private void MarcarFinal204()
    {
        string slotId = PlayerPrefs.GetString("slotActivo", "slot1");

        // Si LevelIdentity ya setea nivelActivo, úsalo; si no, cae a 204.
        int nivelLogico = finalNivelLogico;
        if (PlayerPrefs.HasKey("nivelActivo"))
            nivelLogico = Mathf.Max(nivelLogico, PlayerPrefs.GetInt("nivelActivo"));

        // Marcar completo el 204
        PlayerPrefs.SetInt($"{slotId}_level_{nivelLogico}_completed", 1);

        // (Opcional) marca de “logro perfecto” si quieres ver el diamante encendido en el tile:
        // PlayerPrefs.SetInt($"{slotId}_level_{nivelLogico}_diamond", 1);

        // Subir el nivel máximo (así GameHome pinta 204/204 y muestra el mapa 8)
        int nivelMax = PlayerPrefs.GetInt($"{slotId}_nivelMax", 1);
        int proximo = nivelLogico + 1; // tu sistema usa +1 para “desbloquear el siguiente”
        if (proximo > nivelMax) PlayerPrefs.SetInt($"{slotId}_nivelMax", proximo);

        // Mantener coherente “nivelActivo”
        PlayerPrefs.SetInt("nivelActivo", nivelLogico);

        PlayerPrefs.Save();

        Debug.Log($"[SlideshowController] ✅ Marcado nivel {nivelLogico} como COMPLETADO. nivelMax={PlayerPrefs.GetInt($"{slotId}_nivelMax", 1)}");
    }

    private System.Collections.IEnumerator LoadHomeNextFrame()
    {
        // Esperar un frame (por si Unity tarda en flush de PlayerPrefs en algunas plataformas)
        yield return null;
        SceneManager.LoadScene(gameHomeBuildIndex);
    }
}
