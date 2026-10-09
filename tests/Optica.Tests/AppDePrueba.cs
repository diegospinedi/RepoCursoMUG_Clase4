using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Optica.Api.Arca;

namespace Optica.Tests;

/// <summary>
/// Host de prueba: base SQLite temporal por instancia, reloj falso, espía de ARCA y la IP remota que se
/// pida (loopback por defecto) para probar las acciones que solo se permiten desde la PC del sistema.
/// </summary>
public sealed class AppDePrueba : WebApplicationFactory<Program>
{
    /// <summary>Encabezado que solo entiende el host de prueba para fijar la IP remota del pedido.</summary>
    public const string EncabezadoIp = "X-Prueba-IP";

    public static readonly IPAddress IpRemota = IPAddress.Parse("192.168.1.50");

    /// <summary>Contraseña que usa <see cref="IngresarAsync"/>.</summary>
    public const string Contrasena = "clave-de-prueba";

    private readonly string _base = Path.Combine(Path.GetTempPath(), $"optica-{Guid.NewGuid():N}.db");
    private readonly Dictionary<string, string?> _ajustes = [];
    private readonly List<Action<IServiceCollection>> _servicios = [];

    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 10, 9, 10, 0, 0, TimeSpan.FromHours(-3)));

    public EspiaArca Arca { get; } = new();

    public string RutaBase => _base;

    public string RutaRegistroSeguridad => Path.ChangeExtension(_base, ".seguridad.log");

    /// <summary>Configuración adicional (por ejemplo "Arca:TiempoEsperaSegundos").</summary>
    public AppDePrueba Con(string clave, string? valor)
    {
        _ajustes[clave] = valor;
        return this;
    }

    /// <summary>Servicios adicionales (por ejemplo, interceptores de EF Core para simular fallas).</summary>
    public AppDePrueba ConServicios(Action<IServiceCollection> configurar)
    {
        _servicios.Add(configurar);
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Optica", $"Data Source={_base}");
        builder.UseSetting("Arca:TiempoEsperaSegundos", "1");
        builder.UseSetting("RegistroSeguridad:Archivo", RutaRegistroSeguridad);
        foreach (var (clave, valor) in _ajustes) builder.UseSetting(clave, valor);

        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(Reloj);
            servicios.RemoveAll<IServicioArca>();
            servicios.AddSingleton<IServicioArca>(Arca);
            servicios.AddSingleton<IStartupFilter, FiltroIpDePrueba>();
            foreach (var configurar in _servicios) configurar(servicios);
        });
    }

    /// <summary>Cliente con sesión iniciada; define la contraseña si todavía no está definida.</summary>
    public async Task<HttpClient> IngresarAsync()
    {
        var cliente = CreateClient();
        await cliente.PostAsJsonAsync("/api/acceso/definir", new { contrasena = Contrasena });
        var r = await cliente.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = Contrasena });
        if (r.StatusCode != HttpStatusCode.NoContent)
            throw new InvalidOperationException($"No se pudo ingresar: {(int)r.StatusCode}");
        return cliente;
    }

    /// <summary>Cliente cuyos pedidos llegan desde otro equipo de la red local.</summary>
    public HttpClient ClienteRemoto()
    {
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Add(EncabezadoIp, IpRemota.ToString());
        return cliente;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        foreach (var archivo in new[] { _base, _base + "-wal", _base + "-shm", RutaRegistroSeguridad })
            if (File.Exists(archivo)) File.Delete(archivo);
    }

    private sealed class FiltroIpDePrueba : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
        {
            app.Use((contexto, continuar) =>
            {
                contexto.Connection.RemoteIpAddress =
                    contexto.Request.Headers.TryGetValue(EncabezadoIp, out var ip) ? IPAddress.Parse(ip!) : IPAddress.Loopback;
                contexto.Request.Headers.Remove(EncabezadoIp);
                return continuar(contexto);
            });
            siguiente(app);
        };
    }
}
