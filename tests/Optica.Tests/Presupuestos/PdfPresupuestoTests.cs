using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Presupuestos;

public static class LectorPdf
{
    public static (string Texto, int ImagenesPrimeraPagina) Leer(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        var texto = string.Join(" ", documento.GetPages().SelectMany(p => p.GetWords()).Select(w => w.Text));
        return (texto, documento.GetPage(1).GetImages().Count());
    }
}

public class PdfPresupuestoTests
{
    [Fact]
    public async Task Un_Borrador_no_genera_PDF()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearPresupuestoAsync(Cliente(), Linea(a.Codigo, 1));

        var r = await cliente.GetAsync($"/api/presupuestos/{p.Id}/pdf");

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("presupuesto-borrador", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
    }

    [Fact]
    public async Task Un_Final_genera_el_PDF_con_logo_cliente_lineas_y_leyenda()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 2, 10));

        var r = await cliente.GetAsync($"/api/presupuestos/{p.Id}/pdf");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/pdf", r.Content.Headers.ContentType!.MediaType);
        var (texto, imagenes) = LectorPdf.Leer(await r.Content.ReadAsByteArrayAsync());
        Assert.True(imagenes >= 1, "La primera página tiene que tener el logo");
        Assert.Contains($"Presupuesto N° {p.Numero}", texto);
        Assert.Contains("González", texto);
        Assert.Contains("23.456.789", texto);
        Assert.Contains("Armazón metal", texto);
        Assert.Contains("$ 1.815,00", texto);
        Assert.Contains("$ 3.267,00", texto);
        Assert.Contains("Precios finales, IVA incluido", texto);
        Assert.Single(Regex.Matches(texto, @"\bIVA\b"));
        Assert.DoesNotContain("Neto", texto);
    }

    [Fact]
    public async Task Un_presupuesto_inexistente_responde_404()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/presupuestos/99/pdf")).StatusCode);
    }
}
