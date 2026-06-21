using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
public class KingFree : MonoBehaviour, IPointerClickHandler, IPieceWithPosition, IFicha, IFichaAliada
{
    public bool tieneEscudo = false;
    public int puntosMovimientoMax = 3;
    public int rangoAtaqueKing = 1;
    public int puntosAccionMax = 5;
    public bool esInamovible = false;

    [HideInInspector] public int puntosMovimientoActual;
    public int puntosAccionActual = 5;
    public int turnosRestantes = 7;

    [SerializeField]
    public Vector2Int posicionActual;
    private bool juegoActivo = false;
    public bool mostrandoMovimientos = false;

    private void Start()
    {
        puntosMovimientoActual = puntosMovimientoMax;
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            Garden7.Instance.AgregarMensajeInterno($"♔ Rey Libre inició en {posicionActual}");
        }
        else
        {
            Garden7.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en el Rey Libre. Usando (0,0).");
            posicionActual = new Vector2Int(0, 0);
        }

        // 🔍 Registrar si no estaba
        var objetosEnCasilla = Garden7.Instance.ObtenerObjetosEn(posicionActual);
        if (!objetosEnCasilla.Contains(this))
        {
            Garden7.Instance.RegistrarMovimiento(this, posicionActual);
            Garden7.Instance.AgregarMensajeInterno($"✅ ♔ Rey Libre registrado manualmente en {posicionActual}.");
        }
        else
        {
            Garden7.Instance.AgregarMensajeInterno($"ℹ️ ♔ Rey Libre ya estaba registrado.");
        }

        Garden7.Instance.ReportarEstadoActualDelTablero();
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

        Garden7.Instance?.RegistrarMovimiento(this, nuevaPos);
        Garden7.Instance.AgregarMensajeInterno($"♔ Rey Libre actualizado a {nuevaPos}.");

