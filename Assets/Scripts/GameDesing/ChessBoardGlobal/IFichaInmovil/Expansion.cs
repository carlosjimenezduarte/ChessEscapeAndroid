using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Expansion : MonoBehaviour, ITileEffect, IFicha, IFichaInmovil, IPieceWithPosition
{
    public Vector2Int tileCoords;

    private bool esInamovible = false;

    private void Start()
    {
        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null)
        {
            tileCoords = posicionador.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧩 Expansion inició en {tileCoords}");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚠️ Expansion sin PiecePositioner. tileCoords no inicializado.");
        }
        if (BoardManagerGlobal.Instance != null)
        {
            // ✅ Reporte manual al Árbitro Silencioso
            BoardManagerGlobal.Instance.ReportarFichaInamovible(this);
        }

    }
    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (posicionRey == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey llegó a casilla con Expansion en {tileCoords}.");
            ActivarExpansion(rey);
            rey.MostrarMovimientoPosible();           
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

   
    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        if (posicionPeon == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón llegó a casilla con Expansion en {tileCoords}.");
            ActivarExpansion(peon);
            peon.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiAlfilLlegó(Vector2Int posicionAlfil, BishopController alfil)
    {
       if (posicionAlfil == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♗ Alfil llegó a casilla con Expansion en {tileCoords}.");
            ActivarExpansion(alfil);
            alfil.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }
    public void RevisarSiCaballoLlegó(Vector2Int posicionCaballo, KnightController caballo)
    {
        if (posicionCaballo == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♘ Caballo llegó a casilla con Expansion en {tileCoords}.");
            ActivarExpansion(caballo);
            caballo.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }
    public void RevisarSiTorreLlegó(Vector2Int posicionTorre, RookController torre)
    {
        if (posicionTorre == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♖ Torre llegó a casilla con Expansion en {tileCoords}.");
            ActivarExpansion(torre);
            torre.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    } 
    
    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
        if (posicionReina == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♕ Peón llegó a casilla con Expansion en {tileCoords}.");
            ActivarExpansion(reina);
            reina.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        //
    }

    public void VerificarTurnoActual(int turnoActual) { }

    private void ActivarExpansion(MonoBehaviour activador)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💢 Expansion en {tileCoords} se activa por {activador.name}.");

        List<MovableTileObject> todos = BoardManagerGlobal.Instance.GetObjetosMoviblesOrdenadosDesde(tileCoords);

        foreach (var obj in todos)
        {
            if (EsIgnorable(obj, activador))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 {obj.name} fue ignorado por Expansion.");
                continue;
            }

            if (!EsDentroTablero(obj.tileCoords))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🌌 {obj.name} está fuera del tablero en {obj.tileCoords}.");
                continue;
            }

            Vector2Int dir = CalcularDireccion(obj.tileCoords - tileCoords);
            Vector2Int destino = obj.tileCoords + dir;

            if (!EsDentroTablero(destino))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 {obj.name} no puede empujarse fuera del tablero hacia {destino}.");
                continue;
            }

            BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 {obj.name} empujado de {obj.tileCoords} a {destino}.");
            obj.MoverA(destino);
        }
        SoundManager.Instance.PlaySound(34);
    }

    private bool EsIgnorable(MovableTileObject obj, MonoBehaviour activador)
    {
        if (obj.tileCoords == tileCoords) return true;

        if (activador != null && ReferenceEquals(obj.gameObject, activador.gameObject))
            return true;

        return false;
    }
    private Vector2Int CalcularDireccion(Vector2Int delta)
    {
        if (delta.x == 0 && delta.y == 0) return Vector2Int.zero;

        if (Mathf.Abs(delta.x) == Mathf.Abs(delta.y))
            return new Vector2Int((int)Mathf.Sign(delta.x), (int)Mathf.Sign(delta.y));
        if (delta.y == 0)
            return new Vector2Int((int)Mathf.Sign(delta.x), 0);
        if (delta.x == 0)
            return new Vector2Int(0, (int)Mathf.Sign(delta.y));

        return Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? new Vector2Int((int)Mathf.Sign(delta.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(delta.y));
    }

    private bool EsDentroTablero(Vector2Int p)
        => p.x >= 0 && p.x <= 7 && p.y >= 0 && p.y <= 7;

    public bool EsInamovible()
    {
        return esInamovible;
    }

    public void SetPosicionActual(Vector2Int nuevaPos)
    {
        tileCoords = nuevaPos;
    }

    public Vector2Int GetPosicionActual()
    {
        return tileCoords;
    }

    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
    {
        //
    }


    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinanegraenemiga)
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

    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
} 