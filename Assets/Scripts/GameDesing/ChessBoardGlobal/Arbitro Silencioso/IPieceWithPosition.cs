using UnityEngine;

public interface IPieceWithPosition
{
    void SetPosicionActual(Vector2Int nuevaPos);
    Vector2Int GetPosicionActual();
}