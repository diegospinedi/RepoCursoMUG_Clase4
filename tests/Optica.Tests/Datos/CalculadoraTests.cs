using System.Globalization;
using System.Text.Json;
using Optica.Api.Datos;

namespace Optica.Tests.Datos;

public class CalculadoraTests
{
    private static readonly JsonElement Vectores =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "vectores-calculo.json"))).RootElement;

    private static decimal D(JsonElement e, string campo) =>
        decimal.Parse(e.GetProperty(campo).GetString()!, CultureInfo.InvariantCulture);

    public static TheoryData<string> Casos(string seccion)
    {
        var datos = new TheoryData<string>();
        foreach (var v in Vectores.GetProperty(seccion).EnumerateArray()) datos.Add(v.GetRawText());
        return datos;
    }

    [Theory]
    [MemberData(nameof(Casos), "precioVenta")]
    public void PrecioVenta_coincide_con_los_vectores(string json)
    {
        var v = JsonDocument.Parse(json).RootElement;
        Assert.Equal(D(v, "esperado"), Calculadora.PrecioVenta(D(v, "costo"), D(v, "margen"), D(v, "multiplo")));
    }

    [Theory]
    [MemberData(nameof(Casos), "lineas")]
    public void Linea_coincide_con_los_vectores(string json)
    {
        var v = JsonDocument.Parse(json).RootElement;
        var (conDescuento, final) = Calculadora.Linea(D(v, "precioUnitario"), D(v, "descuento"), v.GetProperty("cantidad").GetInt32());
        Assert.Equal(D(v, "precioConDescuento"), conDescuento);
        Assert.Equal(D(v, "precioFinal"), final);
    }

    [Theory]
    [MemberData(nameof(Casos), "totales")]
    public void Total_suma_los_precios_finales_redondeados(string json)
    {
        var v = JsonDocument.Parse(json).RootElement;
        var finales = v.GetProperty("preciosFinales").EnumerateArray()
            .Select(p => decimal.Parse(p.GetString()!, CultureInfo.InvariantCulture));
        Assert.Equal(D(v, "esperado"), Calculadora.Total(finales));
    }

    [Theory]
    [MemberData(nameof(Casos), "desgloseFacturaB")]
    public void DesgloseFacturaB_cierra_con_el_total(string json)
    {
        var v = JsonDocument.Parse(json).RootElement;
        var (neto, iva) = Calculadora.DesgloseFacturaB(D(v, "total"), D(v, "alicuota"));
        Assert.Equal(D(v, "neto"), neto);
        Assert.Equal(D(v, "iva"), iva);
        Assert.Equal(D(v, "total"), neto + iva);
    }
}
