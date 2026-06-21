using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement; // para usar SceneManager

public class GardenGame : MonoBehaviour
{
    public KingFree rey;

    public Button startButton;
    public Button passTurnButton;

    [Header("Ficha seleccionada actualmente (Rey u otra aliada)")]
    public MonoBehaviour fichaSeleccionadaActual;

    // 🔥 NUEVOS BOTONES
    public Button restartButton;
    public Button exitButton;

    //public TMP_Text timerText;
    //public TMP_Text pmText;
   // public TMP_Text turnosText;
   // public TMP_Text paText;

    private float turnoDuration = 30f;
    private float tiempoRestante;
    private float tiempoNivelAcumulado = 0f;

    private bool turnoActivo = false;
    private int turnoActual = 0;

    private void Start()
    {
        passTurnButton.gameObject.SetActive(false);
        startButton.onClick.AddListener(IniciarJuego);
        passTurnButton.onClick.AddListener(PasarTurno);

        // 🔥 NUEVOS listeners para los botones
        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
        if (exitButton != null)
            exitButton.onClick.AddListener(Exit);

        //timerText.text = "";
        //pmText.text = "";
       // turnosText.text = "";
        //paText.text = "";

        ActualizarHUD();
        
        
    }

    private void Update()
    {
        if (turnoActivo)
        {
            tiempoRestante -= Time.deltaTime;
            tiempoNivelAcumulado += Time.deltaTime;
            //timerText.text = Mathf.CeilToInt(tiempoRestante).ToString();

            if (tiempoRestante <= 0f)
            {
                PasarTurno();
                
            }
        }
    }

    private void IniciarJuego()
    {
        Debug.Log("🌳 ¡Inicio del juego en el Jardín!");
        startButton.gameObject.SetActive(false);
        passTurnButton.gameObject.SetActive(true);

        turnoActivo = true;
        tiempoRestante = turnoDuration;
        tiempoNivelAcumulado = 0f;
        turnoActual = 1;

        // ✅ Seleccionar al Rey como ficha activa inicial
        fichaSeleccionadaActual = rey;

        // ✅ Activar el Rey
        rey.ActivarJuego();

        // ✅ Mostrar su rango inicial
        Invoke(nameof(MostrarRangoInicialRey), 0.02f);

        // 🔄 Activar todas las fichas aliadas
        var aliadas = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IFichaAliada>();
        foreach (var ficha in aliadas)
        {
            ficha.ActivarJuego();

            if (ficha is IPieceWithPosition pieza)
            {
                var tile = Garden7.Instance.GetTileAt(pieza.GetPosicionActual());
                if (tile != null)
                {
                    tile.HighlightMove(false); // oculta highlight al inicio
                }
            }
        }

        ActualizarHUD();
        NotificarEfectosTurno();
       
        MusicManager.Instance.PlayMusic();
       
    }


    private void MostrarRangoInicialRey()
    {
        rey.mostrandoMovimientos = true;
        rey.MostrarMovimientoPosible();

    }


    private void PasarTurno()
    {
        Debug.Log("🌿 ¡Pasando turno en el Jardín!");
        tiempoRestante = turnoDuration;
        turnoActual++;

        // 🔁 Reiniciar fichas aliadas
        var aliadas = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IFichaAliada>();
        foreach (var aliada in aliadas)
        {
            aliada.ReiniciarTurno();
        }

        // 🔁 Reiniciar fichas enemigas
        var enemigas = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IFichaEnemiga>();
        foreach (var enemiga in enemigas)
        {
            enemiga.ReiniciarTurno();
        }

        // ♔ El Rey también reinicia su turno
        rey.ReiniciarTurno();
        rey.RestarTurno();

        // 🔄 Rango visible si una ficha sigue seleccionada
        if (fichaSeleccionadaActual is IFichaAliada fichaAliada)
        {
            fichaAliada.MostrarRango();
        }

        NotificarEfectosTurno();
        ActualizarHUD();
    }

    private void NotificarEfectosTurno()
    {
        var efectos = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ITileEffect>().ToList();
        Debug.Log($"🚀 Jardín: Notificando turno {turnoActual} a {efectos.Count} objetos ITileEffect.");

        foreach (var efecto in efectos)
        {
            Debug.Log($"📦 Jardín: Notificando objeto {((MonoBehaviour)efecto).gameObject.name}");
            efecto.VerificarTurnoActual(turnoActual);
        }
    }

    public void ActualizarHUD()
    {
        //pmText.text = rey.puntosMovimientoActual.ToString();
       // turnosText.text = rey.turnosRestantes.ToString();
        //paText.text = rey.puntosAccionActual.ToString();
    }

    public void DetenerJuego()
    {
        turnoActivo = false;
        Debug.Log("⏸ Jardín detenido, reloj pausado.");
    }

    public void AgregarTiempoAlTurno(float segundos)
    {
        tiempoRestante += segundos;
        tiempoRestante = Mathf.Max(tiempoRestante, 0f);
        Debug.Log($"⏰ Jardín: tiempo ajustado {segundos:+0.##;-0.##}s -> Tiempo restante: {tiempoRestante:F1}s");
    }

    public float GetTiempoRestante()
    {
        return tiempoRestante;
    }

    public float GetTiempoNivelAcumulado()
    {
        return tiempoNivelAcumulado;
    }

    public bool IsJuegoActivo()
    {
        return turnoActivo;
    }

    // === NUEVAS FUNCIONES PARA LOS BOTONES ===
    public void Restart()
    {
        Debug.Log("🔄 Jardín reiniciado...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Exit()
    {
        Debug.Log("🚪 Jardín: Exit presionado (aquí decides la acción).");
    }

    public void NotifyBoardChanged()
    {
        var reinas = FindObjectsByType<QueenEnemyController>(FindObjectsSortMode.None);
        foreach (var reina in reinas)
        {
            reina.RevisarAmenazasEnZona();
        }
    }
}
