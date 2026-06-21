using UnityEngine;

public class AlignDangerOverlaysToChessBoard : MonoBehaviour
{
    public RectTransform chessBoardRect;

    void Start()
    {
    RectTransform myRect = GetComponent<RectTransform>();
    if (chessBoardRect == null)
    {
        Debug.LogWarning("No se ha asignado el ChessBoard RectTransform.");
        return;
    }

    myRect.anchorMin = chessBoardRect.anchorMin;
    myRect.anchorMax = chessBoardRect.anchorMax;
    myRect.pivot = chessBoardRect.pivot;
    myRect.sizeDelta = chessBoardRect.sizeDelta;
    myRect.anchoredPosition = chessBoardRect.anchoredPosition;

    BoardManagerGlobal.Instance.AgregarMensajeInterno($"✅ DangerOverlays alineado: Pos {myRect.anchoredPosition}, Size {myRect.sizeDelta}, Pivot {myRect.pivot}");
    }
}