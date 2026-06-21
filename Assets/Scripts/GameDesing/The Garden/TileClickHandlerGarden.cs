using UnityEngine;
using UnityEngine.EventSystems;

public class TileClickHandlerGarden : MonoBehaviour, IPointerClickHandler
{
    [Header("Coordenadas de esta casilla (Jardín 9x9)")]
    public Vector2Int tileCoords;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"🖱 Click detectado en casilla {tileCoords} en Jardín.");

        var gameManager = FindFirstObjectByType<GardenGame>();
        if (gameManager == null) return;

        if (gameManager.fichaSeleccionadaActual is KingFree reyLibre)
        {
            // 🔹 Mueve al Rey Libre
            reyLibre.MoverA(tileCoords);
        }
        else if (gameManager.fichaSeleccionadaActual is IFichaAliada fichaAliada
                 && fichaAliada is IPieceWithPosition aliadaConPos)
        {
            // 🔹 Aquí luego podrás mover otras piezas aliadas
            // por ahora solo debug
            Debug.Log($"⚠️ Aún no implementado movimiento de {fichaAliada} en Jardín.");
        }
    }
    private void OnMouseDown()
    {
        // 👑 Si el jugador clickea el tile (4,4), abrimos cofre
        if (tileCoords == new Vector2Int(4, 4))
        {
            FindFirstObjectByType<CofreController>()?.AbrirCofre();
        }
    }
}
