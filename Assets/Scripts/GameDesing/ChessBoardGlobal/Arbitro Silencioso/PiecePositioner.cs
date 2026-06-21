using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PiecePositioner : MonoBehaviour
{
    [Header("Ubicación deseada en el tablero")]
    public Vector2Int tileCoords;

    [ContextMenu("Posicionar pieza en tablero")]
    public void PosicionarEnTablero()
    {
#if UNITY_EDITOR
        BoardManagerGlobal manager = BoardManagerGlobal.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<BoardManagerGlobal>();
        }

        if (manager != null)
        {
            transform.localPosition = manager.GetTileWorldPosition(tileCoords);
            Debug.Log($"{name} posicionado automáticamente en {tileCoords}");
            EditorUtility.SetDirty(gameObject);
        }
        else
        {
            Debug.LogWarning("No se encontró BoardManagerGlobal en escena. Asegúrate que el tablero está activo.");
        }
#endif
    }

    
}