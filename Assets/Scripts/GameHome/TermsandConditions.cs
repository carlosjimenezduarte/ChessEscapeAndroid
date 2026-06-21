using UnityEngine;
using System.Collections.Generic;

public class TermsAndConditionsOpener : MonoBehaviour
{
    private Dictionary<SystemLanguage, string> enlacesPorIdioma = new Dictionary<SystemLanguage, string>()
    {
        { SystemLanguage.Spanish,  "https://chessescape.com/WebOfficial/es/terms-es.html" },
        { SystemLanguage.English,  "https://chessescape.com/WebOfficial/en/terms-en.html" },
        { SystemLanguage.Japanese, "https://chessescape.com/WebOfficial/ja/terms-ja.html" },
        { SystemLanguage.Korean,   "https://chessescape.com/WebOfficial/ko/terms-ko.html" },
        { SystemLanguage.Greek,    "https://chessescape.com/WebOfficial/el/terms-el.html" },
        { SystemLanguage.Russian,  "https://chessescape.com/WebOfficial/ru/terms-ru.html" },
        { SystemLanguage.French,   "https://chessescape.com/WebOfficial/fr/terms-fr.html" },
        { SystemLanguage.German,   "https://chessescape.com/WebOfficial/de/terms-de.html" },
        { SystemLanguage.Italian,  "https://chessescape.com/WebOfficial/it/terms-it.html" },
        { SystemLanguage.Portuguese,"https://chessescape.com/WebOfficial/pt/terms-pt.html" }
    };

    [SerializeField] private string urlDefault = "https://chessescape.com/WebOfficial/en/terms-en.html"; // fallback

    public void AbrirWeb()
    {
        SystemLanguage idioma = Application.systemLanguage;
        Debug.Log("🌍 Idioma detectado: " + idioma);

        string url;
        if (!enlacesPorIdioma.TryGetValue(idioma, out url))
        {
            Debug.Log("⚠️ Idioma no soportado, usando inglés por defecto.");
            url = urlDefault;
        }

        Debug.Log("🔗 Abriendo URL: " + url);
        Application.OpenURL(url);
    }
}
