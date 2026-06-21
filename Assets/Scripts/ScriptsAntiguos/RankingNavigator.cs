using UnityEngine;

public class RankingNavigator : MonoBehaviour
{
    [Header("Mapas en orden")]
    public GameObject[] mapas; // arrastras aquí los 8 mapas en orden

    [Header("Botones de navegación")]
    public GameObject botonPrevious;
    public GameObject botonNext;

    private int currentIndex = 0;

    void Start()
    {
        MostrarRanking(currentIndex);
    }

    public void OnNext()
    {
        if (currentIndex < mapas.Length - 1)
        {
            currentIndex++;
            MostrarRanking(currentIndex);
        }
    }

    public void OnPrevious()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            MostrarRanking(currentIndex);
        }
    }

    private void MostrarRanking(int index)
    {
        // Apagar todos
        for (int i = 0; i < mapas.Length; i++)
        {
            mapas[i].SetActive(i == index);
        }

        // Actualizar botones según posición
        if (botonPrevious) botonPrevious.SetActive(index > 0);
        if (botonNext) botonNext.SetActive(index < mapas.Length - 1);
    }
}
