using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Optica.Api.Catalogo;

public enum ResultadoFila
{
    NoProcesada,
    ActualizadoPrecioNegativoOCero,
    CreadoPrecioNegativoOCero,
}

/// <summary>Importación aplicada (FR-048a). Una planilla rechazada por formato o límites no se guarda.</summary>
public sealed class Importacion
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public string NombreArchivo { get; set; } = "";
    public int Creados { get; set; }
    public int Actualizados { get; set; }
    public int NoProcesados { get; set; }
    public List<FilaImportacion> Filas { get; set; } = [];
}

/// <summary>Solo las filas no procesadas o con precio negativo o cero.</summary>
public sealed class FilaImportacion
{
    public int Id { get; set; }
    public int ImportacionId { get; set; }

    /// <summary>Número de fila de la planilla (la primera de datos es la 2).</summary>
    public int NumeroFila { get; set; }

    /// <summary>Tal cual en la planilla; puede estar vacío.</summary>
    public string CodigoProveedor { get; set; } = "";
    public ResultadoFila Resultado { get; set; }
    public string Razon { get; set; } = "";
}

internal sealed class ConfiguracionImportacion : IEntityTypeConfiguration<Importacion>
{
    public void Configure(EntityTypeBuilder<Importacion> e)
    {
        e.ToTable("Importaciones");
        e.Property(x => x.NombreArchivo).HasMaxLength(255);
        e.HasIndex(x => x.ProveedorId);
        e.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        e.HasMany(x => x.Filas).WithOne().HasForeignKey(x => x.ImportacionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ConfiguracionFilaImportacion : IEntityTypeConfiguration<FilaImportacion>
{
    public void Configure(EntityTypeBuilder<FilaImportacion> e)
    {
        e.ToTable("FilasImportacion");
        e.Property(x => x.CodigoProveedor).HasMaxLength(Articulo.LargoCodigo * 4);
        e.Property(x => x.Resultado).HasConversion<string>().HasMaxLength(40);
        e.Property(x => x.Razon).HasMaxLength(100);
    }
}
