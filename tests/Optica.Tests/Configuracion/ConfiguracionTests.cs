using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Optica.Tests.Catalogo;

namespace Optica.Tests.Configuracion;

public class ConfiguracionTests
{
    private static object Pedido(decimal alicuota = 21m, decimal multiplo = 0.01m, decimal tope = 10_000_000m, decimal? margen = 50m,
        string condicion = "ResponsableInscripto") =>
        new { alicuotaIva = alicuota, condicionFiscal = condicion, topeIdentificacion = tope, multiploRedondeo = multiplo, margenPredeterminado = margen };

    private static async Task<Dictionary<string, string[]>> ErroresAsync(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ToDictionary();
    }

    [Fact]
    public async Task Arranca_con_RI_IVA_21_tope_10_millones_multiplo_0_01_y_sin_margen_predeterminado()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var c = await cliente.GetFromJsonAsync<ConfiguracionDto>("/api/configuracion");
        Assert.Equal(new ConfiguracionDto(21m, "ResponsableInscripto", 10_000_000m, 0.01m, null), c);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(19.5)]
    [InlineData(-1)]
    public async Task Rechaza_alicuotas_que_ARCA_no_acepta(decimal alicuota)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var errores = await ErroresAsync(await cliente.PutAsJsonAsync("/api/configuracion", Pedido(alicuota: alicuota)));
        Assert.Contains("0; 2,5; 5; 10,5; 21; 27", Assert.Single(errores["alicuotaIva"]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2.5)]
    [InlineData(10.5)]
    [InlineData(27)]
    public async Task Acepta_las_alicuotas_de_ARCA(decimal alicuota)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        Assert.Equal(alicuota, (await cliente.ConfigurarAsync(alicuota: alicuota)).AlicuotaIva);
    }

    [Fact]
    public async Task Rechaza_un_multiplo_menor_a_0_01_indicando_el_minimo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var errores = await ErroresAsync(await cliente.PutAsJsonAsync("/api/configuracion", Pedido(multiplo: 0.001m)));
        Assert.Contains("0,01", errores["multiploRedondeo"][0]);
    }

    [Fact]
    public async Task Rechaza_un_multiplo_mayor_a_1000()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var errores = await ErroresAsync(await cliente.PutAsJsonAsync("/api/configuracion", Pedido(multiplo: 1000.01m)));
        Assert.True(errores.ContainsKey("multiploRedondeo"));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1000.01)]
    public async Task Rechaza_un_margen_predeterminado_fuera_de_0_a_1000(decimal margen)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var errores = await ErroresAsync(await cliente.PutAsJsonAsync("/api/configuracion", Pedido(margen: margen)));
        Assert.True(errores.ContainsKey("margenPredeterminado"));
    }

    [Fact]
    public async Task Acepta_el_margen_predeterminado_sin_configurar()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        Assert.Null((await cliente.ConfigurarAsync(margenPredeterminado: null)).MargenPredeterminado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000000000)]
    public async Task Rechaza_un_tope_fuera_de_rango(decimal tope)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var errores = await ErroresAsync(await cliente.PutAsJsonAsync("/api/configuracion", Pedido(tope: tope)));
        Assert.True(errores.ContainsKey("topeIdentificacion"));
    }

    [Fact]
    public async Task Rechaza_valores_con_mas_de_2_decimales()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var errores = await ErroresAsync(await cliente.PutAsJsonAsync("/api/configuracion", Pedido(tope: 1000.005m, margen: 10.123m)));
        Assert.Contains("2 decimales", errores["topeIdentificacion"][0]);
        Assert.Contains("2 decimales", errores["margenPredeterminado"][0]);
    }

    [Fact]
    public async Task Cambiar_el_multiplo_recalcula_el_catalogo_sin_editar_los_articulos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync();
        var proveedor = await cliente.CrearProveedorAsync();
        var articulo = await cliente.CrearArticuloAsync(proveedor.Id, "ABC-1", 1210m, 50m);
        Assert.Equal(1815m, articulo.PrecioVenta);

        var c = await cliente.ConfigurarAsync(multiplo: 50m);

        Assert.Equal(1, c.ArticulosRecalculados);
        Assert.Equal(1850m, (await cliente.GetFromJsonAsync<ArticuloDto>($"/api/articulos/{articulo.Codigo}"))!.PrecioVenta);
    }

    [Fact]
    public async Task Cambiar_alicuota_o_condicion_fiscal_no_cambia_ningun_precio()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync();
        var proveedor = await cliente.CrearProveedorAsync();
        var articulo = await cliente.CrearArticuloAsync(proveedor.Id, "ABC-1", 1210m, 50m);

        var c = await cliente.ConfigurarAsync(alicuota: 10.5m, condicion: "Monotributo");

        Assert.Equal(0, c.ArticulosRecalculados);
        Assert.Equal(1815m, (await cliente.GetFromJsonAsync<ArticuloDto>($"/api/articulos/{articulo.Codigo}"))!.PrecioVenta);
    }

    [Fact]
    public async Task La_respuesta_no_expone_certificado_ni_punto_de_venta()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var texto = (await cliente.GetStringAsync("/api/configuracion")).ToLowerInvariant();
        Assert.DoesNotContain("certificado", texto);
        Assert.DoesNotContain("puntoventa", texto);
        Assert.DoesNotContain("punto", texto);
    }

    [Fact]
    public async Task El_cambio_queda_en_el_registro_de_seguridad()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync(multiplo: 50m);
        Assert.Contains("Configuración modificada", await File.ReadAllTextAsync(app.RutaRegistroSeguridad));
    }
}
