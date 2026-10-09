namespace Optica.Api.Datos;

/// <summary>
/// Cálculos con valor legal (research R3). El frontend replica los de línea en centavos y los dos lados
/// se prueban con tests/vectores-calculo.json.
/// </summary>
public static class Calculadora
{
    /// <summary>Redondeo a 2 decimales, mitad hacia arriba (RF-19).</summary>
    public static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Precio de venta = costo × (1 + margen / 100), redondeado hacia el valor mayor al múltiplo comercial
    /// (FR-012, FR-013). Se usa la identidad de RF-21 con alícuota única: los tres pasos literales dejan
    /// residuos que el redondeo hacia arriba convierte en un centavo de más.
    /// </summary>
    public static decimal PrecioVenta(decimal costo, decimal margen, decimal multiplo)
    {
        var precio = costo * (1 + margen / 100m);
        return Redondear(Math.Ceiling(precio / multiplo) * multiplo);
    }

    /// <summary>Precio con descuento y precio final de una línea, redondeados por línea (RF-44).</summary>
    public static (decimal PrecioConDescuento, decimal PrecioFinal) Linea(decimal precioUnitario, decimal descuento, int cantidad)
    {
        var conDescuento = Redondear(precioUnitario * (1 - descuento / 100m));
        return (conDescuento, Redondear(conDescuento * cantidad));
    }

    /// <summary>Total = suma de los precios finales ya redondeados (RF-15).</summary>
    public static decimal Total(IEnumerable<decimal> preciosFinales) => preciosFinales.Sum();

    /// <summary>Desglose de la Factura B sobre el total: neto redondeado e IVA = total − neto (FR-028).</summary>
    public static (decimal Neto, decimal Iva) DesgloseFacturaB(decimal total, decimal alicuota)
    {
        var neto = Redondear(total / (1 + alicuota / 100m));
        return (neto, total - neto);
    }
}
