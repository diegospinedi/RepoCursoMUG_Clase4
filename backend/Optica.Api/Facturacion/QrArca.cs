using System.Text;
using System.Text.Json;
using Optica.Api.Arca;
using Optica.Api.Datos;
using QRCoder;

namespace Optica.Api.Facturacion;

public sealed record DatosQr(
    DateOnly Fecha, string Cuit, int PuntoVenta, TipoComprobante Tipo, long Numero, decimal Importe, int TipoDocRec, long NroDocRec, string Cae);

/// <summary>
/// Código QR de comprobantes electrónicos de ARCA (research R11): URL con el JSON en base64. El dominio y
/// la versión se validan contra la especificación vigente antes de pasar a homologación.
/// </summary>
public static class QrArca
{
    public const string UrlBase = "https://www.arca.gob.ar/fe/qr/?p=";

    public static string Url(DatosQr d)
    {
        var json = JsonSerializer.Serialize(new
        {
            ver = 1,
            fecha = d.Fecha.ToString("yyyy-MM-dd"),
            cuit = long.TryParse(Normalizacion.Digitos(d.Cuit), out var cuit) ? cuit : 0,
            ptoVta = d.PuntoVenta,
            tipoCmp = (int)d.Tipo,
            nroCmp = d.Numero,
            importe = d.Importe,
            moneda = "PES",
            ctz = 1,
            tipoDocRec = d.TipoDocRec,
            nroDocRec = d.NroDocRec,
            tipoCodAut = "E",
            codAut = long.Parse(d.Cae),
        });
        return UrlBase + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static byte[] Png(string url)
    {
        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(datos).GetGraphic(6);
    }
}
