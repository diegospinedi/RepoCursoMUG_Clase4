using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Optica.Api.Arca;
using Optica.Api.Catalogo;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;

namespace Optica.Api.Facturacion;

public abstract record ResultadoEmision
{
    public sealed record Autorizada(Factura Factura) : ResultadoEmision;
    public sealed record Rechazada(string Codigo, string Descripcion) : ResultadoEmision;
    public sealed record SinRespuesta(int? FacturaId) : ResultadoEmision;
    public sealed record PresupuestoBorrador : ResultadoEmision;
    public sealed record YaFacturado : ResultadoEmision;
    public sealed record NoEncontrado : ResultadoEmision;

    /// <summary>El número registrado figura autorizado con otro total (FR-037, RF-91).</summary>
    public sealed record Bloqueada(Factura Factura) : ResultadoEmision;
    public sealed record NoPendiente : ResultadoEmision;
    public sealed record Descartada : ResultadoEmision;
    public sealed record NoBloqueada : ResultadoEmision;
}

/// <summary>Las emisiones son de a una en toda la aplicación (research R10).</summary>
public sealed class CandadoEmision
{
    public SemaphoreSlim Semaforo { get; } = new(1, 1);
}

/// <summary>
/// Emisión segura (FR-030, FR-034 a FR-036): registra el número antes de enviar, espera como máximo
/// Arca:TiempoEsperaSegundos y no reintenta por su cuenta. Un rechazo no deja nada registrado.
/// </summary>
public sealed class ServicioEmision(
    OpticaDbContext db, IServicioArca arca, ServicioPrecios precios, IOptions<OpcionesArca> opciones,
    TimeProvider reloj, CandadoEmision candado)
{
    public async Task<ResultadoEmision> FacturarAsync(int presupuestoId)
    {
        var p = await db.Set<Presupuesto>().SingleOrDefaultAsync(x => x.Id == presupuestoId);
        if (p is null) return new ResultadoEmision.NoEncontrado();
        if (p.Estado != EstadoPresupuesto.Final) return new ResultadoEmision.PresupuestoBorrador();

        await candado.Semaforo.WaitAsync();
        try
        {
            if (await db.Set<Factura>().AnyAsync(f => f.PresupuestoId == presupuestoId && f.Estado != EstadoFactura.Descartada))
                return new ResultadoEmision.YaFacturado();

            var factura = ArmadorComprobante.Armar(p, await precios.ParametrosAsync(), opciones.Value.PuntoVenta);
            long ultimo;
            using (var espera = Espera())
            {
                try
                {
                    ultimo = await arca.UltimoAutorizadoAsync(factura.PuntoVenta, factura.Tipo, espera.Token);
                }
                catch (Exception e) when (e is ArcaSinRespuestaException or OperationCanceledException)
                {
                    return new ResultadoEmision.SinRespuesta(null);
                }
            }

            factura.Numerar(ultimo + 1);
            factura.Fecha = reloj.Hoy();
            db.Add(factura);
            await db.SaveChangesAsync();
            return await EnviarAsync(factura);
        }
        finally
        {
            candado.Semaforo.Release();
        }
    }

    /// <summary>
    /// Reintenta una emisión Pendiente consultando primero el número registrado (RF-65): con el mismo total
    /// recupera el CAE (RF-75, RF-90); si no existe, la envía con la fecha de hoy; con otro total la bloquea
    /// para revisión humana sin emitir ni recuperar nada (RF-91, RF-92).
    /// </summary>
    public async Task<ResultadoEmision> ReintentarAsync(int facturaId)
    {
        await candado.Semaforo.WaitAsync();
        try
        {
            var factura = await db.Set<Factura>().SingleOrDefaultAsync(f => f.Id == facturaId);
            if (factura is null) return new ResultadoEmision.NoEncontrado();
            if (factura.Estado == EstadoFactura.Bloqueada) return new ResultadoEmision.Bloqueada(factura);
            if (factura.Estado != EstadoFactura.Pendiente) return new ResultadoEmision.NoPendiente();

            ResultadoConsulta consulta;
            using (var espera = Espera())
            {
                try
                {
                    consulta = await arca.ConsultarAsync(factura.PuntoVenta, factura.Tipo, factura.Numero, espera.Token);
                }
                catch (Exception e) when (e is ArcaSinRespuestaException or OperationCanceledException)
                {
                    return new ResultadoEmision.SinRespuesta(factura.Id);
                }
            }

            switch (consulta)
            {
                case Existe e when e.Total == factura.Total:
                    factura.Cae = e.Cae;
                    factura.VencimientoCae = e.Vencimiento;
                    factura.Estado = EstadoFactura.Autorizada;
                    await db.SaveChangesAsync();
                    return new ResultadoEmision.Autorizada(factura);
                case Existe:
                    factura.Estado = EstadoFactura.Bloqueada;
                    await db.SaveChangesAsync();
                    return new ResultadoEmision.Bloqueada(factura);
                default:
                    factura.Fecha = reloj.Hoy();
                    await db.SaveChangesAsync();
                    return await EnviarAsync(factura);
            }
        }
        finally
        {
            candado.Semaforo.Release();
        }
    }

    /// <summary>La operadora confirmó que revisó el punto de venta en ARCA: la Bloqueada pasa a Descartada (FR-038b).</summary>
    public async Task<ResultadoEmision> ConfirmarRevisionAsync(int facturaId)
    {
        var factura = await db.Set<Factura>().SingleOrDefaultAsync(f => f.Id == facturaId);
        if (factura is null) return new ResultadoEmision.NoEncontrado();
        if (factura.Estado != EstadoFactura.Bloqueada) return new ResultadoEmision.NoBloqueada();
        factura.Estado = EstadoFactura.Descartada;
        await db.SaveChangesAsync();
        return new ResultadoEmision.Descartada();
    }

    /// <summary>Envía la emisión Pendiente con la fecha del envío efectivo (research R10).</summary>
    private async Task<ResultadoEmision> EnviarAsync(Factura factura)
    {
        ResultadoSolicitud respuesta;
        using (var espera = Espera())
        {
            try
            {
                respuesta = await arca.SolicitarCaeAsync(ArmadorComprobante.Solicitud(factura), espera.Token);
            }
            catch (Exception e) when (e is ArcaSinRespuestaException or OperationCanceledException)
            {
                // No se sabe si ARCA autorizó: queda Pendiente y la operadora decide cuándo reintentar.
                return new ResultadoEmision.SinRespuesta(factura.Id);
            }
        }

        switch (respuesta)
        {
            case Autorizado a:
                factura.Cae = a.Cae;
                factura.VencimientoCae = a.Vencimiento;
                factura.Estado = EstadoFactura.Autorizada;
                await db.SaveChangesAsync();
                return new ResultadoEmision.Autorizada(factura);
            case Rechazado r:
                db.Remove(factura);
                await db.SaveChangesAsync();
                return new ResultadoEmision.Rechazada(r.Codigo, r.Descripcion);
            default:
                throw new InvalidOperationException($"Respuesta de ARCA desconocida: {respuesta}");
        }
    }

    private CancellationTokenSource Espera() => new(TimeSpan.FromSeconds(opciones.Value.TiempoEsperaSegundos));
}
