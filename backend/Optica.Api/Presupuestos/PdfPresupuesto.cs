using Optica.Api.Datos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Optica.Api.Presupuestos;

/// <summary>PDF del presupuesto Final: logo, colores de la marca y leyenda de precios finales (RF-02, RF-37, RF-55).</summary>
public static class PdfPresupuesto
{
    public const string Leyenda = "Precios finales, IVA incluido";

    public static byte[] Generar(Presupuesto p, RecursosPdf recursos) =>
        Document.Create(doc => doc.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(1.5f, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontFamily(RecursosPdf.FuenteTexto).FontSize(10));

            pagina.Header().BorderBottom(3).BorderColor(recursos.ColorPrimario).PaddingBottom(8).Row(fila =>
            {
                fila.ConstantItem(170).Image(recursos.Logo).FitArea();
                fila.RelativeItem().AlignRight().AlignMiddle().Column(col =>
                {
                    col.Item().AlignRight().Text($"Presupuesto N° {p.Numero}")
                        .FontFamily(RecursosPdf.FuenteTitulos).Bold().FontSize(16).FontColor(recursos.ColorPrimario);
                    col.Item().AlignRight().Text($"Fecha: {FormatoArgentino.Fecha(p.Fecha)}");
                });
            });

            pagina.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("Cliente").FontFamily(RecursosPdf.FuenteTitulos).SemiBold().FontColor(recursos.ColorPrimario);
                col.Item().Text($"{p.Apellido}, {p.Nombre} — DNI {FormatoArgentino.Dni(p.Dni)}");
                var contacto = new[] { p.Domicilio, p.Email, p.Telefono }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                if (contacto.Length > 0) col.Item().Text(string.Join(" · ", contacto));

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(50);
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                        c.ConstantColumn(45);
                        c.ConstantColumn(55);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    tabla.Header(h =>
                    {
                        foreach (var titulo in new[] { "Código", "Descripción", "Precio unitario", "Cant.", "Desc.", "Con descuento", "Precio final" })
                            h.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(titulo)
                                .FontFamily(RecursosPdf.FuenteTitulos).SemiBold().FontSize(8).FontColor(recursos.ColorPrimario);
                    });
                    foreach (var l in p.Lineas.OrderBy(l => l.Orden))
                    {
                        Celda(tabla).Text(l.ArticuloCodigo.ToString());
                        Celda(tabla).Text(l.Descripcion);
                        Celda(tabla).AlignRight().Text(FormatoArgentino.Importe(l.PrecioUnitario));
                        Celda(tabla).AlignRight().Text(l.Cantidad.ToString());
                        Celda(tabla).AlignRight().Text(FormatoArgentino.Porcentaje(l.Descuento));
                        Celda(tabla).AlignRight().Text(FormatoArgentino.Importe(l.PrecioConDescuento));
                        Celda(tabla).AlignRight().Text(FormatoArgentino.Importe(l.PrecioFinal));
                    }
                });

                col.Item().AlignRight().Text($"Total {FormatoArgentino.Importe(p.Total)}")
                    .FontFamily(RecursosPdf.FuenteTitulos).Bold().FontSize(13).FontColor(recursos.ColorPrimario);
                col.Item().AlignRight().Text(Leyenda).Italic();
            });

            pagina.Footer().AlignCenter().Text(t =>
            {
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        })).GeneratePdf();

    private static IContainer Celda(TableDescriptor tabla) =>
        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
}
