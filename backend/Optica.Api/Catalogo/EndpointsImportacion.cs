using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;

namespace Optica.Api.Catalogo;

public static class EndpointsImportacion
{
    public sealed record FilaDto(int NumeroFila, string CodigoProveedor, ResultadoFila Resultado, string Razon);
    public sealed record ResumenDto(int Id, int Creados, int Actualizados, IEnumerable<FilaDto> Filas);
    public sealed record HistorialDto(int Id, DateTimeOffset Fecha, string NombreArchivo, int Creados, int Actualizados, int NoProcesados);

    public static void MapImportacion(this WebApplication app)
    {
        // Sin antiforgery: la cookie de sesión es SameSite=Strict.
        app.MapPost("/api/proveedores/{id:int}/importaciones", async (int id, IFormFile? archivo, ServicioImportacion servicio) =>
        {
            if (archivo is null) return Formato("Elegí la planilla a importar.");
            await using var contenido = archivo.OpenReadStream();
            return await servicio.ImportarAsync(id, archivo.FileName, contenido, archivo.Length) switch
            {
                ResultadoImportacion.Aplicada a => Results.Ok(Resumen(a.Importacion)),
                ResultadoImportacion.ProveedorInexistente => Results.NotFound(),
                ResultadoImportacion.MargenSinConfigurar => Results.Problem(statusCode: 409, type: "margen-sin-configurar",
                    title: "Antes de importar, configurá el margen predeterminado para artículos nuevos en Configuración."),
                ResultadoImportacion.EnCurso => Results.Problem(statusCode: 409, type: "importacion-en-curso",
                    title: "Hay otra importación en curso. Esperá a que termine y volvé a intentar."),
                ResultadoImportacion.FormatoInvalido f => Formato(f.Detalle),
                _ => Results.Problem(statusCode: 500, type: "importacion-no-aplicada",
                    title: "No se pudo aplicar la importación; el catálogo quedó como estaba. Volvé a intentar."),
            };
        }).DisableAntiforgery();

        app.MapGet("/api/proveedores/{id:int}/importaciones", async (int id, OpticaDbContext db) =>
            await db.Set<Importacion>().Where(i => i.ProveedorId == id).OrderByDescending(i => i.Id)
                .Select(i => new HistorialDto(i.Id, i.Fecha, i.NombreArchivo, i.Creados, i.Actualizados, i.NoProcesados))
                .ToListAsync());

        app.MapGet("/api/importaciones/{id:int}", async (int id, OpticaDbContext db) =>
            await db.Set<Importacion>().Include(i => i.Filas).SingleOrDefaultAsync(i => i.Id == id) is { } i
                ? Results.Ok(Resumen(i))
                : Results.NotFound());
    }

    private static ResumenDto Resumen(Importacion i) =>
        new(i.Id, i.Creados, i.Actualizados,
            i.Filas.OrderBy(f => f.NumeroFila).Select(f => new FilaDto(f.NumeroFila, f.CodigoProveedor, f.Resultado, f.Razon)));

    private static IResult Formato(string detalle) =>
        Results.Problem(statusCode: 400, type: "formato-planilla", title: "La planilla no tiene el formato esperado.", detail: detalle);
}
