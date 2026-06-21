using UnityEngine;

public class WebOfficial : MonoBehaviour
{
    [SerializeField] private string officialUrl = "https://chessescape.com/WebOfficial/index.html";

    public void AbrirWebOfficial()
    {
        OpenOfficialWebsite();
    }

    public void OpenOfficialWebsite()
    {
        if (string.IsNullOrWhiteSpace(officialUrl))
        {
            Debug.LogWarning("[WebOfficial] No hay URL configurada.");
            return;
        }

        Debug.Log("[WebOfficial] Abriendo URL: " + officialUrl);
        Application.OpenURL(officialUrl);
    }
}
