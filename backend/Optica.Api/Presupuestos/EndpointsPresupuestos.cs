using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;
using Optica.Api.Facturacion;

namespace Optica.Api.Presupuestos;

public static class EndpointsPresupuestos
{
    public sealed record ClienteDto(string Apellido, string Nombre, string Dni, string? Domicilio, string? Email, string? Telefono);
    public sealed record LineaDto(int Id, int Orden, int ArticuloCodigo, string Descripcion, decimal PrecioUnitario, int Cantidad, decimal Descuento, decimal PrecioConDescuento, decimal PrecioFinal);
    public sealed record EmisionDto(int FacturaId, string Estado);
    public sealed record ResumenDto(int Id, int Numero, DateOnly Fecha, EstadoPresupuesto Estado, string Apellido, string Nombre, string Dni, decimal Total);
    public sealed record PresupuestoDto(int Id, int Numero, DateOnly Fecha, EstadoPresupuesto Estado, ClienteDto Cliente, IEnumerable<LineaDto> Lineas, decimal Total, EmisionDto? Emision);

    public static void MapPresupuestos(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/presupuestos");

        grupo.MapGet("", async (string? apellido, string? nombre, string? dni, DateOnly? desde, DateOnly? hasta, OpticaDbContext db) =>
            await BusquedaPresupuestos.Filtrar(db.Set<Presupuesto>().AsNoTracking(), apellido, nombre, dni, desde, hasta)
                .Select(p => new ResumenDto(p.Id, p.Numero, p.Fecha, p.Estado, p.Apellido, p.Nombre, p.Dni, p.Total))
                .ToListAsync());

        grupo.MapGet("/{id:int}/pdf", async (int id, OpticaDbContext db, RecursosPdf recursos) =>
        {
            var p = await db.Set<Presupuesto>().AsNoTracking().Include(x => x.Lineas).SingleOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            if (p.Estado != EstadoPresupuesto.Final)
                return Results.Problem(statusCode: 409, type: "presupuesto-borrador",
                    title: "El PDF se genera cuando el presupuesto está en estado Final.");
            return Results.File(PdfPresupuesto.Generar(p, recursos), "application/pdf", $"presupuesto-{p.Numero}.pdf");
        });

        grupo.MapGet("/{id:int}", async (int id, OpticaDbContext db) =>
        {
            var p = await db.Set<Presupuesto>().AsNoTracking().Include(x => x.Lineas).SingleOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            // La emisión viva del presupuesto (Pendiente, Bloqueada o Autorizada), para mostrar su estado (FR-038a).
            var emision = await db.Set<Factura>().AsNoTracking()
                .Where(f => f.PresupuestoId == id && f.Estado != EstadoFactura.Descartada)
                .Select(f => new EmisionDto(f.Id, f.Estado.ToString()))
                .SingleOrDefaultAsync();
            return Results.Ok(Dto(p, emision));
        });

        grupo.MapPost("", async (PedidoPresupuesto pedido, ServicioPresupuestos servicio) =>
            Responder(await servicio.CrearAsync(pedido), creado: true));

        grupo.MapPut("/{id:int}", async (int id, PedidoPresupuesto pedido, ServicioPresupuestos servicio) =>
            Responder(await servicio.ModificarAsync(id, pedido), creado: false));
    }

    public static PresupuestoDto Dto(Presupuesto p, EmisionDto? emision = null) => new(
        p.Id, p.Numero, p.Fecha, p.Estado,
        new ClienteDto(p.Apellido, p.Nombre, p.Dni, p.Domicilio, p.Email, p.Telefono),
        p.Lineas.OrderBy(l => l.Orden).Select(l => new LineaDto(
            l.Id, l.Orden, l.ArticuloCodigo, l.Descripcion, l.PrecioUnitario, l.Cantidad, l.Descuento, l.PrecioConDescuento, l.PrecioFinal)),
        p.Total, emision);

    private static IResult Responder(ResultadoPresupuesto r, bool creado) => r switch
    {
        ResultadoPresupuesto.Grabado g when creado => Results.Created($"/api/presupuestos/{g.Presupuesto.Id}", Dto(g.Presupuesto)),
        ResultadoPresupuesto.Grabado g => Results.Ok(Dto(g.Presupuesto)),
        ResultadoPresupuesto.Invalido i => i.Errores.Respuesta(),
        ResultadoPresupuesto.Cerrado => Results.Problem(statusCode: 409, type: "presupuesto-cerrado",
            title: "El presupuesto está en estado Final y no se puede modificar."),
        _ => Results.NotFound(),
    };
}
