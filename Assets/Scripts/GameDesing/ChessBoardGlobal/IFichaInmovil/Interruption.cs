using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class Interruption : MonoBehaviour, ITileEffect, IFicha, IFichaInmovil, IPieceWithPosition
{
    public Vector2Int tileCoords;
    public bool esInamovible = true;

    private int turnoActivado = -1;

    private void Start()
    {
        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null)
        {
            tileCoords = posicionador.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⏸️ Interruption colocada en {tileCoords}");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ Interruption sin PiecePositioner. Usando coordenadas por defecto.");
        }

        if (BoardManagerGlobal.Instance != null)
            BoardManagerGlobal.Instance.ReportarFichaInamovible(this);
    }

    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (posicionRey == tileCoords && turnoActivado != BoardManagerGlobal.Instance.GetTurnoActual())
        {
            ActivarInterruption(rey);
        }
    }

    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        if (posicionPeon == tileCoords && turnoActivado != BoardManagerGlobal.Instance.GetTurnoActual())
        {
            ActivarInterruption(peon);
        }
    }

    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
    {
        //RevisarSiReinaEnemigaLlegó(posicion, rey);
    }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        if (posicionFicha == tileCoords && turnoActivado != BoardManagerGlobal.Instance.GetTurnoActual())
        {
            ActivarInterruption((MonoBehaviour)ficha);
        }
    }

    public void VerificarTurnoActual(int turnoActual)
    {
        // No hace nada por sí sola en cada turno, solo se activa al ser pisada
    }

    private void ActivarInterruption(MonoBehaviour activador)
    {
        turnoActivado = BoardManagerGlobal.Instance.GetTurnoActual();

        BoardManagerGlobal.Instance.AgregarMensajeInterno($"⏹️ Interruption activada en {tileCoords} por {activador.name}");

        // Cancelar movimientos de todas las fichas aliadas excepto el activador
        var fichasAliadas = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IFichaAliada>();

        foreach (var ficha in fichasAliadas)
        {
            if (ficha == (object)activador) continue;
            ficha.OcultarMovimientos();
            ficha.rangoAtaque = 0;
            ficha.rangoMovimientoBase = 0;
            SoundManager.Instance.PlaySound(35);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧊 Movimiento bloqueado para aliada {((MonoBehaviour)ficha).name}");
        }

        // ♔ Penalizar al Rey solo si él NO fue el activador
        var rey = Object.FindFirstObjectByType<KingController>();
        if (rey != null && rey != (object)activador)
        {
            rey.OcultarMovimientos();
            rey.puntosMovimientoActual = 0;
            rey.rangoAtaqueKing = 0;
            SoundManager.Instance.PlaySound(35);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey penalizado por Interruption (no fue el activador)");
        }

        // Cancelar ataques de enemigos
        var fichasEnemigas = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IFichaEnemiga>();

        foreach (var enemigo in fichasEnemigas)
        {
            enemigo.OcultarRango();
            enemigo.rangoKillZone = 0;
            enemigo.rangoRangeZone = 0;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧊 Ataque bloqueado para enemiga {((MonoBehaviour)enemigo).name}");
        }


        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public bool EsInamovible() => esInamovible;

    public void SetPosicionActual(Vector2Int nuevaPos)
    {
        tileCoords = nuevaPos;
    }

    public Vector2Int GetPosicionActual()
    {
        return tileCoords;
    }

    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinaenemiga)
    {
        //RevisarSiReinaEnemigaLlegó(posicion, rey);
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
