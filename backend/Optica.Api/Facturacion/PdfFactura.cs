using Optica.Api.Datos;
using Optica.Api.Presupuestos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Optica.Api.Facturacion;

/// <summary>PDF de la factura autorizada con los datos de RF-30, el QR de ARCA y la marca (RF-55).</summary>
public static class PdfFactura
{
    public const string LeyendaSimulado = "COMPROBANTE SIMULADO — SIN VALIDEZ FISCAL";

    public static byte[] Generar(Factura f, Presupuesto origen, OpcionesEmisor emisor, RecursosPdf recursos, bool simulado)
    {
        var qr = QrArca.Png(QrArca.Url(new DatosQr(
            f.Fecha, emisor.Cuit, f.PuntoVenta, f.Tipo, f.Numero, f.Total, f.ReceptorDocTipo, long.Parse(f.ReceptorDocNro), f.Cae!)));

        return Document.Create(doc => doc.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(1.5f, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontFamily(RecursosPdf.FuenteTexto).FontSize(10));

            pagina.Header().BorderBottom(3).BorderColor(recursos.ColorPrimario).PaddingBottom(8).Row(fila =>
            {
                fila.RelativeItem().Column(col =>
                {
                    col.Item().Height(60).Image(recursos.Logo).FitArea();
                    col.Item().Text(emisor.RazonSocial).SemiBold();
                    col.Item().Text(emisor.Domicilio);
                    col.Item().Text($"CUIT: {emisor.Cuit}");
                    col.Item().Text(emisor.CondicionIva);
                    col.Item().Text($"Ingresos Brutos: {emisor.IngresosBrutos}");
                    col.Item().Text($"Inicio de actividades: {emisor.InicioActividades}");
                });
                fila.ConstantItem(60).AlignCenter().Border(2).BorderColor(recursos.ColorPrimario).Padding(6).AlignCenter()
                    .Text(f.Letra).FontFamily(RecursosPdf.FuenteTitulos).Bold().FontSize(28).FontColor(recursos.ColorPrimario);
                fila.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text($"FACTURA {f.Letra}")
                        .FontFamily(RecursosPdf.FuenteTitulos).Bold().FontSize(16).FontColor(recursos.ColorPrimario);
                    col.Item().AlignRight().Text($"Punto de venta: {f.PuntoVenta:D4}");
                    col.Item().AlignRight().Text($"Comp. N°: {f.Numero:D8}");
                    col.Item().AlignRight().Text($"Fecha de emisión: {FormatoArgentino.Fecha(f.Fecha)}");
                });
            });

            pagina.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(10);
                if (simulado)
                    col.Item().Background(Colors.Red.Lighten4).Padding(6).AlignCenter()
                        .Text(LeyendaSimulado).Bold().FontColor(Colors.Red.Darken3);

                col.Item().Text("Receptor").FontFamily(RecursosPdf.FuenteTitulos).SemiBold().FontColor(recursos.ColorPrimario);
                col.Item().Text(f.ReceptorDocTipo == ArmadorComprobante.DocTipoDni
                    ? $"DNI {FormatoArgentino.Dni(f.ReceptorDocNro)} — {f.Apellido}, {f.Nombre} — Consumidor Final"
                    : "Consumidor Final");

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(6);
                        c.ConstantColumn(60);
                        c.RelativeColumn(2);
                    });
                    tabla.Header(h =>
                    {
                        foreach (var titulo in new[] { "Descripción", "Cantidad", "Importe" })
                            h.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(titulo)
                                .FontFamily(RecursosPdf.FuenteTitulos).SemiBold().FontSize(8).FontColor(recursos.ColorPrimario);
                    });
                    foreach (var l in origen.Lineas.OrderBy(l => l.Orden))
                    {
                        Celda(tabla).Text(l.Descripcion);
                        Celda(tabla).AlignRight().Text(l.Cantidad.ToString());
                        Celda(tabla).AlignRight().Text(FormatoArgentino.Importe(l.PrecioFinal));
                    }
                });

                col.Item().AlignRight().Text($"Importe total {FormatoArgentino.Importe(f.Total)}")
                    .FontFamily(RecursosPdf.FuenteTitulos).Bold().FontSize(13).FontColor(recursos.ColorPrimario);

                col.Item().PaddingTop(10).Row(fila =>
                {
                    fila.ConstantItem(110).Image(qr);
                    fila.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                    {
                        c.Item().Text($"CAE: {f.Cae}").SemiBold();
                        c.Item().Text($"Vencimiento del CAE: {FormatoArgentino.Fecha(f.VencimientoCae!.Value)}");
                        c.Item().Text($"Presupuesto de origen N° {origen.Numero}").FontSize(8);
                    });
                });
            });
        })).GeneratePdf();
    }

    private static IContainer Celda(TableDescriptor tabla) =>
        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
}
