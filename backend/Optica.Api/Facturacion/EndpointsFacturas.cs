using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Optica.Api.Arca;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;

namespace Optica.Api.Facturacion;

public static class EndpointsFacturas
{
    public sealed record EmitidaDto(int FacturaId, EstadoFactura Estado, string Tipo, int PuntoVenta, long Numero, string? Cae, DateOnly? VencimientoCae);
    public sealed record LineaDto(string Descripcion, int Cantidad, decimal PrecioFinal);
    public sealed record OrigenDto(int Id, int Numero);
    public sealed record DetalleDto(
        int Id, EstadoFactura Estado, string Tipo, int PuntoVenta, long Numero, DateOnly Fecha, int ReceptorDocTipo, string ReceptorDocNro,
        decimal Total, decimal? Neto, decimal? Iva, decimal? AlicuotaIva, string? Cae, DateOnly? VencimientoCae,
        OrigenDto Presupuesto, string Apellido, string Nombre, string Dni, IEnumerable<LineaDto> Lineas);

    // No hay PUT ni DELETE: una factura no se modifica ni se elimina (FR-031).
    public static void MapFacturas(this WebApplication app)
    {
        app.MapPost("/api/presupuestos/{id:int}/factura", async (int id, ServicioEmision servicio) =>
            Responder(await servicio.FacturarAsync(id)));

        app.MapGet("/api/facturas/{id:int}", async (int id, OpticaDbContext db) =>
            await db.Set<Factura>().AsNoTracking().Include(f => f.Presupuesto!).ThenInclude(p => p.Lineas).SingleOrDefaultAsync(f => f.Id == id) is { } f
                ? Results.Ok(Detalle(f))
                : Results.NotFound());

        app.MapGet("/api/facturas/{id:int}/pdf", async (int id, OpticaDbContext db, IOptions<OpcionesEmisor> emisor,
            IOptions<OpcionesArca> arca, RecursosPdf recursos) =>
        {
            var f = await db.Set<Factura>().AsNoTracking().Include(x => x.Presupuesto!).ThenInclude(p => p.Lineas).SingleOrDefaultAsync(x => x.Id == id);
            if (f is null) return Results.NotFound();
            if (f.Estado != EstadoFactura.Autorizada)
                return Results.Problem(statusCode: 409, type: "factura-no-autorizada", title: "El PDF está disponible cuando la factura tiene CAE.");
            var pdf = PdfFactura.Generar(f, f.Presupuesto!, emisor.Value, recursos, simulado: arca.Value.Entorno == "Simulado");
            return Results.File(pdf, "application/pdf", $"factura-{f.Letra}-{f.NumeroCompleto}.pdf");
        });
    }

    public static IResult Responder(ResultadoEmision r) => r switch
    {
        ResultadoEmision.Autorizada a => Results.Created($"/api/facturas/{a.Factura.Id}", Emitida(a.Factura)),
        ResultadoEmision.PresupuestoBorrador => Results.Problem(statusCode: 409, type: "presupuesto-borrador",
            title: "Solo se factura un presupuesto en estado Final."),
        ResultadoEmision.YaFacturado => Results.Problem(statusCode: 409, type: "ya-facturado",
            title: "Este presupuesto ya tiene una factura emitida o en curso."),
        ResultadoEmision.Rechazada x => Results.Problem(statusCode: 502, type: "arca-rechazo",
            title: $"ARCA rechazó el comprobante: {x.Codigo} — {x.Descripcion}",
            extensions: new Dictionary<string, object?> { ["codigo"] = x.Codigo, ["descripcion"] = x.Descripcion }),
        ResultadoEmision.SinRespuesta s => Results.Problem(statusCode: 504, type: "arca-sin-respuesta",
            title: s.FacturaId is null
                ? "ARCA no respondió. No se registró ninguna factura; probá de nuevo más tarde."
                : "ARCA no respondió a tiempo. La emisión quedó pendiente; podés reintentarla más tarde.",
            extensions: new Dictionary<string, object?> { ["facturaId"] = s.FacturaId }),
        _ => Results.NotFound(),
    };

    public static EmitidaDto Emitida(Factura f) => new(f.Id, f.Estado, f.Letra, f.PuntoVenta, f.Numero, f.Cae, f.VencimientoCae);

    private static DetalleDto Detalle(Factura f) => new(
        f.Id, f.Estado, f.Letra, f.PuntoVenta, f.Numero, f.Fecha, f.ReceptorDocTipo, f.ReceptorDocNro,
        f.Total, f.Neto, f.Iva, f.AlicuotaIva, f.Cae, f.VencimientoCae,
        new OrigenDto(f.Presupuesto!.Id, f.Presupuesto.Numero), f.Apellido, f.Nombre, f.Dni,
        f.Presupuesto.Lineas.OrderBy(l => l.Orden).Select(l => new LineaDto(l.Descripcion, l.Cantidad, l.PrecioFinal)));
}
