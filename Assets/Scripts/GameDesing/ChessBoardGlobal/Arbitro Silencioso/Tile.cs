using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    [Header("Coordenadas en el tablero (x,y)")]
    public Vector2Int tileCoords;

    private Image myImage;
    private bool isShieldActive = false;

    private void Awake()
    {
        myImage = GetComponent<Image>();
    }

    public void HighlightMove(bool isActive)
    {
        if (myImage == null) return;

        myImage.color = isActive
            ? (isShieldActive 
                ? new Color(1f, 0.827f, 0f, 1f) // dorado si hay escudo
                : new Color(0.5f, 1f, 0.5f, 1f)) // verde normal
            : Color.white;
    }

    public void HighlightMoveForKing(bool isActive)
    {
        if (myImage == null) return;

        myImage.color = isActive
            ? (isShieldActive
                ? new Color(1f, 0.827f, 0f, 1f) // dorado si hay escudo
                : new Color(0.75f, 0.6f, 0.6f, 0.6f)) 
            : Color.white;
    }


    public void HighlightEnemyKillZone(bool isActive)
    {
        if (myImage == null) return;
        myImage.color = isActive ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
    }

    public void HighlightEnemyRangeZone(bool isActive)
    {
        if (myImage == null) return;
        myImage.color = isActive ? new Color(1f, 0.5f, 0.7f, 1f) : Color.white;
    }

    public void HighlightEnemyAttack(bool state)
    {
        if (myImage == null) return;
        myImage.color = state ? new Color(1f, 0.2f, 0.9f, 1f) : Color.clear;
    }

    public void HighlightBlackAttack(bool state)
    {
        if (myImage == null) return;
        myImage.color = state ? new Color(0.322f, 0.318f, 0.314f, 0.8f) : Color.clear;
    }

    public void Shield(bool isActive)
    {
        isShieldActive = isActive; // ✅ ahora solo cambia el flag
    }

    public void HighlightSpecific(bool isActive)
    {
        if (myImage != null)
            myImage.color = isActive ? new Color(1f, 0.2f, 0.9f, 1f) : Color.white;
    }

    public bool EsCasillaDeAtaque()
    {
        if (myImage == null) return false;
        return myImage.color.Equals(new Color(1f, 0.2f, 0.9f, 1f));
    }

    public void ResetColor()
    {
        if (myImage != null)
            myImage.color = Color.white;

        isShieldActive = false; // ✅ evita que quede activo después
    }
}
