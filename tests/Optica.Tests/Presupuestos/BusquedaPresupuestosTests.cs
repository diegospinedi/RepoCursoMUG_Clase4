using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;

namespace Optica.Tests.Presupuestos;

public sealed record PresupuestoResumen(int Id, int Numero, DateOnly Fecha, string Estado, string Apellido, string Nombre, string Dni, decimal Total);

public static class SiembraPresupuestos
{
    /// <summary>Inserta presupuestos con la fecha indicada (el reloj de prueba no retrocede).</summary>
    public static async Task SembrarAsync(this AppDePrueba app, params (int Numero, string Fecha, string Apellido, string Nombre, string Dni)[] filas)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        foreach (var f in filas)
            db.Add(Presupuesto.Nuevo(f.Numero, DateOnly.Parse(f.Fecha), f.Apellido, f.Nombre, f.Dni, null, null, null));
        await db.SaveChangesAsync();
    }
}

public class BusquedaPresupuestosTests
{
    private static async Task<int[]> BuscarAsync(HttpClient cliente, string consulta) =>
        (await cliente.GetFromJsonAsync<PresupuestoResumen[]>($"/api/presupuestos?{consulta}"))!.Select(p => p.Numero).Order().ToArray();

    [Fact]
    public async Task Busca_el_apellido_por_coincidencia_parcial_sin_mayusculas_ni_acentos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await app.SembrarAsync((1, "2026-03-10", "González", "Ana", "23456789"), (2, "2026-03-10", "Gómez", "Luis", "30111222"));

        Assert.Equal(new[] { 1 }, await BuscarAsync(cliente, "apellido=ONZALEZ"));
        Assert.Equal(new[] { 1 }, await BuscarAsync(cliente, "apellido=gonzalez"));
    }

    [Fact]
    public async Task Combina_apellido_con_fecha_desde_sin_hasta()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await app.SembrarAsync((1, "2026-03-10", "González", "Ana", "23456789"), (2, "2026-03-20", "González", "Ana", "23456789"),
            (3, "2026-03-20", "Gómez", "Luis", "30111222"));

        Assert.Equal(new[] { 2 }, await BuscarAsync(cliente, "apellido=González&desde=2026-03-15"));
    }

    [Fact]
    public async Task El_rango_de_fechas_incluye_ambos_limites()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await app.SembrarAsync((1, "2026-03-09", "A", "A", "1111111"), (2, "2026-03-10", "B", "B", "2222222"),
            (3, "2026-03-20", "C", "C", "3333333"), (4, "2026-03-21", "D", "D", "4444444"));

        Assert.Equal(new[] { 2, 3 }, await BuscarAsync(cliente, "desde=2026-03-10&hasta=2026-03-20"));
        Assert.Equal(new[] { 1, 2, 3 }, await BuscarAsync(cliente, "hasta=2026-03-20"));
    }

    [Fact]
    public async Task Busca_el_DNI_por_coincidencia_parcial_ignorando_puntos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await app.SembrarAsync((1, "2026-03-10", "González", "Ana", "23456789"), (2, "2026-03-10", "Gómez", "Luis", "30111222"));

        Assert.Equal(new[] { 1 }, await BuscarAsync(cliente, "dni=3456"));
        Assert.Equal(new[] { 1 }, await BuscarAsync(cliente, "dni=23.456"));
    }

    [Fact]
    public async Task Busca_por_nombre_y_devuelve_todos_sin_filtros()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await app.SembrarAsync((1, "2026-03-10", "González", "Ana María", "23456789"), (2, "2026-03-10", "Gómez", "Luis", "30111222"));

        Assert.Equal(new[] { 1 }, await BuscarAsync(cliente, "nombre=maria"));
        Assert.Equal(new[] { 1, 2 }, await BuscarAsync(cliente, ""));
    }
}
