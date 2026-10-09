using System.Net;
using System.Text;
using System.Text.Json;
using Optica.Api.Arca;
using Optica.Api.Facturacion;
using Optica.Tests.Catalogo;
using Optica.Tests.Presupuestos;

namespace Optica.Tests.Facturacion;

public class PdfFacturaTests
{
    private static AppDePrueba ConEmisor() => new AppDePrueba()
        .Con("Emisor:RazonSocial", "Óptica Sistema de Prueba")
        .Con("Emisor:Domicilio", "Calle 42 n° 767, La Plata")
        .Con("Emisor:Cuit", "20-12345678-9")
        .Con("Emisor:CondicionIva", "IVA Responsable Inscripto")
        .Con("Emisor:IngresosBrutos", "20-12345678-9")
        .Con("Emisor:InicioActividades", "01/03/2010");

    [Fact]
    public async Task El_PDF_tiene_los_datos_de_RF30_el_QR_y_la_leyenda_de_simulado()
    {
        await using var app = ConEmisor();
        var cliente = await app.IngresarAsync();
        var f = await cliente.FacturarBienAsync((await cliente.PresupuestoFinalAsync()).Id);

        var r = await cliente.GetAsync($"/api/facturas/{f.FacturaId}/pdf");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/pdf", r.Content.Headers.ContentType!.MediaType);
        var (texto, imagenes) = LectorPdf.Leer(await r.Content.ReadAsByteArrayAsync());
        foreach (var esperado in new[]
        {
            "Óptica Sistema de Prueba", "Calle 42 n° 767, La Plata", "20-12345678-9", "IVA Responsable Inscripto",
            "FACTURA B", "Punto de venta: 0003", "Comp. N°: 00000001", "Fecha de emisión: 09/10/2026",
            "Consumidor Final", "Armazón metal", "$ 1.815,00", $"CAE: {f.Cae}", "Vencimiento del CAE: 19/10/2026",
            "COMPROBANTE SIMULADO — SIN VALIDEZ FISCAL",
        })
            Assert.Contains(esperado, texto);
        Assert.True(imagenes >= 2, "Tiene que tener el logo y el código QR");
    }

    [Fact]
    public async Task Con_DNI_el_receptor_se_identifica_con_su_documento_y_nombre()
    {
        await using var app = ConEmisor();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        await cliente.ConfigurarAsync(tope: 1000m);
        var f = await cliente.FacturarBienAsync(p.Id);

        var (texto, _) = LectorPdf.Leer(await cliente.GetByteArrayAsync($"/api/facturas/{f.FacturaId}/pdf"));

        Assert.Contains("DNI 23.456.789", texto);
        Assert.Contains("González, Ana", texto);
    }

    [Fact]
    public void El_QR_codifica_los_datos_del_comprobante_en_la_URL_de_ARCA()
    {
        var url = QrArca.Url(new DatosQr(
            new DateOnly(2026, 10, 9), "20-12345678-9", 3, TipoComprobante.FacturaB, 1, 1815m, 96, 23456789, "76000000000001"));

        const string prefijo = "https://www.arca.gob.ar/fe/qr/?p=";
        Assert.StartsWith(prefijo, url);
        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(url[prefijo.Length..])));
        var d = json.RootElement;
        Assert.Equal(1, d.GetProperty("ver").GetInt32());
        Assert.Equal("2026-10-09", d.GetProperty("fecha").GetString());
        Assert.Equal(20123456789, d.GetProperty("cuit").GetInt64());
        Assert.Equal(3, d.GetProperty("ptoVta").GetInt32());
        Assert.Equal(6, d.GetProperty("tipoCmp").GetInt32());
        Assert.Equal(1, d.GetProperty("nroCmp").GetInt64());
        Assert.Equal(1815m, d.GetProperty("importe").GetDecimal());
        Assert.Equal("PES", d.GetProperty("moneda").GetString());
        Assert.Equal(1, d.GetProperty("ctz").GetDecimal());
        Assert.Equal(96, d.GetProperty("tipoDocRec").GetInt32());
        Assert.Equal(23456789, d.GetProperty("nroDocRec").GetInt64());
        Assert.Equal("E", d.GetProperty("tipoCodAut").GetString());
        Assert.Equal(76000000000001, d.GetProperty("codAut").GetInt64());
        Assert.NotEmpty(QrArca.Png(url));
    }

    [Fact]
    public async Task Una_factura_no_autorizada_no_tiene_PDF()
    {
        await using var app = ConEmisor();
        var cliente = await app.IngresarAsync();
        var p = await cliente.PresupuestoFinalAsync();
        app.Arca.SinRespuesta = true;
        var r = await cliente.FacturarAsync(p.Id);
        var id = (await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<JsonElement>(r.Content)).GetProperty("facturaId").GetInt32();

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.GetAsync($"/api/facturas/{id}/pdf")).StatusCode);
    }
}
