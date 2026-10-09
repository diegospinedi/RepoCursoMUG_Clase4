using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Optica.Api.Catalogo;

/// <summary>Proveedor: solo el nombre, único sin distinguir mayúsculas; sin baja (FR-011a).</summary>
public sealed class Proveedor
{
    public const int LargoNombre = 100;

    public int Id { get; set; }
    public string Nombre { get; private set; } = "";

    /// <summary>Nombre en minúsculas para la unicidad.</summary>
    public string NombreClave { get; private set; } = "";

    public void Renombrar(string nombre)
    {
        Nombre = nombre.Trim();
        NombreClave = Clave(nombre);
    }

    public static string Clave(string nombre) => nombre.Trim().ToLowerInvariant();
}

internal sealed class ConfiguracionProveedor : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> e)
    {
        e.ToTable("Proveedores");
        e.Property(x => x.Nombre).HasMaxLength(Proveedor.LargoNombre);
        e.Property(x => x.NombreClave).HasMaxLength(Proveedor.LargoNombre);
        e.HasIndex(x => x.NombreClave).IsUnique();
    }
}
