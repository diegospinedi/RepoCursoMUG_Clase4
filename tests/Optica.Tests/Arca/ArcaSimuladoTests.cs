using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Optica.Api.Arca;

namespace Optica.Tests.Arca;

public sealed class ArcaSimuladoTests : IDisposable
{
    private readonly string _archivo = Path.Combine(Path.GetTempPath(), $"arca-{Guid.NewGuid():N}.json");
    private readonly FakeTimeProvider _reloj = new(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.FromHours(-3)));

    private ArcaSimulado Crear(string modo) => new(
        new OpcionesFijas(new OpcionesArca
        {
            Entorno = "Simulado",
            PuntoVenta = 3,
            TiempoEsperaSegundos = 30,
            Simulador = new OpcionesSimulador { Modo = modo, Archivo = _archivo },
        }),
        _reloj);

    private static SolicitudComprobante Solicitud(long numero, decimal total = 1815m) => new(
        PuntoVenta: 3, Tipo: TipoComprobante.FacturaB, Numero: numero, Fecha: new DateOnly(2026, 10, 9),
        DocTipo: 99, DocNro: 0, Total: total, Neto: 1500m, Iva: 315m, Alicuota: 21m);

    private static CancellationToken Corto() => new CancellationTokenSource(TimeSpan.FromMilliseconds(200)).Token;

    [Fact]
    public async Task Normal_autoriza_y_persiste_el_comprobante()
    {
        var arca = Crear("Normal");
        var r = await arca.SolicitarCaeAsync(Solicitud(1), CancellationToken.None);

        var autorizado = Assert.IsType<Autorizado>(r);
        Assert.Matches("^[0-9]{14}$", autorizado.Cae);
        Assert.Equal(1, await arca.UltimoAutorizadoAsync(3, TipoComprobante.FacturaB, CancellationToken.None));

        // Otra instancia lee el mismo archivo.
        var consulta = await Crear("Normal").ConsultarAsync(3, TipoComprobante.FacturaB, 1, CancellationToken.None);
        var existe = Assert.IsType<Existe>(consulta);
        Assert.Equal(1815m, existe.Total);
        Assert.Equal(autorizado.Cae, existe.Cae);
    }

    [Fact]
    public async Task Rechazar_devuelve_codigo_y_descripcion_sin_persistir()
    {
        var arca = Crear("Rechazar");
        var r = Assert.IsType<Rechazado>(await arca.SolicitarCaeAsync(Solicitud(1), CancellationToken.None));
        Assert.False(string.IsNullOrWhiteSpace(r.Codigo));
        Assert.False(string.IsNullOrWhiteSpace(r.Descripcion));
        Assert.Equal(0, await arca.UltimoAutorizadoAsync(3, TipoComprobante.FacturaB, CancellationToken.None));
    }

    [Fact]
    public async Task SinRespuesta_lanza_al_cancelarse_y_no_autoriza()
    {
        var arca = Crear("SinRespuesta");
        await Assert.ThrowsAsync<ArcaSinRespuestaException>(() => arca.SolicitarCaeAsync(Solicitud(1), Corto()));
        Assert.IsType<NoExiste>(await arca.ConsultarAsync(3, TipoComprobante.FacturaB, 1, CancellationToken.None));
    }

    [Fact]
    public async Task AutorizarSinResponder_persiste_y_lanza()
    {
        var arca = Crear("AutorizarSinResponder");
        await Assert.ThrowsAsync<ArcaSinRespuestaException>(() => arca.SolicitarCaeAsync(Solicitud(1), Corto()));
        Assert.IsType<Existe>(await arca.ConsultarAsync(3, TipoComprobante.FacturaB, 1, CancellationToken.None));
        Assert.Equal(1, await arca.UltimoAutorizadoAsync(3, TipoComprobante.FacturaB, CancellationToken.None));
    }

    [Fact]
    public async Task UltimoAutorizado_es_por_tipo_de_comprobante()
    {
        var arca = Crear("Normal");
        await arca.SolicitarCaeAsync(Solicitud(1), CancellationToken.None);
        await arca.SolicitarCaeAsync(Solicitud(2), CancellationToken.None);
        Assert.Equal(2, await arca.UltimoAutorizadoAsync(3, TipoComprobante.FacturaB, CancellationToken.None));
        Assert.Equal(0, await arca.UltimoAutorizadoAsync(3, TipoComprobante.FacturaC, CancellationToken.None));
    }

    public void Dispose()
    {
        if (File.Exists(_archivo)) File.Delete(_archivo);
    }

    private sealed class OpcionesFijas(OpcionesArca valor) : IOptionsMonitor<OpcionesArca>
    {
        public OpcionesArca CurrentValue => valor;
        public OpcionesArca Get(string? name) => valor;
        public IDisposable? OnChange(Action<OpcionesArca, string?> listener) => null;
    }
}
