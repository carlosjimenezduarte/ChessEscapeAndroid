using UnityEngine;

public class Vortex : MonoBehaviour, ITileEffect, IFicha, IFichaInmovil, IPieceWithPosition
{
    public Vector2Int tileCoords;

    private bool esInamovible = false;

    private void Start()
    {
        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null)
        {
            tileCoords = posicionador.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🌪️ Vortex inició en {tileCoords}");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚠️ Vortex sin PiecePositioner. tileCoords no inicializado.");
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
            SoundManager.Instance.PlaySound(32);
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♔ Rey fue absorbido por Vortex en {tileCoords}. Enviando a (0,0).");

            var destino = BoardManagerGlobal.Instance.CalcularCasillaSeguraParaVortex(rey);
            rey.TeletransportarA(destino);
            rey.MostrarMovimientoPosible(); // refresco UI si lo usas
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiPeonLlegó(Vector2Int pos, PawnController peon)
    {
        if (pos == tileCoords)
        {
            SoundManager.Instance.PlaySound(32);
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♙ Peón fue absorbido por Vortex en {tileCoords}. Enviando a Dimensión Divina y destruyendo.");
            ExiliarAliadoYDestruir(peon);
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiAlfilLlegó(Vector2Int pos, BishopController alfil)
    {
        if (pos == tileCoords)
        {
            SoundManager.Instance.PlaySound(32);
            alfil.OcultarMovimientos();
            alfil.mostrandoMovimientos = false;
            alfil.rangoMovimientoBase = -9000;
            ExiliarAliadoYDestruir(alfil);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♗ Alfil fue absorbido por Vortex en {tileCoords}. Enviando a Dimensión Divina y destruyendo.");
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiCaballoLlegó(Vector2Int pos, KnightController caballo)
    {
        if (pos == tileCoords)
        {
            SoundManager.Instance.PlaySound(32);
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♘ Caballo fue absorbido por Vortex en {tileCoords}. Enviando a Dimensión Divina y destruyendo.");
            ExiliarAliadoYDestruir(caballo);
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiTorreLlegó(Vector2Int pos, RookController torre)
    {
        if (pos == tileCoords)
        {
            SoundManager.Instance.PlaySound(32);
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♖ Torre fue absorbida por Vortex en {tileCoords}. Enviando a Dimensión Divina y destruyendo.");
            ExiliarAliadoYDestruir(torre);
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiReinaLlegó(Vector2Int pos, QueenController reina)
    {
        if (pos == tileCoords)
        {
            SoundManager.Instance.PlaySound(32);
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♕ Reina fue absorbida por Vortex en {tileCoords}. Enviando a Dimensión Divina y destruyendo.");
            ExiliarAliadoYDestruir(reina);
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Enemigas: no aplica (opcional: log informativo)
    // ─────────────────────────────────────────────────────────────────────────────
    public void RevisarSiReinaEnemigaLlegó(Vector2Int pos, QueenEnemyController r) { /* sin efecto */ }
    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int pos, BlackQueenEnemyController r) { /* sin efecto */ }
    public void RevisarSiTorreNegraEnemigaLlegó(Vector2Int pos, BlackRookEnemyController r) { /* sin efecto */ }
    public void RevisarSiAlfilNegroEnemigoLlegó(Vector2Int pos, BlackBishopEnemyController r) { /* sin efecto */ }
    public void RevisarSiCaballoNegroEnemigoLlegó(Vector2Int pos, BlackKnightEnemyController r) { /* sin efecto */ }
    public void RevisarSiTorreEnemigaLlegó(Vector2Int pos, RookEnemyController r) { /* sin efecto */ }
    public void RevisarSiAlfilEnemigoLlegó(Vector2Int pos, BishopEnemyController r) { /* sin efecto */ }
    public void RevisarSiCaballoEnemigoLlegó(Vector2Int pos, KnightEnemyController r) { /* sin efecto */ }
    public void RevisarSiPeonEnemigoLlegó(Vector2Int pos, PawnEnemyController r) { /* sin efecto */ }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        // Genérico opcional si más adelante deseas enrutar por tipo dinámicamente
    }

    public void VerificarTurnoActual(int turnoActual) { }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers núcleo
    // ─────────────────────────────────────────────────────────────────────────────
    private void ExiliarAliadoYDestruir(MonoBehaviour aliado)
    {
        Vector2Int exilio = BoardManagerGlobal.DimensionDivina;

        // Actualiza las coords lógicas antes de destruir
        (aliado as IPieceWithPosition)?.SetPosicionActual(exilio);

        // Si quieres notificar o desocupar explícitamente, hazlo aquí (si tienes un método en tu BMG)
        // BoardManagerGlobal.Instance.Desregistrar(aliado); // <-- solo si existe

        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"🕳️ {aliado.name} enviado a Dimensión Divina {exilio} y destruido.");

        Destroy(aliado.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // IPieceWithPosition + utilidades
    // ─────────────────────────────────────────────────────────────────────────────
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
