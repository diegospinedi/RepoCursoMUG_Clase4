namespace Optica.Api.Arca;

/// <summary>
/// Única puerta hacia WSFEv1 (contracts/servicio-arca.md). El módulo Facturacion depende solo de este
/// contrato; el adaptador real con WSAA llega cuando haya certificado de homologación.
/// </summary>
public interface IServicioArca
{
    /// <summary>FECompUltimoAutorizado: último número autorizado (0 si no hay).</summary>
    Task<long> UltimoAutorizadoAsync(int puntoVenta, TipoComprobante tipo, CancellationToken ct);

    /// <summary>FECAESolicitar. Lanza <see cref="ArcaSinRespuestaException"/> si se cancela por tiempo.</summary>
    Task<ResultadoSolicitud> SolicitarCaeAsync(SolicitudComprobante solicitud, CancellationToken ct);

    /// <summary>FECompConsultar.</summary>
    Task<ResultadoConsulta> ConsultarAsync(int puntoVenta, TipoComprobante tipo, long numero, CancellationToken ct);
}

/// <summary>Códigos de comprobante de ARCA.</summary>
public enum TipoComprobante
{
    FacturaB = 6,
    FacturaC = 11,
}

/// <summary>Datos que se envían a FECAESolicitar. En Factura C: neto = total, IVA 0 y sin alícuota.</summary>
public sealed record SolicitudComprobante(
    int PuntoVenta,
    TipoComprobante Tipo,
    long Numero,
    DateOnly Fecha,
    int DocTipo,
    long DocNro,
    decimal Total,
    decimal Neto,
    decimal Iva,
    decimal? Alicuota)
{
    /// <summary>Concepto 1: productos.</summary>
    public int Concepto => 1;

    /// <summary>Condición frente al IVA del receptor: 5 = Consumidor Final.</summary>
    public int CondicionIvaReceptor => 5;

    /// <summary>Id de alícuota de ARCA (0 % = 3, 2,5 % = 9, 5 % = 8, 10,5 % = 4, 21 % = 5, 27 % = 6).</summary>
    public int? IdAlicuota => Alicuota switch
    {
        null => null,
        0m => 3,
        2.5m => 9,
        5m => 8,
        10.5m => 4,
        21m => 5,
        27m => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(Alicuota), Alicuota, "Alícuota no admitida por ARCA."),
    };
}

public abstract record ResultadoSolicitud;

public sealed record Autorizado(string Cae, DateOnly Vencimiento) : ResultadoSolicitud;

public sealed record Rechazado(string Codigo, string Descripcion) : ResultadoSolicitud;

public abstract record ResultadoConsulta;

public sealed record NoExiste : ResultadoConsulta;

public sealed record Existe(decimal Total, string Cae, DateOnly Vencimiento) : ResultadoConsulta;

/// <summary>ARCA no respondió a tiempo: no se sabe si autorizó (RF-65).</summary>
public sealed class ArcaSinRespuestaException(Exception? interna = null)
    : Exception("ARCA no respondió dentro del tiempo de espera.", interna);
