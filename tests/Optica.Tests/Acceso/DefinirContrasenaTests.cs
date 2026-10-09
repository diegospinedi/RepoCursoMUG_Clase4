using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace Optica.Tests.Acceso;

public class DefinirContrasenaTests
{
    [Fact]
    public async Task Una_contrasena_de_7_caracteres_se_rechaza_indicando_el_minimo()
    {
        await using var app = new AppDePrueba();
        var r = await app.CreateClient().PostAsJsonAsync("/api/acceso/definir", new { contrasena = "1234567" });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("8 caracteres", Assert.Single(problema!.Errors["contrasena"]));
    }

    [Fact]
    public async Task Se_define_desde_la_pc_local_y_queda_definida()
    {
        await using var app = new AppDePrueba();
        var cliente = app.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync("/api/acceso/definir", new { contrasena = "clave-segura" })).StatusCode);

        var estado = await cliente.GetFromJsonAsync<EstadoAcceso>("/api/acceso/estado");
        Assert.True(estado!.Definida);
    }

    [Fact]
    public async Task Desde_otro_equipo_de_la_red_se_rechaza_con_403()
    {
        await using var app = new AppDePrueba();
        var r = await app.ClienteRemoto().PostAsJsonAsync("/api/acceso/definir", new { contrasena = "clave-segura" });

        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("solo-pc-local", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
    }

    [Fact]
    public async Task Si_ya_esta_definida_se_rechaza_con_409()
    {
        await using var app = new AppDePrueba();
        var cliente = app.CreateClient();
        await cliente.PostAsJsonAsync("/api/acceso/definir", new { contrasena = "clave-segura" });
        var r = await cliente.PostAsJsonAsync("/api/acceso/definir", new { contrasena = "otra-clave-larga" });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task La_contrasena_no_queda_en_texto_plano_en_la_base_ni_en_la_configuracion()
    {
        const string contrasena = "clave-muy-secreta-AC80";
        await using var app = new AppDePrueba();
        await app.CreateClient().PostAsJsonAsync("/api/acceso/definir", new { contrasena });
        SqliteConnection.ClearAllPools();

        var archivos = new[] { app.RutaBase, app.RutaBase + "-wal" }
            .Concat(Directory.GetFiles(AppContext.BaseDirectory, "appsettings*.json"))
            .Where(File.Exists);
        foreach (var archivo in archivos)
        {
            var contenido = await File.ReadAllBytesAsync(archivo);
            Assert.DoesNotContain(contrasena, Encoding.UTF8.GetString(contenido));
            Assert.DoesNotContain(contrasena, Encoding.Unicode.GetString(contenido));
        }
    }
}

public sealed record EstadoAcceso(bool Definida, bool SesionIniciada, DateTimeOffset? BloqueadoHasta);
