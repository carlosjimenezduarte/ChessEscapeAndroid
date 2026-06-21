using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CofreController : MonoBehaviour
{
    [Header("Panel del Cofre")]
    public GameObject panelCofre;

    [Header("Imágenes de la llave")]
    public GameObject crownKey;        // llave viva
    public GameObject crownKeyEnabled; // llave deshabilitada

    [Header("Textos")]
    public TMP_Text textNoKey;
    public TMP_Text textSure;

    [Header("Botones")]
    public Button btnYes;
    public Button btnBack;
    public Button btnDeAcuerdo;

    // 📌 Esta variable indica si el Rey recogió la llave
    private bool tieneLlave = false;

    // ======================================
    //  Métodos públicos para integrarse con el juego
    // ======================================

    // Llamado cuando el Rey recoge la llave
    public void SetTieneLlave(bool valor)
    {
        tieneLlave = valor;
    }

    // Llamado cuando el usuario hace clic en la casilla (4,4)
    public void AbrirCofre()
    {
        panelCofre.SetActive(true);

        if (tieneLlave)
        {
            // ✅ Caso con llave
            crownKey.SetActive(true);
            crownKeyEnabled.SetActive(false);

            textSure.gameObject.SetActive(true);
            textNoKey.gameObject.SetActive(false);

            btnYes.gameObject.SetActive(true);
            btnBack.gameObject.SetActive(true);
            btnDeAcuerdo.gameObject.SetActive(false);
        }
        else
        {
            // ❌ Caso sin llave
            crownKey.SetActive(false);
            crownKeyEnabled.SetActive(true);

            textSure.gameObject.SetActive(false);
            textNoKey.gameObject.SetActive(true);

            btnYes.gameObject.SetActive(false);
            btnBack.gameObject.SetActive(false);
            btnDeAcuerdo.gameObject.SetActive(true);
        }
    }

    // Cerrar el panel del Cofre
    public void CerrarCofre()
    {
        panelCofre.SetActive(false);
    }
}
