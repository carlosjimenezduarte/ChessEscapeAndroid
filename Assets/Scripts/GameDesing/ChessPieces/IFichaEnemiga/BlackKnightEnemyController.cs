using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BlackKnightEnemyController : MonoBehaviour, IPointerClickHandler, ITileEffect, IPieceWithPosition, IFicha, IFichaEnemiga
{
    [Header("Jerarquía de ataque")]
    public int rangoKillZone { get; set; } = 1; // Caballo solo ataca en su salto (1 salto = 1 ataque)
    public int rangoRangeZone { get; set; } = 2; // Solo revisa casillas en L

    public bool esInamovible = false;

    private Vector2Int posicionActual;
    private bool mostrandoRango = false;
    private bool killContabilizada = false; 

    // Movimientos posibles del caballo (en L)
    private readonly Vector2Int[] movimientosL = new Vector2Int[]
    {
        new Vector2Int(1,2), new Vector2Int(2,1),
        new Vector2Int(-1,2), new Vector2Int(-2,1),
        new Vector2Int(1,-2), new Vector2Int(2,-1),
        new Vector2Int(-1,-2), new Vector2Int(-2,-1)
    };

    private void Start()
    {
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♞ Caballo Negro inició en {posicionActual}");
        }
        else
        {
            posicionActual = new Vector2Int(0, 0);
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en el Caballo Negro. Usando (0,0).");
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
            LevelProgress.Instance?.AddEnemyKnightBlackKill();
            Debug.Log("☠️ BlackKnightEnemy contado como kill (Dimensión Divina).");
        }


        posicionActual = nuevaPos;
        if (TryGetComponent<PiecePositioner>(out var piecePositioner))
            piecePositioner.tileCoords = nuevaPos;

        foreach (var efecto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual).OfType<ITileEffect>())
            efecto.RevisarSiCaballoNegroEnemigoLlegó(posicionActual, null);

        BoardManagerGlobal.Instance.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♞ Caballo Negro actualizó su posición lógica a {nuevaPos}");
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void OnDestroy()
    {
        if (!killContabilizada && Application.isPlaying && gameObject.scene.isLoaded)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyKnightBlackKill();
            Debug.Log("☠️ BlackKnightEnemy contado en OnDestroy (fallback).");
        }
    }


    public Vector2Int GetPosicionActual() => posicionActual;

    public void OnPointerClick(PointerEventData eventData)
    {
        var manager = FindFirstObjectByType<ChessGameManager>();
        if (manager == null || !manager.IsJuegoActivo())
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ No se puede mostrar rango: juego no activo.");
            BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
            return;
        }

        mostrandoRango = !mostrandoRango;

        if (mostrandoRango)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Mostrando rango de ataque (L)");
            MostrarRangoDeAtaque();
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Ocultando rango de ataque");
            OcultarRangoDeAtaque();
        }

        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void RevisarAmenazaAPieza(Vector2Int posicionPieza, System.Action efectoSobrePieza)
    {
        // Solo ataca si la pieza está exactamente a un salto de caballo
        foreach (var delta in movimientosL)
        {
            if (posicionActual + delta == posicionPieza)
            {
                efectoSobrePieza.Invoke();
                return;
            }
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
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Alfil Negro aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Alfil Negro aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Alfil Negro aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Alfil Negro aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            
            BoardManagerGlobal.Instance.caballoNegraAtaco = true;
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
            $"💀 {pieza.name} ejecutado por el Caballo Negro en {posicion}"
        );

        // 🔹 Marcar ataque para jerarquía
        BoardManagerGlobal.Instance.caballoNegraAtaco = true;
        SoundManager.Instance.PlaySound(5);
        yield return new WaitForSeconds(1f);
    }

    public void MostrarRangoDeAtaque()
    {
         if (rangoRangeZone <= 0)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            "🛡♞ Caballo Negro sin rango de amenaza por ESCUDO."
        );
        OcultarRangoDeAtaque();
        return;
    }
        OcultarRangoDeAtaque();

        Tile tileCentral = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (tileCentral != null)
            tileCentral.HighlightBlackAttack(true); // Casilla del caballo

        foreach (var delta in movimientosL)
        {
            Vector2Int coord = posicionActual + delta;
            if (coord.x < 0 || coord.y < 0 || coord.x > 7 || coord.y > 7)
                continue;

            var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(coord);

            // 💡 NUEVO: Si la casilla final está ocupada por otra ficha enemiga, no coloreamos
            bool bloqueVisual = objetos.Any(obj =>
                obj is IFichaEnemiga && (Object)obj != this);

            if (bloqueVisual)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno(
                    $"👁️ Caballo Negro no colorea {coord} (ocupado por otra ficha enemiga)"
                );
                continue; // No colorea, pero sigue revisando las demás L
            }

            Tile tile = BoardManagerGlobal.Instance.GetTileAt(coord);
            if (tile != null)
                tile.HighlightBlackAttack(true); // Siempre letal en su salto
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

        // 🔹 Pequeña espera para respetar jerarquía
        yield return new WaitForSeconds(0.05f);

        // 🔹 Verificación jerárquica
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Negro cede: Reina Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoNegro(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Negro cede: Torre Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoNegro(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Negro cede: Alfil Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoNegro(false);
            yield break;
        }

        if (rangoKillZone <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Negro sin energía letal este turno.");
            yield break;
        }

        // ♻️ Lógica de ataque depredador
        bool continuar = true;
        while (continuar && asesinatos < 7)
        {
            continuar = false; // Se activa solo si mata a alguien en este ciclo

            foreach (var delta in movimientosL)
            {
                Vector2Int destino = posicionActual + delta;
                if (destino.x < 0 || destino.y < 0 || destino.x > 7 || destino.y > 7)
                    continue;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(destino);
                var fichaAliada = objetos.OfType<IFichaAliada>().FirstOrDefault();

                if (fichaAliada != null)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno(
                        $"💥 Caballo Negro ejecuta a {((MonoBehaviour)fichaAliada).name} en {destino}"
                    );

                    yield return StartCoroutine(MatarPiezaDespuesDelay((MonoBehaviour)fichaAliada, destino));
                    asesinatos++;
                    continuar = true;
                    BoardManagerGlobal.Instance.caballoNegraAtaco = true;

                    if (asesinatos >= 7)
                    {
                        BoardManagerGlobal.Instance.AgregarMensajeInterno("🩸 Caballo Negro alcanzó su límite de 7 ejecuciones.");
                        BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoNegro(true);
                        yield break;
                    }

                    // 🔹 Pausa corta para claridad visual
                    yield return new WaitForSeconds(0.1f);

                    // 🔹 Rompe el foreach para reescanear desde nueva posición
                    break;
                }
            }
        }

        // 🔹 Informe final y notificación al árbitro
        BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoNegro(asesinatos > 0);

        if (asesinatos > 0)
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♞ Caballo Negro completó su barrido con {asesinatos} ejecución(es)."
            );
        else
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "♞ Caballo Negro no encontró víctimas en este barrido."
            );

        yield break;
    }


    public void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento)
    {
        RevisarAmenazasEnZona();
    }

    public void MostrarRango() => MostrarRangoDeAtaque();
    public void OcultarRango() => OcultarRangoDeAtaque();
    public bool EsInamovible() => esInamovible;
    public void ReiniciarTurno() { rangoKillZone = 1; rangoRangeZone = 2; }

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

    public void RevisarSiReinaNegraEnemigaLlegó(Vector2Int posicion, BlackQueenEnemyController reinanegraenemiga)
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
