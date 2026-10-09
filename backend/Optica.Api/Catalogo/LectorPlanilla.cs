using System.Globalization;
using ClosedXML.Excel;

namespace Optica.Api.Catalogo;

public sealed record FilaLeida(int Numero, string Codigo, string Descripcion, decimal? Precio);

/// <summary>La planilla no respeta el formato o los límites: se rechaza completa (FR-043).</summary>
public sealed class FormatoPlanillaException(string detalle) : Exception(detalle);

/// <summary>
/// Lectura estricta de la planilla del proveedor (contracts/planilla-proveedor.md, research R6): encabezados
/// exactos, precio solo en celdas numéricas y código tal como está en la celda.
/// </summary>
public static class LectorPlanilla
{
    public const long TamanoMaximo = 10 * 1024 * 1024;
    public const int FilasMaximas = 20_000;
    public static readonly string[] Encabezados = ["Código en el proveedor", "Descripción", "Precio de Costo"];

    private const string DetalleColumnas =
        "Tiene que tener exactamente las columnas Código en el proveedor, Descripción y Precio de Costo, en ese orden y en la primera fila.";

    public static IReadOnlyList<FilaLeida> Leer(Stream contenido, long longitud)
    {
        if (longitud > TamanoMaximo) throw new FormatoPlanillaException("El archivo supera los 10 MB.");

        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(contenido);
        }
        catch (Exception)
        {
            throw new FormatoPlanillaException("El archivo no es una planilla de Excel (.xlsx).");
        }

        using (libro)
        {
            var hoja = libro.Worksheets.First();
            var encabezadosOk = Encabezados.Select((e, i) => hoja.Cell(1, i + 1).GetString() == e).All(x => x);
            if (!encabezadosOk || (hoja.LastColumnUsed()?.ColumnNumber() ?? 0) > Encabezados.Length)
                throw new FormatoPlanillaException(DetalleColumnas);

            var ultima = hoja.LastRowUsed()?.RowNumber() ?? 1;
            if (ultima - 1 > FilasMaximas)
                throw new FormatoPlanillaException("La planilla tiene más de 20.000 filas de datos.");

            var filas = new List<FilaLeida>(Math.Max(0, ultima - 1));
            for (var r = 2; r <= ultima; r++)
            {
                var (codigo, descripcion, precio) = (hoja.Cell(r, 1), hoja.Cell(r, 2), hoja.Cell(r, 3));
                if (codigo.Value.IsBlank && descripcion.Value.IsBlank && precio.Value.IsBlank) continue;
                filas.Add(new FilaLeida(r, Codigo(codigo), descripcion.GetFormattedString().Trim(), Precio(precio)));
            }
            return filas;
        }
    }

    /// <summary>Texto sin recortar espacios; una celda numérica se toma por su valor ("00123" con formato de número → "123").</summary>
    private static string Codigo(IXLCell celda) =>
        celda.Value.IsNumber ? ((decimal)celda.Value.GetNumber()).ToString(CultureInfo.InvariantCulture) : celda.GetString();

    /// <summary>Solo celdas numéricas con hasta 2 decimales; cualquier otra cosa es "precio inválido" (FR-045a).</summary>
    private static decimal? Precio(IXLCell celda)
    {
        if (!celda.Value.IsNumber) return null;
        var valor = (decimal)celda.Value.GetNumber();
        return decimal.Round(valor, 2) == valor ? valor : null;
    }
}
