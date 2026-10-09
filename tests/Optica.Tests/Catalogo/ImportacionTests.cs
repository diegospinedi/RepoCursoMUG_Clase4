using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Catalogo;
using Optica.Tests.Planillas;

namespace Optica.Tests.Catalogo;

public sealed record FilaInformada(int NumeroFila, string CodigoProveedor, string Resultado, string Razon);
public sealed record ResumenImportacion(int Id, int Creados, int Actualizados, FilaInformada[] Filas);
public sealed record ImportacionHistorial(int Id, DateTimeOffset Fecha, string NombreArchivo, int Creados, int Actualizados, int NoProcesados);

public static class AyudasImportacion
{
    public static Task<HttpResponseMessage> ImportarAsync(this HttpClient cliente, int proveedorId, byte[] archivo, string nombre = "lista.xlsx")
    {
        var contenido = new ByteArrayContent(archivo);
        contenido.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var form = new MultipartFormDataContent { { contenido, "archivo", nombre } };
        return cliente.PostAsync($"/api/proveedores/{proveedorId}/importaciones", form);
    }

    public static async Task<ResumenImportacion> ImportarBienAsync(this HttpClient cliente, int proveedorId, byte[] archivo)
    {
        var r = await cliente.ImportarAsync(proveedorId, archivo);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ResumenImportacion>())!;
    }

    public static async Task<ArticuloDto[]> ArticulosAsync(this HttpClient cliente, int proveedorId) =>
        (await cliente.GetFromJsonAsync<ArticuloDto[]>($"/api/articulos?proveedorId={proveedorId}"))!;
}

public class ImportacionTests
{
    private static async Task<(AppDePrueba App, HttpClient Cliente, ProveedorDto Proveedor)> PrepararAsync(
        decimal? margenPredeterminado = 50m, AppDePrueba? app = null)
    {
        app ??= new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.ConfigurarAsync(margenPredeterminado: margenPredeterminado);
        return (app, cliente, await cliente.CrearProveedorAsync("Lentes SA"));
    }

    private static async Task AssertFormatoInvalidoAsync(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("formato-planilla", problema!.Type);
    }

