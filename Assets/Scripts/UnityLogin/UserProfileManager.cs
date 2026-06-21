using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserProfileManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text usernameText;   // Texto donde aparece el nombre
    public Image avatarImage;       // Imagen donde se pinta el avatar

    [Header("Avatars disponibles")]
    public Sprite[] avatarSprites;  // Lista de avatares disponibles
    // ⚠️ Debe coincidir el orden o tener un mapeo con los avatarId que guardamos

    private void Start()
    {
        LoadUserProfile();
    }

    public void LoadUserProfile()
    {
        // Cargar nombre
        string username = PlayerPrefs.GetString("username", "Guest");
        if (usernameText != null)
            usernameText.text = username;

        // Cargar avatar
        string avatarId = PlayerPrefs.GetString("avatarId", "");
        if (!string.IsNullOrEmpty(avatarId))
        {
            // Intentar buscar sprite según el avatarId
            Sprite avatarSprite = GetAvatarById(avatarId);
            if (avatarSprite != null && avatarImage != null)
                avatarImage.sprite = avatarSprite;
        }
    }

    private Sprite GetAvatarById(string avatarId)
    {
        // 🚩 Ejemplo simple: avatarId = "avatar0", "avatar1", ...
        // Si prefieres, puedes mapear con un diccionario en vez de índices

        for (int i = 0; i < avatarSprites.Length; i++)
        {
            if (avatarId == "avatar" + i)
            {
                return avatarSprites[i];
            }
        }
        return null;
    }
}
