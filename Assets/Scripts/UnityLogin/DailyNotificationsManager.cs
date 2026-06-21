using UnityEngine;
using System;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class DailyNotificationsManager : MonoBehaviour
{
    public static DailyNotificationsManager Instance { get; private set; }

    private const string CHANNEL_ID = "chessescape_daily";
    private const string GLOBAL_ENABLED_KEY = "ce_notif_enabled"; // 0 = no, 1 = sí (ya programadas)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeChannel();
    }

    private void InitializeChannel()
    {
    #if UNITY_ANDROID
        var channel = new AndroidNotificationChannel
        {
            Id          = CHANNEL_ID,
            Name        = "Recordatorios diarios",
            Description = "Recordatorios para seguir tu viaje en ChessEscape",
            Importance  = Importance.Default
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    #endif
    }

    /// <summary>
    /// Programa 1 notificación diaria durante 30 días a las 3:00 p.m.
    /// Solo se ejecuta una vez: si ya estaban programadas no vuelve a hacerlo.
    /// </summary>
    public void ScheduleForNext30DaysIfNeeded()
    {
    #if UNITY_ANDROID
        if (PlayerPrefs.GetInt(GLOBAL_ENABLED_KEY, 0) == 1)
        {
            Debug.Log("[Notif] Ya había notificaciones programadas. No se duplican.");
            return;
        }

        DateTime now = DateTime.Now;

        // Primera ejecución: hoy a las 15:00; si ya pasó, mañana a las 15:00
        DateTime first = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0);
        if (first <= now)
            first = first.AddDays(1);

        string[] mensajes = GetDailyMessages();

        for (int i = 0; i < 30; i++)
        {
            DateTime fireTime = first.AddDays(i);

            var notif = new AndroidNotification
            {
                Title     = "ChessEscape",
                Text      = mensajes[i % mensajes.Length],
                FireTime  = fireTime,
                SmallIcon = "default",
                LargeIcon = "default"
            };

            AndroidNotificationCenter.SendNotification(notif, CHANNEL_ID);
        }

        PlayerPrefs.SetInt(GLOBAL_ENABLED_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("[Notif] Programadas 30 notificaciones diarias a las 3:00 p.m.");
    #endif
    }

    private string[] GetDailyMessages()
    {
        return new[]
        {
            "¿Y si fueras tú el que está destinado a ser Rey? 👑", // tu frase fija
            "Tu Rey interior te espera en el tablero. Juega una partida hoy.",
            "Cada nivel es un paso hacia tu corona. Continúa tu camino en ChessEscape.",
            "La próxima jugada podría cambiar tu historia. Vuelve al Reino.",
            "Incluso el Rey descansa, pero siempre regresa al tablero. Retoma tu partida.",
            "Un solo movimiento consciente vale más que mil distracciones. Juega ChessEscape.",
            "¿Y si hoy derrotas a esa Reina que ayer te venció?",
            "Tu racha de valor no se alimenta sola. Dale vida con una partida.",
            "Recuerda: el tablero es un espejo de tu mente. Mírala hoy de nuevo.",
            "Hay 204 experiencias esperándote. ¿Cuál será la de hoy?",
            "Las llaves, las coronas y los cofres quieren un nuevo guardián. Entra al juego.",
            "El Reino no se construye en un día, sino jugada a jugada. Continúa.",
            "Un Rey que no avanza, retrocede. Da aunque sea un movimiento hoy.",
            "Tu próximo logro puede estar a un solo nivel de distancia.",
            "Si el miedo mueve las piezas, el amor las corona. Juega desde el corazón.",
            "La estrategia también es una oración silenciosa. Ora jugando.",
            "Tu mente merece un rato de juego sagrado. Abre ChessEscape.",
            "No estás solo en el tablero: la Vida mueve contigo. Vuelve a jugar.",
            "Cada enemigo derrotado te enseña algo de ti mismo.",
            "¿Cuántos diamantes de conciencia descubrirás hoy?",
            "La corona no es solo un premio: es un recuerdo de quién eres.",
            "Un buen Rey cuida su Reino. Visita el tuyo de nuevo.",
            "El tablero te espera en silencio, pero tu alma ya escucha el llamado.",
            "Hoy puede ser el día en que domines ese nivel que parecía imposible.",
            "Tus manos saben el camino. Solo tienes que tocar 'Jugar'.",
            "Un pequeño gesto: abrir el juego. El resto lo hace tu corazón.",
            "La Maestría se construye con constancia. Ven a por una jugada más.",
            "Hay un mensaje escondido en el próximo nivel. ¿Te animas a encontrarlo?",
            "Tus logros no caducan, pero tu oportunidad de hoy sí.",
            "Cuando el Rey avanza, el miedo se retira. Da el siguiente paso en ChessEscape."
        };
    }
}
