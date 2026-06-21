using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public class LevelIdentity : MonoBehaviour
{
    [Header("Override opcional (déjalo en -1 para auto)")]
    public int nivelLogicoInspector = -1;

    [SerializeField] private int buildOffset = 14;

    // === Origen de verdad global ===
    public static bool HasNivelLogico { get; private set; }
    public static int NivelLogico { get; private set; }

    void Awake()
    {
        int nivelLogicoActual = (nivelLogicoInspector >= 0)
            ? nivelLogicoInspector
            : SceneManager.GetActiveScene().buildIndex - buildOffset;

        HasNivelLogico = true;
        NivelLogico = nivelLogicoActual;

        // Mantener PlayerPrefs coherente (opcional)
        if (PlayerPrefs.GetInt("nivelActivo", -9999) != nivelLogicoActual)
        {
            PlayerPrefs.SetInt("nivelActivo", nivelLogicoActual);
            PlayerPrefs.Save();
        }

        Debug.Log($"[LevelIdentity] Nivel lógico = {NivelLogico} (build={SceneManager.GetActiveScene().buildIndex}, offset={buildOffset})");
    }
}
