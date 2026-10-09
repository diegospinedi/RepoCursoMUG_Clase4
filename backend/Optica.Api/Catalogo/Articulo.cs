using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Optica.Api.Datos;

namespace Optica.Api.Catalogo;

/// <summary>Artículo del catálogo; siempre activo (no hay bajas). Precio de venta calculado (FR-012–FR-014).</summary>
public sealed class Articulo
{
    public const int LargoCodigo = 50;
    public const int LargoDescripcion = 200;

    /// <summary>Código autonumérico visible (RF-20).</summary>
    public int Codigo { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    /// <summary>Único por proveedor, comparación binaria: sensible a mayúsculas y espacios (FR-011, FR-045a).</summary>
    public string CodigoProveedor { get; set; } = "";
    public string Descripcion { get; private set; } = "";
    public decimal PrecioCosto { get; set; }
    public decimal Margen { get; set; }
    public decimal PrecioVenta { get; set; }
    public string DescripcionBusqueda { get; private set; } = "";

    public void Describir(string descripcion)
    {
        Descripcion = descripcion;
        DescripcionBusqueda = Normalizacion.Texto(descripcion);
    }

    public static Articulo Nuevo(int proveedorId, string codigoProveedor, string descripcion, decimal costo, decimal margen, decimal precioVenta)
    {
        var a = new Articulo { ProveedorId = proveedorId, CodigoProveedor = codigoProveedor, PrecioCosto = costo, Margen = margen, PrecioVenta = precioVenta };
        a.Describir(descripcion);
        return a;
    }
}

internal sealed class ConfiguracionArticulo : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> e)
    {
        e.ToTable("Articulos");
        e.HasKey(x => x.Codigo);
        e.Property(x => x.CodigoProveedor).HasMaxLength(Articulo.LargoCodigo).UseCollation("BINARY");
        e.Property(x => x.Descripcion).HasMaxLength(Articulo.LargoDescripcion);
        e.Property(x => x.DescripcionBusqueda).HasMaxLength(Articulo.LargoDescripcion);
        e.HasIndex(x => new { x.ProveedorId, x.CodigoProveedor }).IsUnique();
        e.HasIndex(x => x.DescripcionBusqueda);
        e.HasOne(x => x.Proveedor).WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
    }
}
