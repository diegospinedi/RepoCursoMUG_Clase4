using System.Net;
using System.Net.Http.Json;

namespace Optica.Tests.Acceso;

public class IngresoTests
{
    private const string Clave = "clave-segura";

    private static async Task<HttpClient> DefinidaAsync(AppDePrueba app)
    {
        var cliente = app.CreateClient();
        await cliente.PostAsJsonAsync("/api/acceso/definir", new { contrasena = Clave });
        return cliente;
    }

    [Fact]
    public async Task Con_la_contrasena_correcta_inicia_sesion()
    {
        await using var app = new AppDePrueba();
        var cliente = await DefinidaAsync(app);

        var r = await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Clave });

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.True((await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado"))!.SesionIniciada);
    }

    [Fact]
    public async Task Con_una_contrasena_incorrecta_responde_401()
    {
        await using var app = new AppDePrueba();
        var cliente = await DefinidaAsync(app);
        var r = await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "otra-cosa" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Cinco_fallos_bloquean_5_minutos_aun_con_la_correcta_y_despues_se_libera_solo()
    {
        await using var app = new AppDePrueba();
        var cliente = await DefinidaAsync(app);
        for (var i = 0; i < 5; i++)
            await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "incorrecta" });

        var bloqueado = await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Clave });
        Assert.Equal((HttpStatusCode)423, bloqueado.StatusCode);

        app.Reloj.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal((HttpStatusCode)423, (await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Clave })).StatusCode);

        app.Reloj.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Clave })).StatusCode);
    }

    [Fact]
    public async Task Cuatro_fallos_y_un_acierto_reinician_el_contador()
    {
        await using var app = new AppDePrueba();
        var cliente = await DefinidaAsync(app);
        for (var i = 0; i < 4; i++)
            await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "incorrecta" });
        await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Clave });
        for (var i = 0; i < 4; i++)
            await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "incorrecta" });

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Clave })).StatusCode);
    }

    [Fact]
    public async Task Los_fallos_y_el_bloqueo_quedan_en_el_registro_de_seguridad_sin_la_contrasena()
    {
        await using var app = new AppDePrueba();
        var cliente = await DefinidaAsync(app);
        for (var i = 0; i < 5; i++)
            await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "intento-erroneo" });

        var registro = await File.ReadAllTextAsync(app.RutaRegistroSeguridad);
        Assert.Contains("2026-10-09", registro);
        Assert.Equal(5, registro.Split('\n').Count(l => l.Contains("Ingreso fallido")));
        Assert.Contains("Acceso bloqueado", registro);
        Assert.DoesNotContain("intento-erroneo", registro);
        Assert.DoesNotContain(Clave, registro);
    }
}
