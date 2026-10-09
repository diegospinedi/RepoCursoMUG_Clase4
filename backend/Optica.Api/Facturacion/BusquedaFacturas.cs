using Optica.Api.Datos;

namespace Optica.Api.Facturacion;

/// <summary>
/// Facturas en todos sus estados, con los mismos criterios que los presupuestos (FR-039): filtros con "Y",
/// coincidencia parcial sin acentos, DNI y número de comprobante solo con dígitos, rango de fechas inclusive.
/// </summary>
public static class BusquedaFacturas
{
    public const int MaximoResultados = 200;

    public static IQueryable<Factura> Filtrar(IQueryable<Factura> consulta, EstadoFactura? estado, string? apellido, string? nombre,
        string? dni, string? numero, DateOnly? desde, DateOnly? hasta)
    {
        if (estado is { } e) consulta = consulta.Where(f => f.Estado == e);
        if (Normalizacion.Texto(apellido) is { Length: > 0 } a) consulta = consulta.Where(f => f.ApellidoBusqueda.Contains(a));
        if (Normalizacion.Texto(nombre) is { Length: > 0 } n) consulta = consulta.Where(f => f.NombreBusqueda.Contains(n));
        if (Normalizacion.Digitos(dni) is { Length: > 0 } d) consulta = consulta.Where(f => f.Dni.Contains(d));
        if (Normalizacion.Digitos(numero) is { Length: > 0 } c) consulta = consulta.Where(f => f.NumeroBusqueda.Contains(c));
        if (desde is { } fd) consulta = consulta.Where(f => f.Fecha >= fd);
        if (hasta is { } fh) consulta = consulta.Where(f => f.Fecha <= fh);
        return consulta.OrderByDescending(f => f.Id).Take(MaximoResultados);
    }
}
