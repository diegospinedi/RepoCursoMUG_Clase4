using System.Globalization;
using System.Text;

namespace Optica.Api.Datos;

/// <summary>Columnas de búsqueda sin acentos ni mayúsculas, y solo con dígitos (RF-82, RF-88).</summary>
public static class Normalizacion
{
    public static string Texto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Digitos(string? texto) =>
        texto is null ? "" : new string(texto.Where(char.IsAsciiDigit).ToArray());
}