        if (nuevaPos == Garden7.DimensionDivina)
        {
            puntosMovimientoActual = 0;
            puntosAccionActual = 0;
            turnosRestantes = 0;
        }
    }

    public Vector2Int GetPosicionActual() => posicionActual;

    public void ActivarJuego()
    {
        juegoActivo = true;
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        Garden7.Instance.ReportarEstadoActualDelTablero();
    }

    public void MostrarMovimientoPosible()
    {
        if (!juegoActivo) return;

        Garden7.Instance.AgregarMensajeInterno($"👣 Alcance Rey Libre: {puntosMovimientoActual} PM.");
        foreach (Tile t in Garden7.Instance.tiles)
            t.HighlightMoveForKing(false);

        foreach (Tile tile in Garden7.Instance.tiles)
        {
            int distancia = Mathf.Abs(tile.tileCoords.x - posicionActual.x) + Mathf.Abs(tile.tileCoords.y - posicionActual.y);

            bool puedeMover = distancia <= puntosMovimientoActual
               && (tile.tileCoords == posicionActual
               || Garden7.Instance.EsCasillaAccesiblePorAliado(tile.tileCoords))
               && !casillasBloqueadas.Contains(tile.tileCoords);

            if (puedeMover && tieneEscudo)
                tile.Shield(true);

            tile.HighlightMoveForKing(puedeMover);
            
        }

        // --- Ataque adyacente ---
        Vector2Int[] direcciones =
        {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(-1,1),
            new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        foreach (var delta in direcciones)
        {
            Vector2Int destino = posicionActual + delta;
            if (destino.x < 0 || destino.y < 0 || destino.x > 8 || destino.y > 8)
                continue;

            var objetosEnDestino = Garden7.Instance.ObtenerObjetosEn(destino).ToList();
            var enemigo = objetosEnDestino.FirstOrDefault(obj => obj is IFichaEnemiga);
            if (enemigo != null)
            {
                int dx = Mathf.Abs(destino.x - posicionActual.x);
                int dy = Mathf.Abs(destino.y - posicionActual.y);
                if (dx <= rangoAtaqueKing && dy <= rangoAtaqueKing)
                {
                    Tile tile = Garden7.Instance.GetTileAt(destino);
                    tile?.HighlightEnemyAttack(true);
                }
            }
            
        }
        //SoundManager.Instance.PlaySound(0);
    }

    public void OcultarMovimientos()
    {
        foreach (Tile tile in Garden7.Instance.tiles)
            tile.HighlightMoveForKing(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var gameManager = FindFirstObjectByType<GardenGame>();
        bool esNuevaSeleccion = gameManager.fichaSeleccionadaActual != this;
        gameManager.fichaSeleccionadaActual = this;

        var rey = FindFirstObjectByType<KingFree>();
        if (rey != null && rey != this)
            rey.OcultarMovimientos();

        if (esNuevaSeleccion || !mostrandoMovimientos)
        {
            mostrandoMovimientos = true;
            MostrarMovimientoPosible();
        }
        else
        {
            mostrandoMovimientos = false;
            OcultarMovimientos();
        }
        

        Garden7.Instance.ReportarEstadoActualDelTablero();
    }

    public void MoverA(Vector2Int nuevaPos)
    {
        if (!juegoActivo) return;

        // 🚫 impedir movimiento a casillas bloqueadas
        if (casillasBloqueadas.Contains(nuevaPos))
        {
            Garden7.Instance.AgregarMensajeInterno($"🚫 {nuevaPos} es inaccesible (espejo).");
            return;
        }

        int manhattan = Mathf.Abs(posicionActual.x - nuevaPos.x) + Mathf.Abs(posicionActual.y - nuevaPos.y);
        if (manhattan > puntosMovimientoActual)
        {
            Garden7.Instance.AgregarMensajeInterno("🚫 Movimiento no permitido, sin PM suficientes.");
            return;
        }

        // 🚪 Notificar salida de la casilla actual antes de moverse
        EspejoManager.Instance?.SalirDeCasilla(posicionActual);

        // 🔄 Movimiento directo
        SetPosicionActual(nuevaPos);
        transform.localPosition = Garden7.Instance.GetTileWorldPosition(nuevaPos);

        // 🔻 Descontar PM
        puntosMovimientoActual -= manhattan;

        // 🪞 Revisar si la nueva casilla tiene un espejo asociado
        EspejoManager.Instance?.RevisarEspejo(nuevaPos);

        // ✨ Notificar a TODOS los efectos (llaves, portales, etc.)
        foreach (ITileEffect efecto in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>())
        {
            efecto.RevisarSiReyLibreLlegó(nuevaPos, this);  // ✅ llamado explícito para KingFree
        }
        if (nuevaPos == new Vector2Int(4, 4))
        {
            FindFirstObjectByType<CofreController>()?.AbrirCofre();
            return; // 🚫 No mover al Rey, porque el cofre es inaccesible
        }


        // 🔄 Actualizar highlights según los PM que queden
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;

        // Opcional: notificar tablero y HUD
        FindFirstObjectByType<GardenGame>()?.ActualizarHUD();
        Garden7.Instance.ReportarEstadoActualDelTablero();
        SoundManager.Instance.PlaySound(0);
    }




    public void ReiniciarTurno()
    {
        puntosMovimientoActual = puntosMovimientoMax;
        puntosAccionActual = puntosAccionMax;
        rangoAtaqueKing = 1;
        mostrandoMovimientos = true;
        Garden7.Instance.ReportarEstadoActualDelTablero();
        //Garden7.Instance.ResetearAtaquesEnemigos();
        //Garden7.Instance.QuitarEscudos();
        tieneEscudo = false;

        foreach (Tile tile in Garden7.Instance.tiles)
            tile.Shield(false);
    }

    public void GanarPuntoMovimiento(int cantidad)
    {
        puntosMovimientoActual += cantidad;
        MostrarMovimientoPosible();
        mostrandoMovimientos = true;
        FindFirstObjectByType<GardenGame>()?.ActualizarHUD();
    }

    public void GanarVida(int cantidad)
    {
        turnosRestantes += cantidad;
        FindFirstObjectByType<GardenGame>()?.ActualizarHUD();
    }

    public void RestarTurno()
    {
        //turnosRestantes--;
        FindFirstObjectByType<GardenGame>()?.ActualizarHUD();
    }

    public void DesactivarJuego() => juegoActivo = false;

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

    public bool EsInamovible() => esInamovible;

    public void IntentarAtacar(Vector2Int destino)
    {
        int dx = Mathf.Abs(destino.x - posicionActual.x);
        int dy = Mathf.Abs(destino.y - posicionActual.y);
        bool dentroRango = dx <= rangoAtaqueKing && dy <= rangoAtaqueKing;

        if (!dentroRango) return;
        var objetivo = Garden7.Instance.ObtenerObjetosEn(destino).FirstOrDefault(o => o is IFichaEnemiga);
        if (objetivo == null) return;
        if (puntosAccionActual <= 0) return;

        if (objetivo is IPieceWithPosition enemigo)
            enemigo.SetPosicionActual(Garden7.DimensionDivina);

        Destroy(((MonoBehaviour)objetivo).gameObject);
        SetPosicionActual(destino);
        transform.localPosition = Garden7.Instance.GetTileWorldPosition(destino);

        puntosAccionActual--;
        FindFirstObjectByType<GardenGame>()?.ActualizarHUD();
        MostrarMovimientoPosible();
    }

    public int rangoMovimientoBase { get => 0; set { } }
    public int rangoAtaque { get => 0; set { } }

    public void TeletransportarA(Vector2Int nuevaPos)
    {
        posicionActual = nuevaPos;

        var movible = GetComponent<MovableTileObject>();
        if (movible != null) movible.tileCoords = nuevaPos;

        var posicionador = GetComponent<PiecePositioner>();
        if (posicionador != null) posicionador.tileCoords = nuevaPos;

        transform.localPosition = Garden7.Instance.GetTileWorldPosition(nuevaPos);
        Garden7.Instance.RegistrarMovimiento(this, nuevaPos);
    }
    private static readonly HashSet<Vector2Int> casillasBloqueadas = new HashSet<Vector2Int>
{
    new Vector2Int(2,0), new Vector2Int(4,0), new Vector2Int(6,0),
    new Vector2Int(0,2), new Vector2Int(8,2),new Vector2Int(3,5),new Vector2Int(5,5),
    new Vector2Int(0,4), new Vector2Int(8,4),new Vector2Int(5,3),
    new Vector2Int(0,6), new Vector2Int(8,6), new Vector2Int(3,3),
    new Vector2Int(2,8), new Vector2Int(4,8), new Vector2Int(6,8)
};

}
