using Optica.Api.Datos;

namespace Optica.Tests.Datos;

public class ConversoresTests
{
    [Theory]
    [InlineData("1815.00", 181500L)]
    [InlineData("0.01", 1L)]
    [InlineData("-1800.00", -180000L)]
    [InlineData("999999999.99", 99999999999L)]
    public void Importe_se_guarda_en_centavos_sin_perdida(string importe, long centavos)
    {
        var valor = decimal.Parse(importe, System.Globalization.CultureInfo.InvariantCulture);
        var conversor = new ConversorCentesimos();
        Assert.Equal(centavos, (long)conversor.ConvertToProvider(valor)!);
        Assert.Equal(valor, (decimal)conversor.ConvertFromProvider(centavos)!);
    }

    [Theory]
    [InlineData("21", 2100L)]
    [InlineData("10.5", 1050L)]
    [InlineData("2.5", 250L)]
    [InlineData("1000", 100000L)]
    public void Porcentaje_se_guarda_en_centesimos_sin_perdida(string porcentaje, long centesimos)
    {
        var valor = decimal.Parse(porcentaje, System.Globalization.CultureInfo.InvariantCulture);
        var conversor = new ConversorCentesimos();
        Assert.Equal(centesimos, (long)conversor.ConvertToProvider(valor)!);
        Assert.Equal(valor, (decimal)conversor.ConvertFromProvider(centesimos)!);
    }
}
