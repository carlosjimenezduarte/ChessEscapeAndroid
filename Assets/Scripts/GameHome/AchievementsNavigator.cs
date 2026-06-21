using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementsNavigator : MonoBehaviour
{
    [Header("Todos los paneles en orden (20 logros + títulos + trofeos + condecoraciones)")]
    public GameObject[] mapas;

    [Header("Botones de navegación")]
    public GameObject botonPrevious;
    public GameObject botonNext;

    [Header("Toggles de filtros")]
    public Toggle toggleTitulos;
    public Toggle toggleTrofeos;
    public Toggle toggleCondecoraciones;

    [Header("Textos principales")]
    public TMP_Text textAchievements;
    public TMP_Text textTitles;
    public TMP_Text textTrophies;
    public TMP_Text textDecorations;

    // Rangos de índices
    private int rangoTitulosStart = 20;
    private int rangoTitulosEnd = 21;
    private int indexTrofeos = 22;
    private int indexCondecoracionesStart = 23;
    private int indexCondecoracionesEnd = 24;

    private int currentIndex = 0;
    private List<int> indicesActivos = new List<int>();

    void Start()
    {
        // Inicial: todos los paneles habilitados
        ResetearIndices();
        MostrarLogro(currentIndex);

        // Conectar listeners de los toggles
        if (toggleTitulos) toggleTitulos.onValueChanged.AddListener(delegate { AplicarFiltros(); });
        if (toggleTrofeos) toggleTrofeos.onValueChanged.AddListener(delegate { AplicarFiltros(); });
        if (toggleCondecoraciones) toggleCondecoraciones.onValueChanged.AddListener(delegate { AplicarFiltros(); });

        MostrarTitulo("Achievements"); // por defecto
    }

    public void OnNext()
    {
        int pos = indicesActivos.IndexOf(currentIndex);
        if (pos < indicesActivos.Count - 1)
        {
            currentIndex = indicesActivos[pos + 1];
            MostrarLogro(currentIndex);
        }
    }

    public void OnPrevious()
    {
        int pos = indicesActivos.IndexOf(currentIndex);
        if (pos > 0)
        {
            currentIndex = indicesActivos[pos - 1];
            MostrarLogro(currentIndex);
        }
    }

    private void MostrarLogro(int index)
    {
        for (int i = 0; i < mapas.Length; i++)
            mapas[i].SetActive(i == index);

        int pos = indicesActivos.IndexOf(index);
        if (botonPrevious) botonPrevious.SetActive(pos > 0);
        if (botonNext) botonNext.SetActive(pos < indicesActivos.Count - 1);
    }

    private void AplicarFiltros()
    {
        if (toggleTitulos != null && toggleTitulos.isOn)
        {
            indicesActivos.Clear();
            for (int i = rangoTitulosStart; i <= rangoTitulosEnd; i++)
                indicesActivos.Add(i);
            currentIndex = indicesActivos[0];
            MostrarLogro(currentIndex);
            MostrarTitulo("Titles");
            return;
        }

        if (toggleTrofeos != null && toggleTrofeos.isOn)
        {
            indicesActivos.Clear();
            indicesActivos.Add(indexTrofeos);
            currentIndex = indexTrofeos;
            MostrarLogro(currentIndex);
            MostrarTitulo("Trophies");
            return;
        }

        if (toggleCondecoraciones != null && toggleCondecoraciones.isOn)
        {
            indicesActivos.Clear();
            /*indicesActivos.Add(indexCondecoraciones);
            currentIndex = indexCondecoraciones;*/            
            for (int i = indexCondecoracionesStart; i <= indexCondecoracionesEnd; i++)
                indicesActivos.Add(i);
            currentIndex = indicesActivos[0];
            MostrarLogro(currentIndex);
            MostrarTitulo("Decorations");
            return;
        }

        // Ningún filtro → reset a todos
        ResetearIndices();
        MostrarLogro(currentIndex);
        MostrarTitulo("Achievements");
    }

    private void ResetearIndices()
    {
        indicesActivos.Clear();
        for (int i = 0; i < mapas.Length; i++)
            indicesActivos.Add(i);

        if (!indicesActivos.Contains(currentIndex))
            currentIndex = indicesActivos[0];
    }

    private void MostrarTitulo(string categoria)
    {
        // Apagar todos
        if (textAchievements) textAchievements.gameObject.SetActive(false);
        if (textTitles) textTitles.gameObject.SetActive(false);
        if (textTrophies) textTrophies.gameObject.SetActive(false);
        if (textDecorations) textDecorations.gameObject.SetActive(false);

        // Encender el correspondiente
        switch (categoria)
        {
            case "Titles":
                if (textTitles) textTitles.gameObject.SetActive(true);
                break;
            case "Trophies":
                if (textTrophies) textTrophies.gameObject.SetActive(true);
                break;
            case "Decorations":
                if (textDecorations) textDecorations.gameObject.SetActive(true);
                break;
            default: // Achievements
                if (textAchievements) textAchievements.gameObject.SetActive(true);
                break;
        }
    }
}
