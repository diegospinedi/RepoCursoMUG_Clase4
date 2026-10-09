using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Arca;
using Optica.Api.Datos;
using Optica.Api.Facturacion;
using Optica.Tests.Catalogo;
using Optica.Tests.Presupuestos;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Facturacion;

public class EmisionTests
{
    [Fact]
    public async Task Factura_un_Final_con_sus_3_lineas_y_el_mismo_total()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var b = await cliente.CrearArticuloAsync(a.ProveedorId, "B-2", 1000m, 0m, "Cristal");
        var c = await cliente.CrearArticuloAsync(a.ProveedorId, "C-3", 100m, 0m, "Estuche");
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1), Linea(b.Codigo, 2), Linea(c.Codigo, 1, 50));

        var f = await cliente.FacturarBienAsync(p.Id);

        Assert.Equal("Autorizada", f.Estado);
        Assert.Equal(p.Total, Assert.Single(app.Arca.Solicitudes).Total);
        var detalle = await cliente.FacturaAsync(f.FacturaId);
        Assert.Equal(["Armazón metal", "Cristal", "Estuche"], detalle.Lineas.Select(l => l.Descripcion));
        Assert.Equal(p.Total, detalle.Total);
    }

    [Fact]
    public async Task Un_Borrador_no_se_factura_ni_llama_a_ARCA()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearPresupuestoAsync(Cliente(), Linea(a.Codigo, 1));

        var r = await cliente.FacturarAsync(p.Id);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("presupuesto-borrador", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        Assert.Empty(app.Arca.Llamadas);
    }

    [Fact]
    public async Task Guarda_numero_CAE_vencimiento_y_presupuesto_de_origen()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();

        var f = await cliente.FacturarBienAsync(p.Id);

        var d = await cliente.FacturaAsync(f.FacturaId);
        Assert.Equal((3, 1L, f.Cae, new DateOnly(2026, 10, 19)), (d.PuntoVenta, d.Numero, d.Cae, d.VencimientoCae));
        Assert.Equal(new PresupuestoOrigen(p.Id, p.Numero), d.Presupuesto);
        Assert.Equal(new DateOnly(2026, 10, 9), d.Fecha);
    }

    [Theory]
    [InlineData(1, 1815, 1500, 315)]
    [InlineData(0, 1000, 826.45, 173.55)]
    public async Task Con_Responsable_Inscripto_emite_Factura_B_con_neto_e_IVA(int margenCero, decimal total, decimal neto, decimal iva)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = margenCero == 1 ? await cliente.PrepararCatalogoAsync() : await cliente.PrepararCatalogoAsync(costo: 1000m, margen: 0m);
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1));

        var f = await cliente.FacturarBienAsync(p.Id);

        var s = Assert.Single(app.Arca.Solicitudes);
        Assert.Equal((TipoComprobante.FacturaB, total, neto, iva, (decimal?)21m), (s.Tipo, s.Total, s.Neto, s.Iva, s.Alicuota));
        Assert.Equal(5, s.IdAlicuota);
        Assert.Equal("B", f.Tipo);
    }

    [Fact]
    public async Task Con_Monotributo_emite_Factura_C_solo_con_el_total()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        await cliente.ConfigurarAsync(condicion: "Monotributo");

        var f = await cliente.FacturarBienAsync(p.Id);

        var s = Assert.Single(app.Arca.Solicitudes);
        Assert.Equal((TipoComprobante.FacturaC, 1815m, 1815m, 0m, (decimal?)null), (s.Tipo, s.Total, s.Neto, s.Iva, s.Alicuota));
        Assert.Equal("C", f.Tipo);
        var d = await cliente.FacturaAsync(f.FacturaId);
        Assert.Null(d.Neto);
        Assert.Null(d.Iva);
    }

    [Theory]
    [InlineData(10_000_000, 99, 0L)]
    [InlineData(1815, 99, 0L)]
    [InlineData(1814.99, 96, 23456789L)]
    public async Task Identifica_al_receptor_segun_el_tope(decimal tope, int docTipo, long docNro)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        await cliente.ConfigurarAsync(tope: tope);

        await cliente.FacturarBienAsync(p.Id);

        var s = Assert.Single(app.Arca.Solicitudes);
        Assert.Equal((docTipo, docNro, 5), (s.DocTipo, s.DocNro, s.CondicionIvaReceptor));
    }

    [Fact]
    public async Task Un_presupuesto_ya_facturado_no_se_vuelve_a_facturar()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        await cliente.FacturarBienAsync(p.Id);

        var r = await cliente.FacturarAsync(p.Id);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("ya-facturado", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        Assert.Single(app.Arca.Solicitudes);
    }

    [Fact]
    public async Task Dos_pedidos_simultaneos_generan_un_solo_comprobante()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        var otra = await app.IngresarAsync();
        app.Arca.Demora = TimeSpan.FromMilliseconds(200);

        var respuestas = await Task.WhenAll(cliente.FacturarAsync(p.Id), otra.FacturarAsync(p.Id));

        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], respuestas.Select(r => r.StatusCode).Order());
        Assert.Single(app.Arca.Solicitudes);
    }

    [Fact]
    public async Task Pide_el_ultimo_autorizado_mas_uno_y_lo_registra_antes_de_solicitar_el_CAE()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.AutorizarPorFuera(3, TipoComprobante.FacturaB, 41, 500m);
        (string Estado, long Numero)? registrada = null;
        app.Arca.AlSolicitar = _ =>
        {
            using var scope = app.Services.CreateScope();
            var f = scope.ServiceProvider.GetRequiredService<OpticaDbContext>().Set<Factura>().AsNoTracking().Single();
            registrada = (f.Estado.ToString(), f.Numero);
        };

        var emitida = await cliente.FacturarBienAsync(p.Id);

        Assert.Equal(42, Assert.Single(app.Arca.Solicitudes).Numero);
        Assert.Equal(("Pendiente", 42L), registrada);
        Assert.Equal(42, emitida.Numero);
    }
}
