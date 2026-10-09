using System.Globalization;

namespace Optica.Api.Datos;

/// <summary>Formatos de la skill frontend-design para los PDF: $ 1.815,00, 23.456.789, 09/10/2026.</summary>
public static class FormatoArgentino
{
    private static readonly NumberFormatInfo Numeros = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

    public static string Importe(decimal valor) =>
        (valor < 0 ? "-$ " : "$ ") + Math.Abs(valor).ToString("#,##0.00", Numeros);

    public static string Porcentaje(decimal valor) => valor.ToString("0.##", Numeros) + " %";

    public static string Dni(string digitos) =>
        long.TryParse(digitos, out var n) ? n.ToString("#,##0", Numeros) : digitos;

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
