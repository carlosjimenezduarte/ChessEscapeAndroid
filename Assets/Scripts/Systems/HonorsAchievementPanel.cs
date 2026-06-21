using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HonorsAchievementPanel : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        [Tooltip("Logro que controla este panel")]
        public AchievementId id;

        [Tooltip("Graphic principal del panel (ej: el Image del panel). Si prefieres teñir todo el panel, marca 'pintarHijos'.")]
        public Graphic target;

        [Tooltip("Si está activo, pinta TODOS los Graphics hijos del target")]
        public bool pintarHijos = false;

        [Tooltip("Opcional: si lo dejas por defecto, se usará el color original del Graphic como 'locked'.")]
        public Color lockedColorOverride = new Color(0, 0, 0, 0); // 'sin override'

        // Internos (no toques desde el inspector)
        [NonSerialized] public Color lockedColorCapturado;
        [NonSerialized] public bool inicializado;
    }

    [Header("Bindings: logro → panel")]
    public List<Item> items = new List<Item>();

    [Header("Colores")]
    public Color unlockedColor = Color.green;

    string slotId;

    void Awake()
    {
        slotId = PlayerPrefs.GetString("slotActivo", "slot1");

        // Capturar color "natural" (locked) de cada panel
        foreach (var it in items)
        {
            if (it.target == null) continue;

            if (it.lockedColorOverride.a > 0f)
                it.lockedColorCapturado = it.lockedColorOverride;       // override explícito
            else
                it.lockedColorCapturado = it.target.color;               // color natural del panel

            it.inicializado = true;
        }

        // Pintar estado actual (por si ya hay logros desbloqueados)
        PaintAll();

        // Escuchar desbloqueos en vivo
        AchievementsManager.OnAchievementUnlocked += OnAchievementUnlocked;
    }

    void OnEnable()
    {
        // Al volver a la escena, asegurar coherencia visual
        PaintAll();
    }

    void OnDestroy()
    {
        AchievementsManager.OnAchievementUnlocked -= OnAchievementUnlocked;
    }

    void OnAchievementUnlocked(string slot, AchievementId id)
    {
        if (slot != slotId) return;

        // Pintar solo el/los panel(es) cuyo logro coincide
        for (int i = 0; i < items.Count; i++)
            if (items[i].id == id)
                Paint(items[i]);
    }

    void PaintAll()
    {
        foreach (var it in items)
            Paint(it);
    }

    void Paint(Item it)
    {
        if (it.target == null || !it.inicializado) return;

        bool unlocked = AchievementsManager.IsUnlocked(slotId, it.id);
        Color c = unlocked ? unlockedColor : it.lockedColorCapturado;

        if (!it.pintarHijos)
        {
            it.target.color = c;
        }
        else
        {
            // Pinta el target y todos sus hijos gráficos
            var gfx = it.target.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < gfx.Length; i++)
                gfx[i].color = c;
        }
    }

    // Útil si alguna vez cambias de slot en runtime
    public void Refresh(string newSlotId = null)
    {
        if (!string.IsNullOrEmpty(newSlotId)) slotId = newSlotId;
        PaintAll();
    }
}
