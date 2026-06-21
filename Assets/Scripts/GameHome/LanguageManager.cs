using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageManager : MonoBehaviour
{
    public void SetLanguage(Locale locale)
    {
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            Debug.Log("Idioma cambiado a: " + locale.Identifier.Code);
        }
    }
}
