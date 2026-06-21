using UnityEngine;
using System.Collections.Generic;

public class RankingMundial : MonoBehaviour
{
    [Header("Opcional: si guardas el idioma del juego en PlayerPrefs")]
    [Tooltip("Clave en PlayerPrefs que guarda el código de idioma (es,en,fr,de,it,pt,ja,ko,el,ru). Si queda vacío, se usará Application.systemLanguage.")]
    [SerializeField] private string playerPrefsLangKey = "langCode";

    [Header("URLs por idioma")]
    [SerializeField] private string baseUrl = "https://chessescape.com/ranking/";
    [SerializeField] private string urlDefault = "https://chessescape.com/ranking/ranking-en.html";

    // Mapa directo SystemLanguage -> archivo
    private static readonly Dictionary<SystemLanguage, string> POR_IDIOMA = new()
    {
        { SystemLanguage.Spanish,    "ranking-es.html" },
        { SystemLanguage.English,    "ranking-en.html" },
        { SystemLanguage.Japanese,   "ranking-ja.html" },
        { SystemLanguage.Korean,     "ranking-ko.html" },
        { SystemLanguage.Greek,      "ranking-el.html" },
        { SystemLanguage.Russian,    "ranking-ru.html" },
        { SystemLanguage.German,     "ranking-de.html" },
        { SystemLanguage.French,     "ranking-fr.html" },   // <-- ojo: 'fr.html' (corregido)
        { SystemLanguage.Italian,    "ranking-it.html" },
        { SystemLanguage.Portuguese, "ranking-pt.html" },
    };

    // Mapa de códigos cortos -> SystemLanguage (para override por PlayerPrefs)
    private static readonly Dictionary<string, SystemLanguage> CODE2LANG = new()
    {
        { "es", SystemLanguage.Spanish },
        { "en", SystemLanguage.English },
        { "ja", SystemLanguage.Japanese },
        { "ko", SystemLanguage.Korean },
        { "el", SystemLanguage.Greek },
        { "ru", SystemLanguage.Russian },
        { "de", SystemLanguage.German },
        { "fr", SystemLanguage.French },
        { "it", SystemLanguage.Italian },
        { "pt", SystemLanguage.Portuguese },
    };

    public void AbrirWeb()
    {
        // 1) Intentar override por PlayerPrefs (si existe y es válido)
        SystemLanguage idiomaDet = DetectarIdioma();

        // 2) Resolver archivo por idioma
        if (!POR_IDIOMA.TryGetValue(idiomaDet, out string archivo))
        {
            Debug.LogWarning($"[Ranking] Idioma {idiomaDet} no mapeado. Fallback a EN.");
            Application.OpenURL(urlDefault);
            return;
        }

        string url = CombinarUrl(baseUrl, archivo);
        Debug.Log($"[Ranking] Idioma: {idiomaDet} -> {url}");
        Application.OpenURL(url);
    }

    private SystemLanguage DetectarIdioma()
    {
        if (!string.IsNullOrEmpty(playerPrefsLangKey) && PlayerPrefs.HasKey(playerPrefsLangKey))
        {
            string code = PlayerPrefs.GetString(playerPrefsLangKey, "").ToLowerInvariant().Trim();
            if (CODE2LANG.TryGetValue(code, out var lang))
            {
                return lang;
            }
            Debug.LogWarning($"[Ranking] langCode='{code}' no reconocido. Uso systemLanguage.");
        }
        return Application.systemLanguage;
    }

    private string CombinarUrl(string baseU, string archivo)
    {
        if (string.IsNullOrEmpty(baseU)) return archivo;
        if (!baseU.EndsWith("/")) baseU += "/";
        return baseU + archivo;
    }

    // Para probar desde el inspector (clic derecho en el componente)
    [ContextMenu("Probar abrir ranking")]
    private void _TestOpen() => AbrirWeb();
}
