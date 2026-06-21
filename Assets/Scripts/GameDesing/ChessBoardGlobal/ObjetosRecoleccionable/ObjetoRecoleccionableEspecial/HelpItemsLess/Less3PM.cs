using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;

public class Less3PM : MonoBehaviour, ITileEffect, IObjetoRecoleccionable, IPieceWithPosition
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

        if (piecePositioner == null)
            Debug.LogWarning($"⚠️ {name} no tiene PiecePositioner.");
        if (movable == null)
            Debug.LogWarning($"⚠️ {name} no tiene MovableTileObject.");

        if (vieneDelFuturo)
        {
            ColocarEn(tileCoordsFuturosInciertos);
            if (movable != null) movable.activoEnTablero = false;
            image.enabled = false;
            activadoEnJuego = false;
        }
        else
        {
            tileCoords = piecePositioner != null ? piecePositioner.tileCoords : tileCoords;
            if (movable != null) movable.activoEnTablero = true;
            image.enabled = true;
            activadoEnJuego = true;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ {name} inicializado en tablero en {tileCoords}.");
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
        if (piecePositioner != null)
            piecePositioner.tileCoords = coords;
        if (movable != null)
            movable.tileCoords = coords;

        if (BoardManagerGlobal.Instance != null)
            transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(coords);

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, coords);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧩 {name} colocado en {coords}");
    }

    public void VerificarTurnoActual(int turnoActual)
    {
        if (!activadoEnJuego && turnoActual >= turnoAparece)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"⏳ Turno {turnoActual}. {name} programado para aparecer en {posicionReal}.");
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
            image.enabled = true;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ {name} se materializó en {posicionReal}.");
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
        if (tileCoords == posicionRey)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🧪 {name} detecta al Rey encima. Se activa.");
            rey.GanarPuntoMovimiento(-3);
            Destroy(gameObject);
        }
    }

    public void RevisarSiFichaAliadaLlegó(Vector2Int posicionFicha, IFichaAliada ficha)
    {
        if (tileCoords != posicionFicha || desactivado) return;
        if ((ficha as Object) == null) return; // Unity-null (o destruido)
        if (ficha is KingController) return;

        desactivado = true; // evitamos doble aplicación en el mismo frame

        // Nombre seguro sin arriesgar NRE
        string nombreFicha = "FichaAliada";
        try { nombreFicha = ficha.GetType().Name; } catch { }

        BoardManagerGlobal.Instance?.AgregarMensajeInterno(
            $"🧪 {name} detecta ficha aliada ({nombreFicha}) encima. Bonus de rango aplicado."
        );

        try
        {
            var tipo = ficha.GetType();
            var metodo = tipo.GetMethod("RestarRangoMovimientoSilencioso", new[] { typeof(int) })
                      ?? tipo.GetMethod("RestarRangoMovimiento", new[] { typeof(int) });

            if (metodo != null)
            {
                metodo.Invoke(ficha, new object[] { 3 });
            }
            else
            {
                Debug.LogWarning($"⚠️ {nombreFicha} no implementa AumentarRangoMovimiento ni su versión silenciosa.");
            }
        }
        catch (TargetInvocationException ex)
        {
            Debug.LogError($"💥 Error interno al aplicar bonus a {nombreFicha}: {ex.InnerException?.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 Error al invocar método de bonus en {nombreFicha}: {ex.Message}");
        }

        Destroy(gameObject);
    }

    public void ExiliarADimensionDivina()
    {
        Vector2Int coordsDivinos = BoardManagerGlobal.ObtenerProximaPosicionDivina();
        ColocarEn(coordsDivinos);

        if (movable != null)
            movable.activoEnTablero = false;

        gameObject.SetActive(false);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚀 {name} exiliado a Dimensión Divina en {coordsDivinos}.");
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

            if (obj.TryGetComponent<Less3PM>(out var otro))
            {
                turnoOtro = otro.turnoAparece;
                estaActivo = otro.IsVisible();
            }

            if (pos == coords)
            {
                if (obj is IObjetoRecoleccionableEspecial)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🚫 {name} no puede reemplazar a {obj.name} porque es Especial.");
                    return false;
                }
                if (estaActivo || turnoOtro <= turnoAparece)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 {name} destruye a {obj.name} en {coords}");
                    if (otro != null)
                        otro.ExiliarADimensionDivina();
                    else
                        obj.gameObject.SetActive(false);
                }
                else
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🕊 {name} NO puede aparecer en {coords} por futura más temprana (turno {turnoOtro})");
                    return false;
                }
            }
        }
        return true;
    }

    public void SetPosicionActual(Vector2Int nuevaPos)
    {
        tileCoords = nuevaPos;
    }

    public Vector2Int GetPosicionActual()
    {
        return tileCoords;
    }

    public bool IsVisible() => activadoEnJuego;

    public bool EstaRealmenteEnTablero()
        => tileCoords.x >= 0 && tileCoords.y >= 0 && tileCoords.x <= 7 && tileCoords.y <= 7;

    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        RevisarSiFichaAliadaLlegó(posicionPeon, peon);
    }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        // Este objeto no reacciona a fichas enemigas directamente.
    }

    public bool EsInamovible()
    {
        return esInamovible;
    }

    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
    {
        //RevisarSiReinaEnemigaLlegó(posicion, rey);
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
        RevisarSiFichaAliadaLlegó(posicionAlfil, alfil);
    }

    public void RevisarSiCaballoLlegó(Vector2Int posicionCaballo, KnightController caballo)
    {
        RevisarSiFichaAliadaLlegó(posicionCaballo, caballo);
    }

    public void RevisarSiTorreLlegó(Vector2Int posicionTorre, RookController torre)
    {
        RevisarSiFichaAliadaLlegó(posicionTorre, torre);
    }

    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
        RevisarSiFichaAliadaLlegó(posicionReina, reina);
    }
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
}
