using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class BishopController : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♝ Alfil inició en {posicionActual}.");

        var objetosEnCasilla = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual);
        bool yaRegistrado = objetosEnCasilla.Contains(this);

        if (!yaRegistrado)
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ ♝ Alfil registrado manualmente en {posicionActual}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"ℹ️ ♝ Alfil ya estaba registrado.");
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♝ Alfil movido a {nuevaPos}.");
    }

    public Vector2Int GetPosicionActual() => posicionActual;

    // 🧱 True si hay un Wall entre origen y destino (o en destino) recorriendo DIAGONAL a pasos de 1.
    private bool HayMuroEntreDiagonal(Vector2Int origen, Vector2Int destino)
    {
        // Debe ser diagonal perfecta
        if (Mathf.Abs(destino.x - origen.x) != Mathf.Abs(destino.y - origen.y)) return false;

        int stepx = destino.x > origen.x ? 1 : -1;
        int stepy = destino.y > origen.y ? 1 : -1;

        Vector2Int paso = origen;
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ Rey sin PA, Alfil no puede moverse.");
            OcultarMovimientos();
            mostrandoMovimientos = false;
            return;
        }

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🔍 Mostrando posibles movimientos diagonales del Alfil:");

        Vector2Int[] direcciones = new Vector2Int[]
        {
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        // 🔵 Casilla actual
        var casillaActual = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (casillaActual != null)
        {
            if (tieneEscudo) casillaActual.Shield(true); // ✅ marcar dorado si hay escudo
            casillaActual.HighlightMove(true);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔵 Casilla central del Alfil ({posicionActual}) marcada como centro.");
        }

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= RangoMovimientoActual; i++)
            {
                Vector2Int destino = posicionActual + dir * i;

                // ⛔ corta si hay obstáculo (aliado, inmóvil o recoleccionable) antes de destino
                if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, destino, this))
                    break;

                if (HayMuroEntreDiagonal(posicionActual, destino))
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
                if (objetos.Any(obj => obj is IFicha || obj is IFichaInmovil))
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🟢 Mostrando previsualización automática del Alfil.");
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🔴 Ocultando previsualización del Alfil.");
        }
        
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos, KingController rey)
    {
    if (!juegoActivo) return;
    if (rey.puntosAccionActual <= 0)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno("🚫 Movimiento inválido. Rey sin PA.");
        return;
    }

    Vector2Int delta = nuevaPos - posicionActual;
    bool esDiagonal = Mathf.Abs(delta.x) == Mathf.Abs(delta.y);
    int distancia = Mathf.Abs(delta.x); // = Mathf.Abs(delta.y)

    // 1) Patrón + rango
    if (!esDiagonal || distancia > RangoMovimientoActual)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"🚫 Movimiento inválido para el Alfil desde {posicionActual} a {nuevaPos} (patrón/rango).");
        return;
    }

    // 2) Obstáculo en trayecto (aliado / inmóvil / recoleccionable bloquea)
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

    // 3) Analizar destino (enemigo/recoleccionable permitido)
        var objetosEnDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(nuevaPos);
    var enemigo = objetosEnDestino.FirstOrDefault(o => o is IFichaEnemiga);

    // 4) Simular recorrido SOLO para efectos (no mover todavía)
    Vector2Int cursor = posicionActual;
    int stepX = (nuevaPos.x > cursor.x) ? 1 : -1;
    int stepY = (nuevaPos.y > cursor.y) ? 1 : -1;
    while (cursor != nuevaPos)
    {
        cursor.x += stepX;
        cursor.y += stepY;

        // Disparar efectos en cada casilla intermedia/destino
        foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
            efecto.RevisarSiAlfilLlegó(cursor, this);
    }

    // 5) Mover una sola vez (coherente con Torre/Reina)
    SetPosicionActual(nuevaPos);
    transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);

    // 6) Resolver combate si hay enemigo
    if (enemigo != null)
    {
        if (enemigo is IPieceWithPosition enemigoPos)
            enemigoPos.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

        Destroy(((MonoBehaviour)enemigo).gameObject);
        SoundManager.Instance.PlaySound(1);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 Alfil eliminó a un enemigo en {nuevaPos}.");
    }

    // 7) Ajuste de rango temporal si se usó más que el base
    if (distancia > rangoMovimientoBase)
    {
        int extraUsado = distancia - rangoMovimientoBase;
        rangoMovimientoExtra = Mathf.Max(0, rangoMovimientoExtra - extraUsado);
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"🧪 Rango temporal reducido en {extraUsado}. Rango restante: {RangoMovimientoActual}.");
    }

    // 8) Coste y refrescos
    rey.puntosAccionActual--;
    OcultarMovimientos();
    rangoMovimientoBase -= 1;
    MostrarMovimientoPosible();
    RevisarObjetosEnCasilla();
    StartCoroutine(EvaluarCasillasDeAtaque());
    FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
    BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);
    SoundManager.Instance.PlaySound(0);

    if (posicionActual == new Vector2Int(7, 7))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♕ Alfil coronado en H8. Bonificaciones aplicadas.");
            BoardManagerGlobal.Instance?.NotifyAllyCoronated(BoardManagerGlobal.AllyKind.Bishop);
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
                efecto.RevisarSiAlfilLlegó(posicionActual, this);
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

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🔁 Evaluando casillas de ataque reales del Alfil...");

        Vector2Int[] direcciones = new Vector2Int[]
        {
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= rangoAtaque; i++)
            {
                Vector2Int destino = posicionActual + dir * i;

                // ⛔ Bordes del tablero
                if (destino.x < 0 || destino.y < 0 || destino.x > 7 || destino.y > 7)
                    break;

                // 🧱 Si hay obstáculo ENTRE origen y destino, cortar la línea
                if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, destino, this))
                    break;

                var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
                if (tile == null) break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);

                // 👥 Aliado o inmóvil en destino bloquea (no es casilla de ataque)
                if (objetos.Any(obj => obj is IFichaAliada || obj is IFichaInmovil))
                    break;

                // 🎒 Recoleccionable en destino también bloquea (no “mires” más allá)
                if (objetos.Any(obj => obj is IObjetoRecoleccionable))
                    break;

                // 🎯 Enemigo en destino: marcar y cortar la línea
                if (objetos.Any(obj => obj is IFichaEnemiga))
                {
                    tile.HighlightEnemyAttack(true);
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎯 Casilla {destino} marcada como zona de ataque (post-movimiento).");
                    break;
                }

                // Si no hay enemigo, seguimos buscando hasta rangoAtaque (sin pintar)
            }
        }
    }

    public void AumentarRangoMovimiento(int cantidad)
    {
        rangoMovimientoBase += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"📏 Alfil ganó +{cantidad} de rango temporal. Total: {RangoMovimientoActual}.");
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
    }
    public void AumentarRangoMovimientoSilencioso(int cantidad)
    {
        rangoMovimientoBase += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Alfil ganó +{cantidad} de rango temporal en modo silencioso. Total: {RangoMovimientoActual}.");
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

    

    public bool EstaActivo() => juegoActivo;
    public bool EsInamovible() => esInamovible;

    public void RecibirPenalizacionReina()
    {
        var reyalfil = FindFirstObjectByType<KingController>();
        if (reyalfil != null)
        {
            reyalfil.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Alfil en {posicionActual} penalizado: -1 PA.");
        }
    }

    public void RecibirPenalizacionTorre()
    {
        var reyalfiltorre = FindFirstObjectByType<KingController>();
        if (reyalfiltorre != null)
        {
            reyalfiltorre.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Alfil en {posicionActual} penalizado: -1 PA.");
        }
    }

    public void RecibirPenalizacionAlfil()
    {
        var reyalfilalfil = FindFirstObjectByType<KingController>();
        if (reyalfilalfil != null)
        {
            reyalfilalfil.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♝ Alfil en {posicionActual} penalizado: -1 PA.");
        }
    }
}
