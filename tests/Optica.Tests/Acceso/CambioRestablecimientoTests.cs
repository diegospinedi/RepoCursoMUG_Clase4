using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Optica.Tests.Acceso;

public class CambioRestablecimientoTests
{
    [Fact]
    public async Task Cambiar_con_la_actual_incorrecta_se_rechaza()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var r = await cliente.PostAsJsonAsync("/api/acceso/cambiar", new { actual = "no-es-esta", nueva = "nueva-clave-larga" });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.True((await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ContainsKey("actual"));
    }

    [Fact]
    public async Task Cambiar_con_una_nueva_corta_se_rechaza()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var r = await cliente.PostAsJsonAsync("/api/acceso/cambiar", new { actual = AppDePrueba.Contrasena, nueva = "corta" });
        Assert.True((await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ContainsKey("nueva"));
    }

    [Fact]
    public async Task Cambiar_deja_vigente_la_nueva_y_cierra_las_otras_sesiones()
    {
        await using var app = new AppDePrueba();
        var a = await app.IngresarAsync();
        var b = await app.IngresarAsync();

        var r = await a.PostAsJsonAsync("/api/acceso/cambiar", new { actual = AppDePrueba.Contrasena, nueva = "nueva-clave-larga" });

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.True((await a.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
        Assert.False((await b.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
        var c = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = AppDePrueba.Contrasena })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "nueva-clave-larga" })).StatusCode);
    }

    [Fact]
    public async Task Restablecer_desde_la_pc_local_no_pide_la_anterior_y_libera_el_bloqueo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var otro = app.CreateClient();
        for (var i = 0; i < 5; i++)
            await otro.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "incorrecta" });

        var r = await app.CreateClient().PostAsJsonAsync("/api/acceso/restablecer", new { nueva = "restablecida-123" });

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.False((await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
        Assert.Equal(HttpStatusCode.NoContent, (await otro.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "restablecida-123" })).StatusCode);
    }

    [Fact]
    public async Task Restablecer_desde_otro_equipo_se_rechaza_con_403()
    {
        await using var app = new AppDePrueba();
        await app.IngresarAsync();
        var r = await app.ClienteRemoto().PostAsJsonAsync("/api/acceso/restablecer", new { nueva = "restablecida-123" });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Cambiar_y_restablecer_quedan_en_el_registro_de_seguridad()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PostAsJsonAsync("/api/acceso/cambiar", new { actual = AppDePrueba.Contrasena, nueva = "nueva-clave-larga" });
        await app.CreateClient().PostAsJsonAsync("/api/acceso/restablecer", new { nueva = "restablecida-123" });

        var registro = await File.ReadAllTextAsync(app.RutaRegistroSeguridad);
        Assert.Contains("Contraseña cambiada", registro);
        Assert.Contains("Contraseña restablecida", registro);
        Assert.DoesNotContain("nueva-clave-larga", registro);
    }
}
