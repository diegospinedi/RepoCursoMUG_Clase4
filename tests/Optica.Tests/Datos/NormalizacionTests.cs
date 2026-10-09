using Optica.Api.Datos;

namespace Optica.Tests.Datos;

public class NormalizacionTests
{
    [Theory]
    [InlineData("González", "gonzalez")]
    [InlineData("ONZALEZ", "onzalez")]
    [InlineData("Ñandú Pérez", "nandu perez")]
    [InlineData("  Gómez ", "gomez")]
    public void Texto_queda_en_minusculas_y_sin_acentos(string entrada, string esperado) =>
        Assert.Equal(esperado, Normalizacion.Texto(entrada));

    [Theory]
    [InlineData("23.456.789", "23456789")]
    [InlineData("0003-00034561", "000300034561")]
    [InlineData("abc", "")]
    public void Digitos_conserva_solo_los_digitos(string entrada, string esperado) =>
        Assert.Equal(esperado, Normalizacion.Digitos(entrada));
}
