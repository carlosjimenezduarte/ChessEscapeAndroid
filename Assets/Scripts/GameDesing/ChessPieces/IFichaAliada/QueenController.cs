using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class QueenController : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Reina inició en {posicionActual}.");

        var objetosEnCasilla = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual);
        if (!objetosEnCasilla.Contains(this))
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ ♛ Reina registrada manualmente en {posicionActual}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"ℹ️ ♛ Reina ya estaba registrada.");
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Reina movida a {nuevaPos}.");
    }

    public Vector2Int GetPosicionActual() => posicionActual;

    // 🧱 Devuelve true si hay al menos un Wall entre origen y destino (intermedio o en destino).
    private bool HayMuroEntre(Vector2Int origen, Vector2Int destino)
    {
        Vector2Int paso = origen;
        int dx = destino.x - origen.x;
        int dy = destino.y - origen.y;

        // Normaliza dirección (ortogonal o diagonal) a pasos de 1 casilla
        int stepx = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepy = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // Recorremos casillas intermedias
        while (true)
        {
            paso = new Vector2Int(paso.x + stepx, paso.y + stepy);
            if (paso == destino) break;

            var objsInter = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);
            if (objsInter.OfType<Wall>().Any())
                return true;
        }

        // Valida también si el destino es un Wall (no debería poder caer sobre él)
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ Rey sin PA, Reina no puede moverse.");
        OcultarMovimientos();
        mostrandoMovimientos = false;
        return;
    }

    BoardManagerGlobal.Instance.AgregarMensajeInterno("🔍 Mostrando posibles movimientos de la Reina (diagonales y ortogonales):");

    Vector2Int[] direcciones = new Vector2Int[]
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
    };

    var casillaActual = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
    if (casillaActual != null)
    {
        if (tieneEscudo) casillaActual.Shield(true); // 🔹 aplicar dorado si hay escudo
        casillaActual.HighlightMove(true);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔵 Casilla central de la Reina ({posicionActual}) marcada como centro.");
    }

    foreach (var dir in direcciones)
    {
        for (int i = 1; i <= RangoMovimientoActual; i++)
        {
            Vector2Int destino = posicionActual + dir * i;

            if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, destino, this))
                break;

                if (HayMuroEntre(posicionActual, destino))
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧱 Muro bloquea visual hacia {destino}.");
                    break;
                }


            if (!BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(destino))
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"⛔ Casilla {destino} no accesible.");
                    break;
                }

            var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
            if (tile == null) break;

            if (tieneEscudo) tile.Shield(true); // 🔹 aplicar dorado si hay escudo
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🟢 Mostrando previsualización de la Reina.");
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🔴 Ocultando previsualización de la Reina.");
        }
        
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos, KingController rey)
    {
        if (!juegoActivo) return;

        Vector2Int delta = nuevaPos - posicionActual;
        bool esDiagonal = Mathf.Abs(delta.x) == Mathf.Abs(delta.y);
        bool esOrtogonales = delta.x == 0 || delta.y == 0;
        int distancia = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));

        // 1) Validación de patrón y rango
        if (!(esDiagonal || esOrtogonales) || distancia > RangoMovimientoActual)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"🚫 Movimiento inválido para la Reina desde {posicionActual} a {nuevaPos} (patrón/rango).");
            return;
        }

        // 2) Bloquear si hay obstáculos entre origen y destino (incluye recoleccionables)
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

        // 3) Evaluar destino (enemigo o recoleccionable es válido)
        var objetosEnDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(nuevaPos);
        var enemigo = objetosEnDestino.FirstOrDefault(o => o is IFichaEnemiga);

        // 4) Mover
        SetPosicionActual(nuevaPos);
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);

        // 5) Resolver combate si hay enemigo en destino
        if (enemigo != null)
        {
            if (enemigo is IPieceWithPosition enemigoPos)
                enemigoPos.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

            Destroy(((MonoBehaviour)enemigo).gameObject);
            SoundManager.Instance.PlaySound(1);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 Reina eliminó a un enemigo en {nuevaPos}.");
        }

        // 6) Disparar efectos (incluye recoger pociones/llaves si tu ITileEffect lo maneja)
        foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
            efecto.RevisarSiReinaLlegó(nuevaPos, this);

        // 7) Coste y refrescos
        rey.puntosAccionActual--;
        rangoMovimientoBase--;
        OcultarMovimientos();
        MostrarMovimientoPosible();
        RevisarObjetosEnCasilla();
        StartCoroutine(EvaluarCasillasDeAtaque());
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);
        SoundManager.Instance.PlaySound(0);
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();

        if (posicionActual == new Vector2Int(7, 7))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♕ Reina coronada en H8. Bonificaciones aplicadas.");

            BoardManagerGlobal.Instance?.NotifyAllyCoronated(BoardManagerGlobal.AllyKind.Queen);
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

    public void RevisarObjetosEnCasilla()
    {
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is ITileEffect efecto)
            {
                efecto.RevisarSiReinaLlegó(posicionActual, this);
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

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🔁 Evaluando casillas de ataque reales de la Reina...");

        Vector2Int[] direcciones = new Vector2Int[]
        {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= rangoAtaque; i++)
            {
                Vector2Int destino = posicionActual + dir * i;

                // ⛔ Bordes del tablero
                if (destino.x < 0 || destino.y < 0 || destino.x > 7 || destino.y > 7)
                    break;

                // 🧱 Si hay obstáculo ENTRE origen y destino, no seguimos en esta dirección
                if (BoardManagerGlobal.Instance.HayObstaculoEntreAliado(posicionActual, destino, this))
                    break;

                var tile = BoardManagerGlobal.Instance.GetTileAt(destino);
                if (tile == null) break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);

                // 👥 Aliado o inmóvil en destino bloquea (no es casilla de ataque)
                if (objetos.Any(obj => obj is IFichaAliada || obj is IFichaInmovil))
                    break;

                // 🎯 Enemigo en destino: marcar y cortar la línea
                if (objetos.Any(obj => obj is IFichaEnemiga))
                {
                    tile.HighlightEnemyAttack(true);
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎯 Casilla {destino} marcada como zona de ataque.");
                    break;
                }

                // Si no hay enemigo, seguimos buscando hasta rangoAtaque (sin pintar)
            }
        }
    }

    public void AumentarRangoMovimiento(int cantidad)
    {
        rangoMovimientoBase += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"📏 Reina ganó +{cantidad} de rango temporal. Total: {RangoMovimientoActual}.");
        
    }
    public void AumentarRangoMovimientoSilencioso(int cantidad)
    {
        rangoMovimientoBase += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Reina ganó +{cantidad} de rango en silencio. Total: {RangoMovimientoActual}.");
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
        var reyreina = FindFirstObjectByType<KingController>();
        if (reyreina != null)
        {
            reyreina.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Reina en {posicionActual} penalizada: -1 PA.");
        }
    }

    public void RecibirPenalizacionTorre()
    {
        var reyreinatorre = FindFirstObjectByType<KingController>();
        if (reyreinatorre != null)
        {
            reyreinatorre.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Reina en {posicionActual} penalizada por Torre: -1 PA.");
        }
    }

    public void RecibirPenalizacionAlfil()
    {
        var reyreinaalfil = FindFirstObjectByType<KingController>();
        if (reyreinaalfil != null)
        {
            reyreinaalfil.puntosAccionActual -= 1;
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♝ Reina en {posicionActual} penalizada por Alfil: -1 PA.");
        }
    }   
}
