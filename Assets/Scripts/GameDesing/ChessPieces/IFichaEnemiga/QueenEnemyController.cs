using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class QueenEnemyController : MonoBehaviour, IPointerClickHandler, ITileEffect, IPieceWithPosition, IFicha, IFichaEnemiga
{


    [Header("Jerarquía de ataque")]

    public int rangoKillZone { get; set; } = 3;

    public bool esInamovible = false;
    public int rangoRangeZone { get; set; } = 5;

    [Header("Prefab para zonas peligrosas")]
    public GameObject prefabRojo;

    [Header("Padre para overlays")]
    public Transform dangerOverlayParent;

    private Vector2Int posicionActual;
    private bool mostrandoRango = false;

    private List<GameObject> overlaysInstanciados = new List<GameObject>();
    private Vector2Int ultimaPosicionAmenaza = new Vector2Int(-99, -99);
    private bool killContabilizada = false;

    private void Start()
    {
        // 1️⃣ Determinar posición inicial
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Reina inició en {posicionActual}");
        }
        else
        {
            posicionActual = new Vector2Int(0, 0);
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en la Reina. Usando (0,0).");
        }

        // 2️⃣ Registrar posición en el tablero global
        BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);

        // 3️⃣ Registrar como ficha enemiga para el Árbitro Silencioso
        BoardManagerGlobal.Instance.RegistrarFichaEnemiga(this);

        // 4️⃣ Reporte final
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
            LevelProgress.Instance?.AddEnemyQueenKill();
            Debug.Log("☠️ QueenEnemy contado como kill del jugador (exiliado a Dimensión Divina).");
        }


        posicionActual = nuevaPos;
        if (TryGetComponent<PiecePositioner>(out var piecePositioner))
            piecePositioner.tileCoords = nuevaPos;
        foreach (var efecto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual).OfType<ITileEffect>())
        {
            efecto.RevisarSiReinaEnemigaLlegó(posicionActual, this);
        }

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Reina actualizó su posición lógica a {nuevaPos}");
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();

    }
    private void OnDestroy()
    {
        if (!killContabilizada && Application.isPlaying && gameObject.scene.isLoaded)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyQueenKill();
            Debug.Log("☠️ QueenEnemy contado en OnDestroy (fallback).");
        }
    }


    public Vector2Int GetPosicionActual() => posicionActual;

    public void OnPointerClick(PointerEventData eventData)
    {
        var manager = FindFirstObjectByType<ChessGameManager>();


        if (manager == null || !manager.IsJuegoActivo())
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ No se puede mostrar rango: juego no activo.");
            BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
            return;
        }

        mostrandoRango = !mostrandoRango;

        if (mostrandoRango)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Mostrando rango de ataque (Tiles)");
            MostrarRangoDeAtaque();
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Ocultando rango de ataque (Tiles)");
            OcultarRangoDeAtaque();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void RevisarAmenazaAPieza(Vector2Int posicionPieza, System.Action efectoSobrePieza)
    {
        int dx = posicionPieza.x - posicionActual.x;
        int dy = posicionPieza.y - posicionActual.y;

        bool esDireccionValida = dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy);
        if (!esDireccionValida) return;

        // 🛑 NUEVO: cancelamos amenaza si hay obstáculo entre Reina y la ficha
        if (HayObstaculoEntre(posicionActual, posicionPieza))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛡️ Amenaza bloqueada por obstáculo entre Reina {posicionActual} y pieza {posicionPieza}");
            return;
        }

        // Si no hay obstáculo, aplicamos efecto
        if (Vector2Int.Distance(posicionActual, posicionPieza) <= rangoKillZone)
        {
            efectoSobrePieza.Invoke();
        }
        else
        {
            efectoSobrePieza.Invoke();
        }
    }


    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Negro no ataca: Reina Negra ya ejecutó al Rey.");
            return;
        }

        if (BoardManagerGlobal.Instance.torreNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Negro no ataca: Torre Negra ya ejecutó al Rey.");
            return;
        } 

        if (BoardManagerGlobal.Instance.alfilNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Negro no ataca: Torre Negra ya ejecutó al Rey.");
            return;
        } 
        if (BoardManagerGlobal.Instance.caballoNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Negro no ataca: Torre Negra ya ejecutó al Rey.");
            return;
        }

        if (BoardManagerGlobal.Instance.reinaRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Negro no ataca: Torre Negra ya ejecutó al Rey.");
            return;
        }

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

        // 1️⃣ Moverse lógicamente
        SetPosicionActual(posicion);

        if (TryGetComponent<MovableTileObject>(out var movable))
            movable.tileCoords = posicion;

        transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(posicion);

        // 2️⃣ Exiliar la pieza víctima
        if (pieza is IPieceWithPosition piezaVictima)
            piezaVictima.SetPosicionActual(BoardManagerGlobal.DimensionDivina);

        if (pieza is KingController rey)
        {
            if (BoardManagerGlobal.Instance.reinaNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Reina Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Reina Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Reina Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Reina Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.reinaRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Reina Roja aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            rey.OcultarMovimientos();
            rey.GetComponent<UnityEngine.UI.Image>().enabled = false;
            BoardManagerGlobal.Instance.reinaRojaAtaco = true;
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
            $"💀 {pieza.name} ejecutado por la Reina en {posicion}"
        );

        // 🔹 Marcar ataque para jerarquía
        BoardManagerGlobal.Instance.reinaRojaAtaco = true;
        SoundManager.Instance.PlaySound(5);
        yield return new WaitForSeconds(1f);
        BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Reina Roja bloqueada para el resto del turno tras su primer ataque.");
    }


    public void MostrarRangoDeAtaque()
    {
        OcultarRangoDeAtaque();

        Tile tileCentral = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (tileCentral != null)
            tileCentral.HighlightEnemyKillZone(true); // Casilla de la Reina

        Vector2Int[] direcciones = {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1),
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
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
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"👁️ Reina no colorea {coord} (ocupado por otra enemiga)");
                    break; // 🔺 No pinta ni sigue la línea
                }

                Tile tile = BoardManagerGlobal.Instance.GetTileAt(coord);
                if (tile == null) break;

                if (i <= rangoKillZone)
                    tile.HighlightEnemyKillZone(true);
                else
                    tile.HighlightEnemyRangeZone(true);

                // 🛑 Obstáculo que detiene visión (fichas o recolectables)
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
        // ✅ Ahora revisa con el árbitro silencioso
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Reina destruye objeto {objeto} en {posicionActual}");
                if (objeto is Potion1PM pocion)
                    pocion.ExiliarADimensionDivina();

                Destroy(((MonoBehaviour)objeto).gameObject);
            }
        }
    }

    public IEnumerator VerificarAmenazaSobre(Vector2Int posicionPieza)
    {
        // 🔹 Pequeño delay visual para simular "pensamiento"
        yield return new WaitForSeconds(0.1f);

        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            $"♛ [DEBUG] Iniciando VerificarAmenazaSobre hacia {posicionPieza}"
        );

        // 🧹 Limpiar overlays previos
        foreach (var obj in overlaysInstanciados)
            Destroy(obj);
        overlaysInstanciados.Clear();

        // 1️⃣ Vectores de dirección de la Reina (ajedrez)
        Vector2Int[] direcciones = {
        new Vector2Int(1,0),   // Este
        new Vector2Int(-1,0),  // Oeste
        new Vector2Int(0,1),   // Norte
        new Vector2Int(0,-1),  // Sur
        new Vector2Int(1,1),   // NE
        new Vector2Int(-1,1),  // NO
        new Vector2Int(1,-1),  // SE
        new Vector2Int(-1,-1)  // SO
    };

        bool amenazaCreada = false;

        // 2️⃣ Revisar cada dirección
        foreach (var dir in direcciones)
        {
            Vector2Int paso = posicionActual;
            List<Vector2Int> lineaDeAtaque = new List<Vector2Int>();

            for (int i = 1; i <= rangoRangeZone; i++)
            {
                paso += dir;

                // 🚫 Fuera de tablero
                if (paso.x < 0 || paso.y < 0 || paso.x > 7 || paso.y > 7)
                    break;

                // 🔍 Chequear obstáculos
                var objetosEnPaso = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);
                bool hayObstaculo = objetosEnPaso.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
                );

                // ✅ Guardamos este paso como parte de la línea visual
                lineaDeAtaque.Add(paso);

                // 🎯 Si encontramos la posición de la pieza en esta dirección
                if (paso == posicionPieza)
                {
                    // ⚡ Si hay obstáculo previo, abortamos amenaza
                    if (HayObstaculoEntre(posicionActual, posicionPieza))
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno(
                            $"♛ [BLOQUEO] Obstáculo detectado, amenaza abortada hacia {posicionPieza}"
                        );
                        ultimaPosicionAmenaza = new Vector2Int(-99, -99);
                        yield break;
                    }
                    // 🔹 VALIDACIÓN FINAL
                    var objetivo = BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionPieza)
                                    .OfType<IFichaAliada>()
                                    .FirstOrDefault();
                    if (objetivo == null)
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno(
                            $"♛ [ABORTADO] Ficha en {posicionPieza} ya no existe. Amenaza cancelada."
                        );
                        ultimaPosicionAmenaza = new Vector2Int(-99, -99);
                        yield break;
                    }

                    // 🌟 Agregamos también la propia casilla de la Reina al inicio
                    lineaDeAtaque.Insert(0, posicionActual);

                    // 🌟 Instanciar overlays para toda la línea (incluyendo la Reina)
                    foreach (var coord in lineaDeAtaque)
                    {
                        GameObject overlay = Instantiate(prefabRojo, dangerOverlayParent);
                        overlay.GetComponent<RectTransform>().anchoredPosition =
                            BoardManagerGlobal.Instance.GetTileAnchoredPosition(coord);
                        overlaysInstanciados.Add(overlay);
                        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ [PREFAB] Overlay rojo en {coord}");
                        
                    }

                    ultimaPosicionAmenaza = posicionPieza;
                    amenazaCreada = true;
                    SoundManager.Instance.PlaySound(11);
                    break; // ✅ No seguimos más en esta dirección
                }

                // 🛑 Si hay obstáculo en esta casilla, detenemos la línea
                if (hayObstaculo) break;
            }

            if (amenazaCreada) break; // ✅ Salimos si ya sembramos amenaza
        }

        if (!amenazaCreada)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ [INFO] No se creó amenaza visual hacia {posicionPieza} (no alineada o bloqueada)"
            );
            ultimaPosicionAmenaza = new Vector2Int(-99, -99);
        }
        
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void MostrarRango()
    {
        MostrarRangoDeAtaque();
    }

    public void OcultarRango()
    {
        OcultarRangoDeAtaque();
    }

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        if (posicionFicha != posicionActual) return;

        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"♛ Reina destruye objeto {objeto} porque ficha {ficha} lo trajo encima");
                if (objeto is Potion1PM pocion)
                    pocion.ExiliarADimensionDivina();

                Destroy(((MonoBehaviour)objeto).gameObject);
            }
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public bool EsInamovible()
    {
        return esInamovible;
    }

    public void ReiniciarTurno()
    {

        rangoKillZone = 3;
        rangoRangeZone = 5;
    }


    public void RevisarSiReinaEnemigaLlegó(Vector2Int posicion, QueenEnemyController reinaenemiga)
    {
        // 🚫 Evitar bucles infinitos
        if (reinaenemiga == this) return; // No procesar a sí misma

        // 🚫 Evitar múltiples llamadas en cascada dentro del mismo frame
        if (ultimaPosicionAmenaza == posicion)
            return;

        ultimaPosicionAmenaza = posicion;

        // ✅ Solo activar efectos de la casilla (no otras Reinas)
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is ITileEffect efecto && !(objeto is IFichaEnemiga))
            {
                efecto.RevisarSiReinaEnemigaLlegó(posicion, this);
                BoardManagerGlobal.Instance.AgregarMensajeInterno(
                    $"♛ TileEffect activado por llegada de la Reina a {posicion}"
                );
            }
        }
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


    private IEnumerator ProcesarAmenazasDesdeArbitro()
    {
        yield return new WaitForSeconds(0.06f);

        if (BoardManagerGlobal.Instance.reinaRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "♛ Reina Roja aborta inspección: ya realizó su ataque este turno."
            );
            yield break;
        }

        // 🔹 Verificación jerárquica
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Reina Roja cede: Reina Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Reina Roja cede: Torre Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Reina Roja cede: Alfil Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(false);
            yield break;
        }

        if (BoardManagerGlobal.Instance.caballoNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Reina Roja cede: Alfil Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(false);
            yield break;
        }

        int asesinatos = 0;

        // Si ya no tiene rango letal, no hace nada
        if (rangoKillZone <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♛ Reina Roja no tiene energía letal este turno.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(false);
            yield break;
        }

        // Direcciones de ajedrez
        Vector2Int[] direcciones = new Vector2Int[]
        {
        new Vector2Int(1,0),   // Este
        new Vector2Int(-1,0),  // Oeste
        new Vector2Int(0,1),   // Norte
        new Vector2Int(0,-1),  // Sur
        new Vector2Int(1,1),   // NE
        new Vector2Int(-1,1),  // NO
        new Vector2Int(1,-1),  // SE
        new Vector2Int(-1,-1), // SO
        };

        // Buscar objetivos en rango
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

                    // 🔹 Kill o Penalización según distancia
                    if (distancia <= rangoKillZone)
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Reina Roja ejecuta a {((MonoBehaviour)fichaAliada).name} en {paso}");
                        yield return StartCoroutine(MatarPiezaDespuesDelay((MonoBehaviour)fichaAliada, paso));
                        asesinatos++;
                        //rangoKillZone = 0; // Solo un asesinato por turno
                        BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(true);
                        yield break;
                    }
                    else
                    {
                        // 🔹 Penalización por estar en rango visual
                        if (fichaAliada is KingController rey)
                        {
                            rey.GanarPuntoMovimiento(-1);                            
                            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                                $"♛ Reina Roja penaliza al Rey en {paso}. PA: {rey.puntosAccionActual}"
                            );
                        }
                        else if (fichaAliada is PawnController peon)
                        {
                            peon.RecibirPenalizacionReina();
                        }
                        else if (fichaAliada is RookController torre)
                        {
                            torre.RecibirPenalizacionReina();
                        }
                        else if (fichaAliada is BishopController alfil)
                        {
                            alfil.RecibirPenalizacionReina();
                        }

                        // Penalización no rompe la exploración, sigue buscando otras víctimas
                    }
                }

                // Si hay obstáculo enemigo o recolectable, detiene la línea
                bool hayObstaculo = objetos.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
                );
                if (hayObstaculo) break;
            }
        }

        // Si llegó aquí sin matar, igual reporta inspección
        BoardManagerGlobal.Instance.ReportarFinInspeccionReinaRoja(asesinatos > 0);
        RevisarObjetosRecoleccionablesEnCasilla();
        yield break;
    }



    public void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento)
    {
        if (BoardManagerGlobal.Instance.reinaRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♛ Reina Roja ignora movimiento {idMovimiento} porque ya atacó este turno."
            );
            return;
        }

        // ✅ Llamamos a la verificación visual con delay y chequeo de supervivencia
        StartCoroutine(VerificarAmenazaSobre(posAliada));



        // ✅ Solo intenta atacar si nadie más ha atacado en este movimiento


        // ✅ Si llega aquí, es la atacante autorizada
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

    }
    public void RevisarSiReyLibreLlegó(Vector2Int posicion, KingFree reyLibre)
    {
        //
    }

}