using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;

namespace Optica.Api.Catalogo;

public static class EndpointsArticulos
{
    public const int MaximoResultados = 50;

    public sealed record ArticuloDto(
        int Codigo, int ProveedorId, string Proveedor, string CodigoProveedor, string Descripcion,
        decimal PrecioCosto, decimal Margen, decimal PrecioVenta);

    /// <summary>El precio de venta no se recibe: es de solo lectura (FR-014).</summary>
    public sealed record PedidoArticulo(int ProveedorId, string? CodigoProveedor, string? Descripcion, decimal PrecioCosto, decimal Margen);

    public static void MapArticulos(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/articulos");

        grupo.MapGet("", async (string? texto, int? proveedorId, OpticaDbContext db) =>
        {
            var consulta = db.Set<Articulo>().AsNoTracking().AsQueryable();
            if (proveedorId is { } pid) consulta = consulta.Where(a => a.ProveedorId == pid);
            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim();
                var normalizado = Normalizacion.Texto(t);
                var codigo = int.TryParse(t, out var n) ? n : -1;
                consulta = consulta.Where(a =>
                    a.Codigo == codigo ||
                    EF.Functions.Like(a.CodigoProveedor, "%" + t + "%") ||
                    a.DescripcionBusqueda.Contains(normalizado));
            }
            return await consulta.OrderBy(a => a.Descripcion).ThenBy(a => a.Codigo).Take(MaximoResultados)
                .Select(a => new ArticuloDto(a.Codigo, a.ProveedorId, a.Proveedor!.Nombre, a.CodigoProveedor, a.Descripcion, a.PrecioCosto, a.Margen, a.PrecioVenta))
                .ToListAsync();
        });

        grupo.MapGet("/{codigo:int}", async (int codigo, OpticaDbContext db) =>
            await db.Set<Articulo>().Include(a => a.Proveedor).SingleOrDefaultAsync(a => a.Codigo == codigo) is { } a
                ? Results.Ok(Dto(a))
                : Results.NotFound());

        grupo.MapPost("", async (PedidoArticulo p, OpticaDbContext db, ServicioPrecios precios) =>
        {
            var errores = await ValidarAsync(p, db, null);
            if (errores.Hay) return errores.Respuesta();
            var multiplo = (await precios.ParametrosAsync()).MultiploRedondeo;
            var a = Articulo.Nuevo(p.ProveedorId, p.CodigoProveedor!, p.Descripcion!.Trim(), p.PrecioCosto, p.Margen,
                ServicioPrecios.Calcular(p.PrecioCosto, p.Margen, multiplo));
            db.Add(a);
            await db.SaveChangesAsync();
            await db.Entry(a).Reference(x => x.Proveedor).LoadAsync();
            return Results.Created($"/api/articulos/{a.Codigo}", Dto(a));
        });

        grupo.MapPut("/{codigo:int}", async (int codigo, PedidoArticulo p, OpticaDbContext db, ServicioPrecios precios) =>
        {
            var a = await db.Set<Articulo>().FindAsync(codigo);
            if (a is null) return Results.NotFound();
            var errores = await ValidarAsync(p, db, codigo);
            if (errores.Hay) return errores.Respuesta();
            var multiplo = (await precios.ParametrosAsync()).MultiploRedondeo;
            a.ProveedorId = p.ProveedorId;
            a.CodigoProveedor = p.CodigoProveedor!;
            a.Describir(p.Descripcion!.Trim());
            a.PrecioCosto = p.PrecioCosto;
            a.Margen = p.Margen;
            a.PrecioVenta = ServicioPrecios.Calcular(p.PrecioCosto, p.Margen, multiplo);
            await db.SaveChangesAsync();
            await db.Entry(a).Reference(x => x.Proveedor).LoadAsync();
            return Results.Ok(Dto(a));
        });
    }

    private static ArticuloDto Dto(Articulo a) =>
        new(a.Codigo, a.ProveedorId, a.Proveedor!.Nombre, a.CodigoProveedor, a.Descripcion, a.PrecioCosto, a.Margen, a.PrecioVenta);

    private static async Task<Errores> ValidarAsync(PedidoArticulo p, OpticaDbContext db, int? codigo)
    {
        var e = new Errores();
        var proveedorExiste = await db.Set<Proveedor>().AnyAsync(x => x.Id == p.ProveedorId);
        if (!proveedorExiste) e.Agregar("proveedorId", "Elegí un proveedor.");

        if (string.IsNullOrEmpty(p.CodigoProveedor)) e.Agregar("codigoProveedor", "Ingresá el código en el proveedor.");
        else if (p.CodigoProveedor.Length > Articulo.LargoCodigo) e.Agregar("codigoProveedor", "El código puede tener como máximo 50 caracteres.");
        else if (proveedorExiste && await db.Set<Articulo>().AnyAsync(x =>
                     x.ProveedorId == p.ProveedorId && x.CodigoProveedor == p.CodigoProveedor && x.Codigo != codigo))
            e.Agregar("codigoProveedor", "Ya existe un artículo con ese código en el proveedor.");

        var descripcion = p.Descripcion?.Trim() ?? "";
        if (descripcion.Length == 0) e.Agregar("descripcion", "Ingresá la descripción.");
        else if (descripcion.Length > Articulo.LargoDescripcion) e.Agregar("descripcion", "La descripción puede tener como máximo 200 caracteres.");

        if (e.DosDecimales("precioCosto", p.PrecioCosto) && p.PrecioCosto < 0)
            e.Agregar("precioCosto", "El precio de costo no puede ser negativo.");
        if (e.DosDecimales("margen", p.Margen) && (p.Margen < 0 || p.Margen > 1000))
            e.Agregar("margen", "El margen debe estar entre 0 y 1000.");
        return e;
    }
}
