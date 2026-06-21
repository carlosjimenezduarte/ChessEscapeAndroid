using UnityEngine;

public interface ITileEffect
{
    void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey);
    void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon);

    void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre);
    void RevisarSiCaballoLlegó(Vector2Int posicionCaballo, KnightController caballo);

    void RevisarSiAlfilLlegó(Vector2Int posicionAlfil, BishopController alfil);

    void RevisarSiTorreLlegó(Vector2Int posicionTorre, RookController torre);

    void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina);

    void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha);

    void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga);

    void RevisarSiTorreEnemigaLlegó(Vector2Int posicion, RookEnemyController torreenemiga);

    void RevisarSiAlfilEnemigoLlegó(Vector2Int posicion, BishopEnemyController alfilenemigo);

    void RevisarSiCaballoEnemigoLlegó(Vector2Int posicion, KnightEnemyController caballoenemigo);

    void RevisarSiPeonEnemigoLlegó(Vector2Int posicion, PawnEnemyController peonenemigo);

    void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinanegraenemiga);

    void RevisarSiTorreNegraEnemigaLlegó(Vector2Int posicion, BlackRookEnemyController torrenegraenemiga);

    void RevisarSiAlfilNegroEnemigoLlegó(Vector2Int posicion, BlackBishopEnemyController alfilnegroenemigo);

    void RevisarSiCaballoNegroEnemigoLlegó(Vector2Int posicion, BlackKnightEnemyController caballonegroenemigo);

    void VerificarTurnoActual(int turnoActual);
    
}