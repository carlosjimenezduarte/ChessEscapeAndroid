using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class MapNavigator : MonoBehaviour
{
    [Header("Mapas en orden")]
    public GameObject[] mapas;

    [Header("Botones de navegación")]
    public GameObject botonPrevious;
    public GameObject botonNext;

    private int currentIndex = 0;
    private bool switching = false;

    void Start()
    {
        MostrarMapa(currentIndex);
    }

    public void OnNext()
    {
        if (switching) return;
        if (currentIndex < mapas.Length - 1)
            StartCoroutine(SwitchTo(currentIndex + 1));
    }

    public void OnPrevious()
    {
        if (switching) return;
        if (currentIndex > 0)
            StartCoroutine(SwitchTo(currentIndex - 1));
    }

    private IEnumerator SwitchTo(int newIndex)
    {
        switching = true;

        // Limpia selección de UI para que el “pointer up” no se propague
        EventSystem.current?.SetSelectedGameObject(null);

        MostrarMapa(newIndex);

        // Desactiva tiles por 1 frame para evitar click doble en el mismo frame
        ToggleTilesClickable(false);
        yield return null;
        ToggleTilesClickable(true);

        switching = false;
    }

    private void ToggleTilesClickable(bool enabled)
    {
        // Si tus LevelTile tienen un componente que puedes habilitar/deshabilitar,
        // actívalo aquí. Ejemplo:
        var ghm = FindFirstObjectByType<GameHomeManager>();
        if (ghm == null) return;

        foreach (var t in ghm.tiles)
        {
            if (t == null) continue;
            // Si usas Button o collider:
            var btn = t.GetComponent<UnityEngine.UI.Button>();
            if (btn) btn.interactable = enabled;

            var col = t.GetComponent<Collider2D>();
            if (col) col.enabled = enabled;
            var col3 = t.GetComponent<Collider>();
            if (col3) col3.enabled = enabled;
        }
    }

    private void MostrarMapa(int index)
    {
        currentIndex = index;

        for (int i = 0; i < mapas.Length; i++)
            mapas[i].SetActive(i == index);

        if (botonPrevious) botonPrevious.SetActive(index > 0);
        if (botonNext) botonNext.SetActive(index < mapas.Length - 1);

        var ghm = FindFirstObjectByType<GameHomeManager>();
        ghm?.RefrescarTilesActuales();

        foreach (var door in mapas[index].GetComponentsInChildren<FinalDoorTile>(true))
            door.RefreshVisual();
    }

    public void IrAlMapa(int index)
    {
        if (index < 0 || index >= mapas.Length) return;
        StartCoroutine(SwitchTo(index));
    }
}
