using UnityEngine;
using UnityEngine.UI;

public class LevelTile : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image backgroundImage;
    public GameObject keyIcon;

    public enum TileState { Locked, Unlocked, Completed }
    public TileState state = TileState.Locked;

    // ✅ Inicializa visual en Awake (antes que cualquier Start del manager)
    private void Awake()
    {
        UpdateVisual(false);
    }

    // ⛔ No vuelvas a tocar el visual en Start (evita pisar al manager)
    private void Start()
    {
        // Intencionalmente vacío
    }

    public void SetState(TileState newState, bool mostrarIcono = false)
    {
        Debug.Log($"[LevelTile] 🔄 SetState -> newState={newState}, mostrarIcono={mostrarIcono}");
        state = newState;
        UpdateVisual(mostrarIcono);
    }

    private void UpdateVisual(bool mostrarIcono)
    {
        switch (state)
        {
            case TileState.Locked:
                backgroundImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                keyIcon.SetActive(false);
                Debug.Log("[LevelTile] 🔒 Locked -> keyIcon=OFF");
                break;

            case TileState.Unlocked:
                backgroundImage.color = Color.white;
                keyIcon.SetActive(false);
                Debug.Log("[LevelTile] 🔓 Unlocked -> keyIcon=OFF");
                break;

            case TileState.Completed:
                backgroundImage.color = Color.white;
                keyIcon.SetActive(mostrarIcono);
                if (mostrarIcono)
                {
                    // Pequeños seguros visuales (no cambian diseño)
                    if (keyIcon.TryGetComponent(out Image img)) { img.enabled = true; var c = img.color; c.a = 1f; img.color = c; }
                    keyIcon.transform.SetAsLastSibling(); // traelo al frente por si acaso
                }
                Debug.Log($"[LevelTile] ✅ Completed -> keyIcon={(mostrarIcono ? "ON" : "OFF")}");
                break;
        }
    }
}
