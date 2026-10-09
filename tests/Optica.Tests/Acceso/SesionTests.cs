using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Optica.Tests.Acceso;

public class SesionTests
{
    [Fact]
    public async Task Sesenta_minutos_sin_actividad_cierran_la_sesion()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();

        app.Reloj.Advance(TimeSpan.FromMinutes(61));

        Assert.False((await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
    }

    [Fact]
    public async Task Un_pedido_antes_de_los_60_minutos_renueva_la_sesion()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();

        app.Reloj.Advance(TimeSpan.FromMinutes(59));
        Assert.True((await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
        app.Reloj.Advance(TimeSpan.FromMinutes(59));
        Assert.True((await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
    }

    [Fact]
    public async Task Sin_sesion_todo_endpoint_de_la_api_responde_401_sin_datos()
    {
        await using var app = new AppDePrueba();
        var cliente = app.CreateClient();
        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is { } ruta && ruta.StartsWith("/api/") && !ruta.StartsWith("/api/acceso/"))
            .ToList();

        foreach (var endpoint in endpoints)
        {
            var ruta = System.Text.RegularExpressions.Regex.Replace(endpoint.RoutePattern.RawText!, "{[^}]+}", "1");
            foreach (var metodo in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
            {
                var r = await cliente.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), ruta));
                Assert.True(r.StatusCode == HttpStatusCode.Unauthorized, $"{metodo} {ruta} respondió {(int)r.StatusCode}");
                Assert.Empty(await r.Content.ReadAsByteArrayAsync());
            }
        }
    }

    [Fact]
    public async Task La_pantalla_inicial_carga_sin_sesion()
    {
        await using var app = new AppDePrueba();
        var r = await app.CreateClient().GetAsync("/");
        Assert.NotEqual(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Salir_cierra_la_sesion()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync("/api/acceso/salir", null)).StatusCode);
        Assert.False((await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
    }
}
