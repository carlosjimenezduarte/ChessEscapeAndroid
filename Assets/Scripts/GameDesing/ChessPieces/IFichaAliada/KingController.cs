using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections;

public class KingController : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
{
    public bool tieneEscudo = false;
    public int puntosMovimientoMax = 3;

    public int rangoAtaqueKing = 1; // 🔺 Rango de ataque fijo del Rey (igual que el Peón)
    public int puntosAccionMax = 5;

    public bool esInamovible = false;

    [HideInInspector]
    public int puntosMovimientoActual;
    public int puntosAccionActual = 5;
    public int turnosRestantes = 7;

    private Vector2Int posicionActual;
    private bool juegoActivo = false;
    public bool mostrandoMovimientos = false;

    private void Start()
    {

        puntosMovimientoActual = 3;
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey inició en {posicionActual}");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en el Rey. Usando (0,0).");
            posicionActual = new Vector2Int(0, 0);
        }

        // 🔍 Verificar si el Rey ya fue registrado en el tablero
        var objetosEnCasilla = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual);
        bool yaRegistrado = objetosEnCasilla.Contains(this);

        if (!yaRegistrado)
        {
            BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ ♔ Rey registrado manualmente en {posicionActual}.");
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"ℹ️ ♔ Rey ya estaba registrado.");
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♔ Rey actualizado a {nuevaPos}.");
        // 🛑 Si el Rey fue enviado a la Dimensión Divina, detener el juego
        if (nuevaPos == BoardManagerGlobal.DimensionDivina)
        {
            puntosMovimientoActual = 0;
            puntosAccionActual = 0;
            turnosRestantes = 0;
            SoundManager.Instance.PlaySound(4);
       
    }
    }

    public Vector2Int GetPosicionActual()
    {
        return posicionActual;
    }

    // True si desde origen hasta destino (avanzando 1 casilla por paso, sin diagonales compuestas)
    // NO hay un Wall en casillas intermedias. Solo permite recolectable si es la casilla final.
    private bool CaminoLibreSoloWall(Vector2Int origen, Vector2Int destino)
    {
        if (origen == destino) return true;

        Vector2Int paso = origen;

        while (paso != destino)
        {
            Vector2Int siguiente = paso;

            int dx = destino.x - paso.x;
            int dy = destino.y - paso.y;

            // Mismo criterio que usas en MoverA (prioriza eje de mayor |d|)
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

            var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(siguiente);

            // 🧱 Si hay Wall en cualquier casilla intermedia, corta visión
            if (objetos.OfType<Wall>().Any())
                return false;

            // Recolectables solo se permiten si es la casilla final
            bool esUltimoPaso = (siguiente == destino);
            if (!esUltimoPaso && objetos.Any(o => o is IObjetoRecoleccionable))
                return false;

            // Aliado/enemigo intermedio también corta (opcional, suele ser deseable)
            if (objetos.Any(o => o is IFichaAliada)) return false;
            if (objetos.Any(o => o is IFichaEnemiga)) return false;

            paso = siguiente;
        }

        // Si el destino es un Wall, tampoco se pinta
        if (BoardManagerGlobal.Instance.ObtenerObjetosEn(destino).OfType<Wall>().Any())
            return false;

        return true;
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

        BoardManagerGlobal.Instance.AgregarMensajeInterno($"👣 Alcance personal del Rey: {puntosMovimientoActual} PM.");

        // (Opcional) limpiar antes
        foreach (Tile t in BoardManagerGlobal.Instance.tiles)
            t.HighlightMove(false);

        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
        {
            /*int distancia = Mathf.Abs(tile.tileCoords.x - posicionActual.x) + Mathf.Abs(tile.tileCoords.y - posicionActual.y);

            // ✅ Solo pinta si está dentro de PM y la casilla NO está ocupada por aliada/inmóvil
            bool puedeMover = distancia <= puntosMovimientoActual
                      && (tile.tileCoords == posicionActual
                          || BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(tile.tileCoords));

            if (puedeMover && tieneEscudo)
                tile.Shield(true); // 🔹 marcar dorado si el Rey tiene escudo

            tile.HighlightMove(puedeMover);*/
            int distancia = Mathf.Abs(tile.tileCoords.x - posicionActual.x) + Mathf.Abs(tile.tileCoords.y - posicionActual.y);

            bool dentroPM = distancia <= puntosMovimientoActual;
            bool destinoAccesible = (tile.tileCoords == posicionActual)
                || BoardManagerGlobal.Instance.EsCasillaAccesiblePorAliado(tile.tileCoords);

            // 🧭 Nuevo: el camino debe estar libre de Wall (y de otros bloqueadores intermedios)
            bool caminoOk = CaminoLibreSoloWall(posicionActual, tile.tileCoords);

            bool puedeMover = dentroPM && destinoAccesible && caminoOk;

            if (puedeMover && tieneEscudo)
                tile.Shield(true);

            tile.HighlightMove(puedeMover);
        }

        // --- Ataque adyacente (igual que antes) ---
        Vector2Int[] direcciones = new Vector2Int[]
        {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        BoardManagerGlobal.Instance.AgregarMensajeInterno("🧠 Revisando casillas adyacentes para posibles ataques del Rey...");

        foreach (var delta in direcciones)
        {
            Vector2Int destino = posicionActual + delta;

            if (destino.x < 0 || destino.y < 0 || destino.x > 7 || destino.y > 7)
                continue;

            var objetosEnDestino = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino).ToList();
            if (objetosEnDestino.Count == 0)
                continue;

            var enemigo = objetosEnDestino.FirstOrDefault(obj =>
                obj is IFichaEnemiga && obj is IPieceWithPosition pwp && pwp.GetPosicionActual() == destino);

            if (enemigo != null)
            {
                int distanciaX = Mathf.Abs(destino.x - posicionActual.x);
                int distanciaY = Mathf.Abs(destino.y - posicionActual.y);

                if (distanciaX <= rangoAtaqueKing && distanciaY <= rangoAtaqueKing)
                {
                    Tile tile = BoardManagerGlobal.Instance.GetTileAt(destino);
                    if (tile != null)
                    {
                        tile.HighlightEnemyAttack(true);
                        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🎯 Casilla {destino} marcada como ataque posible del Rey.");
                    }
                }
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🟢 Mostrando previsualización automática del Rey.");
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🔴 Ocultando previsualización del Rey.");
        }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos)
    {
        if (!juegoActivo) return;

        // 1) Chequeo de alcance Manhattan (diamante)
        int manhattan = Mathf.Abs(posicionActual.x - nuevaPos.x) + Mathf.Abs(posicionActual.y - nuevaPos.y);
        if (manhattan > puntosMovimientoActual)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🚫 Movimiento no permitido, no hay suficientes PM.");
            return;
        }

        // 2) Avance paso a paso ORTOGONAL (nunca diagonal) hacia el destino
        Vector2Int paso = posicionActual;
        int pasosDados = 0;

        while (paso != nuevaPos)
        {
            Vector2Int siguiente = paso;

            int dx = nuevaPos.x - paso.x;
            int dy = nuevaPos.y - paso.y;

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

            // Bordes
            if (siguiente.x < 0 || siguiente.y < 0 || siguiente.x > 7 || siguiente.y > 7)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Movimiento cancelado: {siguiente} fuera del tablero.");
                return;
            }

            // Obstáculos
            var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(siguiente);
            if (objetos.OfType<Wall>().Any())
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧱 Movimiento bloqueado por muro en {siguiente}.");
                return;
            }

            if (objetos.Any(o => o is IFichaAliada))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Movimiento bloqueado por aliado/obstáculo en {siguiente}.");
                return;
            }
            if (objetos.Any(o => o is IFichaEnemiga))
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Movimiento bloqueado por enemigo en {siguiente}.");
                return;
            }

            bool hayReco = objetos.Any(o => o is IObjetoRecoleccionable);
            bool esUltimoPaso = siguiente == nuevaPos;
            if (hayReco && !esUltimoPaso)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Hay un objeto en {siguiente}. Debes caer aquí primero.");
                return;
            }

            // Avanzar un paso
            paso = siguiente;
            pasosDados++;

            SetPosicionActual(paso);
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚶 El Rey pasa por {paso}");

            // 🔹 Revisar efectos en la casilla actual
            foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
            {
                if (efecto is Vortex vortex)
                {
                    // Llamar y verificar si realmente está en la casilla
                    if (paso == vortex.GetPosicionActual())
                    {
                        vortex.RevisarSiReyLlegó(paso, this);
                        return; // Solo cortar si está sobre un Vortex
                    }
                }
                else if (efecto is PusherUp pusherUp)
                {
                    // Verificar si el Rey está en la casilla del PusherUp
                    if (paso == pusherUp.GetPosicionActual())
                    {
                        pusherUp.RevisarSiReyLlegó(paso, this);
                        return; // Detener movimiento si fue empujado
                    }
                }
                else if (efecto is PusherRight pusherRight)
                {
                    // Verificar si el Rey está en la casilla del PusherUp
                    if (paso == pusherRight.GetPosicionActual())
                    {
                        pusherRight.RevisarSiReyLlegó(paso, this);
                        return; // Detener movimiento si fue empujado
                    }
                }
                else if (efecto is PusherLeft pusherLeft)
                {
                    // Verificar si el Rey está en la casilla del PusherUp
                    if (paso == pusherLeft.GetPosicionActual())
                    {
                        pusherLeft.RevisarSiReyLlegó(paso, this);
                        return; // Detener movimiento si fue empujado
                    }
                }
                else if (efecto is PusherDown pusherDown)
                {
                    // Verificar si el Rey está en la casilla del PusherUp
                    if (paso == pusherDown.GetPosicionActual())
                    {
                        pusherDown.RevisarSiReyLlegó(paso, this);
                        return; // Detener movimiento si fue empujado
                    }
                }
                else
                {
                    efecto.RevisarSiReyLlegó(paso, this);
                }
            }

        }

        // 3) Sincroniza visual + descuenta PM reales
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(nuevaPos);
        puntosMovimientoActual -= pasosDados;

        // 4) Refrescos y lógica existente
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        BoardManagerGlobal.Instance.NotificarMovimientoAliado(posicionActual);

        var reina = FindFirstObjectByType<QueenEnemyController>(); if (reina != null) reina.VerificarAmenazaSobre(posicionActual);
        var torre = FindFirstObjectByType<RookEnemyController>(); if (torre != null) torre.VerificarAmenazaSobre(posicionActual);
        var alfil = FindFirstObjectByType<BishopEnemyController>(); if (alfil != null) alfil.VerificarAmenazaSobre(posicionActual);

        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();

        // 🚀 Llegó a la meta (H8), pero la victoria se decide
        // después de que las enemigas tengan oportunidad de atacarlo.
        if (posicionActual == new Vector2Int(7, 7))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "🚀 El Rey llegó a la meta (H8). Esperando resolución de amenazas..."
            );
            StartCoroutine(VerificarEscapeTrasAmenazas());
        }


        /*/ 🚀 Llegó a la meta
        if (posicionActual == new Vector2Int(7, 7))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("🚀 El Rey llegó a la meta (H8). Calculando bonus.");
            int bonus = turnosRestantes * 25;

            // 🔹 Score: solo afecta el nivel, no Stadistics
            PlayerScore.Instance.AgregarPuntaje(bonus, TipoObjetoScore.None);

            SoundManager.Instance.PlaySound(3);
            LevelResultUI.Instance.ShowResults(
                LevelProgress.Instance.keysCollected,
                LevelProgress.Instance.diamondsCollected,
                turnosRestantes,
                PlayerScore.Instance.GetTotalScore()
            );
        }
        */


        // 💀 Se quedó sin turnos
        if (turnosRestantes <= 0)
        {
            //SoundManager.Instance.PlaySound(4);
            FindFirstObjectByType<ChessGameManager>()?.DetenerJuego();
            BoardManagerGlobal.Instance.AgregarMensajeInterno("💀 El Rey ha sido eliminado.");
            StartCoroutine(MostrarResultadosTrasRetraso(3f));
            
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
        SoundManager.Instance.PlaySound(0); 
    }
    
    private IEnumerator MostrarResultadosTrasRetraso(float segundos)
{
    yield return new WaitForSeconds(segundos);
  

        LevelResultUI.Instance.ShowResults(
        LevelProgress.Instance.keysCollected,
        LevelProgress.Instance.diamondsCollected, 
        0,
        PlayerScore.Instance.GetTotalScore()
    );
}
    

    public void ReiniciarTurno()
    {
        puntosMovimientoActual = puntosMovimientoMax;
        puntosAccionActual = puntosAccionMax;
        rangoAtaqueKing = 1;
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
        $"♔ Nuevo turno del Rey → 🧭 PM personales: {puntosMovimientoActual}, 🎖️ PA estratégicos: {puntosAccionActual}.");
        //MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
        BoardManagerGlobal.Instance.ResetearAtaquesEnemigos();
        BoardManagerGlobal.Instance.QuitarEscudos();
        tieneEscudo = false;

        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
        {
            tile.Shield(false); // apagar dorado
        }

    }

    public void GanarPuntoMovimiento(int cantidad)
    {
        puntosMovimientoActual += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧭 El Rey gana {cantidad:+#;-#} PM personales. Total: {puntosMovimientoActual}.");
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void GanarVida(int cantidad)
    {
        turnosRestantes += cantidad;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"❤️ El Rey gana +{cantidad} vida(s). Ahora tiene {turnosRestantes}.");
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RestarTurno()
    {
        turnosRestantes--;
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"⏳ El Rey pierde 1 vida. Turnos restantes: {turnosRestantes}");
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();

        if (turnosRestantes <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("💀 El Rey ha agotado todos sus turnos.");

            LevelResultUI.Instance.ShowResults(
                LevelProgress.Instance.keysCollected,
                LevelProgress.Instance.diamondsCollected,
                0,
                PlayerScore.Instance.GetTotalScore()
            );
            juegoActivo = false;
            FindFirstObjectByType<ChessGameManager>()?.DetenerJuego();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
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
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void OcultarRango()
    {
        OcultarMovimientos();
        mostrandoMovimientos = false;
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }
    public bool EsInamovible()
    {
        return esInamovible;
    }

    public void IntentarAtacar(Vector2Int destino)
    {
        int distanciaX = Mathf.Abs(destino.x - posicionActual.x);
        int distanciaY = Mathf.Abs(destino.y - posicionActual.y);

        bool dentroDelRango = distanciaX <= rangoAtaqueKing && distanciaY <= rangoAtaqueKing;

        if (!dentroDelRango)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"❌ Casilla {destino} fuera del rango de ataque del Rey.");
            return;
        }

        var objetivo = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino)
            .FirstOrDefault(obj => obj is IFichaEnemiga);

        if (objetivo == null)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🕊️ No hay enemigo en {destino}. Nada que atacar.");
            return;
        }

        if (puntosAccionActual <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ El Rey no tiene PA suficientes para atacar.");
            return;
        }

        // 🔥 Eliminar ficha enemiga y marcarla fuera del tablero
        if (objetivo is IPieceWithPosition enemigo)
        {
            if (enemigo.GetPosicionActual() != destino)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"❌ El enemigo {objetivo} no está realmente en {destino}, está en {enemigo.GetPosicionActual()}. No se ejecuta el ataque.");
                return;
            }

            enemigo.SetPosicionActual(BoardManagerGlobal.DimensionDivina);
            SoundManager.Instance.PlaySound(1);
        }

        Destroy(((MonoBehaviour)objetivo).gameObject);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 Rey eliminó al enemigo en {destino}.");

        // 🔄 Actualizar posición lógica y visual del Rey
        SetPosicionActual(destino);
        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(destino);
        SoundManager.Instance.PlaySound(1);

        // ✨ Activar efectos especiales de casilla
        foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
        {
            efecto.RevisarSiReyLlegó(destino, this);
        }

        // 📉 Consumir 1 PA
        puntosAccionActual--;

        // 🔎 Verificar amenazas después del ataque
        foreach (var ficha in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IFichaEnemiga>())
            ficha.RevisarAmenazasEnZona();

        // 🎯 Refrescar HUD y estado del tablero
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"⚔️ Rey atacó y se desplazó a {destino}. PA restantes: {puntosAccionActual}");
        FindFirstObjectByType<ChessGameManager>()?.ActualizarHUD();
        MostrarMovimientoPosible();
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }
    public int rangoMovimientoBase
    {
        get => 0; // El Rey no usa esta propiedad
        set { }   // Ignora cualquier intento de modificarla
    }
    public int rangoAtaque
    {
        get => 0; // El Rey no usa esta propiedad
        set { }   // Ignora cualquier intento de modificarla
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

    private IEnumerator VerificarEscapeTrasAmenazas()
{
    // La Reina Roja espera 0.06 s antes de procesar amenazas.
    // Aquí damos un margen un poco mayor para que todas las corutinas
    // de enemigas alcancen a ejecutar sus efectos (exilio, muerte, etc.).
    yield return new WaitForSeconds(0.12f);

    var gameManager = FindFirstObjectByType<ChessGameManager>();
    if (gameManager == null)
        yield break;

    // Si el juego ya no está activo, asumimos que alguna enemiga
    // detuvo el juego porque ejecutó al Rey.
    if (!gameManager.IsJuegoActivo())
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            "❌ El Rey alcanzó H8 pero el juego ya fue detenido por una amenaza. No hay escape."
        );
        yield break;
    }

    // Si el Rey ya no está en H8, significa que fue empujado/exiliado/matado.
    if (GetPosicionActual() != new Vector2Int(7, 7))
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            "❌ El Rey alcanzó H8 pero fue removido de la casilla antes de escapar."
        );
        yield break;
    }

    // Sin turnos no tiene sentido otorgar victoria.
    if (turnosRestantes <= 0)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            "❌ El Rey llegó a H8 sin vidas. No escapa."
        );
        yield break;
    }

    // Si seguimos aquí, nadie lo mató en la puerta → victoria real.
    BoardManagerGlobal.Instance.AgregarMensajeInterno(
        "✅ El Rey sigue vivo en H8 tras la ventana de amenazas. Se acredita victoria."
    );

    int bonus = turnosRestantes * 25;
    PlayerScore.Instance.AgregarPuntaje(bonus, TipoObjetoScore.None);

    SoundManager.Instance.PlaySound(3);

    LevelResultUI.Instance.ShowResults(
        LevelProgress.Instance.keysCollected,
        LevelProgress.Instance.diamondsCollected,
        turnosRestantes,
        PlayerScore.Instance.GetTotalScore()
    );

    gameManager.DetenerJuego();
}

    
 


}