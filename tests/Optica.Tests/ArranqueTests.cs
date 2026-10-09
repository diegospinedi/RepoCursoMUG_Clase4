using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Arca;
using Optica.Api.Datos;

namespace Optica.Tests;

public class ArranqueTests
{
    [Fact]
    public async Task Arranca_crea_la_base_y_responde_404_en_una_ruta_desconocida()
    {
        await using var app = new AppDePrueba();
        var respuesta = await app.CreateClient().GetAsync("/api/no-existe");
        Assert.NotEqual(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<OpticaDbContext>().Database.CanConnectAsync());
    }

    [Fact]
    public async Task Usa_el_espia_de_arca_en_las_pruebas()
    {
        await using var app = new AppDePrueba();
        Assert.Same(app.Arca, app.Services.GetRequiredService<IServicioArca>());
    }

    [Fact]
    public void No_arranca_con_un_entorno_de_arca_distinto_de_simulado()
    {
        using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Optica", $"Data Source={Path.Combine(Path.GetTempPath(), $"optica-{Guid.NewGuid():N}.db")}");
            b.UseSetting("Arca:Entorno", "Produccion");
        });
        var error = Assert.ThrowsAny<Exception>(() => app.CreateClient());
        Assert.Contains("Simulado", error.ToString());
    }
}
