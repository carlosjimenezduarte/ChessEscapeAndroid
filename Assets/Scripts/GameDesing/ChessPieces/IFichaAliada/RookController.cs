using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class RookController : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
{
    [Header("Rangos")]
    public bool tieneEscudo = false;
    public int rangoMovimientoBase { get; set; } = 5;
    private int rangoMovimientoExtra = 0;
    public int rangoAtaque { get; set; } = 3;

    public bool esInamovible = false;
    public int RangoMovimientoActual => rangoMovimientoBase + rangoMovimientoExtra;

    private Vector2Int posicionActual;
    private bool juegoActivo = false;
    public bool mostrandoMovimientos = false;

    private void Start()
    {
        posicionActual = GetComponent<PiecePositioner>()?.tileCoords ?? new Vector2Int(0, 0);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Torre inició en {posicionActual}.");

        var objetosEnCasilla = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual);
        bool yaRegistrado = objetosEnCasilla.Contains(this);

        if (!yaRegistrado)
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ ♜ Torre registrada manualmente en {posicionActual}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"ℹ️ ♜ Torre ya estaba registrada.");
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Torre movida a {nuevaPos}.");
    }

    public Vector2Int GetPosicionActual() => posicionActual;

    public void ActivarJuego()
    {
        juegoActivo = true;
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    // 🧱 True si hay un Wall entre origen y destino (o en el destino).
    // La Torre solo se mueve ortogonal, así que recorremos en línea recta.
    private bool HayMuroEntreOrtogonal(Vector2Int origen, Vector2Int destino)
    {
        // Si no es ortogonal, no aplica (la Torre no debería llamarlo en diagonal).
        if (origen.x != destino.x && origen.y != destino.y) return false;

        int stepx = origen.x == destino.x ? 0 : (destino.x > origen.x ? 1 : -1);
        int stepy = origen.y == destino.y ? 0 : (destino.y > origen.y ? 1 : -1);

        Vector2Int paso = origen;

        // casillas intermedias
        while (true)
        {
            paso = new Vector2Int(paso.x + stepx, paso.y + stepy);
            if (paso == destino) break;

            var objsInter = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);
            if (objsInter.OfType<Wall>().Any())
                return true;
        }

        // también si el destino es un Wall
        var objsDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);
        if (objsDestino.OfType<Wall>().Any())
            return true;

        return false;
    }



    public void MostrarMovimientoPosible()
    {
        if (!juegoActivo) return;

        var rey = FindFirstObjectByType<KingController>();
        if (rey == null || rey.puntosAccionActual <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ Rey sin PA, Torre no puede moverse.");
            OcultarMovimientos();
            mostrandoMovimientos = false;
            return;
        }

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🔍 Mostrando posibles movimientos ortogonales de la Torre:");

        Vector2Int[] direcciones = new Vector2Int[]
        {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        // 🔵 Casilla actual
        var casillaActual = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (casillaActual != null)
        {
            if (tieneEscudo) casillaActual.Shield(true); // ✅ marcar dorado si hay escudo
            casillaActual.HighlightMove(true);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔵 Casilla central de la Torre ({posicionActual}) marcada como centro.");
        }

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= RangoMovimientoActual; i++)
            {
                Vector2Int destino = posicionActual + dir * i;

                if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, destino, this))
                    break;

                if (HayMuroEntreOrtogonal(posicionActual, destino))
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧱 Muro corta visual hacia {destino}.");
                    break;
                }


                if (!BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(destino))
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"⛔ Casilla {destino} no accesible o fuera del tablero.");
                    break;
                }

                var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
                if (tile == null) break;

                if (tieneEscudo) tile.Shield(true); // ✅ marcar dorado si hay escudo
                tile.HighlightMove(true);

                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🟦 Casilla {destino} marcada como movimiento válido.");

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);
                if (objetos.Any(obj => obj is IFicha))
                    break;
            }
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🟢 Mostrando previsualización automática de la Torre.");
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🔴 Ocultando previsualización de la Torre.");
        }
        
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos, KingController rey)
    {
    if (!juegoActivo) return;

    Vector2Int delta = nuevaPos - posicionActual;

    // 1) Patrón ortogonal + rango
    bool esMovimientoValido = false;
    if (delta.x == 0 || delta.y == 0)
    {
        int distancia = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        esMovimientoValido = distancia <= RangoMovimientoActual;
    }

    if (!esMovimientoValido)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"🚫 Movimiento inválido para la Torre desde {posicionActual} a {nuevaPos} (patrón/rango).");
        return;
    }

    // 2) Bloqueo por obstáculo en el trayecto (aliado, inmóvil o recoleccionable)
    if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, nuevaPos, this))
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"🛑 Movimiento bloqueado: hay un obstáculo entre {posicionActual} y {nuevaPos}.");
        return;
    }
    
    if (rey.puntosAccionActual <= 0)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno("🚫 Movimiento inválido. Rey sin PA.");
        return;
    }

    // 3) Evaluar destino (enemigo/recoleccionable permitido)
        var objetosEnDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(nuevaPos);
    var enemigo = objetosEnDestino.FirstOrDefault(o => o is IFichaEnemiga);

    // 4) Mover
    SetPosicionActual(nuevaPos);
    transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);

    // 5) Resolver combate si hay enemigo
    if (enemigo != null)
    {
        if (enemigo is IPieceWithPosition enemigoPos)
            enemigoPos.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

        Destroy(((MonoBehaviour)enemigo).gameObject);
        SoundManager.Instance.PlaySound(1);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 Torre eliminó a un enemigo en {nuevaPos}.");
    }

    // 6) Disparar efectos de casilla (recolecciones, trampas, etc.)
    foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
        efecto.RevisarSiTorreLlegó(nuevaPos, this);

    // 7) Coste y refrescos
    rey.puntosAccionActual--;
    rangoMovimientoBase--;
    OcultarMovimientos();
    MostrarMovimientoPosible();
    RevisarObjetosEnCasilla();
    StartCoroutine(EvaluarCasillasDeAtaque());
    FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
    SoundManager.Instance.PlaySound(0);
    BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);

        if (posicionActual == new Vector2Int(7, 7))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♕ Torre coronada en H8. Bonificaciones aplicadas.");
            BoardManagerGlobal.Instance?.NotifyAllyCoronated(BoardManagerGlobal.AllyKind.Rook);
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

    BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();  
    }

    public void RevisarObjetosEnCasilla()
    {
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is ITileEffect efecto)
            {
                efecto.RevisarSiTorreLlegó(posicionActual, this);
                MostrarMovimientoPosible();
            }
        }
    }

    private IEnumerator EvaluarCasillasDeAtaque()
    {
        yield return new WaitForSeconds(0.2f);

        var gameManager = FindFirstObjectByType<ChessGameManager>();
        if (gameManager == null || gameManager.fichaSeleccionadaActual != this)
        yield break;

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🔁 Evaluando casillas de ataque reales de la Torre...");

        Vector2Int[] direcciones = new Vector2Int[]
        {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= rangoAtaque; i++)
            {
                Vector2Int destino = posicionActual + dir * i;

                // ⛔ Bordes del tablero
                if (destino.x < 0 || destino.y < 0 || destino.x > 7 || destino.y > 7)
                    break;

                // 🧱 Obstáculo ENTRE origen y destino bloquea la línea
                if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, destino, this))
                    break;

                var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
                if (tile == null) break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);

                // 👥 Aliado / Inmóvil en destino: bloquea (no es atacable)
                if (objetos.Any(obj => obj is IFichaAliada || obj is IFichaInmovil))
                    break;

                // 🎒 Recoleccionable en destino: también bloquea (no mirar más allá)
                if (objetos.Any(obj => obj is IObjetoRecoleccionable))
                    break;

                // 🎯 Enemigo en destino: marcar y cortar la línea
                if (objetos.Any(obj => obj is IFichaEnemiga))
                {
                    tile.HighlightEnemyAttack(true);
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎯 Casilla {destino} marcada como zona de ataque (post-movimiento).");
                    break;
                }

                // Sin enemigo: continuar explorando hasta rangoAtaque
            }
        }
    }


    public void AumentarRangoMovimiento(int cantidad)
    {
        rangoMovimientoBase += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"📏 Torre ganó +{cantidad} de rango temporal. Total: {RangoMovimientoActual}.");
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
    }

    public void RestarRangoMovimiento(int cantidad)
    {
    
        rangoMovimientoExtra -= cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"📏 Reina perdió +{cantidad} de rango temporal. Total: {RangoMovimientoActual}.");
        
    }
    public void RestarRangoMovimientoSilencioso(int cantidad)
    {    
        rangoMovimientoExtra -= cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Reina perdió +{cantidad} de rango en silencio. Total: {RangoMovimientoActual}.");
    }


    public void AumentarVida(int cantidad)
    {
        var reyUP = FindFirstObjectByType<KingController>();
        reyUP.turnosRestantes += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Reina ganó +{cantidad} de rango en silencio. Total: {RangoMovimientoActual}.");
    }

    public void AumentarPA(int cantidad)
    {
        var reyPA = FindFirstObjectByType<KingController>();
        reyPA.puntosAccionActual += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Reina ganó +{cantidad} de rango en silencio. Total: {RangoMovimientoActual}.");
    }

    public void DesactivarJuego() => juegoActivo = false;
    public void MostrarRango() => MostrarMovimientoPosible();
    public void OcultarRango() => OcultarMovimientos();
    public void ReiniciarTurno()
    {
        rangoMovimientoBase = 5;
        rangoAtaque = 3;
        rangoMovimientoExtra = 0;
        StartCoroutine(EvaluarCasillasDeAtaque());
        tieneEscudo = false;

        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
        {
            tile.Shield(false); // apagar dorado
        }
    }

    public void AumentarRangoMovimientoSilencioso(int cantidad)
    {
        rangoMovimientoBase += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Torre ganó +{cantidad} de rango temporal en modo silencioso. Total: {RangoMovimientoActual}.");
    }

    public bool EstaActivo() => juegoActivo;
    public bool EsInamovible() => esInamovible;

    public void RecibirPenalizacionReina()
    {
        var reytorre = FindFirstObjectByType<KingController>();
        if (reytorre != null)
        {
            reytorre.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Torre en {posicionActual} penalizada: -1 PA.");
        }
    }

    public void RecibirPenalizacionTorre()
    {
        var reytorretorre = FindFirstObjectByType<KingController>();
        if (reytorretorre != null)
        {
            reytorretorre.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Torre en {posicionActual} penalizada: -1 PA.");
        }
    }

    public void RecibirPenalizacionAlfil()
    {
        var reytorrealfil = FindFirstObjectByType<KingController>();
        if (reytorrealfil != null)
        {
            reytorrealfil.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♝ Torre en {posicionActual} penalizada: -1 PA.");
        }
    }
}
