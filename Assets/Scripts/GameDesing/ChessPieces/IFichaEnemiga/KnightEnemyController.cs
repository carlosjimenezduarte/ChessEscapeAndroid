using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class KnightEnemyController : MonoBehaviour, IPointerClickHandler, ITileEffect, IPieceWithPosition, IFicha, IFichaEnemiga
{
    [Header("Jerarquía de ataque")]
    public int rangoKillZone { get; set; } = 1;  // Solo la casilla donde cae
    public bool esInamovible = false;
    public int rangoRangeZone { get; set; } = 1; // Solo marca las 8 posiciones posibles

    [Header("Prefab para zonas peligrosas")]

    private Vector2Int posicionActual;
    private bool mostrandoRango = false;
    private bool killContabilizada = false;


    // Movimientos tipo L del Caballo
    private static readonly Vector2Int[] movimientosCaballo = {
        new Vector2Int( 2,  1), new Vector2Int( 2, -1),
        new Vector2Int(-2,  1), new Vector2Int(-2, -1),
        new Vector2Int( 1,  2), new Vector2Int( 1, -2),
        new Vector2Int(-1,  2), new Vector2Int(-1, -2)
    };

    private void Start()
    {
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♞ Caballo inició en {posicionActual}");
        }
        else
        {
            posicionActual = new Vector2Int(0, 0);
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en el Caballo. Usando (0,0).");
        }

        BoardManagerGlobal.Instance.RegistrarMovimiento(this, posicionActual);
        BoardManagerGlobal.Instance.RegistrarFichaEnemiga(this);
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public void SetPosicionActual(Vector2Int nuevaPos)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { posicionActual = nuevaPos; return; }
#endif
        // ✅ Si lo exilian a Dimensión Divina, cuenta como kill del jugador
        if (nuevaPos == BoardManagerGlobal.DimensionDivina && !killContabilizada)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyKnightKill();
            Debug.Log("☠️ KnightEnemy contado como kill del jugador (exiliado a Dimensión Divina).");
        }

        posicionActual = nuevaPos;

        if (TryGetComponent<PiecePositioner>(out var piecePositioner))
            piecePositioner.tileCoords = nuevaPos;

        foreach (var efecto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual).OfType<ITileEffect>())
            efecto.RevisarSiCaballoEnemigoLlegó(posicionActual, this);

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♞ Caballo actualizó su posición lógica a {nuevaPos}");
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void OnDestroy() // 👈 NUEVO fallback seguro
    {
        if (!killContabilizada && Application.isPlaying && gameObject.scene.isLoaded)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyKnightKill();
            Debug.Log("☠️ KnightEnemy contado en OnDestroy (fallback).");
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
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Ocultando rango de ataque (L)");
            OcultarRangoDeAtaque();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void RevisarAmenazaAPieza(Vector2Int posicionPieza, System.Action efectoSobrePieza)
    {
        Vector2Int diff = posicionPieza - posicionActual;
        if (!movimientosCaballo.Contains(diff)) return; // Solo si está en L exacta

        // Caballo salta: no revisa obstáculos
        efectoSobrePieza.Invoke();
    }

    public void RevisarSiReyLlegó(Vector2Int posicionRey, KingController rey)
    {
        RevisarAmenazaAPieza(posicionRey, () => StartCoroutine(ProcesarAmenazasDesdeArbitro()));
    }

    public void RevisarSiPeonLlegó(Vector2Int posicionPeon, PawnController peon)
    {
        RevisarAmenazaAPieza(posicionPeon, () => StartCoroutine(ProcesarAmenazasDesdeArbitro()));
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
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.reinaRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilRojoAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoRojoAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Caballo Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            rey.OcultarMovimientos();
            rey.GetComponent<UnityEngine.UI.Image>().enabled = false;
            BoardManagerGlobal.Instance.caballoRojoAtaco = true;
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
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 {pieza.name} ejecutado por el Caballo en {posicion}");

        BoardManagerGlobal.Instance.caballoRojoAtaco = true;
        SoundManager.Instance.PlaySound(5);
        yield return new WaitForSeconds(0.5f);
    }

    public void MostrarRangoDeAtaque()
    {
        // 🛡 ESCUDO: sin rango de amenaza, no pintamos nada
    if (rangoRangeZone <= 0)
    {
        BoardManagerGlobal.Instance.AgregarMensajeInterno(
            "🛡♞ Caballo Rojo sin rango de amenaza por ESCUDO."
        );
        OcultarRangoDeAtaque();
        return;
    }
    
        OcultarRangoDeAtaque();

        Tile tileCentral = BoardManagerGlobal.Instance.GetTileAt(posicionActual);
        if (tileCentral != null)
            tileCentral.HighlightEnemyKillZone(true);

        foreach (var move in movimientosCaballo)
        {
            Vector2Int coord = posicionActual + move;
            if (coord.x < 0 || coord.y < 0 || coord.x > 7 || coord.y > 7) continue;

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
            if (tile == null) continue;

            tile.HighlightEnemyKillZone(true);
        }
    }

    public void OcultarRangoDeAtaque()
    {
        foreach (Tile tile in BoardManagerGlobal.Instance.tiles)
            tile.ResetColor();
    }

    public void VerificarTurnoActual(int turnoActual) => RevisarAmenazasEnZona();
    public void RevisarAmenazasEnZona() => StartCoroutine(ProcesarAmenazasDesdeArbitro());

    private void RevisarObjetosRecoleccionablesEnCasilla()
    {
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Caballo destruye objeto {objeto} en {posicionActual}");
                if (objeto is Potion1PM pocion)
                    pocion.ExiliarADimensionDivina();

                Destroy(((MonoBehaviour)objeto).gameObject);
            }
        }
    }



    public void MostrarRango() => MostrarRangoDeAtaque();
    public void OcultarRango() => OcultarRangoDeAtaque();

    public void RevisarSiFichaLlegó(Vector2Int posicionFicha, IFicha ficha)
    {
        if (posicionFicha != posicionActual) return;
        RevisarObjetosRecoleccionablesEnCasilla();
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    public bool EsInamovible() => esInamovible;

    public void ReiniciarTurno()
    {
        rangoKillZone = 1;
        rangoRangeZone = 1;
    }

    private IEnumerator ProcesarAmenazasDesdeArbitro()
    {
        yield return new WaitForSeconds(0.09f);

        if (rangoKillZone <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "🛡♞ Caballo Rojo sin energía letal este turno (ESCUDO activo)."
            );
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }


        if (BoardManagerGlobal.Instance.caballoRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "♞ Caballo Rojo aborta inspección: ya realizó su ataque este turno."
            );
            yield break;
        }

        // 🔹 Verificación jerárquica de prioridad
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Reina Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Torre Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Alfil Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.caballoNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Caballo Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.reinaRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Reina Roja ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Torre Roja ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♞ Caballo Rojo cede: Alfil Rojo ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(false);
            yield break;
        }

        int asesinatos = 0;

        foreach (var move in movimientosCaballo)
        {
            Vector2Int target = posicionActual + move;
            if (target.x < 0 || target.y < 0 || target.x > 7 || target.y > 7) continue;

            var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(target);
            var fichaAliada = objetos.OfType<IFichaAliada>().FirstOrDefault();

            if (fichaAliada != null)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Caballo Rojo ejecuta a {((MonoBehaviour)fichaAliada).name} en {target}");
                yield return StartCoroutine(MatarPiezaDespuesDelay((MonoBehaviour)fichaAliada, target));
                asesinatos++;
                break; // Solo un asesinato por turno
            }
        }

        BoardManagerGlobal.Instance.ReportarFinInspeccionCaballoRojo(asesinatos > 0);
        RevisarObjetosRecoleccionablesEnCasilla();
    }

    public void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento)
    {
        if (BoardManagerGlobal.Instance.caballoRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♞ Peón Roja ignora movimiento {idMovimiento} porque ya atacó este turno."
            );
            return;
        }



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
