using UnityEngine;
public interface IFichaAliada : IFicha
{
    int rangoAtaque { get; set; }
    int rangoMovimientoBase { get; set; }

    
    // Solo un marker interface, no necesita métodos

    void MostrarRango();
    void OcultarRango();
    void ActivarJuego();
    void OcultarMovimientos();
    void ReiniciarTurno();
    Vector2Int GetPosicionActual();

    
}