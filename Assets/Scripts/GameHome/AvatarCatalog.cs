// AvatarCatalog.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="ChessEscape/AvatarCatalog", fileName="AvatarCatalog")]
public class AvatarCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry { public string id; public Sprite sprite; }

    [Header("Lista id → sprite")]
    public List<Entry> entries = new();

    [Header("Fallback")]
    public Sprite defaultSprite;

    public Sprite Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return defaultSprite;
        var e = entries.Find(x => x.id == id);
        return e != null && e.sprite != null ? e.sprite : defaultSprite;
    }
}
