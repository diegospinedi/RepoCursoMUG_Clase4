using System.Net;
using System.Net.Http.Json;

namespace Optica.Tests.Catalogo;

public sealed record ConfiguracionDto(
    decimal AlicuotaIva, string CondicionFiscal, decimal TopeIdentificacion, decimal MultiploRedondeo, decimal? MargenPredeterminado,
    int ArticulosRecalculados = 0);

public sealed record ProveedorDto(int Id, string Nombre);

public sealed record ArticuloDto(
    int Codigo, int ProveedorId, string Proveedor, string CodigoProveedor, string Descripcion,
    decimal PrecioCosto, decimal Margen, decimal PrecioVenta);

/// <summary>Atajos para preparar catálogo y configuración en los tests.</summary>
public static class AyudasCatalogo
{
    public static async Task<ConfiguracionDto> ConfigurarAsync(
        this HttpClient cliente, decimal alicuota = 21m, string condicion = "ResponsableInscripto",
        decimal tope = 10_000_000m, decimal multiplo = 0.01m, decimal? margenPredeterminado = 50m)
    {
        var r = await cliente.PutAsJsonAsync("/api/configuracion", new
        {
            alicuotaIva = alicuota, condicionFiscal = condicion, topeIdentificacion = tope,
            multiploRedondeo = multiplo, margenPredeterminado,
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ConfiguracionDto>())!;
    }

    public static async Task<ProveedorDto> CrearProveedorAsync(this HttpClient cliente, string nombre = "Lentes SA")
    {
        var r = await cliente.PostAsJsonAsync("/api/proveedores", new { nombre });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ProveedorDto>())!;
    }

    public static Task<HttpResponseMessage> PostArticuloAsync(
        this HttpClient cliente, int proveedorId, string codigo, decimal costo, decimal margen, string descripcion = "Armazón metal") =>
        cliente.PostAsJsonAsync("/api/articulos", new { proveedorId, codigoProveedor = codigo, descripcion, precioCosto = costo, margen });

    public static async Task<ArticuloDto> CrearArticuloAsync(
        this HttpClient cliente, int proveedorId, string codigo, decimal costo, decimal margen, string descripcion = "Armazón metal")
    {
        var r = await cliente.PostArticuloAsync(proveedorId, codigo, costo, margen, descripcion);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ArticuloDto>())!;
    }
}
