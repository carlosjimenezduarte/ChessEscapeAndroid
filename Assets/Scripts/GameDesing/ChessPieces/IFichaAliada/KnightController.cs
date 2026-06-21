using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class KnightController : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
{
    [Header("Rango de Movimiento")]
    public bool tieneEscudo = false;
    public int rangoMovimientoBase { get; set; } = 1;
    private int rangoMovimientoExtra = 0;
    public int rangoAtaque { get; set; } = 1;

    public bool esInamovible = false;
    public int RangoMovimientoActual => rangoMovimientoBase + rangoMovimientoExtra;

    private Vector2Int posicionActual;
    private bool juegoActivo = false;
    public bool mostrandoMovimientos = false;

    public readonly Vector2Int[] movimientosEnL = new Vector2Int[]
    {
        new Vector2Int(2, 1), new Vector2Int(1, 2),
        new Vector2Int(-1, 2), new Vector2Int(-2, 1),
        new Vector2Int(-2, -1), new Vector2Int(-1, -2),
        new Vector2Int(1, -2), new Vector2Int(2, -1)
    };

   

    private void Start()
    {
        posicionActual = GetComponent<PiecePositioner>()?.tileCoords ?? new Vector2Int(0, 0);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♘ Caballo inició en {posicionActual}.");

        var objetosEnCasilla = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual);
        bool yaRegistrado = objetosEnCasilla.Contains(this);

        if (!yaRegistrado)
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ ♘ Caballo registrado manualmente en {posicionActual}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"ℹ️ ♘ Caballo ya estaba registrado.");
        }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void SetPosicionActual(Vector2Int nuevaPos)
    {
        #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    posicionActual = nuevaPos;
                    return;
                }
        #endif
        posicionActual = nuevaPos;

        GetComponent<MovableTileObject>().tileCoords = nuevaPos;
        GetComponent<PiecePositioner>().tileCoords = nuevaPos;

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♘ Caballo movido a {nuevaPos}.");
    }

    public Vector2Int GetPosicionActual() => posicionActual;

    public void ActivarJuego()
    {
        juegoActivo = true;
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MostrarMovimientoPosible()
{
    if (!juegoActivo) return;

    var rey = FindFirstObjectByType<KingController>();
    if (rey == null || rey.puntosAccionActual <= 0)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ Rey sin PA, Caballo no puede moverse.");
        OcultarMovimientos();
        mostrandoMovimientos = false;
        return;
    }

    BoardManagerGlobal.Instance.AgregarMensajeInterno("🔍 Mostrando posibles movimientos en L del Caballo.");

    // 🔵 Casilla actual
    var casillaActual = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
    if (casillaActual != null)
    {
        if (tieneEscudo) casillaActual.Shield(true); // ✅ marcar dorado si hay escudo
        casillaActual.HighlightMove(true);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔵 Casilla central del Caballo ({posicionActual}) marcada como centro.");
    }

    foreach (var delta in movimientosEnL)
    {
        Vector2Int destino = posicionActual + delta;
        if (!BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(destino))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⛔ Casilla {destino} no accesible para el Caballo.");
            continue;
        }

        var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
        if (tile == null) continue;

        if (tieneEscudo) tile.Shield(true); // ✅ marcar dorado si hay escudo
        tile.HighlightMove(true);

        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🟦 Casilla {destino} marcada como movimiento válido.");
    }
}

    public void OcultarMovimientos()
    {
        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
            tile.HighlightMove(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var gameManager = FindFirstObjectByType<ChessGameManager>();
        bool esNuevaSeleccion = gameManager.fichaSeleccionadaActual != this;
        gameManager.fichaSeleccionadaActual = this;

        var rey = FindFirstObjectByType<KingController>();
        if (rey != null && rey != this)
            rey.OcultarMovimientos();

        if (esNuevaSeleccion || !mostrandoMovimientos)
        {
            mostrandoMovimientos = true;
            MostrarMovimientoPosible();
            StartCoroutine(EvaluarCasillasDeAtaque());
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🟢 Mostrando previsualización automática del Caballo.");
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🔴 Ocultando previsualización del Caballo.");
        }
        
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos, KingController rey)
    {
        if (!juegoActivo) return;

        Vector2Int desplazamiento = nuevaPos - posicionActual;
        if (!movimientosEnL.Contains(desplazamiento))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 Movimiento inválido para el Caballo desde {posicionActual} a {nuevaPos}.");
            return;
        }
        if (rey.puntosAccionActual <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🚫 Movimiento inválido. Rey sin PA.");
            return;
        }

        var objetosEnDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(nuevaPos);
        var enemigo = objetosEnDestino.FirstOrDefault(o => o is IFichaEnemiga);

        SetPosicionActual(nuevaPos);
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);
        SoundManager.Instance.PlaySound(0);

        if (enemigo != null)
        {
            if (enemigo is IPieceWithPosition enemigoPos)
                enemigoPos.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

            Destroy(((MonoBehaviour)enemigo).gameObject);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 Caballo eliminó a un enemigo en {nuevaPos}.");
            SoundManager.Instance.PlaySound(1);
        }

        foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
            efecto.RevisarSiCaballoLlegó(nuevaPos, this);

        rey.puntosAccionActual--;
        OcultarMovimientos();
        MostrarMovimientoPosible();
        StartCoroutine(EvaluarCasillasDeAtaque());
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
        
        if (posicionActual == new Vector2Int(7, 7))
        {
        BoardManagerGlobal.Instance.AgregarMensajeInterno("♕ Caballo coronado en H8. Bonificaciones aplicadas.");
        BoardManagerGlobal.Instance?.NotifyAllyCoronated(BoardManagerGlobal.AllyKind.Knight);
        rey.puntosAccionActual += 7;
        rey.puntosMovimientoActual += 7;
        rey.GanarVida(7);
        var gameManager = FindFirstObjectByType<ChessGameManager>();
        if (gameManager != null)
        {
            gameManager.SeleccionarReyTrasCoronacion();
        }
        else
        {
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        }
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        OcultarMovimientos();
        rey.MostrarMovimientoPosible();
        SoundManager.Instance.PlaySound(2);
        Destroy(gameObject);
        
        }
    }

    public void AumentarRangoMovimiento(int cantidad)
    {
        var reycaballoPA = FindFirstObjectByType<KingController>();
        reycaballoPA.puntosAccionActual += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🏇 Caballo ganó +{cantidad} de rango temporal. Total: {RangoMovimientoActual}.");
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballoPA.puntosAccionActual}"
            );
    }
    public void AumentarRangoMovimientoSilencioso(int cantidad)
    {
        var reycaballoPASilencioso = FindFirstObjectByType<KingController>();
        reycaballoPASilencioso.puntosAccionActual += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Caballo ganó +{cantidad} de rango temporal en modo silencioso. Total: {RangoMovimientoActual}.");
        // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballoPASilencioso.puntosAccionActual}"
            );
    }
    
    public void RestarRangoMovimiento(int cantidad)
    {
        var reycaballoPASilencioso = FindFirstObjectByType<KingController>();
        reycaballoPASilencioso.puntosAccionActual -= cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Caballo ganó +{cantidad} de rango temporal en modo silencioso. Total: {RangoMovimientoActual}.");
        // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballoPASilencioso.puntosAccionActual}"
            );
        
    }
    public void RestarRangoMovimientoSilencioso(int cantidad)
    {    
        var reycaballoPASilencioso = FindFirstObjectByType<KingController>();
        reycaballoPASilencioso.puntosAccionActual -= cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Caballo ganó +{cantidad} de rango temporal en modo silencioso. Total: {RangoMovimientoActual}.");
        // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballoPASilencioso.puntosAccionActual}"
            );
    }


    public void AumentarVida(int cantidad)
    {
        var reyUP = FindFirstObjectByType<KingController>();
        reyUP.turnosRestantes += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Reina ganó +{cantidad} de rango en silencio. Total: {RangoMovimientoActual}.");
        // 🔹 Actualizar HUD inmediatamente
        var gameManager = FindFirstObjectByType<ChessGameManager>();
        if (gameManager != null)
            gameManager.ActualizarHUD();

    }

    public void AumentarPA(int cantidad)
    {
        var reyPA = FindFirstObjectByType<KingController>();
        reyPA.puntosAccionActual += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Reina ganó +{cantidad} de rango en silencio. Total: {RangoMovimientoActual}.");
        // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

    }

    public void DesactivarJuego() => juegoActivo = false;
    public void MostrarRango() => MostrarMovimientoPosible();
    public void OcultarRango() => OcultarMovimientos();
    public void ReiniciarTurno()
    {
        rangoMovimientoBase = 1;
        rangoAtaque = 1;
        StartCoroutine(EvaluarCasillasDeAtaque());
        tieneEscudo = false;

        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
        {
            tile.Shield(false); // apagar dorado
        }
    }

    

    public bool EstaActivo() => juegoActivo;
    public bool EsInamovible() => esInamovible;

    public void RecibirPenalizacionReina()
    {
        var reycaballo = FindFirstObjectByType<KingController>();
        if (reycaballo != null)
        {
            reycaballo.puntosAccionActual -= 1;

            // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballo.puntosAccionActual}"
            );
        }
    }

    public void RecibirPenalizacionTorre()
    {
        var reycaballotorre = FindFirstObjectByType<KingController>();
        if (reycaballotorre != null)
        {
            reycaballotorre.puntosAccionActual -= 1;

            // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♜ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballotorre.puntosAccionActual}"
            );
        }
    }

    public void RecibirPenalizacionAlfil()
    {
        var reycaballoalfil = FindFirstObjectByType<KingController>();
        if (reycaballoalfil != null)
        {
            reycaballoalfil.puntosAccionActual -= 1;

            // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♝ Caballo en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reycaballoalfil.puntosAccionActual}"
            );
        }
    }
    
    private IEnumerator EvaluarCasillasDeAtaque()
    {
    // 🕒 Esperar 0.03 segundos reales antes de evaluar
    yield return new WaitForSeconds(0.2f);

    var gameManager = FindFirstObjectByType<ChessGameManager>();
    if (gameManager == null || gameManager.fichaSeleccionadaActual != this)
    yield break;

    BoardManagerGlobal.Instance.AgregarMensajeInterno("🔁 Evaluando casillas de ataque reales del Caballo...");

    foreach (var delta in movimientosEnL)
    {
        Vector2Int destino = posicionActual + delta;
        if (!BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(destino))
            continue;

        var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
        if (tile == null) continue;

        var objetosEnDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);

        if (objetosEnDestino.Any(obj => obj is IFichaEnemiga))
        {
            tile.HighlightEnemyAttack(true);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎯 Casilla {destino} marcada como zona de ataque (post-movimiento).");
        }
    }
    }
}
