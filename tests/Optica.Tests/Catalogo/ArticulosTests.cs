using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Optica.Tests.Catalogo;

public class ArticulosTests
{
    private static async Task<(AppDePrueba App, HttpClient Cliente, ProveedorDto Proveedor)> PrepararAsync(
        decimal alicuota = 21m, string condicion = "ResponsableInscripto")
    {
        var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync(alicuota: alicuota, condicion: condicion);
        return (app, cliente, await cliente.CrearProveedorAsync());
    }

    private static async Task<Dictionary<string, string[]>> ErroresAsync(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ToDictionary();
    }

    [Fact]
    public async Task El_alta_asigna_codigo_autonumerico_y_precio_de_venta()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var a1 = await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1000m, 50m);
        var a2 = await cliente.CrearArticuloAsync(p.Id, "ABC-2", 1000m, 50m);
        Assert.Equal(a1.Codigo + 1, a2.Codigo);
        Assert.Equal(1500m, a1.PrecioVenta);
        Assert.Equal("Lentes SA", a1.Proveedor);
    }

    [Theory]
    [InlineData(21, "ResponsableInscripto", 1210, 50, 1815)]
    [InlineData(21, "Monotributo", 1210, 50, 1815)]
    [InlineData(10.5, "ResponsableInscripto", 1105, 50, 1657.50)]
    [InlineData(21, "ResponsableInscripto", 1210, 60, 1936)]
    [InlineData(21, "ResponsableInscripto", 2420, 50, 3630)]
    public async Task Calcula_el_precio_de_venta_de_los_ejemplos_del_PRD(decimal alicuota, string condicion, decimal costo, decimal margen, decimal esperado)
    {
        var (app, cliente, p) = await PrepararAsync(alicuota, condicion);
        await using var _ = app;
        Assert.Equal(esperado, (await cliente.CrearArticuloAsync(p.Id, "ABC-1", costo, margen)).PrecioVenta);
    }

    [Fact]
    public async Task Modificar_margen_o_costo_recalcula_el_precio()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var a = await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m);

        var r = await cliente.PutAsJsonAsync($"/api/articulos/{a.Codigo}", new { proveedorId = p.Id, codigoProveedor = "ABC-1", descripcion = "Armazón metal", precioCosto = 1210m, margen = 60m });
        Assert.Equal(1936m, (await r.Content.ReadFromJsonAsync<ArticuloDto>())!.PrecioVenta);

        r = await cliente.PutAsJsonAsync($"/api/articulos/{a.Codigo}", new { proveedorId = p.Id, codigoProveedor = "ABC-1", descripcion = "Armazón metal", precioCosto = 2420m, margen = 50m });
        Assert.Equal(3630m, (await r.Content.ReadFromJsonAsync<ArticuloDto>())!.PrecioVenta);
    }

    [Fact]
    public async Task El_precio_de_venta_enviado_se_ignora()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var r = await cliente.PostAsJsonAsync("/api/articulos", new { proveedorId = p.Id, codigoProveedor = "ABC-1", descripcion = "X", precioCosto = 1000m, margen = 50m, precioVenta = 1m });
        Assert.Equal(1500m, (await r.Content.ReadFromJsonAsync<ArticuloDto>())!.PrecioVenta);
    }

    [Fact]
    public async Task Rechaza_un_margen_mayor_a_1000()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var errores = await ErroresAsync(await cliente.PostArticuloAsync(p.Id, "ABC-1", 1000m, 1000.01m));
        Assert.Contains("entre 0 y 1000", errores["margen"][0]);
    }

    [Fact]
    public async Task Rechaza_un_costo_negativo_cargado_a_mano()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var errores = await ErroresAsync(await cliente.PostArticuloAsync(p.Id, "ABC-1", -1m, 50m));
        Assert.True(errores.ContainsKey("precioCosto"));
    }

    [Fact]
    public async Task Rechaza_costo_o_margen_con_3_decimales()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var errores = await ErroresAsync(await cliente.PostArticuloAsync(p.Id, "ABC-1", 1000.001m, 50.125m));
        Assert.Contains("como máximo 2 decimales", errores["precioCosto"][0]);
        Assert.Contains("como máximo 2 decimales", errores["margen"][0]);
    }

    [Fact]
    public async Task Rechaza_campos_obligatorios_vacios_y_proveedor_inexistente()
    {
        var (app, cliente, _) = await PrepararAsync();
        await using var __ = app;
        var errores = await ErroresAsync(await cliente.PostAsJsonAsync("/api/articulos", new { proveedorId = 99, codigoProveedor = "", descripcion = "", precioCosto = 1m, margen = 1m }));
        Assert.True(errores.ContainsKey("proveedorId"));
        Assert.True(errores.ContainsKey("codigoProveedor"));
        Assert.True(errores.ContainsKey("descripcion"));
    }

    [Fact]
    public async Task El_codigo_es_unico_por_proveedor_y_distingue_mayusculas()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var otro = await cliente.CrearProveedorAsync("Ópticos SRL");
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1000m, 50m);

        var errores = await ErroresAsync(await cliente.PostArticuloAsync(p.Id, "ABC-1", 1000m, 50m));
        Assert.True(errores.ContainsKey("codigoProveedor"));
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostArticuloAsync(p.Id, "abc-1", 1000m, 50m)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostArticuloAsync(otro.Id, "ABC-1", 1000m, 50m)).StatusCode);
    }

    [Fact]
    public async Task Busca_por_codigo_codigo_en_el_proveedor_y_descripcion()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var a = await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1000m, 50m, "Armazón metal dorado");
        await cliente.CrearArticuloAsync(p.Id, "XYZ-9", 1000m, 50m, "Cristal orgánico");

        Assert.Equal([a.Codigo], (await cliente.GetFromJsonAsync<ArticuloDto[]>("/api/articulos?texto=armazon"))!.Select(x => x.Codigo));
        Assert.Equal([a.Codigo], (await cliente.GetFromJsonAsync<ArticuloDto[]>("/api/articulos?texto=ABC"))!.Select(x => x.Codigo));
        Assert.Equal([a.Codigo], (await cliente.GetFromJsonAsync<ArticuloDto[]>($"/api/articulos?texto={a.Codigo}"))!.Select(x => x.Codigo));
    }

    [Fact]
    public async Task La_busqueda_devuelve_como_maximo_50()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        for (var i = 0; i < 55; i++) await cliente.CrearArticuloAsync(p.Id, $"A-{i}", 100m, 10m, "Lente");
        Assert.Equal(50, (await cliente.GetFromJsonAsync<ArticuloDto[]>("/api/articulos?texto=lente"))!.Length);
    }

    [Fact]
    public async Task Un_articulo_inexistente_responde_404()
    {
        var (app, cliente, _) = await PrepararAsync();
        await using var __ = app;
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/articulos/999")).StatusCode);
    }
}
