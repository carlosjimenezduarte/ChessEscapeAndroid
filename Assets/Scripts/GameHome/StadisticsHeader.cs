// StadisticsHeader.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StadisticsHeader : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text usernameText;
    public Image avatarImage;

    [Header("Avatar por defecto para invitados")]
    public Sprite guestDefaultAvatar;

    [Header("Avatares")]
    public AvatarCatalog avatarCatalog;

    [Header("Idioma actual (PlayerPrefs.key = lang)")]
    [Tooltip("Código de idioma guardado en PlayerPrefs 'lang' (ej: es, en...). Si está vacío, se detecta de PlayerPrefs o SystemLanguage.")]
    public string forcedLangOverride = "";

    [Header("Guest en 10 idiomas (orden libre)")]
    public string guest_es = "Invitado";
    public string guest_en = "Guest";
    public string guest_pt = "Convidado";
    public string guest_fr = "Invité";
    public string guest_it = "Ospite";
    public string guest_de = "Gast";
    public string guest_ru = "Гость";
    public string guest_ja = "ゲスト";
    public string guest_ko = "게스트";
    public string guest_zh = "访客";

    // Si prefieres 10 TMP_Text distintos (uno por idioma), puedes declararlos aquí.
    // Pero normalmente basta con poner el texto localizado en usernameText, así no duplicas objetos.

    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        // --- Detección de idioma ---
        string lang = !string.IsNullOrEmpty(forcedLangOverride)
            ? forcedLangOverride.ToLowerInvariant()
            : PlayerPrefs.GetString("lang", GuessLang());

        // --- Detección de usuario ---
        string userType = PlayerPrefs.GetString("userType", "guest");
        string username = PlayerPrefs.GetString("username", "");

        bool isSignedInLike = (userType == "firebase" || userType == "unity") && !string.IsNullOrEmpty(username);

        // --- Texto ---
        string textToShow = isSignedInLike ? username : LocalizedGuest(lang);
        if (usernameText != null) usernameText.text = textToShow;

        // --- Avatar ---
        string avatarId = PlayerPrefs.GetString("avatarId", "");
        if (!isSignedInLike)
        {
            // Invitado → usar imagen fija
            if (avatarImage != null && guestDefaultAvatar != null)
                avatarImage.sprite = guestDefaultAvatar;
        }
        else
        {
            if (avatarImage != null && avatarCatalog != null)
                avatarImage.sprite = avatarCatalog.Get(avatarId);
        }

    }

    string LocalizedGuest(string lang)
    {
        switch (lang)
        {
            case "es": return guest_es;
            case "en": return guest_en;
            case "pt": return guest_pt;
            case "fr": return guest_fr;
            case "it": return guest_it;
            case "de": return guest_de;
            case "ru": return guest_ru;
            case "ja": return guest_ja;
            case "ko": return guest_ko;
            case "zh":
            case "zh-cn":
            case "zh-tw": return guest_zh;
            default: return guest_en; // fallback
        }
    }

    string GuessLang()
    {
        // Fallback si no tienes aún tu selector de idioma persistiendo en PlayerPrefs "lang"
        var sys = Application.systemLanguage;
        return sys switch
        {
            SystemLanguage.Spanish => "es",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.French => "fr",
            SystemLanguage.Italian => "it",
            SystemLanguage.German => "de",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            SystemLanguage.ChineseSimplified => "zh",
            SystemLanguage.ChineseTraditional => "zh",
            _ => "en"
        };
    }
}
