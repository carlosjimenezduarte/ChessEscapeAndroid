using UnityEngine;

public class WebLinkOpener : MonoBehaviour
{
    [SerializeField] private string url = "https://chessescape.com"; 

    public void AbrirWeb()
    {
        Debug.Log("🔗 Abriendo URL: " + url);
        Application.OpenURL(url);
    }
}
