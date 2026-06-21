using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Attraction : MonoBehaviour, ITileEffect, IFicha, IFichaInmovil, IPieceWithPosition
{
    public Vector2Int tileCoords;

    private bool esInamovible = false;

    private void Start()
    {
        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null)
        {
            tileCoords = posicionador.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧲 Atraction inició en {tileCoords}");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚠️ Atraction sin PiecePositioner. tileCoords no inicializado.");
        }

        if (BoardManagerGlobal.Instance != null)
        {
            // ✅ Reporte manual al Árbitro Silencioso
            BoardManagerGlobal.Instance.ReportarFichaInamovible(this);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Disparadores por llegada de fichas ALIADAS
    // ─────────────────────────────────────────────────────────────────────────────
    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (posicionRey == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey llegó a casilla con Atraction en {tileCoords}.");
            ActivarAtraction(rey);
            rey.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        if (posicionPeon == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón llegó a casilla con Atraction en {tileCoords}.");
            ActivarAtraction(peon);
            peon.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiAlfilLlegó(Vector2Int posicionAlfil, BishopController alfil)
    {
        if (posicionAlfil == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♗ Alfil llegó a casilla con Atraction en {tileCoords}.");
            ActivarAtraction(alfil);
            alfil.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiCaballoLlegó(Vector2Int posicionCaballo, KnightController caballo)
    {
        if (posicionCaballo == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♘ Caballo llegó a casilla con Atraction en {tileCoords}.");
            ActivarAtraction(caballo);
            caballo.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiTorreLlegó(Vector2Int posicionTorre, RookController torre)
    {
        if (posicionTorre == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♖ Torre llegó a casilla con Atraction en {tileCoords}.");
            ActivarAtraction(torre);
            torre.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
        if (posicionReina == tileCoords)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♕ Reina llegó a casilla con Atraction en {tileCoords}.");
            ActivarAtraction(reina);
            reina.MostrarMovimientoPosible();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    // Versiones “enemigas/otras” si luego las necesitas:
    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga) { /* opcional */ }
    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinanegraenemiga) { /* opcional */ }
    public void RevisarSiTorreNegraEnemigaLlegó(Vector2Int posicion, BlackRookEnemyController torrenegraenemiga) { /* opcional */ }
    public void RevisarSiAlfilNegroEnemigoLlegó(Vector2Int posicion, BlackBishopEnemyController alfilnegroenemigo) { /* opcional */ }
    public void RevisarSiCaballoNegroEnemigoLlegó(Vector2Int posicion, BlackKnightEnemyController caballonegroenemigo) { /* opcional */ }
    public void RevisarSiTorreEnemigaLlegó(Vector2Int posicion, RookEnemyController torreenemiga) { /* opcional */ }
    public void RevisarSiAlfilEnemigoLlegó(Vector2Int posicion, BishopEnemyController alfilenemigo) { /* opcional */ }
    public void RevisarSiCaballoEnemigoLlegó(Vector2Int posicion, KnightEnemyController caballoenemigo) { /* opcional */ }
    public void RevisarSiPeonEnemigoLlegó(Vector2Int posicion, PawnEnemyController peonenemigo) { /* opcional */ }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        // Implementación genérica opcional si la quieres habilitar luego
    }

    public void VerificarTurnoActual(int turnoActual) { }

    private bool EsCentro(Vector2Int p) => p == tileCoords;

    // ─────────────────────────────────────────────────────────────────────────────
    // Núcleo: ATRACCIÓN (inverso de Expansion)
    // ─────────────────────────────────────────────────────────────────────────────
    // 2) Ajuste en ActivarAtraction: bloquear si ya está en el anillo o si el destino cae en él
    // Núcleo: permitir acercarse, pero jamás aterrizar en el centro
    private void ActivarAtraction(MonoBehaviour activador)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧲 Atraction en {tileCoords} se activa por {activador.name}.");

        List<MovableTileObject> todos = BoardManagerGlobal.Instance.GetObjetosMoviblesOrdenadosDesde(tileCoords);

        foreach (var obj in todos)
        {
            if (EsIgnorable(obj, activador))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 {obj.name} ignorado por Atraction.");
                continue;
            }

            if (!EsDentroTablero(obj.tileCoords))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🌌 {obj.name} fuera del tablero en {obj.tileCoords}.");
                continue;
            }

            Vector2Int dirDesdeCentro = CalcularDireccion(obj.tileCoords - tileCoords);
            if (dirDesdeCentro == Vector2Int.zero)
            {
                // Ya está en el centro (por seguridad extra)
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"😌 {obj.name} ya está en el centro {tileCoords}.");
                continue;
            }

            Vector2Int destino = obj.tileCoords - dirDesdeCentro;

            if (!EsDentroTablero(destino))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 {obj.name} no puede atraerse fuera del tablero hacia {destino}.");
                continue;
            }

            // 💡 Clave: bloquear solo si el destino es el centro
            if (EsCentro(destino))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧱 {obj.name} no se mueve: destino {destino} es el centro. Se permite quedar alrededor.");
                continue;
            }

            BoardManagerGlobal.Instance.AgregarMensajeInterno($"➡️ {obj.name} atraído de {obj.tileCoords} a {destino}.");
            obj.MoverA(destino);
        }
        SoundManager.Instance.PlaySound(33);
    }


    // ─────────────────────────────────────────────────────────────────────────────
    // Utilidades
    // ─────────────────────────────────────────────────────────────────────────────
    private bool EsIgnorable(MovableTileObject obj, MonoBehaviour activador)
    {
        // Ignora el centro (ya está en tileCoords)
        if (EsCentro(obj.tileCoords)) return true;

        // Ignora a la ficha activadora
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

        // Si no es perfectamente ortogonal/diagonal, normalizamos hacia el eje dominante
        return Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? new Vector2Int((int)Mathf.Sign(delta.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(delta.y));
    }

    private bool EsDentroTablero(Vector2Int p)
        => p.x >= 0 && p.x <= 7 && p.y >= 0 && p.y <= 7;

    public bool EsInamovible() => esInamovible;

    public void SetPosicionActual(Vector2Int nuevaPos) => tileCoords = nuevaPos;

    public Vector2Int GetPosicionActual() => tileCoords;
    
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
}
