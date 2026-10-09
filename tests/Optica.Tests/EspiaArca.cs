using System.Collections.Concurrent;
using Optica.Api.Arca;

namespace Optica.Tests;

/// <summary>
/// Doble de ARCA programable: registra las llamadas y por defecto autoriza todo. Permite simular
/// rechazos, falta de respuesta, autorizaciones sin respuesta y comprobantes emitidos por fuera.
/// </summary>
public sealed class EspiaArca : IServicioArca
{
    private readonly ConcurrentDictionary<(int, TipoComprobante, long), Existe> _autorizados = new();
    private int _cae = 70000000;

    public ConcurrentQueue<string> Llamadas { get; } = new();
    public ConcurrentQueue<SolicitudComprobante> Solicitudes { get; } = new();

    /// <summary>Respuesta a la próxima solicitud; null = autorizar.</summary>
    public Rechazado? Rechazo { get; set; }

    /// <summary>No responde (lanza ArcaSinRespuestaException) sin autorizar.</summary>
    public bool SinRespuesta { get; set; }

    /// <summary>Autoriza y no responde.</summary>
    public bool AutorizarSinResponder { get; set; }

    /// <summary>Demora real de ARCA, para medir el tiempo propio del sistema.</summary>
    public TimeSpan Demora { get; set; }

    /// <summary>Registra un comprobante autorizado por fuera del sistema (caso "otro total").</summary>
    public void AutorizarPorFuera(int puntoVenta, TipoComprobante tipo, long numero, decimal total) =>
        _autorizados[(puntoVenta, tipo, numero)] = new Existe(total, NuevoCae(), new DateOnly(2026, 10, 19));

    public Task<long> UltimoAutorizadoAsync(int puntoVenta, TipoComprobante tipo, CancellationToken ct)
    {
        Llamadas.Enqueue(nameof(UltimoAutorizadoAsync));
        var numeros = _autorizados.Keys.Where(k => k.Item1 == puntoVenta && k.Item2 == tipo).Select(k => k.Item3);
        return Task.FromResult(numeros.DefaultIfEmpty(0).Max());
    }

    public async Task<ResultadoSolicitud> SolicitarCaeAsync(SolicitudComprobante s, CancellationToken ct)
    {
        Llamadas.Enqueue(nameof(SolicitarCaeAsync));
        Solicitudes.Enqueue(s);
        if (Demora > TimeSpan.Zero) await Task.Delay(Demora, ct);
        if (Rechazo is not null) return Rechazo;
        if (SinRespuesta) throw new ArcaSinRespuestaException();
        var existe = new Existe(s.Total, NuevoCae(), s.Fecha.AddDays(10));
        _autorizados[(s.PuntoVenta, s.Tipo, s.Numero)] = existe;
        if (AutorizarSinResponder) throw new ArcaSinRespuestaException();
        return new Autorizado(existe.Cae, existe.Vencimiento);
    }

    public Task<ResultadoConsulta> ConsultarAsync(int puntoVenta, TipoComprobante tipo, long numero, CancellationToken ct)
    {
        Llamadas.Enqueue(nameof(ConsultarAsync));
        return Task.FromResult<ResultadoConsulta>(
            _autorizados.TryGetValue((puntoVenta, tipo, numero), out var e) ? e : new NoExiste());
    }

    private string NuevoCae() => $"760{Interlocked.Increment(ref _cae):D11}";
}
