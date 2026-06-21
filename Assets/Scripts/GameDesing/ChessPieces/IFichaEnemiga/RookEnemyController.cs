using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RookEnemyController : MonoBehaviour, IPointerClickHandler, ITileEffect, IPieceWithPosition, IFicha, IFichaEnemiga
{
    [Header("Jerarquía de ataque")]
    public int rangoKillZone { get; set; } = 3;
    public bool esInamovible = false;
    public int rangoRangeZone { get; set; } = 5;

    [Header("Prefab para zonas peligrosas")]
    [SerializeField] public GameObject prefabRojo;

    [Header("Padre para overlays")]
    public Transform dangerOverlayParent;

    private Vector2Int posicionActual;
    private bool mostrandoRango = false;
    private List<GameObject> overlaysInstanciados = new List<GameObject>();
    private Vector2Int ultimaPosicionAmenaza = new Vector2Int(-99, -99);

    private bool killContabilizada = false; 

    private void Start()
    {
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Torre inició en {posicionActual}");
        }
        else
        {
            posicionActual = new Vector2Int(0, 0);
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en la Torre. Usando (0,0).");
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
            LevelProgress.Instance?.AddEnemyRookKill();
            Debug.Log("☠️ RookEnemy contado como kill del jugador (exiliado a Dimensión Divina).");
        }

        posicionActual = nuevaPos;
        if (TryGetComponent<PiecePositioner>(out var piecePositioner))
            piecePositioner.tileCoords = nuevaPos;
        foreach (var efecto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual).OfType<ITileEffect>())
        {
            efecto.RevisarSiTorreEnemigaLlegó(posicionActual, this);
        }

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Torre actualizó su posición lógica a {nuevaPos}");
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void OnDestroy()
    {
        if (!killContabilizada && Application.isPlaying && gameObject.scene.isLoaded)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyRookKill();
            Debug.Log("☠️ RookEnemy contado en OnDestroy (fallback).");
        }
    }


    public Vector2Int GetPosicionActual() => posicionActual;

    public void OnPointerClick(PointerEventData eventData)
    {
        var manager = FindFirstObjectByType<ChessGameManager>();

        if (manager == null || !manager.IsJuegoActivo())
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ No se puede mostrar rango: juego no activo.");
            BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
            return;
        }

        mostrandoRango = !mostrandoRango;

        if (mostrandoRango)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Mostrando rango de ataque (Tiles)");
            MostrarRangoDeAtaque();
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Ocultando rango de ataque (Tiles)");
            OcultarRangoDeAtaque();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void RevisarAmenazaAPieza(Vector2Int posicionPieza, System.Action efectoSobrePieza)
    {
        int dx = posicionPieza.x - posicionActual.x;
        int dy = posicionPieza.y - posicionActual.y;

        bool esDireccionValida = dx == 0 || dy == 0; // Solo ortogonal
        if (!esDireccionValida) return;

        if (HayObstaculoEntre(posicionActual, posicionPieza))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛡️ Amenaza bloqueada por obstáculo entre Torre {posicionActual} y pieza {posicionPieza}");
            return;
        }

        efectoSobrePieza.Invoke();
    }

    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        RevisarAmenazaAPieza(posicionRey, () =>
        {
            StartCoroutine(ProcesarAmenazasDesdeArbitro());
        });
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        RevisarAmenazaAPieza(posicionPeon, () =>
        {
            StartCoroutine(ProcesarAmenazasDesdeArbitro());
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
            if (BoardManagerGlobal.Instance.reinaNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.reinaRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Torre Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            rey.OcultarMovimientos();
            rey.GetComponent<UnityEngine.UI.Image>().enabled = false;
            BoardManagerGlobal.Instance.torreRojaAtaco = true;
            FindFirstObjectByType<ChessGameManager>()?.DetenerJuego();
            yield return new WaitForSeconds(3f);
            
              LevelResultUI.Instance.ShowResults(
                LevelProgress.Instance.keysCollected,
                LevelProgress.Instance.diamondsCollected,
                0, // vidas = 0
                PlayerScore.Instance.GetTotalScore()
            );
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
            $"💀 {pieza.name} ejecutado por la Torre en {posicion}"
        );

        BoardManagerGlobal.Instance.torreRojaAtaco = true;
        SoundManager.Instance.PlaySound(5);
        yield return new WaitForSeconds(1f);
    }

    public void MostrarRangoDeAtaque()
    {
        OcultarRangoDeAtaque();

        Tile tileCentral = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (tileCentral != null)
            tileCentral.HighlightEnemyKillZone(true);

        Vector2Int[] direcciones = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        foreach (var dir in direcciones)
        {
            for (int i = 1; i <= rangoRangeZone; i++)
            {
                Vector2Int coord = posicionActual + dir * i;
                if (coord.x < 0 || coord.y < 0 || coord.x > 7 || coord.y > 7)
                    break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(coord);
                bool bloqueVisual = objetos.Any(obj =>
                    obj is IFichaEnemiga && (Object)obj != this);

                if (bloqueVisual)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"👁️ Torre no colorea {coord} (ocupado por otra enemiga)");
                    break;
                }

                Tile tile = BoardManagerGlobal.Instance.GetTileAt(coord);
                if (tile == null) break;

                if (i <= rangoKillZone)
                    tile.HighlightEnemyKillZone(true);
                else
                    tile.HighlightEnemyRangeZone(true);

                bool hayObstaculo = objetos.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable);

                if (hayObstaculo)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛑 Visión bloqueada por {objetos.First()} en {coord}");
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

    private void RevisarObjetosRecoleccionablesEnCasilla()
    {
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Torre destruye objeto {objeto} en {posicionActual}");
                if (objeto is Potion1PM pocion)
                    pocion.ExiliarADimensionDivina();

                Destroy(((MonoBehaviour)objeto).gameObject);
            }
        }
    }

    public IEnumerator VerificarAmenazaSobre(Vector2Int posicionPieza)
    {
        yield return new WaitForSeconds(0.1f);

        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"♜ [DEBUG] Iniciando VerificarAmenazaSobre hacia {posicionPieza}"
        );

        foreach (var obj in overlaysInstanciados)
            Destroy(obj);
        overlaysInstanciados.Clear();

        Vector2Int[] direcciones = {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
        };

        bool amenazaCreada = false;

        foreach (var dir in direcciones)
        {
            Vector2Int paso = posicionActual;
            List<Vector2Int> lineaDeAtaque = new List<Vector2Int>();

            for (int i = 1; i <= rangoRangeZone; i++)
            {
                paso += dir;

                if (paso.x < 0 || paso.y < 0 || paso.x > 7 || paso.y > 7)
                    break;

                var objetosEnPaso = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);
                bool hayObstaculo = objetosEnPaso.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
                );

                lineaDeAtaque.Add(paso);

                if (paso == posicionPieza)
                {
                    if (HayObstaculoEntre(posicionActual, posicionPieza))
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno(
                            $"♜ [BLOQUEO] Obstáculo detectado, amenaza abortada hacia {posicionPieza}"
                        );
                        ultimaPosicionAmenaza = new Vector2Int(-99, -99);
                        yield break;
                    }
                    var objetivo = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionPieza)
                                    .OfType<IFichaAliada>()
                                    .FirstOrDefault();
                    if (objetivo == null)
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno(
                            $"♜ [ABORTADO] Ficha en {posicionPieza} ya no existe. Amenaza cancelada."
                        );
                        ultimaPosicionAmenaza = new Vector2Int(-99, -99);
                        yield break;
                    }

                    lineaDeAtaque.Insert(0, posicionActual);

                    foreach (var coord in lineaDeAtaque)
                    {
                        GameObject overlay = Instantiate(prefabRojo, dangerOverlayParent);
                        overlay.GetComponent<RectTransform>().anchoredPosition =
                            BoardManagerGlobal.Instance.GetTileAnchoredPosition(coord);
                        overlaysInstanciados.Add(overlay);
                        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ [PREFAB] Overlay rojo en {coord}");
                    }

                    ultimaPosicionAmenaza = posicionPieza;
                    amenazaCreada = true;
                    SoundManager.Instance.PlaySound(11);
                    break;
                }

                if (hayObstaculo) break;
            }

            if (amenazaCreada) break;
        }

        if (!amenazaCreada)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♜ [INFO] No se creó amenaza visual hacia {posicionPieza} (no alineada o bloqueada)"
            );
            ultimaPosicionAmenaza = new Vector2Int(-99, -99);
        }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MostrarRango() => MostrarRangoDeAtaque();
    public void OcultarRango() => OcultarRangoDeAtaque();

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        if (posicionFicha != posicionActual) return;

        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"♜ Torre destruye objeto {objeto} porque ficha {ficha} lo trajo encima");
                if (objeto is Potion1PM pocion)
                    pocion.ExiliarADimensionDivina();

                Destroy(((MonoBehaviour)objeto).gameObject);
            }
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public bool EsInamovible() => esInamovible;

    public void ReiniciarTurno()
    {
        rangoKillZone = 3;
        rangoRangeZone = 5;
    }

    public void RevisarSiTorreEnemigaLlegó(Vector2Int posicion, RookEnemyController torreenemiga)
    {
        if (torreenemiga == this) return;
        if (ultimaPosicionAmenaza == posicion) return;

        ultimaPosicionAmenaza = posicion;

        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is ITileEffect efecto && !(objeto is IFichaEnemiga))
            {
                efecto.RevisarSiTorreEnemigaLlegó(posicion, this);
                BoardManagerGlobal.Instance.AgregarMensajeInterno(
                    $"♜ TileEffect activado por llegada de la Torre a {posicion}"
                );
            }
        }
    }

    private bool HayObstaculoEntre(Vector2Int origen, Vector2Int destino)
    {
        int dx = destino.x - origen.x;
        int dy = destino.y - origen.y;

        if (!(dx == 0 || dy == 0)) // Solo ortogonal
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

    private IEnumerator ProcesarAmenazasDesdeArbitro()
    {
        yield return new WaitForSeconds(0.07f);

        if (BoardManagerGlobal.Instance.torreRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "♜ Torre Roja aborta inspección: ya realizó su ataque este turno."
            );
            yield break;
        }

        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Torre Roja cede: Reina Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Torre Roja cede: Torre Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Torre Roja cede: Alfil Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.caballoNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Torre Roja cede: Caballo Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.reinaRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Torre Roja cede: Reina Roja ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(false);
            yield break;
        }

        int asesinatos = 0;

        if (rangoKillZone <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♜ Torre Roja no tiene energía letal este turno.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(false);
            yield break;
        }

        Vector2Int[] direcciones = {
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
            new Vector2Int(0,1),
            new Vector2Int(0,-1)
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
                    float distancia = Vector2Int.Distance(posicionActual, paso);

                    if (distancia <= rangoKillZone)
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Torre Roja ejecuta a {((MonoBehaviour)fichaAliada).name} en {paso}");
                        yield return StartCoroutine(MatarPiezaDespuesDelay((MonoBehaviour)fichaAliada, paso));
                        asesinatos++;
                        //rangoKillZone = 0;
                        BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(true);
                        yield break;
                    }
                    else
                    {
                        if (fichaAliada is KingController rey)
                        {
                            rey.GanarPuntoMovimiento(-1);                            
                            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                                $"♜ Torre Roja penaliza al Rey en {paso}. PA: {rey.puntosAccionActual}"
                            );
                        }
                        else if (fichaAliada is PawnController peon)
                        {
                            peon.RecibirPenalizacionTorre();
                        }
                        else if (fichaAliada is RookController torre)
                        {
                            torre.RecibirPenalizacionTorre();
                        }
                        else if (fichaAliada is BishopController alfil)
                        {
                            alfil.RecibirPenalizacionTorre();
                        }
                    }
                }

                bool hayObstaculo = objetos.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
                );
                if (hayObstaculo) break;
            }
        }

        BoardManagerGlobal.Instance.ReportarFinInspeccionTorreRoja(asesinatos > 0);
        RevisarObjetosRecoleccionablesEnCasilla();
        yield break;
    }

    public void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento)
    {
        if (BoardManagerGlobal.Instance.torreRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♜ Torre Roja ignora movimiento {idMovimiento} porque ya atacó este turno."
            );
            return;
        }
        StartCoroutine(VerificarAmenazaSobre(posAliada));



        RevisarAmenazasEnZona();
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
    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
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
    public void RevisarSiReinaLlegó(Vector2Int posicionReina, QueenController reina)
    {
        //
    }
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }
    
}