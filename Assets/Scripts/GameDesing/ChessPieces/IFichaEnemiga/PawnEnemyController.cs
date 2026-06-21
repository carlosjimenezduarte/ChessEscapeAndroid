using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PawnEnemyController : MonoBehaviour, IPointerClickHandler, ITileEffect, IPieceWithPosition, IFicha, IFichaEnemiga
{
    [Header("Jerarquía de ataque")]
    public int rangoKillZone { get; set; } = 1;
    public bool esInamovible = false;
    public int rangoRangeZone { get; set; } = 1;

    private Vector2Int posicionActual;
    private bool mostrandoRango = false;

    private bool killContabilizada = false; 


    private void Start()
    {
        // 1️⃣ Determinar posición inicial
        PiecePositioner piecePositioner = GetComponent<PiecePositioner>();
        if (piecePositioner != null)
        {
            posicionActual = piecePositioner.tileCoords;
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón inició en {posicionActual}");
        }
        else
        {
            posicionActual = new Vector2Int(0, 0);
            BoardManagerGlobal.Instance.AgregarMensajeInterno("⚠️ No hay PiecePositioner en el Peón. Usando (0,0).");
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
        // ✅ Si me mandan a la Dimensión Divina, es porque me mató el jugador
        if (nuevaPos == BoardManagerGlobal.DimensionDivina && !killContabilizada)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyPawnKill();
            Debug.Log("☠️ PawnEnemy contado como kill del jugador (exiliado a Dimensión Divina).");
        }

        posicionActual = nuevaPos;

        if (TryGetComponent<PiecePositioner>(out var piecePositioner))
            piecePositioner.tileCoords = nuevaPos;

        foreach (var efecto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual).OfType<ITileEffect>())
            efecto.RevisarSiPeonEnemigoLlegó(posicionActual, this);

        BoardManagerGlobal.Instance?.RegistrarMovimiento(this, nuevaPos);
        BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón actualizó su posición lógica a {nuevaPos}");
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    // (opcional) por si algún atacante no llama SetPosicionActual antes de Destroy
    private void OnDestroy()
    {
        if (!killContabilizada && Application.isPlaying && gameObject.scene.isLoaded)
        {
            killContabilizada = true;
            LevelProgress.Instance?.AddEnemyPawnKill();
            Debug.Log("☠️ PawnEnemy contado en OnDestroy (fallback).");
        }
    
    }

    public Vector2Int GetPosicionActual() => posicionActual;

    public void OnPointerClick(PointerEventData eventData)
    {
        var manager = FindFirstObjectByType<ChessGameManager>();
        if (manager == null || !manager.IsJuegoActivo())
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ No se puede mostrar rango: juego no activo.");
            BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
            return;
        }

        mostrandoRango = !mostrandoRango;

        if (mostrandoRango)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Mostrando rango de ataque (Tiles)");
            MostrarRangoDeAtaque();
        }
        else
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Ocultando rango de ataque (Tiles)");
            OcultarRangoDeAtaque();
        }
        BoardManagerGlobal.Instance.ReportarEstadoActualDelTablero();
    }

    private void RevisarAmenazaAPieza(Vector2Int posicionPieza, System.Action efectoSobrePieza)
    {
        int dx = posicionPieza.x - posicionActual.x;
        int dy = posicionPieza.y - posicionActual.y;

        // ✅ Solo diagonales
        bool esDireccionValida = Mathf.Abs(dx) == Mathf.Abs(dy);
        if (!esDireccionValida) return;

        if (HayObstaculoEntre(posicionActual, posicionPieza))
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno($"🛡️ Amenaza bloqueada por obstáculo entre Peón {posicionActual} y pieza {posicionPieza}");
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
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoNegraAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.reinaRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.torreRojaAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.alfilRojoAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.caballoRojoAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            if (BoardManagerGlobal.Instance.peonRojoAtaco)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno("♖ Peón Rojo aborta: el Rey ya fue ejecutado.");
                yield break;
            }
            rey.OcultarMovimientos();
            rey.GetComponent<UnityEngine.UI.Image>().enabled = false;
            BoardManagerGlobal.Instance.peonRojoAtaco = true;
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

        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💀 {pieza.name} ejecutado por el Peón en {posicion}");

        BoardManagerGlobal.Instance.peonRojoAtaco = true;
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

                bool bloqueVisual = objetos.Any(obj =>
                    obj is IFichaEnemiga && (Object)obj != this);

                if (bloqueVisual)
                {
                    BoardManagerGlobal.Instance.AgregarMensajeInterno($"👁️ Peón no colorea {coord} (ocupado por otra enemiga)");
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

    public void VerificarTurnoActual(int turnoActual) => RevisarAmenazasEnZona();

    public void RevisarAmenazasEnZona() => StartCoroutine(ProcesarAmenazasDesdeArbitro());

    private void RevisarObjetosRecoleccionablesEnCasilla()
    {
        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Peón destruye objeto {objeto} en {posicionActual}");
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

        foreach (var objeto in BoardManagerGlobal.Instance.ObtenerObjetosEn(posicionActual))
        {
            if (objeto is IObjetoRecoleccionable)
            {
                BoardManagerGlobal.Instance.AgregarMensajeInterno($"♙ Peón destruye objeto {objeto} porque ficha {ficha} lo trajo encima");
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
        rangoKillZone = 1;
        rangoRangeZone = 1;
    }

    private bool HayObstaculoEntre(Vector2Int origen, Vector2Int destino)
    {
        int dx = destino.x - origen.x;
        int dy = destino.y - origen.y;

        if (!(Mathf.Abs(dx) == Mathf.Abs(dy)))
            return false;

        Vector2Int direccion = new Vector2Int(dx > 0 ? 1 : -1, dy > 0 ? 1 : -1);
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
        yield return new WaitForSeconds(0.1f);

        if (BoardManagerGlobal.Instance.peonRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                "♙ Peón Rojo aborta inspección: ya realizó su ataque este turno."
            );
            yield break;
        }

        // 🔹 Verificación jerárquica de prioridad
        if (BoardManagerGlobal.Instance.reinaNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Reina Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Torre Negra ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Alfil Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.caballoNegraAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Caballo Negro ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.reinaRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Reina Roja ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.torreRojaAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Torre Roja ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.alfilRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Alfil Rojo ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }
        if (BoardManagerGlobal.Instance.caballoRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo cede: Caballo Rojo ya atacó.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }

        int asesinatos = 0;

        // 🔹 Si no tiene energía letal, solo penaliza
        if (rangoKillZone <= 0)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno("♙ Peón Rojo no tiene energía letal este turno.");
            BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(false);
            yield break;
        }

        // 🔹 Solo diagonales
        Vector2Int[] direcciones = {
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
    };

        foreach (var dir in direcciones)
        {
            Vector2Int paso = posicionActual;

            for (int i = 1; i <= rangoRangeZone; i++)
            {
                paso += dir;

                // 🚫 Fuera del tablero
                if (paso.x < 0 || paso.y < 0 || paso.x > 7 || paso.y > 7)
                    break;

                var objetos = BoardManagerGlobal.Instance.ObtenerObjetosEn(paso);
                var fichaAliada = objetos.OfType<IFichaAliada>().FirstOrDefault();

                if (fichaAliada != null)
                {
                    // ✅ Distancia de casillas diagonales (no euclidiana)
                    int distanciaTablero = Mathf.Abs(paso.x - posicionActual.x);

                    if (distanciaTablero <= rangoKillZone)
                    {
                        // 💀 Ataque letal
                        BoardManagerGlobal.Instance.AgregarMensajeInterno($"💥 Peón Rojo ejecuta a {((MonoBehaviour)fichaAliada).name} en {paso}");
                        yield return StartCoroutine(MatarPiezaDespuesDelay((MonoBehaviour)fichaAliada, paso));
                        asesinatos++;
                        //rangoKillZone = 0; // Solo mata 1 vez por turno
                        BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(true);
                        yield break;
                    }

                }

                // 🛑 Obstáculo: cualquier ficha o recolectable corta la línea
                bool hayObstaculo = objetos.Any(obj =>
                    (obj is IFicha && obj != (object)this) || obj is IObjetoRecoleccionable
                );
                if (hayObstaculo) break;
            }
        }

        BoardManagerGlobal.Instance.ReportarFinInspeccionPeonRojo(asesinatos > 0);
        RevisarObjetosRecoleccionablesEnCasilla();
        yield break;
    }

    public void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento)
    {
        if (BoardManagerGlobal.Instance.peonRojoAtaco)
        {
            BoardManagerGlobal.Instance.AgregarMensajeInterno(
                $"♙ Peón Roja ignora movimiento {idMovimiento} porque ya atacó este turno."
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
