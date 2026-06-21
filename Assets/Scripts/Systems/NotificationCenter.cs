using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NotificationCenter : MonoBehaviour
{
    [Tooltip("Activa/desactiva automáticamente burbuja y texto del badge según el conteo.")]
    public bool autoToggleBadgeObjects = true;

    [Serializable]
    
    
    public struct PanelBinding
    {

        [Tooltip("Selecciona el logro (enum), en vez de escribir texto.")]
        public AchievementId id;


        [Tooltip("Panel ya diseñado dentro del ScrollView. Déjalo INACTIVO por defecto.")]

        public GameObject panel;

        [Tooltip("Botón de cerrar del panel (opcional si ya lo conectas en el Inspector)")]
        public Button closeButton;
    }

    public static NotificationCenter Instance { get; private set; }

    [Header("Escena: Notificaciones (opcional)")]
    [Tooltip("Contenedor del ScrollView (no obligatorio, solo informativo)")]
    public Transform scrollContent;

    [Tooltip("Lista de paneles preexistentes a controlar (asignación MANUAL por enum).")]
    public List<PanelBinding> paneles = new List<PanelBinding>();

    [Header("Escena: GameHome (opcional)")]
    [Tooltip("Burbuja roja del badge (se oculta si count=0)")]
    public GameObject badgeBubble;

    [Tooltip("Texto del badge (ej: 3)")]
    public TMP_Text badgeText;

    [Header("Config")]
    [Tooltip("Si no hay slotActivo, usar este por defecto")]
    public string fallbackSlotId = "slot1";

    [Tooltip("Imprime logs útiles")]
    public bool verbose = false;

    [Tooltip("Si está activo, el badge cuenta SOLO los achievements que tienen panel enlazado.\nSi está desactivado, cuenta todos los AchievementId del enum.")]
    public bool badgeCountsOnlyMapped = true;

    // Mapa rápido id->panel
    private readonly Dictionary<AchievementId, PanelBinding> _map =
        new Dictionary<AchievementId, PanelBinding>();

    // ==========================
    // Ciclo de vida
    // ==========================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (verbose) Debug.Log("[NC] Ya existe instancia. Actualizando refs de escena…");
            Instance.AbsorbSceneRefsFrom(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildMap();
        WireCloseButtonsIfNeeded();
        RefreshPanelsFromPrefs();
        RefreshBadge();
    }

    private void Start()
    {
        // Por si las refs llegan tarde
        RefreshBadge();
        RefreshPanelsFromPrefs();
    }

    /*// <summary>Copiar referencias de otra instancia (cuando existe en varias escenas).</summary>
    public void AbsorbSceneRefsFrom(NotificationCenter other)
    {
        if (other == null) return;

        // Mezcla/actualiza bindings nuevos
        foreach (var b in other.paneles)
        {
            if (b.panel == null) continue;
            if (!_map.ContainsKey(b.id))
                _map[b.id] = b;
        }

        // Actualiza refs de UI
        if (other.scrollContent != null) scrollContent = other.scrollContent;
        if (other.badgeBubble != null)   badgeBubble   = other.badgeBubble;
        if (other.badgeText != null)     badgeText     = other.badgeText;

        WireCloseButtonsIfNeeded();
        RefreshPanelsFromPrefs();
        RefreshBadge();
    }*/

    public void AbsorbSceneRefsFrom(NotificationCenter other)
{
    if (other == null) return;

    // Mezcla/actualiza bindings nuevos (👈 AHORA SIEMPRE REEMPLAZA)
    foreach (var b in other.paneles)
    {
        if (b.panel == null) continue;
        _map[b.id] = b;      // 👈 esta línea es la clave
    }

    // (Opcional) mantener lista paneles coherente
    paneles = other.paneles;

    // Actualiza refs de UI
    if (other.scrollContent != null) scrollContent = other.scrollContent;
    if (other.badgeBubble != null)   badgeBubble   = other.badgeBubble;
    if (other.badgeText != null)     badgeText     = other.badgeText;

    WireCloseButtonsIfNeeded();
    RefreshPanelsFromPrefs();
    RefreshBadge();
}

    private void BuildMap()
    {
        _map.Clear();
        foreach (var b in paneles)
        {
            if (b.panel == null) continue;
            if (!_map.ContainsKey(b.id))
                _map.Add(b.id, b);

            // Asegura inactivo por defecto
            b.panel.SetActive(false);
        }
    }

    private void WireCloseButtonsIfNeeded()
    {
        foreach (var kv in _map)
        {
            var id = kv.Key;
            var pb = kv.Value;
            if (pb.closeButton != null)
            {
                pb.closeButton.onClick.RemoveAllListeners();
                var idCopy = id; // captura
                pb.closeButton.onClick.AddListener(() => ClosePanelByEnum(idCopy));
            }
        }
    }

    // ==========================
    // API para Achievements / HUD
    // ==========================

    /// <summary>
    /// Llamar cuando se desbloquea un logro (una sola vez por slot).
    /// No muestra pop-up inmediato; activa el panel (si existe en la escena) y actualiza badge.
    /// </summary>
    public void OnAchievementUnlocked(string slotId, AchievementId id, string title = null, string body = null)
    {
        if (string.IsNullOrEmpty(slotId)) slotId = GetActiveSlot();

        // Marca como "no cerrado" para esta notificación
        SetDismissed(slotId, id, false);

        // Si hay panel mapeado, activarlo
        ActivatePanelIfPresent(id);

        if (verbose) Debug.Log($"[NC] OnAchievementUnlocked -> {id} (slot {slotId}).");

        RefreshBadge(slotId);
    }

    /// <summary>
    /// Cerrar panel (método tipado). Conéctalo al botón [X] pasando el enum.
    /// </summary>
    public void ClosePanelByEnum(AchievementId id)
    {
        string slotId = GetActiveSlot();
        SetDismissed(slotId, id, true);

        if (_map.TryGetValue(id, out var pb) && pb.panel != null)
            pb.panel.SetActive(false);

        if (verbose) Debug.Log($"[NC] ClosePanel -> {id} (slot {slotId})");
        RefreshBadge(slotId);
    }

    /// <summary>
    /// Wrapper de compatibilidad si tienes botones viejos que pasan string.
    /// </summary>
    public void ClosePanelById(string idAsString)
    {
        if (Enum.TryParse(idAsString, out AchievementId parsed))
            ClosePanelByEnum(parsed);
        else if (verbose)
            Debug.LogWarning($"[NC] ClosePanelById: '{idAsString}' no coincide con AchievementId.");
    }

    /// <summary>Actualiza badge en GameHome.</summary>
    public void RefreshBadge(string slotId = null)
    {
        slotId ??= GetActiveSlot();

        // 1) Conteo con fallback:
        int count = 0;
        if (badgeCountsOnlyMapped && _map.Count > 0)
        {
            // Solo los mapeados (comportamiento tipo Honors manual)
            foreach (var id in _map.Keys)
                if (IsUnlocked(slotId, id) && !IsDismissed(slotId, id))
                    count++;
        }
        else
        {
            // Fallback: cuenta todo el enum (útil cuando GameHome no tiene paneles mapeados)
            foreach (AchievementId id in Enum.GetValues(typeof(AchievementId)))
                if (IsUnlocked(slotId, id) && !IsDismissed(slotId, id))
                    count++;
        }

        bool show = count > 0;

        // 2) Activar/Desactivar ambos objetos del badge
        if (autoToggleBadgeObjects)
        {
            if (badgeBubble != null) badgeBubble.SetActive(show);
            if (badgeText != null) badgeText.gameObject.SetActive(show);
        }

        // 3) Actualizar el número
        if (badgeText != null) badgeText.text = count.ToString();

        if (verbose) Debug.Log($"[NC] Badge actualizado => {count} (show={show})");
    }


    /// <summary>
    /// Llamar al entrar a la escena de Notificaciones para reflejar estado actual.
    /// </summary>
    public void RefreshPanelsFromPrefs()
    {
        string slotId = GetActiveSlot();

        foreach (var kv in _map)
        {
            var id = kv.Key;
            var pb = kv.Value;

            bool visible = IsUnlocked(slotId, id) && !IsDismissed(slotId, id);
            if (pb.panel != null) pb.panel.SetActive(visible);
        }

        if (verbose) Debug.Log("[NC] Panels refrescados desde PlayerPrefs.");
    }

    // ==========================
    // Helpers de estado
    // ==========================

    private void ActivatePanelIfPresent(AchievementId id)
    {
        if (_map.TryGetValue(id, out var pb) && pb.panel != null)
            pb.panel.SetActive(true);
    }

    private string GetActiveSlot()
    {
        return PlayerPrefs.GetString("slotActivo", fallbackSlotId);
    }

    private static string KeyAch(string slotId, AchievementId id) => $"{slotId}_ach_{id}";
    private static string KeyDismissed(string slotId, AchievementId id) => $"{slotId}_notif_{id}_dismissed";

    // "Unlocked" lo escribe AchievementsManager (TryUnlock). Aquí solo lo leemos.
    private bool IsUnlocked(string slotId, AchievementId id)
    {
        return PlayerPrefs.GetInt(KeyAch(slotId, id), 0) == 1;
    }

    private bool IsDismissed(string slotId, AchievementId id)
    {
        return PlayerPrefs.GetInt(KeyDismissed(slotId, id), 0) == 1;
    }

    private void SetDismissed(string slotId, AchievementId id, bool dismissed)
    {
        PlayerPrefs.SetInt(KeyDismissed(slotId, id), dismissed ? 1 : 0);
        PlayerPrefs.Save();
    }
}
