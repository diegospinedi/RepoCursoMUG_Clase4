using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Arca;
using Optica.Api.Datos;
using Optica.Api.Facturacion;
using Optica.Api.Presupuestos;

namespace Optica.Tests.Facturacion;

public sealed record FacturaResumen(int Id, string Estado, string Tipo, int PuntoVenta, long Numero, DateOnly Fecha, string Apellido, string Nombre, decimal Total, int PresupuestoNumero);

public class BusquedaFacturasTests
{
    /// <summary>Inserta presupuestos y facturas con fechas pasadas (el reloj de prueba no retrocede).</summary>
    private static async Task SembrarAsync(AppDePrueba app, params (int Numero, string Fecha, string Apellido, string Dni, EstadoFactura Estado)[] filas)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        var n = 0;
        foreach (var f in filas)
        {
            var fecha = DateOnly.Parse(f.Fecha);
            var p = Presupuesto.Nuevo(++n + 100, fecha, f.Apellido, "Ana", f.Dni, null, null, null);
            p.Total = 1815m;
            db.Add(p);
            await db.SaveChangesAsync();
            var factura = new Factura
            {
                PresupuestoId = p.Id, Estado = f.Estado, Tipo = TipoComprobante.FacturaB, PuntoVenta = 3, Fecha = fecha,
                ReceptorDocTipo = 99, ReceptorDocNro = "0", Total = 1815m, Neto = 1500m, Iva = 315m, AlicuotaIva = 21m,
                Cae = f.Estado == EstadoFactura.Autorizada ? "76000000000001" : null,
                VencimientoCae = f.Estado == EstadoFactura.Autorizada ? fecha.AddDays(10) : null,
                Apellido = p.Apellido, Nombre = p.Nombre, Dni = p.Dni, ApellidoBusqueda = p.ApellidoBusqueda, NombreBusqueda = p.NombreBusqueda,
            };
            factura.Numerar(f.Numero);
            db.Add(factura);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<long[]> BuscarAsync(HttpClient cliente, string consulta) =>
        (await cliente.GetFromJsonAsync<FacturaResumen[]>($"/api/facturas?{consulta}"))!.Select(f => f.Numero).Order().ToArray();

    [Fact]
    public async Task Combina_apellido_sin_acentos_con_fecha_desde()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await SembrarAsync(app,
            (1, "2026-03-10", "González", "23456789", EstadoFactura.Autorizada),
            (2, "2026-03-20", "González", "23456789", EstadoFactura.Autorizada),
            (3, "2026-03-20", "Gómez", "30111222", EstadoFactura.Autorizada));

        Assert.Equal(new long[] { 2 }, await BuscarAsync(cliente, "apellido=gonzalez&desde=2026-03-15"));
    }

    [Fact]
    public async Task Busca_por_parte_del_numero_de_comprobante_y_del_DNI()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await SembrarAsync(app,
            (34561, "2026-03-10", "González", "23456789", EstadoFactura.Autorizada),
            (12, "2026-03-10", "Gómez", "30111222", EstadoFactura.Autorizada));

        Assert.Equal(new long[] { 34561 }, await BuscarAsync(cliente, "numero=34561"));
        Assert.Equal(new long[] { 34561 }, await BuscarAsync(cliente, "numero=0003-00034561"));
        Assert.Equal(new long[] { 34561 }, await BuscarAsync(cliente, "dni=3456"));
    }

    [Fact]
    public async Task Lista_todos_los_estados_y_filtra_por_estado()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await SembrarAsync(app,
            (1, "2026-03-10", "A", "1111111", EstadoFactura.Autorizada),
            (2, "2026-03-10", "B", "2222222", EstadoFactura.Pendiente),
            (3, "2026-03-10", "C", "3333333", EstadoFactura.Bloqueada),
            (4, "2026-03-10", "D", "4444444", EstadoFactura.Descartada));

        Assert.Equal(new long[] { 1, 2, 3, 4 }, await BuscarAsync(cliente, ""));
        Assert.Equal(new long[] { 2 }, await BuscarAsync(cliente, "estado=Pendiente"));
        Assert.Equal(new long[] { 3 }, await BuscarAsync(cliente, "estado=Bloqueada"));
    }

    [Fact]
    public async Task Cada_fila_informa_el_presupuesto_de_origen()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await SembrarAsync(app, (1, "2026-03-10", "González", "23456789", EstadoFactura.Autorizada));

        var f = Assert.Single((await cliente.GetFromJsonAsync<FacturaResumen[]>("/api/facturas"))!);
        Assert.Equal((101, "B", "Autorizada", 1815m), (f.PresupuestoNumero, f.Tipo, f.Estado, f.Total));
    }
}
