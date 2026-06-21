using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class TheCrownKeyofTheSoul : MonoBehaviour, ITileEffect, IObjetoRecoleccionable, IObjetoRecoleccionableEspecial, IPieceWithPosition
{
    public Vector2Int tileCoords;
    public bool esInamovible = false;

    private Image image;
    private PiecePositionerGarden positioner;
    private MovableTileObject movable;
    private bool yaRecolectado = false;

    private void Update()
    {
        if (yaRecolectado) return;
        VerificarAutoChequeoGeneral();
    }

    private void Awake()
    {
        image = GetComponent<Image>();
        positioner = GetComponent<PiecePositionerGarden>();
        movable = GetComponent<MovableTileObject>();

        tileCoords = positioner != null ? positioner.tileCoords : new Vector2Int(-1, -1);
        if (movable != null) movable.activoEnTablero = true;

        // 🔄 Usar Garden7 en lugar de BoardManagerGlobal
        Garden7.Instance?.RegistrarMovimiento(this, tileCoords);
        Garden7.Instance?.AgregarMensajeInterno($"🏆 Trofeo posicionado en {tileCoords}.");
    }



    public void RevisarSiFichaAliadaLlegó(Vector2Int posicion, IFichaAliada ficha)
    {
        // 🔄 Aseguramos sincronización de la posición actual de la llave
        if (positioner != null)
            tileCoords = positioner.tileCoords;

        if (yaRecolectado || tileCoords != posicion) return;

        yaRecolectado = true;
        Garden7.Instance?.AgregarMensajeInterno($"🏆 Trofeo recolectado por {ficha.GetType().Name} en {posicion}.");

        // Puntaje
        var score = FindFirstObjectByType<PlayerScore>();
        if (score != null)
            score.AgregarPuntaje(ObtenerValorPuntaje(), TipoObjetoScore.None); 

        FindFirstObjectByType<CofreController>()?.SetTieneLlave(true);
        RankSystem.Instance?.ReportLegendKey(); 
        SoundManager.Instance.PlaySound(38);
        Destroy(gameObject);
    }



    public void RevisarSiPeonLlegó(Vector2Int posicion, PawnController peon) =>
        RevisarSiFichaAliadaLlegó(posicion, peon);

    public void RevisarSiReyLlegó(Vector2Int posicion, KingController rey) =>
        RevisarSiFichaAliadaLlegó(posicion, rey);

    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
      RevisarSiFichaAliadaLlegó(posicionReina, reina);
    }
    public void RevisarSiAlfilLlegó(Vector2Int posicion, BishopController alfil)
    {
        RevisarSiFichaAliadaLlegó(posicion, alfil);
    }

    public void RevisarSiCaballoLlegó(Vector2Int posicion, KnightController caballo)
    {
        RevisarSiFichaAliadaLlegó(posicion, caballo);
    }

    public void RevisarSiTorreLlegó(Vector2Int posicion, RookController torre)
    {
        RevisarSiFichaAliadaLlegó(posicion, torre);
    } 
    
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        RevisarSiFichaAliadaLlegó(posicion, reyLibre);
    }

    public void RevisarSiFichaLlegó(Vector2Int posicion, IFicha ficha)
    {
        if (ficha is IFichaAliada aliada)
            RevisarSiFichaAliadaLlegó(posicion, aliada);
    }

    public void VerificarTurnoActual(int turnoActual)
    {
        // Nada que hacer: el Diamante no viene del futuro
    }

    public bool EsInamovible() => esInamovible;

    public int ObtenerValorPuntaje() => 7000; // Valor configurable

    public void SetPosicionActual(Vector2Int nuevaPos)
    {
        tileCoords = nuevaPos;
        if (positioner != null) positioner.tileCoords = nuevaPos;
        if (movable != null) movable.tileCoords = nuevaPos;
    }

    private void VerificarAutoChequeoGeneral()
    {
        var fichasAliadas = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IFichaAliada>();

        foreach (var ficha in fichasAliadas)
        {
            RevisarSiFichaAliadaLlegó(ficha.GetPosicionActual(), ficha);
        }
    }


    public Vector2Int GetPosicionActual() => tileCoords;

    public bool EstaRealmenteEnTablero() =>
        tileCoords.x >= 0 && tileCoords.y >= 0 && tileCoords.x <= 7 && tileCoords.y <= 7;

    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinaenemiga)
    {
        //
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

    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
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
}
