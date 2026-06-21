using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class PawnController : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
{
    
    [Header("Rango de Movimiento")]
    public bool tieneEscudo = false;
    public int rangoMovimientoBase { get; set; } = 1;    
    public bool isShieldActive = false;
    private int rangoMovimientoExtra = 0;
    public int rangoAtaque { get; set; } = 1; // 🔺 NUEVO: Rango fijo de ataque en diagonal

    public bool esInamovible = false;
    public int RangoMovimientoActual => rangoMovimientoBase + rangoMovimientoExtra;

    private Vector2Int posicionActual;
    private bool juegoActivo = false;
    public bool mostrandoMovimientos = false;

    private void Start()
    {
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        posicionActual = piecePositioner != null ? piecePositioner.tileCoords : new Vector2Int(0, 0);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón inició en {posicionActual}.");

        // 🔍 Verificar si el Peón ya fue registrado en el tablero
        var objetosEnCasilla = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual);
        bool yaRegistrado = objetosEnCasilla.Contains(this);

        if (!yaRegistrado)
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ ♙ Peón registrado manualmente en {posicionActual}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"ℹ️ ♙ Peón ya estaba registrado.");
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

        var movible = GetComponent<MovableTileObject>();
        if (movible != null) movible.tileCoords = nuevaPos;

        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null) posicionador.tileCoords = nuevaPos;

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón movido a {nuevaPos}.");
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ Rey sin PA, Peón no puede moverse.");
            OcultarMovimientos();
            mostrandoMovimientos = false;
            return;
        }

        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔍 Mostrando rango de movimiento del Peón: {RangoMovimientoActual} casillas.");

        // (opcional) limpia antes
        foreach (Tile t in BoardManagerGlobal.Instance.tiles)
            t.HighlightMove(false);

        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
        {
            int distancia = Mathf.Abs(tile.tileCoords.x - posicionActual.x) + Mathf.Abs(tile.tileCoords.y - posicionActual.y);

            bool puedeMover =
                distancia <= RangoMovimientoActual &&
                (tile.tileCoords == posicionActual ||
                BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(tile.tileCoords));

            if (puedeMover && tieneEscudo)
            {
                tile.Shield(true); // ✅ activa dorado
            }

            tile.HighlightMove(puedeMover); // pinta usando el flag interno del tile
        }        



        // 💥 Mostrar ataques en las diagonales dentro del rango de ataque
        Vector2Int[] diagonales = new Vector2Int[]
        {
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🧪 Iniciando revisión de casillas diagonales para posibles ataques del Peón...");

        foreach (var delta in diagonales)
        {
            Vector2Int diagonal = posicionActual + delta;

            if (diagonal.x < 0 || diagonal.y < 0 || diagonal.x > 7 || diagonal.y > 7)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"⛔ Casilla {diagonal} fuera del tablero. Se ignora.");
                continue;
            }

            // ✅ Validación explícita de movimiento diagonal de una casilla
            if (!(Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 Movimiento {delta} no es diagonal. Se ignora.");
                continue;
            }

            var objetosEnDiagonal = BoardManagerGlobal.Instance.ObtenerObjetosEn(diagonal).ToList();
            if (objetosEnDiagonal.Count == 0)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔍 Casilla {diagonal} está vacía. No hay enemigo.");
                continue;
            }

            var objetivo = objetosEnDiagonal.FirstOrDefault(obj =>
            obj is IFichaEnemiga && obj is IPieceWithPosition pwp && pwp.GetPosicionActual() == diagonal);

            if (objetivo != null)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ Enemigo real encontrado en {diagonal}: {objetivo}");

                Tile tile = BoardManagerGlobal.Instance.GetTileAt(diagonal);
                if (tile != null)
                {
                    tile.HighlightEnemyAttack(true);
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎯 Casilla {diagonal} marcada como zona de ataque (fucsia).");
                }
                else
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚠️ No se encontró Tile visual en {diagonal} para marcar ataque.");
                }
            }
            else
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔎 Ningún enemigo legítimo presente en {diagonal}. No se marcará.");
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🟢 Mostrando previsualización automática del Peón.");
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🔴 Ocultando previsualización del Peón.");
        }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos, KingController rey)
    {
        if (!juegoActivo) return;

        // ⚔️ PRIORIDAD: Ataque diagonal a 1 casilla
        bool esDiagonal = Mathf.Abs(nuevaPos.x - posicionActual.x) == 1 && Mathf.Abs(nuevaPos.y - posicionActual.y) == 1;
        if (esDiagonal)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧐 Intentando ataque diagonal desde {posicionActual} hacia {nuevaPos}...");

            var fichaEnDiagonal = BoardManagerGlobal.Instance.ObtenerObjetosEn(nuevaPos)
                .FirstOrDefault(obj => obj is IFichaEnemiga);

            if (fichaEnDiagonal != null)
            {
                SetPosicionActual(nuevaPos);
                transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);

                if (fichaEnDiagonal is IPieceWithPosition enemigo)
                    enemigo.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

                Destroy(((MonoBehaviour)fichaEnDiagonal).gameObject);
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 Ficha enemiga destruida en {nuevaPos} por el Peón.");

                // 🔹 Consumir PA del Rey
                rey.puntosAccionActual--;
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚔️ Peón consumió 1 PA del Rey. PA restantes: {rey.puntosAccionActual}");
                SoundManager.Instance.PlaySound(1);
                MostrarMovimientoPosible();
                FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
                BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);
                BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
            }
            else
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔍 No se encontró ficha enemiga en {nuevaPos}, ataque cancelado.");
            }
            return;
        }

        // 🔁 MOVIMIENTO ORTOGONAL (sin atravesar)
        int manhattan = Mathf.Abs(posicionActual.x - nuevaPos.x) + Mathf.Abs(posicionActual.y - nuevaPos.y);
        if (manhattan > RangoMovimientoActual)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 Movimiento inválido. Distancia {manhattan} excede el rango actual {RangoMovimientoActual}.");
            return;
        }

        if (rey.puntosAccionActual <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🚫 Movimiento inválido. Rey sin PA.");
            return;
        }

        Vector2Int paso = posicionActual;
        int pasosDados = 0;

        while (paso != nuevaPos)
        {
            Vector2Int siguiente = paso;

            int dx = nuevaPos.x - paso.x;
            int dy = nuevaPos.y - paso.y;

            // Mueve primero en el eje de mayor diferencia (o X primero si están iguales)
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                if (dx != 0) siguiente.x += dx > 0 ? 1 : -1;
                else if (dy != 0) siguiente.y += dy > 0 ? 1 : -1;
            }
            else
            {
                if (dy != 0) siguiente.y += dy > 0 ? 1 : -1;
                else if (dx != 0) siguiente.x += dx > 0 ? 1 : -1;
            }

            // Tablero
            if (siguiente.x < 0 || siguiente.y < 0 || siguiente.x > 7 || siguiente.y > 7)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Movimiento cancelado: {siguiente} fuera del tablero.");
                return;
            }

            var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(siguiente);

            // Aliado o inmóvil → bloquea
            if (objetos.Any(o => o is IFichaAliada))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Movimiento bloqueado por aliado/obstáculo en {siguiente}.");
                return;
            }

            // Enemigo → el Peón no atraviesa ni captura ortogonalmente
            if (objetos.Any(o => o is IFichaEnemiga))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Movimiento bloqueado por enemigo en {siguiente}.");
                return;
            }

            // Recoleccionable → puedes caer, pero no continuar más allá si no es el destino
            bool hayReco = objetos.Any(o => o is IObjetoRecoleccionable);
            bool esUltimoPaso = siguiente == nuevaPos;
            if (hayReco && !esUltimoPaso)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Hay un objeto en {siguiente}. Debes caer aquí primero.");
                return;
            }

            // Avanzar un paso y disparar efectos
            paso = siguiente;
            pasosDados++;

            SetPosicionActual(paso);
            foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
            {
            if (efecto is PusherUp pusherUp)
            {
                // Verificar si el Rey está en la casilla del PusherUp
                if (paso == pusherUp.GetPosicionActual())
                {
                    pusherUp.RevisarSiPeonLlegó(paso, this);
                    return; // Detener movimiento si fue empujado
                }
            }
            else if (efecto is PusherLeft pusherLeft)
            {
                // Verificar si el Rey está en la casilla del PusherUp
                if (paso == pusherLeft.GetPosicionActual())
                {
                    pusherLeft.RevisarSiPeonLlegó(paso, this);
                    return; // Detener movimiento si fue empujado
                }
            }
            else if (efecto is PusherRight pusherRight)
            {
                // Verificar si el Rey está en la casilla del PusherUp
                if (paso == pusherRight.GetPosicionActual())
                {
                    pusherRight.RevisarSiPeonLlegó(paso, this);
                    return; // Detener movimiento si fue empujado
                }
            }
            else if (efecto is PusherDown pusherDown)
            {
                // Verificar si el Rey está en la casilla del PusherUp
                if (paso == pusherDown.GetPosicionActual())
                {
                    pusherDown.RevisarSiPeonLlegó(paso, this);
                    return; // Detener movimiento si fue empujado
                }
            }
            else
                {
                    efecto.RevisarSiPeonLlegó(paso, this);
                }
            }
                
        }

        // Sincronizar visual
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);

        // Ajuste de rango temporal según pasos reales
        if (pasosDados > rangoMovimientoBase)
        {
            int extraUsado = pasosDados - rangoMovimientoBase;
            rangoMovimientoExtra = Mathf.Max(0, rangoMovimientoExtra - extraUsado);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧪 Rango temporal reducido en {extraUsado}. Rango restante: {RangoMovimientoActual}.");
        }

        // Consumir PA del Rey
        rey.puntosAccionActual--;

        // Refrescos
        MostrarMovimientoPosible();
        RevisarObjetosEnCasilla();
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);
        SoundManager.Instance.PlaySound(0);

        // Coronación opcional
        // Coronación opcional
    if (posicionActual == new Vector2Int(7, 7))
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno("♕ Peón coronado en H8. Bonificaciones aplicadas.");

        // 👇 DESBLOQUEO DEL LOGRO (una sola vez + suma 1000 al _scoreTotal del slot)
        AchievementsManager.ReportPawnCoronation();
        BoardManagerGlobal.Instance?.NotifyAllyCoronated(BoardManagerGlobal.AllyKind.Pawn);

        // 📈 Bonos al Rey
        rey.puntosAccionActual     += 7;
        rey.puntosMovimientoActual += 7;
        rey.GanarVida(7);

        // 🧠 Transferir selección al Rey y mostrar su nuevo rango
        var gameManager = FindFirstObjectByType<ChessGameManager>();
        if (gameManager != null)
        {
            gameManager.SeleccionarReyTrasCoronacion();
        }
        else
        {
            // Plan B: al menos que el HUD quede coherente
            FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        }

        // Apagar el rango del Peón en el tablero antes de irse
        OcultarMovimientos();
        rey.MostrarMovimientoPosible();

        SoundManager.Instance.PlaySound(2);

        // 💨 El Peón abandona el plano de juego
        Destroy(gameObject);

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarObjetosEnCasilla()
    {
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is ITileEffect efecto)
            {
                efecto.RevisarSiPeonLlegó(posicionActual, this);
                MostrarMovimientoPosible();
            }
        }
    }


    public void AumentarRangoMovimiento(int cantidad)
    {
        rangoMovimientoExtra += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🏃 Peón ganó +{cantidad} de rango temporal. Total: {RangoMovimientoActual}.");
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
    }

    public void AumentarRangoMovimientoSilencioso(int cantidad)
    {
        rangoMovimientoExtra += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🤫 Peón ganó +{cantidad} de rango temporal en modo silencioso. Total: {RangoMovimientoActual}.");
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

    public void DesactivarJuego()
    {
        juegoActivo = false;
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MostrarRango()
    {
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
    }

    public void OcultarRango()
    {
        OcultarMovimientos();
        mostrandoMovimientos = false;
    }

    public void ReiniciarTurno()
    {
        rangoMovimientoBase = 1;
        rangoAtaque = 1;
        rangoMovimientoExtra = 0;
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
        var rey = FindFirstObjectByType<KingController>();
        if (rey != null)
        {
            rey.puntosAccionActual -= 1;

            // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Peón en {posicionActual} penalizado: -1 PA. PA actual del Rey: {rey.puntosAccionActual}"
            );
        }
    }

    public void RecibirPenalizacionTorre()
    {
        var reytorre = FindFirstObjectByType<KingController>();
        if (reytorre != null)
        {
            reytorre.puntosAccionActual -= 1;

            // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♜ Peón en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reytorre.puntosAccionActual}"
            );
        }
    }

    public void RecibirPenalizacionAlfil()
    {
        var reyalfil = FindFirstObjectByType<KingController>();
        if (reyalfil != null)
        {
            reyalfil.puntosAccionActual -= 1;

            // 🔹 Actualizar HUD inmediatamente
            var gameManager = FindFirstObjectByType<ChessGameManager>();
            if (gameManager != null)
                gameManager.ActualizarHUD();

            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♝ Peón en {posicionActual} penalizado: -1 PA. PA actual del Rey: {reyalfil.puntosAccionActual}"
            );
        }
    }
    
    public void TeletransportarA(Vector2Int nuevaPos)
    {
    posicionActual = nuevaPos;

    var movible = GetComponent<MovableTileObject>();
    if (movible != null) movible.tileCoords = nuevaPos;

    var posicionador = GetComponent<PiecePositioner>();
    if (posicionador != null) posicionador.tileCoords = nuevaPos;

    transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);
    BoardManagerGlobal.Instance.RegistrarMovimiento(this, nuevaPos);

    BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey teletransportado a {nuevaPos}.");
    BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }
   
}
