using ClosedXML.Excel;

namespace Optica.Tests.Planillas;

/// <summary>Arma planillas .xlsx en memoria para los tests de importación.</summary>
public static class GeneradorPlanillas
{
    public static readonly string[] Encabezados = ["Código en el proveedor", "Descripción", "Precio de Costo"];

    /// <summary>
    /// Cada fila es un arreglo de celdas: string → celda de texto, número → celda numérica, null → vacía.
    /// Para un código numérico con ceros visibles, usar <see cref="NumeroConFormato"/>.
    /// </summary>
    public static byte[] Crear(IEnumerable<object?[]> filas, string[]? encabezados = null)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Precios");
        var titulos = encabezados ?? Encabezados;
        for (var c = 0; c < titulos.Length; c++) hoja.Cell(1, c + 1).Value = titulos[c];

        var r = 2;
        foreach (var fila in filas)
        {
            for (var c = 0; c < fila.Length; c++)
            {
                var celda = hoja.Cell(r, c + 1);
                switch (fila[c])
                {
                    case null: break;
                    case string s: celda.Value = s; celda.Style.NumberFormat.Format = "@"; break;
                    case NumeroConFormato n: celda.Value = n.Valor; celda.Style.NumberFormat.Format = n.Formato; break;
                    case decimal d: celda.Value = d; break;
                    case int i: celda.Value = i; break;
                    case double x: celda.Value = x; break;
                    default: throw new ArgumentException($"Tipo de celda no soportado: {fila[c]!.GetType()}");
                }
            }
            r++;
        }
        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] Crear(params object?[][] filas) => Crear(filas.AsEnumerable());

    public sealed record NumeroConFormato(double Valor, string Formato);
}
