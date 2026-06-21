using UnityEngine;
using UnityEngine.UI;

public class LevelTileCodex : MonoBehaviour
{
    public Image backgroundImage;
    public GameObject keyIcon;

    public enum TileState
    {
        Locked,
        Unlocked,
        Completed
    }

    public TileState state = TileState.Locked;

    private void Start()
    {
        UpdateVisual();
    }

    public void SetState(TileState newState)
    {
        state = newState;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        switch (state)
        {
            case TileState.Locked:
                backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                keyIcon.SetActive(false);
                break;

            case TileState.Unlocked:
                backgroundImage.color = Color.white;
                keyIcon.SetActive(false);
                break;

            case TileState.Completed:
                backgroundImage.color = Color.white;
                keyIcon.SetActive(true);
                break;
        }
    }

    // Método de test: lo puedes llamar desde un botón o evento
    public void ToggleState()
    {
        if (state == TileState.Locked)
            SetState(TileState.Unlocked);
        else if (state == TileState.Unlocked)
            SetState(TileState.Completed);
        else
            SetState(TileState.Locked);
    }
}