    [Fact]
    public async Task Un_codigo_nuevo_crea_el_articulo_con_el_margen_predeterminado()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(["ABC-1", "Armazón metal", 1210m]));

        Assert.Equal(1, resumen.Creados);
        Assert.Equal(0, resumen.Actualizados);
        var a = Assert.Single(await cliente.ArticulosAsync(p.Id));
        Assert.Equal(("ABC-1", "Armazón metal", 1210m, 50m, 1815m), (a.CodigoProveedor, a.Descripcion, a.PrecioCosto, a.Margen, a.PrecioVenta));
    }

    [Fact]
    public async Task Un_codigo_existente_actualiza_solo_el_costo_y_recalcula_la_venta()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m, "Armazón metal");

        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(["ABC-1", "Otra descripción", 2420m]));

        Assert.Equal(1, resumen.Actualizados);
        var a = Assert.Single(await cliente.ArticulosAsync(p.Id));
        Assert.Equal(("Armazón metal", 2420m, 50m, 3630m), (a.Descripcion, a.PrecioCosto, a.Margen, a.PrecioVenta));
    }

    [Fact]
    public async Task En_un_codigo_existente_la_descripcion_puede_venir_vacia()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m);
        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(["ABC-1", null, 2420m]));
        Assert.Equal(1, resumen.Actualizados);
        Assert.Empty(resumen.Filas);
    }

    [Fact]
    public async Task Solo_cambia_el_articulo_del_proveedor_elegido()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var otro = await cliente.CrearProveedorAsync("Ópticos SRL");
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m);
        await cliente.CrearArticuloAsync(otro.Id, "ABC-1", 1210m, 50m);

        await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(["ABC-1", "", 2420m]));

        Assert.Equal(2420m, Assert.Single(await cliente.ArticulosAsync(p.Id)).PrecioCosto);
        Assert.Equal(1210m, Assert.Single(await cliente.ArticulosAsync(otro.Id)).PrecioCosto);
    }

    [Fact]
    public async Task Precio_vacio_de_texto_o_con_formato_local_es_precio_invalido()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(
            ["A-1", "Uno", null],
            ["A-2", "Dos", "abc"],
            ["A-3", "Tres", "1.210,50"],
            ["A-4", "Cuatro", 100m]));

        Assert.Equal(1, resumen.Creados);
        Assert.Equal(
            [new FilaInformada(2, "A-1", "NoProcesada", "precio inválido"),
             new FilaInformada(3, "A-2", "NoProcesada", "precio inválido"),
             new FilaInformada(4, "A-3", "NoProcesada", "precio inválido")],
            resumen.Filas);
    }

    [Fact]
    public async Task Informa_falta_de_descripcion_falta_de_codigo_y_codigos_repetidos()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(
            ["N-1", null, 100m],
            [null, "Sin código", 100m],
            ["R-1", "Rep", 100m],
            ["R-1", "Rep", 200m]));

        Assert.Equal(0, resumen.Creados);
        Assert.Equal(
            [new FilaInformada(2, "N-1", "NoProcesada", "falta la descripción"),
             new FilaInformada(3, "", "NoProcesada", "falta el código"),
             new FilaInformada(4, "R-1", "NoProcesada", "código repetido en la planilla"),
             new FilaInformada(5, "R-1", "NoProcesada", "código repetido en la planilla")],
            resumen.Filas);
    }

    [Fact]
    public async Task Precios_negativos_o_cero_se_procesan_y_se_informan()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "E-1", 100m, 50m);
        await cliente.CrearArticuloAsync(p.Id, "E-2", 100m, 50m);

        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(
            ["E-1", null, -100m], ["E-2", null, 0m], ["N-1", "Nuevo", 0m]));

        Assert.Equal((1, 2), (resumen.Creados, resumen.Actualizados));
        Assert.Equal(
            [new FilaInformada(2, "E-1", "ActualizadoPrecioNegativoOCero", "actualizado con precio negativo o cero"),
             new FilaInformada(3, "E-2", "ActualizadoPrecioNegativoOCero", "actualizado con precio negativo o cero"),
             new FilaInformada(4, "N-1", "CreadoPrecioNegativoOCero", "creado con precio negativo o cero")],
            resumen.Filas);
        Assert.Equal(-100m, (await cliente.ArticulosAsync(p.Id)).Single(a => a.CodigoProveedor == "E-1").PrecioCosto);
    }

    public static TheoryData<string[]> EncabezadosInvalidos => new()
    {
        { ["Código en el proveedor", "Descripción", "Precio de Costo", "Extra"] },
        { ["Código en el proveedor", "Descripción"] },
        { ["Codigo en el proveedor", "Descripción", "Precio de Costo"] },
        { ["Código en el proveedor", "Descripción", "precio de costo"] },
    };

    [Theory]
    [MemberData(nameof(EncabezadosInvalidos))]
    public async Task Un_formato_distinto_rechaza_la_planilla_completa(string[] encabezados)
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m);

        await AssertFormatoInvalidoAsync(await cliente.ImportarAsync(p.Id,
            GeneradorPlanillas.Crear([["ABC-1", "X", 2420m, "sobra"]], encabezados)));

        Assert.Equal(1210m, Assert.Single(await cliente.ArticulosAsync(p.Id)).PrecioCosto);
    }

    [Fact]
    public async Task Un_archivo_que_no_es_una_planilla_se_rechaza()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await AssertFormatoInvalidoAsync(await cliente.ImportarAsync(p.Id, "no soy un excel"u8.ToArray()));
    }

    [Fact]
    public async Task Mas_de_10_MB_se_rechaza_sin_cambios()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await AssertFormatoInvalidoAsync(await cliente.ImportarAsync(p.Id, new byte[10 * 1024 * 1024 + 1]));
    }

    [Fact]
    public async Task Mas_de_20000_filas_se_rechaza_sin_cambios()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var filas = Enumerable.Range(0, 20_001).Select(i => new object?[] { $"C-{i}", "Lente", 100m });
        await AssertFormatoInvalidoAsync(await cliente.ImportarAsync(p.Id, GeneradorPlanillas.Crear(filas)));
        Assert.Empty(await cliente.ArticulosAsync(p.Id));
    }

    [Fact]
    public async Task Las_filas_vacias_se_ignoran_y_los_espacios_cuentan_en_el_codigo()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m);

        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(
            [null, null, null], ["ABC-1 ", "Con espacio", 500m], [null, null, null]));

        Assert.Equal((1, 0), (resumen.Creados, resumen.Actualizados));
        Assert.Empty(resumen.Filas);
        Assert.Equal(2, (await cliente.ArticulosAsync(p.Id)).Length);
    }

    [Fact]
    public async Task Un_codigo_numerico_se_toma_por_su_valor()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(
            [new GeneradorPlanillas.NumeroConFormato(123, "00000"), "Numérico", 100m]));
        Assert.Equal("123", Assert.Single(await cliente.ArticulosAsync(p.Id)).CodigoProveedor);
    }

    [Fact]
    public async Task Los_articulos_que_no_vienen_en_la_planilla_no_cambian()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "QUEDA", 1210m, 50m);
        await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(["OTRO", "Otro", 100m]));
        Assert.Equal(1210m, (await cliente.ArticulosAsync(p.Id)).Single(a => a.CodigoProveedor == "QUEDA").PrecioCosto);
    }

    [Fact]
    public async Task Sin_margen_predeterminado_no_importa()
    {
        var (app, cliente, p) = await PrepararAsync(margenPredeterminado: null);
        await using var _ = app;
        var r = await cliente.ImportarAsync(p.Id, GeneradorPlanillas.Crear(["ABC-1", "X", 100m]));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("margen-sin-configurar", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        Assert.Empty(await cliente.ArticulosAsync(p.Id));
    }

    [Fact]
    public async Task Una_falla_al_grabar_no_aplica_nada_ni_queda_en_el_historial()
    {
        var falla = new FallaAlGrabarImportacion();
        var (app, cliente, p) = await PrepararAsync(app: new AppDePrueba().ConServicios(s => s.AddSingleton<IInterceptor>(falla)));
        await using var _ = app;
        await cliente.CrearArticuloAsync(p.Id, "ABC-1", 1210m, 50m);
        falla.Activa = true;

        var r = await cliente.ImportarAsync(p.Id, GeneradorPlanillas.Crear(["ABC-1", null, 2420m], ["N-1", "Nuevo", 100m]));

        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.Equal("importacion-no-aplicada", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        falla.Activa = false;
        var a = Assert.Single(await cliente.ArticulosAsync(p.Id));
        Assert.Equal(1210m, a.PrecioCosto);
        Assert.Empty((await cliente.GetFromJsonAsync<ImportacionHistorial[]>($"/api/proveedores/{p.Id}/importaciones"))!);
    }

    [Fact]
    public async Task Una_segunda_importacion_simultanea_se_rechaza()
    {
        var freno = new FrenoAlGrabarImportacion();
        var (app, cliente, p) = await PrepararAsync(app: new AppDePrueba().ConServicios(s => s.AddSingleton<IInterceptor>(freno)));
        await using var _ = app;
        var otraSesion = await app.IngresarAsync();

        var primera = cliente.ImportarAsync(p.Id, GeneradorPlanillas.Crear(["A-1", "Uno", 100m]));
        await freno.Detenida.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var segunda = await otraSesion.ImportarAsync(p.Id, GeneradorPlanillas.Crear(["A-2", "Dos", 100m]));
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        Assert.Equal("importacion-en-curso", (await segunda.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);

        freno.Liberar.SetResult();
        Assert.Equal(HttpStatusCode.OK, (await primera).StatusCode);
    }

    [Fact]
    public async Task El_historial_guarda_la_importacion_y_sus_filas()
    {
        var (app, cliente, p) = await PrepararAsync();
        await using var _ = app;
        var resumen = await cliente.ImportarBienAsync(p.Id, GeneradorPlanillas.Crear(["A-1", "Uno", 100m], ["A-2", null, 100m]));

        var historial = await cliente.GetFromJsonAsync<ImportacionHistorial[]>($"/api/proveedores/{p.Id}/importaciones");
        var h = Assert.Single(historial!);
        Assert.Equal(("lista.xlsx", 1, 0, 1), (h.NombreArchivo, h.Creados, h.Actualizados, h.NoProcesados));
        var detalle = await cliente.GetFromJsonAsync<ResumenImportacion>($"/api/importaciones/{resumen.Id}");
        Assert.Equal(resumen.Filas, detalle!.Filas);
    }

    [Fact]
    public async Task Un_proveedor_inexistente_responde_404()
    {
        var (app, cliente, _) = await PrepararAsync();
        await using var __ = app;
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.ImportarAsync(99, GeneradorPlanillas.Crear(["A", "B", 1m]))).StatusCode);
    }

    /// <summary>Hace fallar el SaveChanges que graba la importación, después de aplicar los artículos.</summary>
    private sealed class FallaAlGrabarImportacion : SaveChangesInterceptor
    {
        public bool Activa { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
        {
            if (Activa && e.Context!.ChangeTracker.Entries<Importacion>().Any(x => x.State == EntityState.Added))
                throw new IOException("Falla simulada de la base.");
            return ValueTask.FromResult(r);
        }
    }

    /// <summary>Detiene la importación justo antes de grabarla, hasta que el test la libere.</summary>
    private sealed class FrenoAlGrabarImportacion : SaveChangesInterceptor
    {
        public TaskCompletionSource Detenida { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData e, InterceptionResult<int> r, CancellationToken ct = default)
        {
            if (e.Context!.ChangeTracker.Entries<Importacion>().Any(x => x.State == EntityState.Added))
            {
                Detenida.TrySetResult();
                await Liberar.Task;
            }
            return r;
        }
    }
}
