using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;

namespace Optica.Api.Catalogo;

public static class EndpointsProveedores
{
    public sealed record ProveedorDto(int Id, string Nombre);
    public sealed record PedidoProveedor(string? Nombre);

    public static void MapProveedores(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/proveedores");

        grupo.MapGet("", async (OpticaDbContext db) =>
            await db.Set<Proveedor>().OrderBy(p => p.Nombre).Select(p => new ProveedorDto(p.Id, p.Nombre)).ToListAsync());

        grupo.MapPost("", async (PedidoProveedor pedido, OpticaDbContext db) =>
        {
            var errores = await ValidarAsync(pedido, db, null);
            if (errores.Hay) return errores.Respuesta();
            var p = new Proveedor();
            p.Renombrar(pedido.Nombre!);
            db.Add(p);
            await db.SaveChangesAsync();
            return Results.Created($"/api/proveedores/{p.Id}", new ProveedorDto(p.Id, p.Nombre));
        });

        grupo.MapPut("/{id:int}", async (int id, PedidoProveedor pedido, OpticaDbContext db) =>
        {
            var p = await db.Set<Proveedor>().FindAsync(id);
            if (p is null) return Results.NotFound();
            var errores = await ValidarAsync(pedido, db, id);
            if (errores.Hay) return errores.Respuesta();
            p.Renombrar(pedido.Nombre!);
            await db.SaveChangesAsync();
            return Results.Ok(new ProveedorDto(p.Id, p.Nombre));
        });
    }

    private static async Task<Errores> ValidarAsync(PedidoProveedor pedido, OpticaDbContext db, int? id)
    {
        var e = new Errores();
        var nombre = pedido.Nombre?.Trim() ?? "";
        if (nombre.Length == 0) e.Agregar("nombre", "Ingresá el nombre del proveedor.");
        else if (nombre.Length > Proveedor.LargoNombre) e.Agregar("nombre", "El nombre puede tener como máximo 100 caracteres.");
        else
        {
            var clave = Proveedor.Clave(nombre);
            if (await db.Set<Proveedor>().AnyAsync(p => p.NombreClave == clave && p.Id != id))
                e.Agregar("nombre", "Ya existe un proveedor con ese nombre.");
        }
        return e;
    }
}
