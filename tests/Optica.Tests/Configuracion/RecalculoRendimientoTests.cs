using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Catalogo;
using Optica.Api.Datos;
using Optica.Tests.Catalogo;

namespace Optica.Tests.Configuracion;

public class RecalculoRendimientoTests
{
    [Fact]
    [Trait("Categoria", "Rendimiento")]
    public async Task Recalcular_10000_articulos_tarda_menos_de_30_segundos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync();
        var proveedor = await cliente.CrearProveedorAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
            db.AddRange(Enumerable.Range(0, 10_000).Select(i => Articulo.Nuevo(proveedor.Id, $"C-{i}", "Lente", 1210m, 50m, 1815m)));
            await db.SaveChangesAsync();
        }

        var reloj = Stopwatch.StartNew();
        var c = await cliente.ConfigurarAsync(multiplo: 50m);
        reloj.Stop();

        Assert.Equal(10_000, c.ArticulosRecalculados);
        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(30), $"Tardó {reloj.Elapsed.TotalSeconds:N1} s");
    }
}
