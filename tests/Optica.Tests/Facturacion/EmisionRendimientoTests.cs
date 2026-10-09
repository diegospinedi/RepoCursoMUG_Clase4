using System.Diagnostics;
using Optica.Tests.Catalogo;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;
using Optica.Tests.Presupuestos;

namespace Optica.Tests.Facturacion;

public class EmisionRendimientoTests
{
    [Fact]
    [Trait("Categoria", "Rendimiento")]
    public async Task En_20_emisiones_al_menos_19_tienen_un_tiempo_propio_menor_a_2_segundos()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var presupuestos = new List<PresupuestoDto>();
        for (var i = 0; i < 20; i++) presupuestos.Add(await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1)));
        var demoraArca = TimeSpan.FromMilliseconds(300);
        app.Arca.Demora = demoraArca;

        var propios = new List<TimeSpan>();
        foreach (var p in presupuestos)
        {
            var reloj = Stopwatch.StartNew();
            await cliente.FacturarBienAsync(p.Id);
            propios.Add(reloj.Elapsed - demoraArca);
        }

        Assert.True(propios.Count(t => t < TimeSpan.FromSeconds(2)) >= 19, string.Join(", ", propios.Select(t => $"{t.TotalMilliseconds:N0} ms")));
    }
}
