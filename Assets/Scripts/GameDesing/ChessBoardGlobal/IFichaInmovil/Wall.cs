using UnityEngine;

public class Wall : MonoBehaviour, IFicha, IFichaInmovil, IPieceWithPosition
{
    [Header("Coordenadas del muro")]
    public Vector2Int tileCoords;

    
    private const bool esInamovible = true;

    private void Start()
    {
        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null)
        {
            tileCoords = posicionador.tileCoords;
        }
        else
        {
            tileCoords = Vector2Int.zero;
        }

        BoardManagerGlobal.Instance?.AgregarMensajeInterno($"🧱 Wall en {tileCoords}");

        // Registro completo
        if (BoardManagerGlobal.Instance != null)
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, tileCoords);
            BoardManagerGlobal.Instance.ReportarFichaInamovible(this); // Y asegura que sea tratada como ficha fija
        }
    }

    public bool EsInamovible() => esInamovible;

    public Vector2Int GetPosicionActual() => tileCoords;

    public void SetPosicionActual(Vector2Int nuevaPos) => tileCoords = nuevaPos;
}
