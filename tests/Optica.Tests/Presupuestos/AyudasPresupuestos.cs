using System.Net;
using System.Net.Http.Json;
using Optica.Tests.Catalogo;

namespace Optica.Tests.Presupuestos;

public sealed record ClienteDto(string Apellido, string Nombre, string Dni, string? Domicilio, string? Email, string? Telefono);
public sealed record LineaDto(int Id, int Orden, int ArticuloCodigo, string Descripcion, decimal PrecioUnitario, int Cantidad, decimal Descuento, decimal PrecioConDescuento, decimal PrecioFinal);
public sealed record EmisionDto(int FacturaId, string Estado);
public sealed record PresupuestoDto(int Id, int Numero, DateOnly Fecha, string Estado, ClienteDto Cliente, LineaDto[] Lineas, decimal Total, EmisionDto? Emision);

public static class AyudasPresupuestos
{
    public static object Cliente(string apellido = "González", string nombre = "Ana", string dni = "23.456.789",
        string? domicilio = "Calle 42 n° 767", string? email = "ana@example.com", string? telefono = "221 555-1234") =>
        new { apellido, nombre, dni, domicilio, email, telefono };

    public static object Linea(int articuloCodigo, object cantidad, object? descuento = null, int? id = null) =>
        new { id, articuloCodigo, cantidad, descuento = descuento ?? 0 };

    public static Task<HttpResponseMessage> PostPresupuestoAsync(this HttpClient cliente, object datosCliente, params object[] lineas) =>
        cliente.PostAsJsonAsync("/api/presupuestos", new { estado = "Borrador", cliente = datosCliente, lineas });

    public static Task<HttpResponseMessage> PutPresupuestoAsync(this HttpClient cliente, int id, string estado, object datosCliente, params object[] lineas) =>
        cliente.PutAsJsonAsync($"/api/presupuestos/{id}", new { estado, cliente = datosCliente, lineas });

    public static async Task<PresupuestoDto> CrearPresupuestoAsync(this HttpClient cliente, object datosCliente, params object[] lineas)
    {
        var r = await cliente.PostPresupuestoAsync(datosCliente, lineas);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<PresupuestoDto>())!;
    }

    public static async Task<PresupuestoDto> PresupuestoAsync(this HttpClient cliente, int id) =>
        (await cliente.GetFromJsonAsync<PresupuestoDto>($"/api/presupuestos/{id}"))!;

    /// <summary>Graba en Borrador y lo pasa a Final, como en la app (AGENTS.md).</summary>
    public static async Task<PresupuestoDto> CrearFinalAsync(this HttpClient cliente, object datosCliente, params object[] lineas)
    {
        var p = await cliente.CrearPresupuestoAsync(datosCliente, lineas);
        var lineasExistentes = p.Lineas.Select(l => Linea(l.ArticuloCodigo, l.Cantidad, l.Descuento, l.Id)).ToArray();
        var r = await cliente.PutPresupuestoAsync(p.Id, "Final", datosCliente, lineasExistentes);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<PresupuestoDto>())!;
    }

    /// <summary>Catálogo con un proveedor y un artículo a $1.815,00 (costo 1210, margen 50).</summary>
    public static async Task<ArticuloDto> PrepararCatalogoAsync(this HttpClient cliente, decimal costo = 1210m, decimal margen = 50m)
    {
        await cliente.ConfigurarAsync();
        var p = await cliente.CrearProveedorAsync();
        return await cliente.CrearArticuloAsync(p.Id, "ABC-1", costo, margen);
    }
}
