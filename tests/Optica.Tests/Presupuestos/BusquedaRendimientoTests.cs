using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;

namespace Optica.Tests.Presupuestos;

public class BusquedaRendimientoTests
{
    private static readonly string[] Apellidos = ["González", "Gómez", "Pérez", "Rodríguez", "Fernández", "López", "Martínez", "Sánchez"];

    [Fact]
    [Trait("Categoria", "Rendimiento")]
    public async Task Con_10000_presupuestos_19_de_20_busquedas_por_apellido_tardan_menos_de_2_segundos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
            var inicio = new DateOnly(2024, 1, 1);
            db.AddRange(Enumerable.Range(1, 10_000).Select(i => Presupuesto.Nuevo(
                i, inicio.AddDays(i % 900), Apellidos[i % Apellidos.Length], "Cliente", $"{20_000_000 + i}", null, null, null)));
            await db.SaveChangesAsync();
        }

        var tiempos = new List<TimeSpan>();
        for (var i = 0; i < 20; i++)
        {
            var reloj = Stopwatch.StartNew();
            var r = await cliente.GetAsync($"/api/presupuestos?apellido={Uri.EscapeDataString(Apellidos[i % Apellidos.Length][..4])}");
            await r.Content.ReadAsByteArrayAsync();
            tiempos.Add(reloj.Elapsed);
            r.EnsureSuccessStatusCode();
        }

        Assert.True(tiempos.Count(t => t < TimeSpan.FromSeconds(2)) >= 19, string.Join(", ", tiempos.Select(t => $"{t.TotalMilliseconds:N0} ms")));
    }
}
