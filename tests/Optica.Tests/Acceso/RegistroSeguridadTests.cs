using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Optica.Api.Arca;
using Optica.Tests.Catalogo;
using Optica.Tests.Facturacion;
using Optica.Tests.Planillas;

namespace Optica.Tests.Acceso;

public class RegistroSeguridadTests
{
    [Fact]
    public async Task Registra_los_eventos_con_fecha_y_hora_sin_contrasenas_ni_datos_de_clientes()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        await cliente.FacturarBienAsync(p.Id);
        var otro = app.CreateClient();
        for (var i = 0; i < 5; i++) await otro.PostAsJsonAsync("/api/acceso/ingresar", new { contrasena = "secreto-equivocado" });
        await cliente.ConfigurarAsync(multiplo: 50m);
        await cliente.PostAsJsonAsync("/api/acceso/cambiar", new { actual = AppDePrueba.Contrasena, nueva = "nueva-clave-segura" });
        await app.CreateClient().PostAsJsonAsync("/api/acceso/restablecer", new { nueva = "restablecida-segura" });

        var lineas = (await File.ReadAllLinesAsync(app.RutaRegistroSeguridad)).Where(l => l.Length > 0).ToArray();

        Assert.All(lineas, l => Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2} ", l));
        var texto = string.Join('\n', lineas);
        foreach (var evento in new[] { "Ingreso fallido", "Acceso bloqueado", "Configuración modificada", "Contraseña cambiada", "Contraseña restablecida" })
            Assert.Contains(evento, texto);
        foreach (var dato in new[] { "secreto-equivocado", "nueva-clave-segura", "restablecida-segura", AppDePrueba.Contrasena, "González", "Ana", "23456789", "23.456.789" })
            Assert.DoesNotContain(dato, texto);
    }

    [Fact]
    public async Task Los_errores_de_ARCA_y_de_importacion_no_exponen_datos_internos_ni_de_clientes()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.Rechazo = new Rechazado("10016", "Número de comprobante inválido.");

        var rechazo = await (await cliente.FacturarAsync(p.Id)).Content.ReadAsStringAsync();
        var formato = await (await cliente.ImportarAsync(proveedorId: 1, "no es un excel"u8.ToArray())).Content.ReadAsStringAsync();

        foreach (var cuerpo in new[] { rechazo, formato })
        {
            Assert.DoesNotContain("González", cuerpo);
            Assert.DoesNotContain("23456789", cuerpo);
            Assert.DoesNotMatch(new Regex(@"\bat [A-Z][\w.]+\(|Exception|StackTrace|\.cs:line", RegexOptions.IgnoreCase), cuerpo);
        }
        Assert.Contains("10016", rechazo);
        Assert.Contains("Número de comprobante inválido.", rechazo);
    }
}
