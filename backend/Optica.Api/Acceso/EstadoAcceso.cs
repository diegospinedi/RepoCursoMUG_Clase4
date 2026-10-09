using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Optica.Api.Acceso;

/// <summary>Fila única (Id = 1) con la contraseña y el estado de bloqueo (data-model.md, Acceso).</summary>
public sealed class EstadoAcceso
{
    public const int IdUnico = 1;

    public int Id { get; set; } = IdUnico;

    /// <summary>Null hasta la primera definición; PBKDF2 con sal (FR-004).</summary>
    public string? HashContrasena { get; set; }

    /// <summary>0–5; vuelve a 0 con un ingreso correcto.</summary>
    public int IntentosFallidos { get; set; }

    /// <summary>Null, o ahora + 5 minutos tras el 5.º fallo (FR-002).</summary>
    public DateTimeOffset? BloqueadoHasta { get; set; }

    /// <summary>Cambia al cambiar o restablecer la contraseña e invalida las demás sesiones (FR-005c).</summary>
    public string SelloSeguridad { get; set; } = Guid.NewGuid().ToString("N");
}

internal sealed class ConfiguracionEstadoAcceso : IEntityTypeConfiguration<EstadoAcceso>
{
    public void Configure(EntityTypeBuilder<EstadoAcceso> e)
    {
        e.ToTable("Acceso", t => t.HasCheckConstraint("CK_Acceso_Unica", "Id = 1"));
        e.Property(x => x.Id).ValueGeneratedNever();
        e.Property(x => x.SelloSeguridad).HasMaxLength(64);
    }
}
