using UnityEngine;

public enum TipoObjetoScore
{
    None,
    RealCoin,
    Bag,
    Chest,
    Crown
}

public class PlayerScore : MonoBehaviour
{
    public static PlayerScore Instance { get; private set; }
    private int totalScore = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    /// Agrega puntos al score del intento actual (feedback).
    public void AgregarPuntaje(int puntos, TipoObjetoScore tipo)
    {
        totalScore += puntos;
        Debug.Log($"💰 Score actualizado: +{puntos} pts -> Total: {totalScore}");
        // ⚠️ Ya NO llamar a Stadistics aquí (evita farmeo).
    }

    public int GetTotalScore() => totalScore;

    public void ResetScore()
    {
        totalScore = 0;
        Debug.Log("🔄 Score reiniciado a 0.");
    }
}