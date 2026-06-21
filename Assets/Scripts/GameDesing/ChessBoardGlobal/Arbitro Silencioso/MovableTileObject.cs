using UnityEngine;
using System.Linq;

public class MovableTileObject : MonoBehaviour
{
    [SerializeField] public Vector2Int tileCoords;
    public bool activoEnTablero = false;
    public bool esInamovible = false;

    private bool EsDentroTablero(Vector2Int p)
        => p.x >= 0 && p.x <= 7 && p.y >= 0 && p.y <= 7;

    public void MoverA(Vector2Int nuevaPos)
    {
        if (esInamovible)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🪨 {gameObject.name} es inamovible y no se moverá.");
            return;
        }

        if (EstaCasillaOcupada(nuevaPos))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⛔ {gameObject.name} no se moverá a {nuevaPos} porque está ocupado.");
            return;
        }

        tileCoords = nuevaPos;
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);

        var piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            piecePositioner.tileCoords = nuevaPos;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧭 PiecePositioner de {gameObject.name} actualizado a {nuevaPos}");
        }

        if (TryGetComponent<IPieceWithPosition>(out var piece))
        {
            piece.SetPosicionActual(nuevaPos);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧭 IPieceWithPosition de {gameObject.name} actualizado a {nuevaPos}");
        }

        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧭 {gameObject.name} movido global a {nuevaPos}");

    }

    private bool EstaCasillaOcupada(Vector2Int coords)
    {
        var objetosEnTile = BoardManagerGlobal.Instance.ObtenerObjetosEn(coords);

        foreach (var obj in objetosEnTile)
        {
            if ((object)obj == this) continue;

            if (obj is MovableTileObject mov && !mov.activoEnTablero)
                continue;

            string nombre = ((MonoBehaviour)obj).gameObject.name;

            // 🎁 Caso especial: recolectable siendo empujado hacia ficha aliada
            if (this is IObjetoRecoleccionable recolectable && obj is IFichaAliada fichaAliada)
            {
                if (recolectable is ITileEffect efecto)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎁 {gameObject.name} será recolectado por ficha aliada {nombre} en {coords}.");
                    efecto.RevisarSiFichaLlegó(coords, fichaAliada);
                    return false; // No se bloquea el paso, se activa recolección
                }
            }

            // 🔒 Objetos especiales no se destruyen ni absorben
            if (obj is IObjetoRecoleccionableEspecial)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔒 {nombre} es un objeto especial en {coords}. No puede ser reemplazado.");
                return true;
            }

            // 💥 Recolectables comunes destruidos por enemigos
            if (obj is IObjetoRecoleccionable && this is IFichaEnemiga)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 {gameObject.name} destruirá {nombre} (recolectable común) en {coords}.");
                Destroy(((MonoBehaviour)obj).gameObject);
                return false; // Puede pasar luego de destruir
            }

            // 🛡️ Bloqueos sólidos: fichas enemigas o inamovibles
            if (obj is IFichaEnemiga || obj is IFichaInmovil)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛡️ {nombre} bloquea el paso en {coords} (FichaEnemiga o Inmovil).");
                return true;
            }

            // 🚫 Cualquier otra cosa, bloquea
            bool esFicha = obj is IFicha;
            bool esRecolectable = obj is IObjetoRecoleccionable;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 La casilla {coords} está ocupada por {nombre} (Ficha:{esFicha}, Recolectable:{esRecolectable}).");
            return true;
        }

        return false;
    }

    public void ActivarEnTablero(Vector2Int nuevaPos)
    {
        tileCoords = nuevaPos;
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);
        activoEnTablero = true;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ {gameObject.name} activado en tablero en {tileCoords}");
    }

    public bool EsInamovible()
    {
        return esInamovible;
    }
     

   
}
