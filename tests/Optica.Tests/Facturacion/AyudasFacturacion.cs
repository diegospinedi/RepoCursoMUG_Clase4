using System.Net;
using System.Net.Http.Json;
using Optica.Tests.Presupuestos;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Facturacion;

public sealed record FacturaEmitida(int FacturaId, string Estado, string Tipo, int PuntoVenta, long Numero, string Cae, DateOnly VencimientoCae);
public sealed record LineaFactura(string Descripcion, int Cantidad, decimal PrecioFinal);
public sealed record PresupuestoOrigen(int Id, int Numero);
public sealed record FacturaDetalle(
    int Id, string Estado, string Tipo, int PuntoVenta, long Numero, DateOnly Fecha, int ReceptorDocTipo, string ReceptorDocNro,
    decimal Total, decimal? Neto, decimal? Iva, decimal? AlicuotaIva, string? Cae, DateOnly? VencimientoCae,
    PresupuestoOrigen Presupuesto, string Apellido, string Nombre, string Dni, LineaFactura[] Lineas);

public static class AyudasFacturacion
{
    public static Task<HttpResponseMessage> FacturarAsync(this HttpClient cliente, int presupuestoId) =>
        cliente.PostAsync($"/api/presupuestos/{presupuestoId}/factura", null);

    public static async Task<FacturaEmitida> FacturarBienAsync(this HttpClient cliente, int presupuestoId)
    {
        var r = await cliente.FacturarAsync(presupuestoId);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<FacturaEmitida>())!;
    }

    public static async Task<FacturaDetalle> FacturaAsync(this HttpClient cliente, int id) =>
        (await cliente.GetFromJsonAsync<FacturaDetalle>($"/api/facturas/{id}"))!;

    /// <summary>Catálogo con el artículo de $1.815,00 y un presupuesto Final con <paramref name="cantidad"/> unidades.</summary>
    public static async Task<PresupuestoDto> PresupuestoFinalAsync(this HttpClient cliente, int cantidad = 1, object? datosCliente = null)
    {
        var a = await cliente.PrepararCatalogoAsync();
        return await cliente.CrearFinalAsync(datosCliente ?? Cliente(), Linea(a.Codigo, cantidad));
    }
}
