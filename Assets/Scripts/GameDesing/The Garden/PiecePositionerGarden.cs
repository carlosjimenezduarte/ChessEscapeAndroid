using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PiecePositionerGarden : MonoBehaviour
{
    [Header("Ubicación deseada en el tablero 9x9 (Jardín)")]
    public Vector2Int tileCoords;

    [ContextMenu("Posicionar pieza en tablero (Garden7)")]
    public void PosicionarEnTablero()
{
#if UNITY_EDITOR
    Garden7 manager = Garden7.Instance;

    if (manager == null)
    {
        manager = FindFirstObjectByType<Garden7>();
    }

    if (manager != null)
    {
        // 1. Mover visualmente
        transform.localPosition = manager.GetTileWorldPosition(tileCoords);

        // 2. Sincronizar con MovableTileObject (si existe)
        var movible = GetComponent<MovableTileObject>();
        if (movible != null)
            movible.tileCoords = tileCoords;

        // 3. Sincronizar con KingFree (si existe)
        var king = GetComponent<KingFree>();
        if (king != null)
            king.SetPosicionActual(tileCoords);

        Debug.Log($"🌳 {name} posicionado automáticamente en {tileCoords} (Garden7)");
        EditorUtility.SetDirty(gameObject);
    }
    else
    {
        Debug.LogWarning("⚠️ No se encontró Garden7 en escena. Asegúrate que el tablero del Jardín está activo.");
    }
#endif
}
}
