using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BlackRookEnemyController : MonoBehaviour, IPointerClickHandler, ITileEffect, IPieceWithPosition, IFicha, IFichaEnemiga
{
    [Header("Jerarquía de ataque")]
    public int rangoKillZone { get; set; } = 7; // Rango letal en línea recta
    public int rangoRangeZone { get; set; } = 7; // Rango de visión máximo

    public bool esInamovible = false;

    private Vector2Int posicionActual;
    private bool mostrandoRango = false;
    private bool killContabilizada = false; // 👈 NUEVO

    private void Start()
    {
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♖ Torre Negra inició en {posicionActual}");
        }
        else
        {
            posicionActual = new Vector2Int(0, 0);
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en la Torre Negra. Usando (0,0).");
        }

        BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
        BoardManagerGlobal.Instance.RegistrarFichaEnemiga(this);
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
        if (nuevaPos == BoardManagerGlobal.DimensionDivina && !killContabilizada)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyRookBlackKill();
            Debug.Log("☠️ BlackRookEnemy contado como kill (exiliado a Dimensión Divina).");
        }


        posicionActual = nuevaPos;
        if (TryGetComponent<PiecePositioner>(out var piecePositioner))
            piecePositioner.tileCoords = nuevaPos;

        foreach (var efecto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual).OfType<ITileEffect>())
            efecto.RevisarSiTorreNegraEnemigaLlegó(posicionActual, null);

        BoardManagerGlobal.Instance.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♖ Torre Negra actualizó su posición lógica a {nuevaPos}");
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void OnDestroy()
    {
        if (!killContabilizada && Application.isPlaying && gameObject.scene.isLoaded)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyRookBlackKill();
            Debug.Log("☠️ BlackRookEnemy contado en OnDestroy (fallback).");
        }
    }


    public Vector2Int GetPosicionActual() => posicionActual;

    public void OnPointerClick(PointerEventData eventData)
    {
        var manager = FindFirstObjectByType<ChessGameManager>();
        if (manager == null || !manager.IsJuegoActivo())
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ No se puede mostrar rango: juego no activo.");
            BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
            return;
        }

        mostrandoRango = !mostrandoRango;

        if (mostrandoRango)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Mostrando rango de ataque (ortogonal)");
            MostrarRangoDeAtaque();
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Ocultando rango de ataque");
            OcultarRangoDeAtaque();
        }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void RevisarAmenazaAPieza(Vector2Int posicionPieza, System.Action efectoSobrePieza)
    {
        int dx = posicionPieza.x - posicionActual.x;
        int dy = posicionPieza.y - posicionActual.y;

        // 🔹 Movimiento válido solo ortogonal (dx=0 o dy=0)
        bool esDireccionValida = dx == 0 || dy == 0;
        if (!esDireccionValida) return;

        if (HayObstaculoEntre(posicionActual, posicionPieza))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛡️ Amenaza de Torre bloqueada por obstáculo entre {posicionActual} y {posicionPieza}");
            return;
        }

        if (Vector2Int.Distance(posicionActual, posicionPieza) <= rangoKillZone)
            efectoSobrePieza.Invoke();
    }

    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Negra no ataca: Reina Negra ya ejecutó al Rey.");
            return;
        }
    
        RevisarAmenazaAPieza(posicionRey, () =>
        {
            StartCoroutine(MatarPiezaDespuesDelay(rey, posicionRey));
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        RevisarAmenazaAPieza(posicionPeon, () =>
        {
            StartCoroutine(MatarPiezaDespuesDelay(peon, posicionPeon));
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiAlfilLlegó(Vector2Int posicionAlfil, BishopController alfil)
    {
        RevisarAmenazaAPieza(posicionAlfil, () =>
        {
            StartCoroutine(ProcesarAmenazasDesdeArbitro());
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiCaballoLlegó(Vector2Int posicionCaballo, KnightController caballo)
    {
        RevisarAmenazaAPieza(posicionCaballo, () =>
        {
            StartCoroutine(ProcesarAmenazasDesdeArbitro());
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiTorreLlegó(Vector2Int posicionTorre, RookController torre)
    {
        RevisarAmenazaAPieza(posicionTorre, () =>
        {
            StartCoroutine(ProcesarAmenazasDesdeArbitro());
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }
    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
        RevisarAmenazaAPieza(posicionReina, () =>
        {
            StartCoroutine(ProcesarAmenazasDesdeArbitro());
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private IEnumerator MatarPiezaDespuesDelay(MonoBehaviour pieza, Vector2Int posicion)
{
    SetPosicionActual(posicion);

    if (TryGetComponent<MovableTileObject>(out var movable))
        movable.tileCoords = posicion;

    transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(posicion);

    if (pieza is IPieceWithPosition piezaVictima)
        piezaVictima.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

    
    if (pieza is KingController rey)
    {
        if (BoardManagerGlobal.Instance.reinaNegraAtaco || BoardManagerGlobal.Instance.torreNegraAtaco)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Negra aborta: el Rey ya fue ejecutado.");
        yield break;
    }
        BoardManagerGlobal.Instance.torreNegraAtaco = true;
        rey.GetComponent<UnityEngine.UI.Image>().enabled = false;
        yield return new WaitForSeconds(3f);        
    }

    if (pieza is PawnController peon)
    {
        peon.OcultarMovimientos();
        peon.mostrandoMovimientos = false;
    }
    if (pieza is RookController torre)
    {
        torre.OcultarMovimientos();
        torre.mostrandoMovimientos = false;
    }
    if (pieza is BishopController alfil)
    {
        alfil.OcultarMovimientos();
        alfil.mostrandoMovimientos = false;
    }
    if (pieza is KnightController caballo)
    {
        caballo.OcultarMovimientos();
        caballo.mostrandoMovimientos = false;
    }
    if (pieza is QueenController reina)
    {
        reina.OcultarMovimientos();
        reina.mostrandoMovimientos = false;
    }

    Destroy(pieza.gameObject);

    BoardManagerGlobal.Instance.AgregarMensajeInterno(
        $"💀 {pieza.name} ejecutado por la Torre Negra en {posicion}"
    );

    // 🔹 Marcar ataque en jerarquía
    BoardManagerGlobal.Instance.torreNegraAtaco = true;
    SoundManager.Instance.PlaySound(5);
    yield return new WaitForSeconds(1f);
}


    public void MostrarRangoDeAtaque()
    {
        OcultarRangoDeAtaque();

        Tile tileCentral = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (tileCentral != null)
            tileCentral.HighlightBlackAttack(true);

        Vector2Int[] direcciones = {
            new Vector2Int(1,0), new Vector2Int(-1,0), // E-O
            new Vector2Int(0,1), new Vector2Int(0,-1)  // N-S
        };

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= rangoRangeZone; i++)
            {
                Vector2Int coord = posicionActual + dir * i;
                if (coord.x < 0 || coord.y < 0 || coord.x > 7 || coord.y > 7)
                    break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(coord);

                // 💡 NUEVO: Bloqueo visual si hay cualquier otra ficha enemiga
                bool bloqueVisual = objetos.Any(obj =>
                obj is IFichaEnemiga && (Object)obj != this);

                if (bloqueVisual)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"👁️ Torre Negra no colorea {coord} (ocupado por otra enemiga)");
                    break; // 🔺 No pinta ni sigue la línea
                }

                Tile tile = BoardManagerGlobal.Instance.GetTileAt(coord);
                if (tile == null) break;

                if (i <= rangoKillZone)
                    tile.HighlightBlackAttack(true);
                else
                    tile.HighlightBlackAttack(true);

                // 🔹 Detener la visual si hay obstáculo
                bool hayObstaculo = objetos.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable);

                if (hayObstaculo)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Visión de Torre bloqueada por {objetos.First()} en {coord}");
                    break;
                }
            }
        }
    }

    public void OcultarRangoDeAtaque()
    {
        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
            tile.ResetColor();
    }

    public void VerificarTurnoActual(int turnoActual)
    {
        RevisarAmenazasEnZona();
    }

    public void RevisarAmenazasEnZona()
    {
        StartCoroutine(ProcesarAmenazasDesdeArbitro());
    }

    private IEnumerator ProcesarAmenazasDesdeArbitro()
    {
        int asesinatos = 0;

        if (rangoKillZone <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Negra sin energía letal este turno.");
            yield break;
        }

        // 🔹 Pausa inicial mínima para ceder prioridad a ReinaNegra
        yield return new WaitForSeconds(0.03f);

        // 🔹 Jerarquía: si Reina atacó, cedo mi turno
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Negra cede: Reina Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreNegra(false);
            yield break;
        }

        Vector2Int[] direcciones = {
            new Vector2Int(1,0),   // Este
            new Vector2Int(-1,0),  // Oeste
            new Vector2Int(0,1),   // Norte
            new Vector2Int(0,-1)   // Sur
        };

        foreach (var dir in direcciones)
        {
            Vector2Int paso = posicionActual;
            for (int i = 1; i <= rangoRangeZone; i++)
            {
                paso += dir;
                if (paso.x < 0 || paso.y < 0 || paso.x > 7 || paso.y > 7)
                    break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);

                var fichaAliada = objetos.OfType<IFichaAliada>().FirstOrDefault();
                if (fichaAliada != null)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno(
                        $"💥 Torre Negra ejecuta a {((MonoBehaviour)fichaAliada).name} en {paso}"
                    );

                    yield return StartCoroutine(MatarPiezaDespuesDelay((MonoBehaviour)fichaAliada, paso));
                    asesinatos++;

                    if (asesinatos >= 7)
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno("🩸 Torre Negra alcanzó su límite de 7 ejecuciones.");
                        BoardManagerGlobal.Instance.ReportarFinInspeccionTorreNegra(true);
                        yield break;
                    }

                    continue;
                }

                bool hayObstaculo = objetos.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
                );
                if (hayObstaculo) break;
            }
        }

        BoardManagerGlobal.Instance.ReportarFinInspeccionTorreNegra(asesinatos > 0);

        if (asesinatos > 0)
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♖ Torre Negra completó su barrido con {asesinatos} ejecución(es).");
        else
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Negra no encontró víctimas en este barrido.");
    }


    public void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento)
    {
        RevisarAmenazasEnZona();
    }

    private bool HayObstaculoEntre(Vector2Int origen, Vector2Int destino)
    {
        int dx = destino.x - origen.x;
        int dy = destino.y - origen.y;

        if (!(dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy)))
            return false;

        Vector2Int direccion = new Vector2Int(
            dx == 0 ? 0 : (dx > 0 ? 1 : -1),
            dy == 0 ? 0 : (dy > 0 ? 1 : -1)
        );

        Vector2Int paso = origen + direccion;
        while (paso != destino)
        {
            if (paso.x < 0 || paso.y < 0 || paso.x > 7 || paso.y > 7)
                break;

            var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);
            bool hayObstaculo = objetos.Any(obj =>
            (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
            );

            if (hayObstaculo)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"🔰 Obstáculo detectado en {paso}. Línea bloqueada.");
                return true;
            }

            paso += direccion;
        }

        return false;
    }
    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinanegraenemiga)
    {
        //
    }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        //
    }

    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
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

    public void RevisarSiTorreEnemigaLlegó(Vector2Int posicion, RookEnemyController torreenemiga)
    {
        //
    }

    public void RevisarSiAlfilEnemigoLlegó(Vector2Int posicion, BishopEnemyController alfilenemigo)
    {
        //
    }

    public void MostrarRango() => MostrarRangoDeAtaque();
    public void OcultarRango() => OcultarRangoDeAtaque();
    public bool EsInamovible() => esInamovible;
    public void ReiniciarTurno() { rangoKillZone = 7; rangoRangeZone = 7; }

    public void RevisarSiCaballoEnemigoLlegó(Vector2Int posicion, KnightEnemyController caballoenemigo)
    {
        //
    }
    public void RevisarSiPeonEnemigoLlegó(Vector2Int posicion, PawnEnemyController peonenemigo)
    {
        //
    }
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
}
