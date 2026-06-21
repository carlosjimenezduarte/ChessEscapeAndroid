using UnityEngine;
public interface IFichaEnemiga
{
    int rangoKillZone { get; set; }
    int rangoRangeZone { get; set; }

    
    void RevisarAmenazasEnZona();
    void MostrarRango();
    void OcultarRango();
    void ReiniciarTurno();

    Vector2Int GetPosicionActual();
    // 🔹 Integración con el Árbitro Silencioso
    void ProcesarMovimientoAliado(Vector2Int posAliada, int idMovimiento);
    
}