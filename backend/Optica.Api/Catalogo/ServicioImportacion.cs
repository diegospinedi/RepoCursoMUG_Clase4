using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;

namespace Optica.Api.Catalogo;

public abstract record ResultadoImportacion
{
    public sealed record Aplicada(Importacion Importacion) : ResultadoImportacion;
    public sealed record ProveedorInexistente : ResultadoImportacion;
    public sealed record MargenSinConfigurar : ResultadoImportacion;
    public sealed record EnCurso : ResultadoImportacion;
    public sealed record FormatoInvalido(string Detalle) : ResultadoImportacion;
    public sealed record NoAplicada : ResultadoImportacion;
}

/// <summary>
/// Importa la planilla de un proveedor: alta de artículos nuevos y actualización de costos, todo o nada,
/// de a una importación por vez (FR-042 a FR-050a; research R7, R8).
/// </summary>
public sealed class ServicioImportacion(
    OpticaDbContext db, ServicioPrecios precios, TimeProvider reloj, CandadoImportacion candado, ILogger<ServicioImportacion> log)
{

    public async Task<ResultadoImportacion> ImportarAsync(int proveedorId, string nombreArchivo, Stream contenido, long longitud)
    {
        if (!await db.Set<Proveedor>().AnyAsync(p => p.Id == proveedorId)) return new ResultadoImportacion.ProveedorInexistente();
        var parametros = await precios.ParametrosAsync();
        if (parametros.MargenPredeterminado is not { } margenNuevo) return new ResultadoImportacion.MargenSinConfigurar();
        if (!await candado.Semaforo.WaitAsync(0)) return new ResultadoImportacion.EnCurso();

        try
        {
            IReadOnlyList<FilaLeida> filas;
            try
            {
                filas = LectorPlanilla.Leer(contenido, longitud);
            }
            catch (FormatoPlanillaException e)
            {
                return new ResultadoImportacion.FormatoInvalido(e.Message);
            }

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var importacion = await AplicarAsync(proveedorId, filas, margenNuevo, parametros.MultiploRedondeo);
                importacion.NombreArchivo = Path.GetFileName(nombreArchivo);
                importacion.Fecha = reloj.Ahora();
                db.Add(importacion);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return new ResultadoImportacion.Aplicada(importacion);
            }
            catch (Exception e)
            {
                // Todo o nada (FR-050): la transacción se descarta y el catálogo queda como estaba.
                log.LogError(e, "La importación del proveedor {ProveedorId} no se aplicó", proveedorId);
                await tx.RollbackAsync();
                db.ChangeTracker.Clear();
                return new ResultadoImportacion.NoAplicada();
            }
        }
        finally
        {
            candado.Semaforo.Release();
        }
    }

    private async Task<Importacion> AplicarAsync(int proveedorId, IReadOnlyList<FilaLeida> filas, decimal margenNuevo, decimal multiplo)
    {
        var existentes = await db.Set<Articulo>().Where(a => a.ProveedorId == proveedorId)
            .ToDictionaryAsync(a => a.CodigoProveedor, StringComparer.Ordinal);
        var repetidos = filas.Where(f => f.Codigo.Length > 0).GroupBy(f => f.Codigo, StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);

        var importacion = new Importacion { ProveedorId = proveedorId };
        void Informar(FilaLeida f, ResultadoFila resultado, string razon) =>
            importacion.Filas.Add(new FilaImportacion { NumeroFila = f.Numero, CodigoProveedor = f.Codigo, Resultado = resultado, Razon = razon });

        foreach (var f in filas)
        {
            if (f.Codigo.Length == 0) { Informar(f, ResultadoFila.NoProcesada, "falta el código"); continue; }
            if (f.Codigo.Length > Articulo.LargoCodigo) { Informar(f, ResultadoFila.NoProcesada, "código de más de 50 caracteres"); continue; }
            if (repetidos.Contains(f.Codigo)) { Informar(f, ResultadoFila.NoProcesada, "código repetido en la planilla"); continue; }
            if (f.Precio is not { } precio) { Informar(f, ResultadoFila.NoProcesada, "precio inválido"); continue; }

            if (existentes.TryGetValue(f.Codigo, out var articulo))
            {
                // Solo el costo: la descripción y el margen no cambian (FR-044).
                articulo.PrecioCosto = precio;
                articulo.PrecioVenta = ServicioPrecios.Calcular(precio, articulo.Margen, multiplo);
                importacion.Actualizados++;
                if (precio <= 0) Informar(f, ResultadoFila.ActualizadoPrecioNegativoOCero, "actualizado con precio negativo o cero");
                continue;
            }

            if (f.Descripcion.Length == 0) { Informar(f, ResultadoFila.NoProcesada, "falta la descripción"); continue; }
            if (f.Descripcion.Length > Articulo.LargoDescripcion) { Informar(f, ResultadoFila.NoProcesada, "descripción de más de 200 caracteres"); continue; }

            var nuevo = Articulo.Nuevo(proveedorId, f.Codigo, f.Descripcion, precio, margenNuevo, ServicioPrecios.Calcular(precio, margenNuevo, multiplo));
            db.Add(nuevo);
            existentes[f.Codigo] = nuevo;
            importacion.Creados++;
            if (precio <= 0) Informar(f, ResultadoFila.CreadoPrecioNegativoOCero, "creado con precio negativo o cero");
        }

        importacion.NoProcesados = importacion.Filas.Count(x => x.Resultado == ResultadoFila.NoProcesada);
        await db.SaveChangesAsync();
        return importacion;
    }
}

/// <summary>Una importación a la vez en toda la aplicación (singleton).</summary>
public sealed class CandadoImportacion
{
    public SemaphoreSlim Semaforo { get; } = new(1, 1);
}
