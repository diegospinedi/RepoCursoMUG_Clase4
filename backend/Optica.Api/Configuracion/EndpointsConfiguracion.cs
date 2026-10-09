using System.Text.Json.Serialization;
using Optica.Api.Acceso;
using Optica.Api.Catalogo;
using Optica.Api.Datos;

namespace Optica.Api.Configuracion;

public static class EndpointsConfiguracion
{
    /// <summary>Nunca incluye el certificado ni el punto de venta (FR-008).</summary>
    public sealed record ConfiguracionDto(
        decimal AlicuotaIva, CondicionFiscal CondicionFiscal, decimal TopeIdentificacion, decimal MultiploRedondeo,
        decimal? MargenPredeterminado,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ArticulosRecalculados = null);

    public sealed record PedidoConfiguracion(
        decimal AlicuotaIva, CondicionFiscal CondicionFiscal, decimal TopeIdentificacion, decimal MultiploRedondeo, decimal? MargenPredeterminado);

    public static void MapConfiguracion(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/configuracion");

        grupo.MapGet("", async (ServicioPrecios precios) => Dto(await precios.ParametrosAsync()));

        grupo.MapPut("", async (PedidoConfiguracion p, OpticaDbContext db, ServicioPrecios precios, RegistroSeguridad registro) =>
        {
            var errores = Validar(p);
            if (errores.Hay) return errores.Respuesta();

            await using var tx = await db.Database.BeginTransactionAsync();
            var c = await precios.ParametrosAsync();
            var cambioMultiplo = c.MultiploRedondeo != p.MultiploRedondeo;
            c.AlicuotaIva = p.AlicuotaIva;
            c.CondicionFiscal = p.CondicionFiscal;
            c.TopeIdentificacion = p.TopeIdentificacion;
            c.MultiploRedondeo = p.MultiploRedondeo;
            c.MargenPredeterminado = p.MargenPredeterminado;
            await db.SaveChangesAsync();
            // Solo el múltiplo cambia precios: el precio de venta no depende de la alícuota (FR-009, FR-012).
            var recalculados = cambioMultiplo ? await precios.RecalcularTodoAsync(c.MultiploRedondeo) : 0;
            await tx.CommitAsync();

            registro.Registrar("Configuración modificada");
            return Results.Ok(Dto(c) with { ArticulosRecalculados = recalculados });
        });
    }

    private static ConfiguracionDto Dto(ParametrosNegocio c) =>
        new(c.AlicuotaIva, c.CondicionFiscal, c.TopeIdentificacion, c.MultiploRedondeo, c.MargenPredeterminado);

    private static Errores Validar(PedidoConfiguracion p)
    {
        var e = new Errores();
        if (!ParametrosNegocio.AlicuotasValidas.Contains(p.AlicuotaIva))
            e.Agregar("alicuotaIva", "Elegí una alícuota que acepte ARCA: 0; 2,5; 5; 10,5; 21; 27.");

        if (e.DosDecimales("topeIdentificacion", p.TopeIdentificacion) &&
            (p.TopeIdentificacion <= 0 || p.TopeIdentificacion > ParametrosNegocio.TopeMaximo))
            e.Agregar("topeIdentificacion", "El tope de identificación tiene que ser mayor a 0 y como máximo $ 999.999.999,99.");

        // 0,001 se informa como "mínimo 0,01" (AC-82), no como exceso de decimales.
        if (p.MultiploRedondeo < ParametrosNegocio.MultiploMinimo)
            e.Agregar("multiploRedondeo", "El múltiplo de redondeo debe ser como mínimo 0,01.");
        else if (e.DosDecimales("multiploRedondeo", p.MultiploRedondeo) && p.MultiploRedondeo > ParametrosNegocio.MultiploMaximo)
            e.Agregar("multiploRedondeo", "El múltiplo de redondeo puede ser como máximo 1.000.");

        if (e.DosDecimales("margenPredeterminado", p.MargenPredeterminado) &&
            p.MargenPredeterminado is { } m && (m < 0 || m > ParametrosNegocio.MargenMaximo))
            e.Agregar("margenPredeterminado", "El margen predeterminado debe estar entre 0 y 1000.");
        return e;
    }
}
