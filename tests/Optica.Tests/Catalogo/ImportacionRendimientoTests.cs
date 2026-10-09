using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Catalogo;
using Optica.Api.Datos;
using Optica.Tests.Planillas;

namespace Optica.Tests.Catalogo;

public class ImportacionRendimientoTests
{
    [Fact]
    [Trait("Categoria", "Rendimiento")]
    public async Task Importar_10000_filas_sobre_10000_articulos_tarda_menos_de_120_segundos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync();
        var p = await cliente.CrearProveedorAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
            db.AddRange(Enumerable.Range(0, 10_000).Select(i => Articulo.Nuevo(p.Id, $"C-{i}", "Lente", 1210m, 50m, 1815m)));
            await db.SaveChangesAsync();
        }
        var planilla = GeneradorPlanillas.Crear(Enumerable.Range(0, 10_000).Select(i => new object?[] { $"C-{i}", null, 2420m }));

        var reloj = Stopwatch.StartNew();
        var resumen = await cliente.ImportarBienAsync(p.Id, planilla);
        reloj.Stop();

        Assert.Equal(10_000, resumen.Actualizados);
        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(120), $"Tardó {reloj.Elapsed.TotalSeconds:N1} s");
    }
}
