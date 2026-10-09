using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Presupuestos;

public class GrabarPresupuestoTests
{
    private static async Task<Dictionary<string, string[]>> ErroresAsync(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ToDictionary();
    }

    [Fact]
    public async Task Graba_en_Borrador_con_numero_1_y_guarda_los_seis_datos_del_cliente()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();

        var p = await cliente.CrearPresupuestoAsync(Cliente(), Linea(a.Codigo, 1));

        Assert.Equal((1, "Borrador"), (p.Numero, p.Estado));
        Assert.Equal(new ClienteDto("González", "Ana", "23456789", "Calle 42 n° 767", "ana@example.com", "221 555-1234"),
            (await cliente.PresupuestoAsync(p.Id)).Cliente);
        Assert.Equal(new DateOnly(2026, 10, 9), p.Fecha);
    }

    [Fact]
    public async Task Si_el_ultimo_es_154_el_nuevo_es_155()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
            var anterior = Presupuesto.Nuevo(154, new DateOnly(2026, 10, 1), "Pérez", "Juan", "12345678", null, null, null);
            db.Add(anterior);
            await db.SaveChangesAsync();
        }

        Assert.Equal(155, (await cliente.CrearPresupuestoAsync(Cliente(), Linea(a.Codigo, 1))).Numero);
    }

    [Fact]
    public async Task Sin_DNI_no_graba_e_indica_el_campo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(dni: ""), Linea(a.Codigo, 1)));
        Assert.Equal("Ingresá el DNI del cliente", Assert.Single(errores["cliente.dni"]));
    }

    [Fact]
    public async Task Sin_apellido_ni_nombre_no_graba_e_indica_cada_campo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(apellido: " ", nombre: ""), Linea(a.Codigo, 1)));
        Assert.True(errores.ContainsKey("cliente.apellido"));
        Assert.True(errores.ContainsKey("cliente.nombre"));
    }

    [Fact]
    public async Task Graba_sin_domicilio_email_ni_telefono()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var r = await cliente.PostPresupuestoAsync(Cliente(domicilio: null, email: null, telefono: null), Linea(a.Codigo, 1));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Theory]
    [InlineData("23A56789")]
    [InlineData("123456")]
    [InlineData("123456789")]
    public async Task Rechaza_un_DNI_que_no_tiene_7_u_8_digitos(string dni)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(dni: dni), Linea(a.Codigo, 1)));
        Assert.Contains("7 u 8 dígitos", errores["cliente.dni"][0]);
    }

    [Fact]
    public async Task Rechaza_un_email_mal_formado()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente(email: "ana-sin-arroba"), Linea(a.Codigo, 1)));
        Assert.True(errores.ContainsKey("cliente.email"));
    }

    [Fact]
    public async Task Sin_lineas_no_graba()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.PrepararCatalogoAsync();
        var errores = await ErroresAsync(await cliente.PostPresupuestoAsync(Cliente()));
        Assert.True(errores.ContainsKey("lineas"));
    }

    [Fact]
    public async Task En_Borrador_se_modifica_y_se_ve_al_reabrir()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearPresupuestoAsync(Cliente(), Linea(a.Codigo, 1));

        var r = await cliente.PutPresupuestoAsync(p.Id, "Borrador", Cliente(domicilio: "Calle 7 n° 1000"),
            Linea(a.Codigo, 1, id: p.Lineas[0].Id), Linea(a.Codigo, 2));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var reabierto = await cliente.PresupuestoAsync(p.Id);
        Assert.Equal("Calle 7 n° 1000", reabierto.Cliente.Domicilio);
        Assert.Equal(2, reabierto.Lineas.Length);
        Assert.Equal(p.Numero, reabierto.Numero);
    }

    [Fact]
    public async Task Pasa_de_Borrador_a_Final()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1));
        Assert.Equal("Final", (await cliente.PresupuestoAsync(p.Id)).Estado);
    }
}
