using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Optica.Api.Presupuestos;

/// <summary>
/// Línea del presupuesto. Descripción y precio unitario se copian del catálogo al cargarla y no cambian
/// aunque el artículo cambie después (FR-019, FR-024). El precio unitario no es editable.
/// </summary>
public sealed class LineaPresupuesto
{
    public const int CantidadMaxima = 9999;

    public int Id { get; set; }
    public int PresupuestoId { get; set; }
    public int Orden { get; set; }
    public int ArticuloCodigo { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Descuento { get; set; }
    public decimal PrecioConDescuento { get; set; }
    public decimal PrecioFinal { get; set; }
}

internal sealed class ConfiguracionLineaPresupuesto : IEntityTypeConfiguration<LineaPresupuesto>
{
    public void Configure(EntityTypeBuilder<LineaPresupuesto> e)
    {
        e.ToTable("LineasPresupuesto");
        e.Property(x => x.Descripcion).HasMaxLength(200);
    }
}
