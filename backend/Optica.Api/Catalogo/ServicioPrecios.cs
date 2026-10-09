using Microsoft.EntityFrameworkCore;
using Optica.Api.Configuracion;
using Optica.Api.Datos;

namespace Optica.Api.Catalogo;

/// <summary>Precio de venta con el múltiplo vigente y recálculo de todo el catálogo (FR-009, FR-012, FR-013).</summary>
public sealed class ServicioPrecios(OpticaDbContext db)
{
    public async Task<ParametrosNegocio> ParametrosAsync() =>
        await db.Set<ParametrosNegocio>().SingleAsync();

    public static decimal Calcular(decimal costo, decimal margen, decimal multiplo) =>
        Calculadora.PrecioVenta(costo, margen, multiplo);

    /// <summary>Recalcula y guarda el precio de venta de todos los artículos. Corre dentro de la transacción del llamador.</summary>
    public async Task<int> RecalcularTodoAsync(decimal multiplo)
    {
        var articulos = await db.Set<Articulo>().ToListAsync();
        foreach (var a in articulos) a.PrecioVenta = Calcular(a.PrecioCosto, a.Margen, multiplo);
        await db.SaveChangesAsync();
        return articulos.Count;
    }
}
