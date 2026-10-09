using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Optica.Api.Arca;

/// <summary>
/// Simulador de WSFEv1 para desarrollo y pruebas (no hay certificado de homologación). Persiste los
/// comprobantes en un archivo JSON fuera del repositorio. El modo se lee en cada pedido, así se puede
/// cambiar en appsettings.json sin reiniciar.
/// </summary>
public sealed class ArcaSimulado(IOptionsMonitor<OpcionesArca> opciones, TimeProvider reloj) : IServicioArca
{
    private static readonly SemaphoreSlim Candado = new(1, 1);

    public async Task<long> UltimoAutorizadoAsync(int puntoVenta, TipoComprobante tipo, CancellationToken ct)
    {
        var lista = await LeerAsync(ct);
        return lista.Where(c => c.PuntoVenta == puntoVenta && c.Tipo == (int)tipo).Select(c => c.Numero).DefaultIfEmpty(0).Max();
    }

    public async Task<ResultadoConsulta> ConsultarAsync(int puntoVenta, TipoComprobante tipo, long numero, CancellationToken ct)
    {
        var c = (await LeerAsync(ct)).FirstOrDefault(c => c.PuntoVenta == puntoVenta && c.Tipo == (int)tipo && c.Numero == numero);
        return c is null ? new NoExiste() : new Existe(c.Total, c.Cae, c.Vencimiento);
    }

    public async Task<ResultadoSolicitud> SolicitarCaeAsync(SolicitudComprobante s, CancellationToken ct)
    {
        switch (opciones.CurrentValue.Simulador.Modo)
        {
            case "Rechazar":
                return new Rechazado("10016", "El número o fecha del comprobante no se corresponde con el próximo a autorizar (simulado).");
            case "SinRespuesta":
                await EsperarSinResponderAsync(ct);
                throw new ArcaSinRespuestaException();
            case "AutorizarSinResponder":
                await AutorizarAsync(s, ct);
                await EsperarSinResponderAsync(ct);
                throw new ArcaSinRespuestaException();
            default:
                return await AutorizarAsync(s, ct);
        }
    }

    private async Task<Autorizado> AutorizarAsync(SolicitudComprobante s, CancellationToken ct)
    {
        var autorizado = new Autorizado(
            string.Concat(Enumerable.Range(0, 14).Select(_ => RandomNumberGenerator.GetInt32(10))),
            DateOnly.FromDateTime(reloj.GetLocalNow().DateTime).AddDays(10));
        await Candado.WaitAsync(ct);
        try
        {
            var lista = await LeerSinCandadoAsync(ct);
            lista.Add(new Registro(s.PuntoVenta, (int)s.Tipo, s.Numero, s.Total, autorizado.Cae, autorizado.Vencimiento));
            await File.WriteAllTextAsync(opciones.CurrentValue.Simulador.Archivo, JsonSerializer.Serialize(lista), ct);
        }
        finally
        {
            Candado.Release();
        }
        return autorizado;
    }

    private static async Task EsperarSinResponderAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException e)
        {
            throw new ArcaSinRespuestaException(e);
        }
    }

    private async Task<List<Registro>> LeerAsync(CancellationToken ct)
    {
        await Candado.WaitAsync(ct);
        try
        {
            return await LeerSinCandadoAsync(ct);
        }
        finally
        {
            Candado.Release();
        }
    }

    private async Task<List<Registro>> LeerSinCandadoAsync(CancellationToken ct)
    {
        var archivo = opciones.CurrentValue.Simulador.Archivo;
        if (!File.Exists(archivo)) return [];
        return JsonSerializer.Deserialize<List<Registro>>(await File.ReadAllTextAsync(archivo, ct)) ?? [];
    }

    private sealed record Registro(int PuntoVenta, int Tipo, long Numero, decimal Total, string Cae, DateOnly Vencimiento);
}
