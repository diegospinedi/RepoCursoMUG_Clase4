namespace Optica.Api.Arca;

/// <summary>Sección "Arca" de appsettings.json. Sin secretos (constitución, principio IV).</summary>
public sealed class OpcionesArca
{
    public string Entorno { get; set; } = "Simulado";
    public int PuntoVenta { get; set; }
    public int TiempoEsperaSegundos { get; set; } = 30;
    public OpcionesSimulador Simulador { get; set; } = new();
}

public sealed class OpcionesSimulador
{
    /// <summary>Normal, Rechazar, SinRespuesta o AutorizarSinResponder.</summary>
    public string Modo { get; set; } = "Normal";
    public string Archivo { get; set; } = "arca-simulado.json";
}
