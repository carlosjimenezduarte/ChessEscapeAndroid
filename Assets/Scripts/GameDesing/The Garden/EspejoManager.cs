using UnityEngine;
using System.Collections.Generic;

public class EspejoManager : MonoBehaviour
{
    public static EspejoManager Instance;

    [System.Serializable]
    public class EspejoConfig
    {
        public string nombre;
        public Vector2Int posicion;
        public GameObject panel;
    }

    [Header("Asignación de paneles a coordenadas")]
    public List<EspejoConfig> espejos = new List<EspejoConfig>();

    // Estado: qué casillas ya están activas actualmente
    private HashSet<Vector2Int> casillasActivas = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RevisarEspejo(Vector2Int posicion)
    {
        foreach (var espejo in espejos)
        {
            if (espejo.posicion == posicion)
            {
                if (!casillasActivas.Contains(posicion))
                {
                    espejo.panel.SetActive(true);
                    casillasActivas.Add(posicion);
                    Debug.Log($"🪞 {espejo.nombre} activado en {posicion}");
                }
                return;
            }
        }
    }

    public void SalirDeCasilla(Vector2Int posicion)
    {
        if (casillasActivas.Contains(posicion))
        {
            casillasActivas.Remove(posicion);
            Debug.Log($"🚪 Salida de {posicion}, espejo listo para reactivarse.");
        }
    }

    public void CerrarPanel(GameObject panel)
    {
        panel.SetActive(false);
         SoundManager.Instance.PlaySound(39);
    }
}
