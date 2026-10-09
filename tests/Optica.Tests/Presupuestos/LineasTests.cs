using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Catalogo;
using Optica.Api.Datos;
using Optica.Tests.Catalogo;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Presupuestos;

public class LineasTests
{
    private static async Task<Dictionary<string, string[]>> ErroresAsync(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ToDictionary();
    }

    private static async Task<int> ArticuloConPrecioAsync(AppDePrueba app, decimal precioVenta)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        var proveedorId = db.Set<Proveedor>().Select(p => p.Id).First();
        var a = Articulo.Nuevo(proveedorId, $"P-{precioVenta}", "Especial", precioVenta, 0m, precioVenta);
        db.Add(a);
        await db.SaveChangesAsync();
        return a.Codigo;
    }

    [Fact]
    public async Task La_linea_nueva_toma_descripcion_y_precio_del_catalogo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();

        var r = await cliente.PostAsJsonAsync("/api/presupuestos", new
        {
            estado = "Borrador", cliente = Cliente(),
            lineas = new[] { new { articuloCodigo = a.Codigo, cantidad = 1, descuento = 0, precioUnitario = 1, descripcion = "otra" } },
        });

        var l = Assert.Single((await r.Content.ReadFromJsonAsync<PresupuestoDto>())!.Lineas);
        Assert.Equal((a.Codigo, "Armazón metal", 1815m), (l.ArticuloCodigo, l.Descripcion, l.PrecioUnitario));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2.5)]
    [InlineData(10000)]
    public async Task Rechaza_cantidades_que_no_son_enteros_de_1_a_9999(double cantidad)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(), Linea(a.Codigo, 1), Linea(a.Codigo, cantidad)));
        Assert.StartsWith("La cantidad debe ser un número entero mayor a 0", errores["lineas[1].cantidad"][0]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Rechaza_descuentos_fuera_de_0_a_100(decimal descuento)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(), Linea(a.Codigo, 1, descuento)));
        Assert.Equal("El descuento debe estar entre 0 y 100", errores["lineas[0].descuento"][0]);
    }

    [Fact]
    public async Task Rechaza_un_descuento_con_3_decimales()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(), Linea(a.Codigo, 1, 10.125m)));
        Assert.Contains("2 decimales", errores["lineas[0].descuento"][0]);
    }

    [Fact]
    public async Task Rechaza_un_total_mayor_al_maximo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PrepararCatalogoAsync();
        var caro = await ArticuloConPrecioAsync(app, 200_000_000m);
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(), Linea(caro, 5)));
        Assert.Contains("999.999.999,99", errores["total"][0]);
    }

    [Fact]
    public async Task Rechaza_un_articulo_con_precio_de_venta_negativo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PrepararCatalogoAsync();
        var negativo = await ArticuloConPrecioAsync(app, -10m);
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(), Linea(negativo, 1)));
        Assert.Equal("El precio unitario no puede ser negativo", errores["lineas[0].precioUnitario"][0]);
    }

    [Fact]
    public async Task Acepta_un_articulo_con_precio_de_venta_0()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PrepararCatalogoAsync();
        var gratis = await ArticuloConPrecioAsync(app, 0m);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostPresupuestoAsync(Cliente(), Linea(gratis, 1))).StatusCode);
    }

    [Fact]
    public async Task Rechaza_un_articulo_inexistente()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(), Linea(999, 1)));
        Assert.True(errores.ContainsKey("lineas[0].articuloCodigo"));
    }

    [Fact]
    public async Task Los_importes_del_servidor_coinciden_con_los_vectores()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PrepararCatalogoAsync();
        var mil = await ArticuloConPrecioAsync(app, 1000m);
        var p667 = await ArticuloConPrecioAsync(app, 6.67m);
        var p900 = await ArticuloConPrecioAsync(app, 900m);

        var p = await cliente.CrearPresupuestoAsync(Cliente(),
            Linea(mil, 1, 0), Linea(mil, 1, 10), Linea(p667, 3, 50), Linea(p900, 3, 0));

        Assert.Equal([(1000m, 1000m), (900m, 900m), (3.34m, 10.02m), (900m, 2700m)],
            p.Lineas.OrderBy(l => l.Orden).Select(l => (l.PrecioConDescuento, l.PrecioFinal)));
        Assert.Equal(4610.02m, p.Total);
    }

    [Fact]
    public async Task La_linea_grabada_conserva_su_precio_aunque_el_catalogo_cambie()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearPresupuestoAsync(Cliente(), Linea(a.Codigo, 1));

        await cliente.ConfigurarAsync(multiplo: 50m);
        var r = await cliente.PutPresupuestoAsync(p.Id, "Borrador", Cliente(), Linea(a.Codigo, 2, id: p.Lineas[0].Id));

        var l = Assert.Single((await r.Content.ReadFromJsonAsync<PresupuestoDto>())!.Lineas);
        Assert.Equal((1815m, 2, 3630m), (l.PrecioUnitario, l.Cantidad, l.PrecioFinal));
    }
}
