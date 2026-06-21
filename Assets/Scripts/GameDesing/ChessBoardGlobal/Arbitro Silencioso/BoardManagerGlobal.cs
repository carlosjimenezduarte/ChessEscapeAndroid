using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BoardManagerGlobal : MonoBehaviour
{
    private readonly HashSet<AllyKind> coronatedThisLevel = new HashSet<AllyKind>();
    public enum AllyKind { Pawn, Knight, Bishop, Rook, Queen }
    
    public static BoardManagerGlobal Instance;
    public bool reinaNegraAtaco = false;
    public bool torreNegraAtaco = false;
    public bool alfilNegraAtaco = false;
    public bool caballoNegraAtaco = false;
    public bool reinaRojaAtaco = false;
    public bool torreRojaAtaco = false;
    public bool alfilRojoAtaco = false;
    public bool caballoRojoAtaco = false;
    public bool peonRojoAtaco = false;

    private List<IFichaAliada> fichasConEscudo = new List<IFichaAliada>();

    private List<IFichaEnemiga> fichasEnemigasRegistradas = new List<IFichaEnemiga>();

    // 🔁 Control de activaciones por movimiento
    public int idMovimientoActual = 0;
    //private int idUltimoAtaque = -1;
    private static int offsetFuturoIncierto = 0;

    private static int offsetDimensionDivina = 0;
    public static Vector2Int FuturoIncierto = new Vector2Int(100, 100);
    public static Vector2Int DimensionDivina = new Vector2Int(-1, -9999);


    [Header("Lista de todas las casillas del tablero")]
    public List<Tile> tiles = new List<Tile>();

    private Dictionary<Vector2Int, List<IPieceWithPosition>> tableroOcupacion
        = new Dictionary<Vector2Int, List<IPieceWithPosition>>();

    private List<string> mensajesInternos = new List<string>();

    private void Awake()
    {
        Instance = this;
        coronatedThisLevel.Clear();

        foreach (Tile tile in tiles)
        {
            tableroOcupacion[tile.tileCoords] = new List<IPieceWithPosition>();
            AgregarMensajeInterno($"📋 Tile inicializado en {tile.tileCoords}");
        }

    }

    private void Start()
    {
        InicializarRegistroDeFichas();

    }

    public void RegistrarFichaEnemiga(IFichaEnemiga ficha)
    {
        if (!fichasEnemigasRegistradas.Contains(ficha))
        {
            fichasEnemigasRegistradas.Add(ficha);
            AgregarMensajeInterno($"🟥 Ficha enemiga registrada: {((MonoBehaviour)ficha).name}");
        }
    }

    public void RegistrarMovimiento(IPieceWithPosition pieza, Vector2Int nuevaPos)
    {
        foreach (var lista in tableroOcupacion.Values)
            lista.Remove(pieza);

        if (!tableroOcupacion.ContainsKey(nuevaPos))
            tableroOcupacion[nuevaPos] = new List<IPieceWithPosition>();

        tableroOcupacion[nuevaPos].Add(pieza);
        AgregarMensajeInterno($"📌 {pieza} registrado en {nuevaPos}");
    }

    public void EliminarFichaDeCasilla(Vector2Int pos)
    {
        if (tableroOcupacion.TryGetValue(pos, out var lista))
        {
            int cantidadInicial = lista.Count;
            lista.RemoveAll(obj => obj is IFicha);

            int cantidadFinal = lista.Count;
            int eliminados = cantidadInicial - cantidadFinal;

            if (eliminados > 0)
            {
                AgregarMensajeInterno($"❌ Se eliminaron {eliminados} fichas de la casilla {pos}.");
            }
            else
            {
                AgregarMensajeInterno($"ℹ️ No había fichas para eliminar en la casilla {pos}.");
            }
        }
        else
        {
            AgregarMensajeInterno($"⚠️ Casilla {pos} no está registrada en el tablero. No se pudo eliminar ficha.");
        }
    }

    public void RegistrarFichaEnCasilla(Vector2Int pos, IPieceWithPosition ficha)
    {
        if (!tableroOcupacion.ContainsKey(pos))
        {
            tableroOcupacion[pos] = new List<IPieceWithPosition>();
        }

        if (!tableroOcupacion[pos].Contains(ficha))
        {
            tableroOcupacion[pos].Add(ficha);
            AgregarMensajeInterno($"📌 Ficha {ficha} registrada forzadamente en {pos}.");
        }
        else
        {
            AgregarMensajeInterno($"ℹ️ Ficha {ficha} ya estaba registrada en {pos}.");
        }
    }

    public List<IPieceWithPosition> ObtenerObjetosEn(Vector2Int pos, bool incluirRecolectables = true)
    {
        if (tableroOcupacion.TryGetValue(pos, out var lista))
        {
            return lista
                .Where(obj => obj != null && ((MonoBehaviour)obj) != null)
                .Where(obj => incluirRecolectables || !(obj is IObjetoRecoleccionable))
                .ToList();
        }

        return new List<IPieceWithPosition>();
    }

    public bool EstaCasillaOcupada(Vector2Int pos, IPieceWithPosition ignorar = null)
    {
        var objetos = ObtenerObjetosEn(pos);
        foreach (var obj in objetos)
        {
            if (obj == ignorar) continue;

            bool esFicha = obj is IFicha;
            bool esRecolectable = obj is IObjetoRecoleccionable;

            if (esFicha || esRecolectable)
            {
                AgregarMensajeInterno($"🚫 Casilla {pos} ocupada por {obj}");
                return true;
            }
        }
        return false;
    }

    public Vector3 GetTileWorldPosition(Vector2Int tileCoords)
    {
        if (tileCoords.x < 0 || tileCoords.y < 0 || tileCoords.x > 7 || tileCoords.y > 7)
            return new Vector3(10000, 10000, 0);

        foreach (Tile tile in tiles)
        {
            if (tile.tileCoords == tileCoords)
                return tile.transform.localPosition;
        }

        AgregarMensajeInterno($"No se encontró tile en {tileCoords}");
        return Vector3.zero;
    }

    public Tile GetTileAt(Vector2Int coords)
    {
        foreach (Tile tile in tiles)
        {
            if (tile.tileCoords == coords)
                return tile;
        }
        Debug.LogWarning($"No se encontró Tile en {coords}");
        return null;
    }

    public Vector2 GetTileAnchoredPosition(Vector2Int tileCoords)
    {
        Tile tile = GetTileAt(tileCoords);
        if (tile != null)
            return tile.GetComponent<RectTransform>().anchoredPosition;

        Debug.LogWarning($"No se encontró tile en {tileCoords}");
        return Vector2.zero;
    }

    public Vector2Int GetClosestTileCoords(Vector3 worldPos)
    {
        Tile closest = null;
        float minDist = Mathf.Infinity;

        foreach (Tile tile in tiles)
        {
            float dist = Vector3.Distance(tile.transform.localPosition, worldPos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = tile;
            }
        }

        if (closest != null)
            return closest.tileCoords;

        Debug.LogWarning($"No se encontró tile cercano a {worldPos}");
        return Vector2Int.zero;
    }


    public List<MovableTileObject> GetObjetosMoviblesOrdenadosDesde(Vector2Int origen)
    {
        var todos = FindObjectsByType<MovableTileObject>(FindObjectsSortMode.None);

        var movibles = new List<MovableTileObject>();

        foreach (var obj in todos)
        {
            if (!obj.activoEnTablero)
            {
                AgregarMensajeInterno($"🕳 {obj.name} ignorado (no activo en tablero).");
                continue;
            }

            if (obj.tileCoords.x < 0 || obj.tileCoords.x > 7 || obj.tileCoords.y < 0 || obj.tileCoords.y > 7)
            {
                AgregarMensajeInterno($"🌌 {obj.name} ignorado (fuera del tablero en {obj.tileCoords}).");
                continue;
            }

            movibles.Add(obj);
        }

        movibles.Sort((a, b) =>
            Vector2Int.Distance(b.tileCoords, origen).CompareTo(Vector2Int.Distance(a.tileCoords, origen)));

        return movibles;
    }

    private void InicializarRegistroDeFichas()
    {
        AgregarMensajeInterno("📜 Iniciando registro global de fichas...");

        var componentes = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        // 🔹 Paso 1: Registrar todas las fichas (IFicha)
        foreach (var ficha in componentes.OfType<IFicha>())
        {
            var pieza = ficha as MonoBehaviour;

            if (pieza is Expansion || pieza is Interruption ||
                pieza is PusherUp || pieza is PusherRight || pieza is PusherDown
                || pieza is PusherLeft || pieza is Attraction || pieza is Vortex)
            {
                AgregarMensajeInterno($"⛔ {pieza.name} es un Expansion. No se registrará como ficha.");
                continue;
            }

            if (ficha is IPieceWithPosition piezaConPos)
            {
                var posicion = piezaConPos.GetPosicionActual();
                if (posicion.x >= 0)
                {
                    AgregarMensajeInterno($"📍 Registrando ficha inicial: {pieza.name} en {posicion}");
                    RegistrarMovimiento(piezaConPos, posicion);
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ {pieza.name} no implementa IPieceWithPosition. No registrada.");
            }
        }

        // 🔹 Paso 2: Registrar objetos recoleccionables (especiales y normales)
        foreach (var objeto in componentes.OfType<IObjetoRecoleccionable>())
        {
            if (objeto is MonoBehaviour mono && objeto is IPieceWithPosition objetoConPos)
            {
                // Ya fue registrado como ficha (no repetir)
                if (mono is IFicha) continue;

                var pos = objetoConPos.GetPosicionActual();
                if (pos.x >= 0)
                {
                    AgregarMensajeInterno($"📌 {mono.name} ({mono.GetType().Name}) registrado en {pos}");
                    RegistrarMovimiento(objetoConPos, pos);
                }
            }
        }
    }

    // ✅ Recolección de mensajes internos para el Árbitro Silencioso
    public void AgregarMensajeInterno(string mensaje)
    {
        mensajesInternos.Add(mensaje);
    }

    public void ReportarEstadoActualDelTablero()
    {
        StringBuilder reporte = new StringBuilder();

        reporte.AppendLine("🧠 [Árbitro Silencioso] Estado actual del tablero:");

        // 📝 Mensajes personalizados antes del reporte de casillas
        if (mensajesInternos.Count > 0)
        {
            reporte.AppendLine("📝 Mensajes recientes:");
            foreach (var mensaje in mensajesInternos)
                reporte.AppendLine("   " + mensaje);
            reporte.AppendLine();
        }

        mensajesInternos.Clear(); // Limpiar después de imprimir

        foreach (var par in tableroOcupacion)
        {
            Vector2Int coords = par.Key;
            var lista = par.Value;

            if (lista.Count == 0)
            {
                reporte.AppendLine($"📭 Casilla {coords}: vacía.");
                continue;
            }

            reporte.AppendLine($"📍 Casilla {coords}: contiene {lista.Count} objeto(s).");

            foreach (var obj in lista)
            {
                if (obj == null || ((MonoBehaviour)obj) == null) continue;

                string nombre = ((MonoBehaviour)obj).name;
                string tipo = obj.GetType().Name;

                string interfaces = "";
                if (obj is IFicha) interfaces += "IFicha ";
                if (obj is IFichaAliada) interfaces += "IFichaAliada ";
                if (obj is IFichaEnemiga) interfaces += "IFichaEnemiga ";
                if (obj is ITileEffect) interfaces += "ITileEffect ";
                if (obj is IObjetoRecoleccionable) interfaces += "IObjetoRecoleccionable ";

                Vector2Int posicionReportada = obj.GetPosicionActual();
                bool activo = true;
                string estatus = "";

                if (obj is MovableTileObject mto)
                {
                    activo = mto.activoEnTablero;
                    if (mto.esInamovible)
                        estatus = "🪨 Inamovible";
                    else
                        estatus = "Movible";
                }

                reporte.AppendLine($"   🔹 {nombre} ({tipo}) -> Pos: {posicionReportada}, Interfaces: [{interfaces}], Activo: {activo}, {estatus}");

            }
        }

        reporte.AppendLine("✅ Fin del reporte del Árbitro Silencioso.\n");

        Debug.Log(reporte.ToString());
    }
    public void FinalizarTurno()
    {
        // Aquí puedes agregar otras tareas del fin de turno si las hay
        VerificarEfectosTemporales();
    }

    private void VerificarEfectosTemporales()
    {
        foreach (var obj in FindObjectsByType<Potion1PM>(FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            obj.VerificarAutoChequeoGeneral();
        }
    }
    public void ReportarFichaInamovible(IPieceWithPosition pieza)
    {
        if (pieza == null || ((MonoBehaviour)pieza) == null) return;

        string nombre = ((MonoBehaviour)pieza).name;
        string tipo = pieza.GetType().Name;

        string interfaces = "";
        if (pieza is IFicha) interfaces += "IFicha ";
        if (pieza is IFichaAliada) interfaces += "IFichaAliada ";
        if (pieza is IFichaEnemiga) interfaces += "IFichaEnemiga ";
        if (pieza is ITileEffect) interfaces += "ITileEffect ";
        if (pieza is IObjetoRecoleccionable) interfaces += "IObjetoRecoleccionable ";
        if (pieza is IFichaInmovil) interfaces += "IFichaInmovil ";

        Vector2Int posicion = pieza.GetPosicionActual();
        bool activo = true;
        string estatus = "🪨 Inamovible";

        if (pieza is MovableTileObject mto)
        {
            activo = mto.activoEnTablero;
        }

        string reporte = $"🪨 Reporte manual: {nombre} ({tipo}) -> Pos: {posicion}, Interfaces: [{interfaces}], Activo: {activo}, {estatus}";
        AgregarMensajeInterno(reporte);
    }

    public int GetTurnoActual()
    {
        var gm = FindFirstObjectByType<ChessGameManager>();
        if (gm != null)
            return (int)gm
                .GetType()
                .GetField("turnoActual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(gm);

        return -1; // Si no se encuentra, se devuelve un valor inválido
    }

    public static void EnviarADimensionDivina(GameObject objeto)
    {
        Vector2Int posDivina = ObtenerProximaPosicionDivina();

        if (objeto.TryGetComponent<PiecePositioner>(out var piecePositioner))
        {
            piecePositioner.tileCoords = posDivina;
        }

        if (objeto.TryGetComponent<IPieceWithPosition>(out var pieza))
        {
            pieza.SetPosicionActual(posDivina);
        }

        if (objeto.TryGetComponent<UnityEngine.UI.Image>(out var image))
        {
            image.enabled = false;
        }

        // Si tiene un MovableTileObject, también actualiza el world position
        if (objeto.TryGetComponent<MovableTileObject>(out var movable))
        {
            movable.tileCoords = posDivina;
            objeto.transform.localPosition = BoardManagerGlobal.Instance.GetTileWorldPosition(posDivina);
        }

        Debug.Log($"🌀 {objeto.name} fue exiliado a la Dimensión Divina en {posDivina}");
    }

    public static Vector2Int ObtenerProximaPosicionFutura()
    {
        offsetFuturoIncierto++;
        return new Vector2Int(100 + offsetFuturoIncierto, 100);
    }

    public static Vector2Int ObtenerProximaPosicionDivina()
    {
        offsetDimensionDivina++;
        return new Vector2Int(-1, -offsetDimensionDivina);
    }
    public List<ITileEffect> GetTileEffectsEn(Vector2Int coords)
    {
        return ObtenerObjetosEn(coords)
            .OfType<MonoBehaviour>()
            .Where(obj => obj is ITileEffect)
            .Cast<ITileEffect>()
            .ToList();
    }

    public List<IFichaAliada> ObtenerFichasAliadas()
    {
        return tableroOcupacion
            .SelectMany(pair => pair.Value)
            .OfType<IFichaAliada>()
            .ToList();
    }

    public bool RegistrarIntentoDeAtaque(IFichaEnemiga ficha)
    {
        if ((ficha is QueenEnemyController && reinaRojaAtaco) ||
        (ficha is BishopEnemyController && alfilRojoAtaco) ||
        (ficha is RookEnemyController && torreRojaAtaco) ||
        (ficha is KnightEnemyController && caballoRojoAtaco) ||
        (ficha is PawnEnemyController && peonRojoAtaco))
        {
            AgregarMensajeInterno($"⛔ {((MonoBehaviour)ficha).name} ya atacó este turno.");
            return false;
        }

        AgregarMensajeInterno($"✅ {((MonoBehaviour)ficha).name} autorizado para atacar.");
        return true;


        //  if (idUltimoAtaque == idMovimiento)
        {
            //     AgregarMensajeInterno($"⛔ {((MonoBehaviour)ficha).name} no puede atacar: otra ficha ya lo hizo.");
            //   return false;
        }

        //idUltimoAtaque = idMovimiento;
        //AgregarMensajeInterno($"✅ {((MonoBehaviour)ficha).name} autorizado como único atacante del movimiento ID={idMovimiento}");
        //return true;
    }

    public void NotificarMovimientoAliado(Vector2Int posAliada)
    {
        idMovimientoActual++;

        AgregarMensajeInterno($"♟ Movimiento aliado detectado en {posAliada}. Movimiento ID = {idMovimientoActual}");
        ResetearAtaquesEnemigos();
        fichasEnemigasRegistradas = fichasEnemigasRegistradas
        .Where(f => f != null && ((MonoBehaviour)f) != null)
        .ToList();
        foreach (var ficha in fichasEnemigasRegistradas)
        {
            ficha.ProcesarMovimientoAliado(posAliada, idMovimientoActual);
        }

    }
    public void ResetearAtaquesEnemigos()
    {
        reinaNegraAtaco = false;
        torreNegraAtaco = false;
        alfilNegraAtaco = false;
        caballoNegraAtaco = false;
        reinaRojaAtaco = false;
        torreRojaAtaco = false;
        alfilRojoAtaco = false;
        caballoRojoAtaco = false;
        peonRojoAtaco = false;
        AgregarMensajeInterno("♛ Árbitro: Reset de ataques enemigos.");
    }

    public void ProcesarMovimientoAliadoFinalizado()
    {
        // 🔹 Se ejecuta cuando ya todas las fichas enemigas evaluaron el movimiento
        ResetearAtaquesEnemigos();
    }

    public void ReportarFinInspeccionTorreNegra(bool ataco)
    {
        if (!ataco)
        {
            // Si la TorreNegra no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♜ Árbitro: Torre Negra inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♜ Árbitro: Torre Negra atacó. Ciclo completado.");
        }
    }
    public void ReportarFinInspeccionAlfilNegro(bool ataco)
    {
        if (!ataco)
        {
            // Si la TorreNegra no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♝ Árbitro: Alfil Negro inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♝ Árbitro: Alfil Negro atacó. Ciclo completado.");
        }
    }

    public void ReportarFinInspeccionCaballoNegro(bool ataco)
    {
        if (!ataco)
        {
            // Si la TorreNegra no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♞ Árbitro: Caballo Negro inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♞ Árbitro: Caballo Negro atacó. Ciclo completado.");
        }
    }

    public void ReportarFinInspeccionReinaRoja(bool ataco)
    {
        if (!ataco)
        {
            // Si la ReinaRoja no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♛ Árbitro: Reina Roja inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♛ Árbitro: Reina Roja atacó. Ciclo completado.");
        }
    }

    public void ReportarFinInspeccionTorreRoja(bool ataco)
    {
        if (!ataco)
        {
            // Si la ReinaRoja no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♜  Árbitro: Torre Roja inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♜  Árbitro: Torre Roja atacó. Ciclo completado.");
        }
    }

    public void ReportarFinInspeccionAlfilRojo(bool ataco)
    {
        if (!ataco)
        {
            // Si la ReinaRoja no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♝ Árbitro: Alfil Rojo inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♝ Árbitro: Alfil Rojo atacó. Ciclo completado.");
        }
    }

    public void ReportarFinInspeccionCaballoRojo(bool ataco)
    {
        if (!ataco)
        {
            // Si la ReinaRoja no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♞ Árbitro: Caballo Rojo inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♞ Árbitro: Caballo Rojo atacó. Ciclo completado.");
        }
    }
    public void ReportarFinInspeccionPeonRojo(bool ataco)
    {
        if (!ataco)
        {
            // Si la ReinaRoja no atacó, es señal de que el ciclo se completó
            ResetearAtaquesEnemigos();
            AgregarMensajeInterno("♙ Árbitro: Peón Rojo inspeccionó y cedió su turno. Ataques reiniciados.");
        }
        else
        {
            // Si atacó, la prioridad natural sigue; no reiniciamos porque ya actuó
            AgregarMensajeInterno("♙ Árbitro: Peón Rojo atacó. Ciclo completado.");
        }
    }

    public bool EsCasillaAccesiblePorAliado(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x > 7 || pos.y > 7)
            return false;

        var objetos = ObtenerObjetosEn(pos);

        foreach (var obj in objetos)
        {
            if (obj is IFichaAliada || obj is IFichaInmovil)
                return false;
        }

        return true;
    }


    public bool HayObstaculoEntreAliado(Vector2Int origen, Vector2Int destino, object origenFicha = null)
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

            var objetos = ObtenerObjetosEn(paso);
            bool hayObstaculo = objetos.Any(obj =>
                obj != origenFicha && (obj is IFicha || obj is IFichaInmovil || obj is IObjetoRecoleccionable));

            if (hayObstaculo)
            {
                AgregarMensajeInterno($"🔰 Obstáculo detectado en {paso}. Línea bloqueada.");
                return true;
            }

            paso += direccion;
        }

        return false;
    }

    public Vector2Int CalcularCasillaSeguraParaVortex(KingController rey)
    {
        List<Vector2Int> posibles = new List<Vector2Int>();
        for (int x = 0; x <= 7; x++)
        {
            for (int y = 0; y <= 7; y++)
                posibles.Add(new Vector2Int(x, y));
        }

        posibles = posibles
            .OrderBy(p => Vector2Int.Distance(Vector2Int.zero, p))
            .ThenByDescending(p => Vector2Int.Distance(new Vector2Int(7, 7), p))
            .ToList();

        foreach (var pos in posibles)
        {
            if (!EstaCasillaOcupada(pos, rey))
                return pos;
        }

        return rey.GetPosicionActual(); // si no hay nada libre
    }

    public void EfectoPusherUp(IPieceWithPosition pieza)
    {
        if (pieza == null)
        {
            AgregarMensajeInterno("⚠️ EfectoPusherUp: pieza no válida.");
            return;
        }

        Vector2Int posActual = pieza.GetPosicionActual();
        Vector2Int direccion = Vector2Int.up; // mover hacia arriba
        int pasosMax = 3;
        Vector2Int destinoFinal = posActual;

        for (int i = 1; i <= pasosMax; i++)
        {
            Vector2Int siguiente = posActual + direccion * i;

            // Bordes del tablero
            if (siguiente.x < 0 || siguiente.y < 0 || siguiente.x > 7 || siguiente.y > 7)
            {
                AgregarMensajeInterno($"🛑 PusherUp: borde del tablero alcanzado en {siguiente}.");
                break;
            }

            var objetos = ObtenerObjetosEn(siguiente);

            // Bloqueo por aliados o enemigos
            if (objetos.Any(o => o is IFichaAliada || o is IFichaEnemiga))
            {
                AgregarMensajeInterno($"🛑 PusherUp: bloqueado por ficha en {siguiente}.");
                break;
            }

            // Bloqueo por recolectable
            if (objetos.Any(o => o is IObjetoRecoleccionable))
            {
                AgregarMensajeInterno($"🛑 PusherUp: objeto recolectable en {siguiente}. Se detiene.");
                break;
            }

            if (objetos.Any(o => o is Wall))
            {
                AgregarMensajeInterno($"🛑 PusherDown: muro encontrado en {siguiente}. Se detiene.");
                break;
            }

            destinoFinal = siguiente;
        }

        // Si hay un nuevo destino, mover la pieza
        if (destinoFinal != posActual)
        {
            AgregarMensajeInterno($"✅ PusherUp: moviendo a {pieza} de {posActual} a {destinoFinal}.");

            if (pieza is KingController rey)
            {
                rey.TeletransportarA(destinoFinal);
            }
            else if (pieza is PawnController peon)
            {
                peon.TeletransportarA(destinoFinal);
            }

            NotificarMovimientoAliado(destinoFinal);
        }
        else
        {
            AgregarMensajeInterno("ℹ️ PusherUp: no se movió la pieza.");
        }
    }

    public void EfectoPusherLeft(IPieceWithPosition pieza)
    {
        if (pieza == null)
        {
            AgregarMensajeInterno("⚠️ EfectoPusherLeft: pieza no válida.");
            return;
        }

        Vector2Int posActual = pieza.GetPosicionActual();
        Vector2Int direccion = Vector2Int.left; // mover hacia la izquierda
        int pasosMax = 3;
        Vector2Int destinoFinal = posActual;

        for (int i = 1; i <= pasosMax; i++)
        {
            Vector2Int siguiente = posActual + direccion * i;

            // Bordes del tablero
            if (siguiente.x < 0 || siguiente.y < 0 || siguiente.x > 7 || siguiente.y > 7)
            {
                AgregarMensajeInterno($"🛑 PusherLeft: borde del tablero alcanzado en {siguiente}.");
                break;
            }

            var objetos = ObtenerObjetosEn(siguiente);

            // Bloqueo por aliados o enemigos
            if (objetos.Any(o => o is IFichaAliada || o is IFichaEnemiga))
            {
                AgregarMensajeInterno($"🛑 PusherLeft: bloqueado por ficha en {siguiente}.");
                break;
            }

            // Bloqueo por recolectable
            if (objetos.Any(o => o is IObjetoRecoleccionable))
            {
                AgregarMensajeInterno($"🛑 PusherLeft: objeto recolectable en {siguiente}. Se detiene.");
                break;
            }

            if (objetos.Any(o => o is Wall))
            {
                AgregarMensajeInterno($"🛑 PusherDown: muro encontrado en {siguiente}. Se detiene.");
                break;
            }

            destinoFinal = siguiente;
        }

        // Si hay un nuevo destino, mover la pieza
        if (destinoFinal != posActual)
        {
            AgregarMensajeInterno($"✅ PusherLeft: moviendo a {pieza} de {posActual} a {destinoFinal}.");

            if (pieza is KingController rey)
            {
                rey.TeletransportarA(destinoFinal);
            }
            else if (pieza is PawnController peon)
            {
                peon.TeletransportarA(destinoFinal);
            }

            NotificarMovimientoAliado(destinoFinal);
        }
        else
        {
            AgregarMensajeInterno("ℹ️ PusherLeft: no se movió la pieza.");
        }
    }

    public void EfectoPusherRight(IPieceWithPosition pieza)
    {
        if (pieza == null)
        {
            AgregarMensajeInterno("⚠️ EfectoPusherRight: pieza no válida.");
            return;
        }

        Vector2Int posActual = pieza.GetPosicionActual();
        Vector2Int direccion = Vector2Int.right; // mover hacia la derecha
        int pasosMax = 3;
        Vector2Int destinoFinal = posActual;

        for (int i = 1; i <= pasosMax; i++)
        {
            Vector2Int siguiente = posActual + direccion * i;

            // Bordes del tablero
            if (siguiente.x < 0 || siguiente.y < 0 || siguiente.x > 7 || siguiente.y > 7)
            {
                AgregarMensajeInterno($"🛑 PusherRight: borde del tablero alcanzado en {siguiente}.");
                break;
            }

            var objetos = ObtenerObjetosEn(siguiente);

            // Bloqueo por aliados o enemigos
            if (objetos.Any(o => o is IFichaAliada || o is IFichaEnemiga))
            {
                AgregarMensajeInterno($"🛑 PusherRight: bloqueado por ficha en {siguiente}.");
                break;
            }

            // Bloqueo por recolectable
            if (objetos.Any(o => o is IObjetoRecoleccionable))
            {
                AgregarMensajeInterno($"🛑 PusherRight: objeto recolectable en {siguiente}. Se detiene.");
                break;
            }

            if (objetos.Any(o => o is Wall))
            {
                AgregarMensajeInterno($"🛑 PusherDown: muro encontrado en {siguiente}. Se detiene.");
                break;
            }

            destinoFinal = siguiente;
        }

        // Si hay un nuevo destino, mover la pieza
        if (destinoFinal != posActual)
        {
            AgregarMensajeInterno($"✅ PusherRight: moviendo a {pieza} de {posActual} a {destinoFinal}.");

            if (pieza is KingController rey)
            {
                rey.TeletransportarA(destinoFinal);
            }
            else if (pieza is PawnController peon)
            {
                peon.TeletransportarA(destinoFinal);
            }

            NotificarMovimientoAliado(destinoFinal);
        }
        else
        {
            AgregarMensajeInterno("ℹ️ PusherRight: no se movió la pieza.");
        }
    }

    public void EfectoPusherDown(IPieceWithPosition pieza)
    {
        if (pieza == null)
        {
            AgregarMensajeInterno("⚠️ EfectoPusherDown: pieza no válida.");
            return;
        }

        Vector2Int posActual = pieza.GetPosicionActual();
        Vector2Int direccion = Vector2Int.down; // mover hacia abajo
        int pasosMax = 3;
        Vector2Int destinoFinal = posActual;

        for (int i = 1; i <= pasosMax; i++)
        {
            Vector2Int siguiente = posActual + direccion * i;

            // Bordes del tablero
            if (siguiente.x < 0 || siguiente.y < 0 || siguiente.x > 7 || siguiente.y > 7)
            {
                AgregarMensajeInterno($"🛑 PusherDown: borde del tablero alcanzado en {siguiente}.");
                break;
            }

            var objetos = ObtenerObjetosEn(siguiente);

            // Bloqueo por aliados o enemigos
            if (objetos.Any(o => o is IFichaAliada || o is IFichaEnemiga))
            {
                AgregarMensajeInterno($"🛑 PusherDown: bloqueado por ficha en {siguiente}.");
                break;
            }

            // Bloqueo por recolectable
            if (objetos.Any(o => o is IObjetoRecoleccionable))
            {
                AgregarMensajeInterno($"🛑 PusherDown: objeto recolectable en {siguiente}. Se detiene.");
                break;
            }

            if (objetos.Any(o => o is Wall))
            {
                AgregarMensajeInterno($"🛑 PusherDown: muro encontrado en {siguiente}. Se detiene.");
                break;
            }

            destinoFinal = siguiente;
        }

        // Si hay un nuevo destino, mover la pieza
        if (destinoFinal != posActual)
        {
            AgregarMensajeInterno($"✅ PusherDown: moviendo a {pieza} de {posActual} a {destinoFinal}.");

            if (pieza is KingController rey)
            {
                rey.TeletransportarA(destinoFinal);
            }
            else if (pieza is PawnController peon)
            {
                peon.TeletransportarA(destinoFinal);
            }

            NotificarMovimientoAliado(destinoFinal);
        }
        else
        {
            AgregarMensajeInterno("ℹ️ PusherDown: no se movió la pieza.");
        }
    }

    public void RegistrarFichaConEscudo(IFichaAliada ficha)
    {
        if (!fichasConEscudo.Contains(ficha))
            fichasConEscudo.Add(ficha);
    }

    // Llamar este método al INICIO del turno del jugador
    public void QuitarEscudos()
    {
        if (fichasConEscudo.Count == 0) return;

        AgregarMensajeInterno("🛡 Fin del efecto de ESCUDO. Restaurando estado normal.");

        // Restaurar a cada ficha su color normal
        foreach (var ficha in fichasConEscudo)
        {
            if ((ficha as Object) == null) continue;

            var metodoShield = ficha.GetType().GetMethod("Shield", new[] { typeof(bool) });
            metodoShield?.Invoke(ficha, new object[] { false });
        }

        // Aquí podrías restaurar los rangos originales de los enemigos
        var enemigos = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IFichaEnemiga>();

        foreach (var enemigo in enemigos)
        {
            try
            {
                enemigo.ReiniciarTurno(); // Método que deberías tener en tus enemigos
            }
            catch { }
        }

        fichasConEscudo.Clear();
    }


    public void NotifyAllyCoronated(AllyKind kind)
{
    if (!coronatedThisLevel.Add(kind))
    {
        AgregarMensajeInterno($"ℹ️ Coronación repetida de {kind} ignorada para Liberador.");
        return;
    }

    AgregarMensajeInterno($"🏁 Coronó aliado: {kind}. Progreso Liberador: {coronatedThisLevel.Count}/5");

    string slotId = PlayerPrefs.GetString("slotActivo", "slot1");

    if (coronatedThisLevel.Count == 5 && !AchievementsManager.IsUnlocked(slotId, AchievementId.Liberador))
    {
        AchievementsManager.TryUnlock(
            slotId,
            AchievementId.Liberador,
            5000,
            "Logro desbloqueado: Liberador",
            "+5000 puntos por coronar a todas las fichas en un mismo nivel."
        );
    }
}

}