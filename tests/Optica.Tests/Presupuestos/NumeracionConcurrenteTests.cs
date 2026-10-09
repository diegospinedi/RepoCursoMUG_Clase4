using System.Net;
using System.Net.Http.Json;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Presupuestos;

public class NumeracionConcurrenteTests
{
    [Fact]
    public async Task Dos_sesiones_grabando_a_la_vez_obtienen_numeros_distintos_y_consecutivos()
    {
        await using var app = new AppDePrueba();
        var a = await (await app.IngresarAsync()).PrepararCatalogoAsync();
        var sesiones = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => app.IngresarAsync()));

        for (var ronda = 0; ronda < 10; ronda++)
        {
            var respuestas = await Task.WhenAll(sesiones.Select(s => s.PostPresupuestoAsync(Cliente(), Linea(a.Codigo, 1))));
            Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        }

        var numeros = new List<int>();
        for (var id = 1; id <= 20; id++) numeros.Add((await sesiones[0].PresupuestoAsync(id)).Numero);
        Assert.Equal(Enumerable.Range(1, 20), numeros.Order());
    }
}
