using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Garden7 : MonoBehaviour
{
    public static Garden7 Instance;
    /*public bool reinaNegraAtaco = false;
    public bool torreNegraAtaco = false;
    public bool alfilNegraAtaco = false;
    public bool caballoNegraAtaco = false;
    public bool reinaRojaAtaco = false;
    public bool torreRojaAtaco = false;
    public bool alfilRojoAtaco = false;
    public bool caballoRojoAtaco = false;
    public bool peonRojoAtaco = false;*/

   /* private List<IFichaAliada> fichasConEscudo = new List<IFichaAliada>();
    private List<IFichaEnemiga> fichasEnemigasRegistradas = new List<IFichaEnemiga>();*/

    public int idMovimientoActual = 0;
    //private static int offsetFuturoIncierto = 0;
    //private static int offsetDimensionDivina = 0;

    public static Vector2Int FuturoIncierto = new Vector2Int(100, 100);
    public static Vector2Int DimensionDivina = new Vector2Int(-1, -9999);

    [Header("Lista de todas las casillas del tablero 9x9")]
    public List<Tile> tiles = new List<Tile>();

    private Dictionary<Vector2Int, List<IPieceWithPosition>> tableroOcupacion
        = new Dictionary<Vector2Int, List<IPieceWithPosition>>();

    private List<string> mensajesInternos = new List<string>();

    private void Awake()
    {
        Instance = this;

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

    // ======================
    // REGISTROS Y MOVIMIENTOS
    // ======================

    /*public void RegistrarFichaEnemiga(IFichaEnemiga ficha)
    {
        if (!fichasEnemigasRegistradas.Contains(ficha))
        {
            fichasEnemigasRegistradas.Add(ficha);
            AgregarMensajeInterno($"🟥 Ficha enemiga registrada: {((MonoBehaviour)ficha).name}");
        }
    }*/

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
                AgregarMensajeInterno($"❌ Se eliminaron {eliminados} fichas de la casilla {pos}.");
            else
                AgregarMensajeInterno($"ℹ️ No había fichas para eliminar en la casilla {pos}.");
        }
        else
        {
            AgregarMensajeInterno($"⚠️ Casilla {pos} no está registrada en el tablero.");
        }
    }

    public void RegistrarFichaEnCasilla(Vector2Int pos, IPieceWithPosition ficha)
    {
        if (!tableroOcupacion.ContainsKey(pos))
            tableroOcupacion[pos] = new List<IPieceWithPosition>();

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

    // ======================
    // GETTERS DE TILE
    // ======================

    public Vector3 GetTileWorldPosition(Vector2Int tileCoords)
    {
        if (tileCoords.x < 0 || tileCoords.y < 0 || tileCoords.x > 8 || tileCoords.y > 8)
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

    // ======================
    // REGISTRO DE FICHAS (igual al original)
    // ======================
   private void InicializarRegistroDeFichas()
{
    AgregarMensajeInterno("📜 Iniciando registro global de fichas en el Jardín...");

    var componentes = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

    // 🔹 Registrar fichas normales
    foreach (var ficha in componentes.OfType<IFicha>())
    {
        var pieza = ficha as MonoBehaviour;

        if (pieza is Expansion || pieza is Interruption ||
            pieza is PusherUp || pieza is PusherRight ||
            pieza is PusherDown || pieza is PusherLeft ||
            pieza is Attraction || pieza is Vortex)
        {
            AgregarMensajeInterno($"⛔ {pieza.name} es especial y no se registra.");
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
    }

    // 🔹 Registrar objetos recolectables (como la llave)
    foreach (var recolectable in componentes.OfType<IObjetoRecoleccionable>())
    {
        if (recolectable is IPieceWithPosition objetoConPos)
        {
            var posicion = objetoConPos.GetPosicionActual();
            if (posicion.x >= 0)
            {
                AgregarMensajeInterno($"📦 Registrando objeto inicial: {(recolectable as MonoBehaviour).name} en {posicion}");
                RegistrarMovimiento(objetoConPos, posicion);
            }
        }
    }
}


    // ======================
    // REPORTES
    // ======================
    public void AgregarMensajeInterno(string mensaje)
    {
        mensajesInternos.Add(mensaje);
    }

    public void ReportarEstadoActualDelTablero()
    {
        StringBuilder reporte = new StringBuilder();
        reporte.AppendLine("🌳 [Árbitro del Jardín] Estado actual del tablero 9x9:");

        if (mensajesInternos.Count > 0)
        {
            reporte.AppendLine("📝 Mensajes recientes:");
            foreach (var mensaje in mensajesInternos)
                reporte.AppendLine("   " + mensaje);
            reporte.AppendLine();
        }

        mensajesInternos.Clear();

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

                Vector2Int posicionReportada = obj.GetPosicionActual();
                reporte.AppendLine($"   🔹 {nombre} ({tipo}) -> Pos: {posicionReportada}");
            }
        }

        reporte.AppendLine("✅ Fin del reporte del Árbitro del Jardín.\n");
        Debug.Log(reporte.ToString());
    }

    public bool EsCasillaAccesiblePorAliado(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x > 8 || pos.y > 8)
            return false;

        var objetos = ObtenerObjetosEn(pos);

        foreach (var obj in objetos)
        {
            if (obj is IFichaAliada || obj is IFichaInmovil)
                return false;
        }

        return true;
    }


}
