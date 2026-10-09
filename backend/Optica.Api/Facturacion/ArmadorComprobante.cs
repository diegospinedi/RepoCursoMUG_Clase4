using Optica.Api.Arca;
using Optica.Api.Configuracion;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;

namespace Optica.Api.Facturacion;

/// <summary>Arma el comprobante a partir del presupuesto Final y la Configuración vigente.</summary>
public static class ArmadorComprobante
{
    public const int DocTipoSinIdentificar = 99;
    public const int DocTipoDni = 96;

    public static Factura Armar(Presupuesto p, ParametrosNegocio c, int puntoVenta)
    {
        var f = new Factura
        {
            PresupuestoId = p.Id,
            PuntoVenta = puntoVenta,
            Total = p.Total,
            Apellido = p.Apellido,
            Nombre = p.Nombre,
            Dni = p.Dni,
            ApellidoBusqueda = p.ApellidoBusqueda,
            NombreBusqueda = p.NombreBusqueda,
        };

        // Factura B con Responsable Inscripto y desglose sobre el total; Factura C con Monotributo (FR-028).
        if (c.CondicionFiscal == CondicionFiscal.ResponsableInscripto)
        {
            f.Tipo = TipoComprobante.FacturaB;
            (f.Neto, f.Iva) = Calculadora.DesgloseFacturaB(p.Total, c.AlicuotaIva);
            f.AlicuotaIva = c.AlicuotaIva;
        }
        else
        {
            f.Tipo = TipoComprobante.FacturaC;
        }

        // Hasta el tope inclusive, consumidor final sin identificar; si lo supera, con su DNI (FR-029).
        (f.ReceptorDocTipo, f.ReceptorDocNro) = p.Total <= c.TopeIdentificacion
            ? (DocTipoSinIdentificar, "0")
            : (DocTipoDni, p.Dni);
        return f;
    }

    public static SolicitudComprobante Solicitud(Factura f) => new(
        f.PuntoVenta, f.Tipo, f.Numero, f.Fecha, f.ReceptorDocTipo, long.Parse(f.ReceptorDocNro),
        f.Total, f.Neto ?? f.Total, f.Iva ?? 0m, f.AlicuotaIva);
}
