using UnityEngine;

public class PusherLeft : MonoBehaviour, ITileEffect, IFichaInmovil, IFicha, IPieceWithPosition
{
    public Vector2Int tileCoords;
    public bool esInamovible = true;

    private void Start()
    {
        var pos = GetComponent<PiecePositioner>();
        if (pos != null)
        {
            tileCoords = pos.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"📦 PusherUp activo en {tileCoords}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚠️ PusherUp sin PiecePositioner.");
        }

        BoardManagerGlobal.Instance?.ReportarFichaInamovible(this);
    }

    public void RevisarSiFichaLlegó(Vector2Int posicion, IFicha ficha)
    {
        //
    }



    public bool EsInamovible() => esInamovible;
    public void SetPosicionActual(Vector2Int nuevaPos) => tileCoords = nuevaPos;
    public Vector2Int GetPosicionActual() => tileCoords;

    public void VerificarTurnoActual(int turnoActual) { }

    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (posicionRey == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey llegó a PusherUp en {tileCoords}.");
            BoardManagerGlobal.Instance.EfectoPusherLeft(rey);
            SoundManager.Instance.PlaySound(31);
            rey.MostrarMovimientoPosible(); // refresco UI si lo usas
        }
    }


    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        if (posicionPeon == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón llegó a PusherUp en {tileCoords}.");
            BoardManagerGlobal.Instance.EfectoPusherLeft(peon);
            SoundManager.Instance.PlaySound(31);
            peon.MostrarMovimientoPosible(); // refresco UI si lo usas
        }
    }

    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reina)
    {
        //
    }

    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinaenemiga)
    {
        //
    }
    public void RevisarSiTorreNegraEnemigaLlegó(Vector2Int posicion, BlackRookEnemyController torrenegraenemiga)
    {
        //
    }
    public void RevisarSiAlfilNegroEnemigoLlegó(Vector2Int posicion, BlackBishopEnemyController alfilnegroenemigo)
    {
        //
    }

    public void RevisarSiCaballoNegroEnemigoLlegó(Vector2Int posicion, BlackKnightEnemyController caballonegroenemigo)
    {
        //
    }

    public void RevisarSiTorreEnemigaLlegó(Vector2Int posicion, RookEnemyController torreenemiga)
    {
        //
    }

    public void RevisarSiAlfilEnemigoLlegó(Vector2Int posicion, BishopEnemyController alfilenemigo)
    {
        //
    }

    public void RevisarSiCaballoEnemigoLlegó(Vector2Int posicion, KnightEnemyController caballoenemigo)
    {
        //
    }
    public void RevisarSiPeonEnemigoLlegó(Vector2Int posicion, PawnEnemyController peonenemigo)
    {
        //
    }

    public void RevisarSiAlfilLlegó(Vector2Int posicionAlfil, BishopController alfil)
    {
        //
    }
    public void RevisarSiCaballoLlegó(Vector2Int posicionCaballo, KnightController caballo)
    {
        //
    }
    public void RevisarSiTorreLlegó(Vector2Int posicionTorre, RookController torre)
    {
        //
    }
    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
        //
    }
    
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
}
