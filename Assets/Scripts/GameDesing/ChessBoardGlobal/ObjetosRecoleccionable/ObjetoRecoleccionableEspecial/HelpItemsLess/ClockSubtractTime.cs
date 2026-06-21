using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ClockSubtractTime : MonoBehaviour, ITileEffect, IObjetoRecoleccionable, IPieceWithPosition
{
    [Header("Configuración general")]
    [SerializeField] public bool vieneDelFuturo = false;

    [Header("Turno y posiciones")]
    [SerializeField] public int turnoAparece = 1;
    [SerializeField] public Vector2Int posicionReal = new Vector2Int(0, 0);
    [SerializeField] public Vector2Int tileCoordsFuturosInciertos = new Vector2Int(100, 100);

    private Vector2Int tileCoords;
    public bool esInamovible = false;
    private bool activadoEnJuego = false;
    private bool desactivado = false;

    private Image image;
    private PiecePositioner piecePositioner;
    private MovableTileObject movable;

    private void Awake()
    {
        image = GetComponent<Image>();
        piecePositioner = GetComponent<PiecePositioner>();
        movable = GetComponent<MovableTileObject>();

        if (vieneDelFuturo)
        {
            ColocarEn(tileCoordsFuturosInciertos);
            if (movable != null) movable.activoEnTablero = false;
            if (image != null) image.enabled = false;
            activadoEnJuego = false;
        }
        else
        {
            tileCoords = piecePositioner != null ? piecePositioner.tileCoords : tileCoords;
            if (movable != null) movable.activoEnTablero = true;
            if (image != null) image.enabled = true;
            activadoEnJuego = true;
            BoardManagerGlobal.Instance?.AgregarMensajeInterno($"⏳ {name} inicializado en {tileCoords}.");
        }
    }

    private void Update()
    {
        if (!activadoEnJuego) return;
        VerificarAutoChequeoGeneral();
    }

    private void ColocarEn(Vector2Int coords)
    {
        tileCoords = coords;
        if (piecePositioner != null) piecePositioner.tileCoords = coords;
        if (movable != null) movable.tileCoords = coords;

        if (BoardManagerGlobal.Instance != null)
            transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(coords);

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, coords);
        BoardManagerGlobal.Instance?.AgregarMensajeInterno($"⏳ {name} colocado en {coords}");
    }

    public void VerificarTurnoActual(int turnoActual)
    {
        if (!activadoEnJuego && turnoActual >= turnoAparece)
        {
            BoardManagerGlobal.Instance?.AgregarMensajeInterno($"📅 Turno {turnoActual}. {name} programado para aparecer en {posicionReal}.");
            TeletransportarAlTablero();
        }
    }

    private void TeletransportarAlTablero()
    {
        if (PuedeAparecerEn(posicionReal))
        {
            ColocarEn(posicionReal);
            if (movable != null) movable.ActivarEnTablero(posicionReal);
            activadoEnJuego = true;
            if (image != null) image.enabled = true;
            BoardManagerGlobal.Instance?.AgregarMensajeInterno($"✅ {name} se materializó en {posicionReal}.");
        }
        else
        {
            ExiliarADimensionDivina();
        }
    }

    public void VerificarAutoChequeoGeneral()
    {
        if (desactivado || !activadoEnJuego) return;

        var rey = FindFirstObjectByType<KingController>();
        if (rey != null)
            RevisarSiReyLlegó(rey.GetPosicionActual(), rey);

        var fichasAliadas = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IFichaAliada>();

        foreach (var ficha in fichasAliadas)
        {
            if (ficha is KingController) continue;
            RevisarSiFichaAliadaLlegó(ficha.GetPosicionActual(), ficha);
        }
    }

    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (tileCoords == posicionRey && !desactivado)
        {
            desactivado = true;
            BoardManagerGlobal.Instance?.AgregarMensajeInterno($"⏳ Tiempo Negativo recogido por Rey en {posicionRey}. -15s al turno.");
            FindFirstObjectByType<ChessGameManager>()?.AgregarTiempoAlTurno(-15f);
            SoundManager.Instance.PlaySound(29);
            Destroy(gameObject);
        }
    }

    public void RevisarSiFichaAliadaLlegó(Vector2Int posicionFicha, IFichaAliada ficha)
    {
        if (tileCoords != posicionFicha || desactivado) return;
        if ((ficha as Object) == null) return; // Unity-null (destruido)
        if (ficha is KingController) return;

        desactivado = true;

        string nombreFicha = "FichaAliada";
        try { nombreFicha = ficha.GetType().Name; } catch { }

        BoardManagerGlobal.Instance?.AgregarMensajeInterno($"⏳ Tiempo Negativo recogido por {nombreFicha} en {posicionFicha}. -15s al turno.");
        FindFirstObjectByType<ChessGameManager>()?.AgregarTiempoAlTurno(-15f);
        SoundManager.Instance.PlaySound(29);
        Destroy(gameObject);
    }

    public void ExiliarADimensionDivina()
    {
        Vector2Int coordsDivinos = BoardManagerGlobal.ObtenerProximaPosicionDivina();
        ColocarEn(coordsDivinos);

        if (movable != null) movable.activoEnTablero = false;

        gameObject.SetActive(false);
        BoardManagerGlobal.Instance?.AgregarMensajeInterno($"🚀 {name} exiliado a Dimensión Divina en {coordsDivinos}.");
    }

    private bool PuedeAparecerEn(Vector2Int coords)
    {
        var objetos = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(obj => obj is IObjetoRecoleccionable && obj != this);

        foreach (var obj in objetos)
        {
            Vector2Int pos = Vector2Int.zero;
            int turnoOtro = 0;
            bool estaActivo = false;

            if (obj.TryGetComponent<PiecePositioner>(out var posr))
                pos = posr.tileCoords;
            else if (obj is IPieceWithPosition pieza)
                pos = pieza.GetPosicionActual();

            Potion1PM otro = obj.GetComponent<Potion1PM>();
            if (otro != null)
            {
                turnoOtro = otro.turnoAparece;
                estaActivo = otro.IsVisible();
            }

            if (pos == coords)
            {
                if (obj is IObjetoRecoleccionableEspecial)
                {
                    BoardManagerGlobal.Instance?.AgregarMensajeInterno($"🚫 {name} no puede reemplazar a {obj.name} porque es Especial.");
                    return false;
                }
                if (estaActivo || turnoOtro <= turnoAparece)
                {
                    BoardManagerGlobal.Instance?.AgregarMensajeInterno($"💥 {name} destruye a {obj.name} en {coords}");
                    if (otro != null)
                        otro.ExiliarADimensionDivina();
                    else
                        obj.gameObject.SetActive(false);
                }
                else
                {
                    BoardManagerGlobal.Instance?.AgregarMensajeInterno($"🕊 {name} NO puede aparecer en {coords} por futura más temprana (turno {turnoOtro})");
                    return false;
                }
            }
        }
        return true;
    }

    public void SetPosicionActual(Vector2Int nuevaPos) => tileCoords = nuevaPos;
    public Vector2Int GetPosicionActual() => tileCoords;
    public bool IsVisible() => activadoEnJuego;

    public bool EstaRealmenteEnTablero()
        => tileCoords.x >= 0 && tileCoords.y >= 0 && tileCoords.x <= 7 && tileCoords.y <= 7;

    public bool EsInamovible() => esInamovible;

    // === Stubs requeridos por interfaces ===
    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon) => RevisarSiFichaAliadaLlegó(posicionPeon, peon);
    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha) { }
    public void RevisarSiReinaEnemigaLlegó(Vector2Int p, QueenEnemyController r) { }
    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int p, BlackQueenEnemyController r) { }
    public void RevisarSiTorreNegraEnemigaLlegó(Vector2Int p, BlackRookEnemyController r) { }
    public void RevisarSiAlfilNegroEnemigoLlegó(Vector2Int p, BlackBishopEnemyController r) { }
    public void RevisarSiCaballoNegroEnemigoLlegó(Vector2Int p, BlackKnightEnemyController r) { }
    public void RevisarSiTorreEnemigaLlegó(Vector2Int p, RookEnemyController r) { }
    public void RevisarSiAlfilEnemigoLlegó(Vector2Int p, BishopEnemyController r) { }
    public void RevisarSiCaballoEnemigoLlegó(Vector2Int p, KnightEnemyController r) { }
    public void RevisarSiPeonEnemigoLlegó(Vector2Int p, PawnEnemyController r) { }
    public void RevisarSiAlfilLlegó(Vector2Int pos, BishopController alfil) => RevisarSiFichaAliadaLlegó(pos, alfil);
    public void RevisarSiCaballoLlegó(Vector2Int pos, KnightController caballo) => RevisarSiFichaAliadaLlegó(pos, caballo);
    public void RevisarSiTorreLlegó(Vector2Int pos, RookController torre) => RevisarSiFichaAliadaLlegó(pos, torre);
    public void RevisarSiReinaLlegó(Vector2Int pos, QueenController reina) => RevisarSiFichaAliadaLlegó(pos, reina);
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
}
