using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Arca;
using Optica.Api.Datos;
using Optica.Api.Facturacion;
using Optica.Tests.Catalogo;
using Optica.Tests.Presupuestos;

namespace Optica.Tests.Facturacion;

public class FallasArcaTests
{
    private static async Task<int> PendienteAsync(AppDePrueba app, HttpClient cliente, int presupuestoId)
    {
        var r = await cliente.FacturarAsync(presupuestoId);
        Assert.Equal(HttpStatusCode.GatewayTimeout, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("facturaId").GetInt32();
    }

    private static Task<HttpResponseMessage> ReintentarAsync(HttpClient cliente, int facturaId) =>
        cliente.PostAsync($"/api/facturas/{facturaId}/reintentar", null);

    private static async Task<string?> TipoAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type;

    [Fact]
    public async Task Un_rechazo_muestra_codigo_y_descripcion_y_no_registra_factura()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.Rechazo = new Rechazado("10016", "Número de comprobante inválido.");

        var r = await cliente.FacturarAsync(p.Id);

        Assert.Equal(HttpStatusCode.BadGateway, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("arca-rechazo", problema.GetProperty("type").GetString());
        Assert.Equal("10016", problema.GetProperty("codigo").GetString());
        Assert.Equal("Número de comprobante inválido.", problema.GetProperty("descripcion").GetString());
        using var scope = app.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<OpticaDbContext>().Set<Factura>());
        Assert.Null((await cliente.PresupuestoAsync(p.Id)).Emision);
    }

    [Fact]
    public async Task Sin_respuesta_queda_Pendiente_avisa_y_no_reintenta_solo()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;

        var r = await cliente.FacturarAsync(p.Id);

        Assert.Equal(HttpStatusCode.GatewayTimeout, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("arca-sin-respuesta", problema.GetProperty("type").GetString());
        var titulo = problema.GetProperty("title").GetString()!;
        Assert.Contains("no respondió", titulo);
        Assert.Contains("pendiente", titulo);
        Assert.Contains("reintentar", titulo);
        var id = problema.GetProperty("facturaId").GetInt32();
        Assert.Equal("Pendiente", (await cliente.FacturaAsync(id)).Estado);
        Assert.Equal(new EmisionDto(id, "Pendiente"), (await cliente.PresupuestoAsync(p.Id)).Emision);
        Assert.Equal(["UltimoAutorizadoAsync", "SolicitarCaeAsync"], app.Arca.Llamadas);
    }

    [Fact]
    public async Task Reintentar_una_que_ARCA_autorizo_recupera_el_CAE_sin_emitir_otra()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.AutorizarSinResponder = true;
        var id = await PendienteAsync(app, cliente, p.Id);
        app.Arca.AutorizarSinResponder = false;

        var r = await ReintentarAsync(cliente, id);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var f = await cliente.FacturaAsync(id);
        Assert.Equal("Autorizada", f.Estado);
        Assert.False(string.IsNullOrEmpty(f.Cae));
        Assert.Single(app.Arca.Solicitudes);
        Assert.Equal(new EmisionDto(id, "Autorizada"), (await cliente.PresupuestoAsync(p.Id)).Emision);
    }

    [Fact]
    public async Task Reintentar_una_que_no_se_autorizo_consulta_primero_y_despues_envia()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;
        var id = await PendienteAsync(app, cliente, p.Id);
        app.Arca.SinRespuesta = false;
        var antes = app.Arca.Llamadas.Count;

        var r = await ReintentarAsync(cliente, id);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(["ConsultarAsync", "SolicitarCaeAsync"], app.Arca.Llamadas.Skip(antes));
        Assert.Equal("Autorizada", (await cliente.FacturaAsync(id)).Estado);
        Assert.Equal(1, app.Arca.Solicitudes.Last().Numero);
    }

    [Fact]
    public async Task Si_el_numero_figura_con_otro_total_queda_Bloqueada_sin_emitir_ni_recuperar()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;
        var id = await PendienteAsync(app, cliente, p.Id);
        app.Arca.SinRespuesta = false;
        app.Arca.AutorizarPorFuera(3, TipoComprobante.FacturaB, 1, 999m);

        var r = await ReintentarAsync(cliente, id);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("emision-bloqueada", await TipoAsync(r));
        var f = await cliente.FacturaAsync(id);
        Assert.Equal(("Bloqueada", (string?)null), (f.Estado, f.Cae));
        Assert.Single(app.Arca.Solicitudes);

        var otraVez = await ReintentarAsync(cliente, id);
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
        Assert.Equal("emision-bloqueada", await TipoAsync(otraVez));
        Assert.Single(app.Arca.Solicitudes);
    }

    [Fact]
    public async Task Confirmar_la_revision_descarta_y_permite_facturar_con_el_numero_siguiente()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;
        var id = await PendienteAsync(app, cliente, p.Id);
        app.Arca.SinRespuesta = false;
        app.Arca.AutorizarPorFuera(3, TipoComprobante.FacturaB, 1, 999m);
        await ReintentarAsync(cliente, id);

        var r = await cliente.PostAsync($"/api/facturas/{id}/confirmar-revision", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Descartada", (await cliente.FacturaAsync(id)).Estado);
        Assert.Null((await cliente.PresupuestoAsync(p.Id)).Emision);
        var nueva = await cliente.FacturarBienAsync(p.Id);
        Assert.Equal(2, nueva.Numero);
    }

    [Fact]
    public async Task Solo_se_confirma_la_revision_de_una_Bloqueada_y_solo_se_reintenta_una_Pendiente()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var f = await cliente.FacturarBienAsync((await cliente.PresupuestoFinalAsync()).Id);

        var confirmar = await cliente.PostAsync($"/api/facturas/{f.FacturaId}/confirmar-revision", null);
        var reintentar = await ReintentarAsync(cliente, f.FacturaId);

        Assert.Equal(HttpStatusCode.Conflict, confirmar.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, reintentar.StatusCode);
        Assert.Equal("emision-no-pendiente", await TipoAsync(reintentar));
    }

    [Fact]
    public async Task El_reintento_usa_la_foto_aunque_cambie_la_configuracion()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;
        var id = await PendienteAsync(app, cliente, p.Id);
        app.Arca.SinRespuesta = false;
        await cliente.ConfigurarAsync(condicion: "Monotributo", tope: 100m);

        await ReintentarAsync(cliente, id);

        var s = app.Arca.Solicitudes.Last();
        Assert.Equal((TipoComprobante.FacturaB, 99, 1500m), (s.Tipo, s.DocTipo, s.Neto));
    }

    [Fact]
    public async Task La_fecha_enviada_es_la_del_reintento()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;
        var id = await PendienteAsync(app, cliente, p.Id);
        app.Arca.SinRespuesta = false;

        app.Reloj.Advance(TimeSpan.FromDays(1));
        var otroDia = await app.IngresarAsync();
        await ReintentarAsync(otroDia, id);

        Assert.Equal(new DateOnly(2026, 10, 10), app.Arca.Solicitudes.Last().Fecha);
        Assert.Equal(new DateOnly(2026, 10, 10), (await otroDia.FacturaAsync(id)).Fecha);
    }
}
