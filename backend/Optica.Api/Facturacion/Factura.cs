using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Optica.Api.Arca;
using Optica.Api.Datos;
using Optica.Api.Presupuestos;

namespace Optica.Api.Facturacion;

public enum EstadoFactura
{
    /// <summary>Registrada antes de enviar; sin respuesta de ARCA, se puede reintentar.</summary>
    Pendiente,

    /// <summary>El número figura autorizado con otro total: espera la revisión de la operadora.</summary>
    Bloqueada,

    /// <summary>Con CAE; final e inmutable.</summary>
    Autorizada,

    /// <summary>Bloqueada que la operadora revisó; final, sin comprobante.</summary>
    Descartada,
}

/// <summary>
/// Emisión de un comprobante a consumidor final (data-model.md, Factura). Guarda la "foto" del comprobante
/// al registrarla, así un reintento manda lo mismo aunque cambie la Configuración (FR-037).
/// </summary>
public sealed class Factura
{
    public int Id { get; set; }
    public int PresupuestoId { get; set; }
    public Presupuesto? Presupuesto { get; set; }
    public EstadoFactura Estado { get; set; } = EstadoFactura.Pendiente;
    public TipoComprobante Tipo { get; set; }
    public int PuntoVenta { get; set; }

    /// <summary>Último autorizado + 1, registrado antes de enviar (FR-036).</summary>
    public long Numero { get; set; }
    public DateOnly Fecha { get; set; }

    /// <summary>99 = Consumidor Final sin identificar; 96 = DNI.</summary>
    public int ReceptorDocTipo { get; set; }
    public string ReceptorDocNro { get; set; } = "0";
    public decimal Total { get; set; }

    /// <summary>Solo Factura B (FR-028).</summary>
    public decimal? Neto { get; set; }
    public decimal? Iva { get; set; }
    public decimal? AlicuotaIva { get; set; }

    public string? Cae { get; set; }
    public DateOnly? VencimientoCae { get; set; }

    public string Apellido { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Dni { get; set; } = "";
    public string ApellidoBusqueda { get; set; } = "";
    public string NombreBusqueda { get; set; } = "";

    /// <summary>Punto de venta y número solo con dígitos, para la búsqueda parcial (RF-88).</summary>
    public string NumeroBusqueda { get; private set; } = "";

    public string NumeroCompleto => $"{PuntoVenta:D4}-{Numero:D8}";

    public string Letra => Tipo == TipoComprobante.FacturaB ? "B" : "C";

    public void Numerar(long numero)
    {
        Numero = numero;
        NumeroBusqueda = Normalizacion.Digitos(NumeroCompleto);
    }
}

internal sealed class ConfiguracionFactura : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> e)
    {
        e.ToTable("Facturas");
        e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(12);
        e.Property(x => x.ReceptorDocNro).HasMaxLength(11);
        e.Property(x => x.Cae).HasMaxLength(14);
        e.Property(x => x.Apellido).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.Nombre).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.ApellidoBusqueda).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.NombreBusqueda).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.Dni).HasMaxLength(8);
        e.Property(x => x.NumeroBusqueda).HasMaxLength(12);
        e.Ignore(x => x.NumeroCompleto);
        e.Ignore(x => x.Letra);
        e.HasOne(x => x.Presupuesto).WithMany().HasForeignKey(x => x.PresupuestoId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.PuntoVenta, x.Tipo, x.Numero }).IsUnique();
        // Un presupuesto origina como máximo una factura (FR-027a): una sola emisión viva por presupuesto.
        e.HasIndex(x => x.PresupuestoId).IsUnique().HasFilter("Estado IN ('Pendiente', 'Bloqueada', 'Autorizada')");
        e.HasIndex(x => x.Fecha);
    }
}
