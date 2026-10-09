using Optica.Api.Datos;

namespace Optica.Api.Presupuestos;

/// <summary>
/// Filtros combinados con "Y" (RF-81): apellido y nombre por coincidencia parcial sin mayúsculas ni acentos
/// (RF-82), DNI parcial solo con dígitos (RF-88) y rango de fechas inclusive con un solo límite admitido (RF-83).
/// </summary>
public static class BusquedaPresupuestos
{
    /// <summary>Tope de resultados para que la grilla responda rápido; se pide refinar la búsqueda.</summary>
    public const int MaximoResultados = 200;

    public static IQueryable<Presupuesto> Filtrar(
        IQueryable<Presupuesto> consulta, string? apellido, string? nombre, string? dni, DateOnly? desde, DateOnly? hasta)
    {
        if (Normalizacion.Texto(apellido) is { Length: > 0 } a) consulta = consulta.Where(p => p.ApellidoBusqueda.Contains(a));
        if (Normalizacion.Texto(nombre) is { Length: > 0 } n) consulta = consulta.Where(p => p.NombreBusqueda.Contains(n));
        if (Normalizacion.Digitos(dni) is { Length: > 0 } d) consulta = consulta.Where(p => p.Dni.Contains(d));
        if (desde is { } fd) consulta = consulta.Where(p => p.Fecha >= fd);
        if (hasta is { } fh) consulta = consulta.Where(p => p.Fecha <= fh);
        return consulta.OrderByDescending(p => p.Numero).Take(MaximoResultados);
    }
}
