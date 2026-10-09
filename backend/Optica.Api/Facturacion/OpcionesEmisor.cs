namespace Optica.Api.Facturacion;

/// <summary>Datos públicos del emisor para el PDF (RF-30), en la sección "Emisor" de appsettings.json.</summary>
public sealed class OpcionesEmisor
{
    public string RazonSocial { get; set; } = "";
    public string Domicilio { get; set; } = "";
    public string Cuit { get; set; } = "";
    public string CondicionIva { get; set; } = "";
    public string IngresosBrutos { get; set; } = "";
    public string InicioActividades { get; set; } = "";
}
