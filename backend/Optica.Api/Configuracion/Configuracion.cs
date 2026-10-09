using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Optica.Api.Configuracion;

public enum CondicionFiscal
{
    ResponsableInscripto,
    Monotributo,
}

/// <summary>Parámetros de negocio, fila única (data-model.md, Configuracion; FR-006).</summary>
public sealed class ParametrosNegocio
{
    public const int IdUnico = 1;

    /// <summary>Las que acepta ARCA (FR-007a).</summary>
    public static readonly decimal[] AlicuotasValidas = [0m, 2.5m, 5m, 10.5m, 21m, 27m];

    public const decimal TopeMaximo = 999_999_999.99m;
    public const decimal MultiploMinimo = 0.01m;
    public const decimal MultiploMaximo = 1000m;
    public const decimal MargenMaximo = 1000m;

    public int Id { get; set; } = IdUnico;
    public decimal AlicuotaIva { get; set; } = 21m;
    public CondicionFiscal CondicionFiscal { get; set; } = CondicionFiscal.ResponsableInscripto;
    public decimal TopeIdentificacion { get; set; } = 10_000_000m;
    public decimal MultiploRedondeo { get; set; } = 0.01m;

    /// <summary>Null = sin configurar: la importación no arranca (FR-045).</summary>
    public decimal? MargenPredeterminado { get; set; }
}

internal sealed class ConfiguracionParametrosNegocio : IEntityTypeConfiguration<ParametrosNegocio>
{
    public void Configure(EntityTypeBuilder<ParametrosNegocio> e)
    {
        e.ToTable("Configuracion", t => t.HasCheckConstraint("CK_Configuracion_Unica", "Id = 1"));
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.CondicionFiscal).HasConversion<string>().HasMaxLength(30);
        e.HasData(new ParametrosNegocio());
    }
}
