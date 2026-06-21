using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManagerGO : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject[] imagenes;          // 20 GOs de imagen (en orden)
    public TextMeshProUGUI[] textos;       // 20 TMPs (en orden)
    public Button nextButton;
    public Button previousButton;

    private int currentIndex = 0;
    private bool loopEnabled = false;      // Se activa solo después de la primera lectura completa

    void Start()
    {
        // Sane checks opcionales
        if (imagenes == null || textos == null || imagenes.Length == 0 || textos.Length == 0)
        {
            Debug.LogError("TutorialManagerGO: Arrays vacíos o nulos.");
            enabled = false; 
            return;
        }
        if (imagenes.Length != textos.Length)
        {
            Debug.LogWarning($"TutorialManagerGO: imagenes({imagenes.Length}) y textos({textos.Length}) no coinciden.");
        }

        MostrarContenido();

        nextButton.onClick.AddListener(Siguiente);
        previousButton.onClick.AddListener(Anterior);
    }

    void MostrarContenido()
    {
        for (int i = 0; i < imagenes.Length; i++)
        {
            bool activo = (i == currentIndex);
            if (i < imagenes.Length && imagenes[i] != null) imagenes[i].SetActive(activo);
            if (i < textos.Length && textos[i]  != null) textos[i].gameObject.SetActive(activo);
        }

        // Botones: antes de activar el loop, se bloquean en extremos; después, siempre activos
        int lastIndex = Mathf.Max(0, imagenes.Length - 1);
        if (!loopEnabled)
        {
            previousButton.interactable = currentIndex > 0;
            nextButton.interactable = currentIndex < lastIndex;
        }
        else
        {
            previousButton.interactable = true;
            nextButton.interactable = true;
        }
    }

    public void Siguiente()
    {
        int lastIndex = Mathf.Max(0, imagenes.Length - 1);

        if (currentIndex < lastIndex)
        {
            currentIndex++;

            // Si llegamos al final por primera vez usando Next, habilitamos el loop
            if (currentIndex == lastIndex)
                loopEnabled = true;
        }
        else
        {
            // Ya estamos en el final: si el loop está habilitado, volvemos al inicio
            if (loopEnabled)
                currentIndex = 0;
            // Si no, nos quedamos en el último (botón estará deshabilitado)
        }

        MostrarContenido();
    }

    public void Anterior()
    {
        int lastIndex = Mathf.Max(0, imagenes.Length - 1);

        if (currentIndex > 0)
        {
            currentIndex--;
        }
        else
        {
            // En el inicio: si el loop está habilitado, vamos al último
            if (loopEnabled)
                currentIndex = lastIndex;
            // Si no, nos quedamos en 0 (botón deshabilitado)
        }

        MostrarContenido();
    }
}
