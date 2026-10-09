using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Optica.Api.Datos;

namespace Optica.Api.Presupuestos;

public enum EstadoPresupuesto
{
    Borrador,
    Final,
}

/// <summary>
/// Presupuesto con los datos del cliente guardados adentro (RF-38). En Final queda cerrado: no se modifica
/// ni vuelve a Borrador, y la base lo refuerza con triggers (FR-018, migración CierrePresupuestos).
/// </summary>
public sealed class Presupuesto
{
    public const int LargoNombre = 100;
    public const int LargoDomicilio = 200;
    public const int LargoEmail = 200;
    public const int LargoTelefono = 30;

    public int Id { get; set; }
    public int Numero { get; private set; }
    public DateOnly Fecha { get; private set; }
    public EstadoPresupuesto Estado { get; private set; } = EstadoPresupuesto.Borrador;
    public string Apellido { get; private set; } = "";
    public string Nombre { get; private set; } = "";

    /// <summary>Solo dígitos (7 u 8).</summary>
    public string Dni { get; private set; } = "";
    public string? Domicilio { get; private set; }
    public string? Email { get; private set; }
    public string? Telefono { get; private set; }
    public decimal Total { get; set; }
    public string ApellidoBusqueda { get; private set; } = "";
    public string NombreBusqueda { get; private set; } = "";
    public List<LineaPresupuesto> Lineas { get; set; } = [];

    public static Presupuesto Nuevo(int numero, DateOnly fecha, string apellido, string nombre, string dni, string? domicilio, string? email, string? telefono)
    {
        var p = new Presupuesto { Numero = numero, Fecha = fecha };
        p.AsignarCliente(apellido, nombre, dni, domicilio, email, telefono);
        return p;
    }

    public void AsignarCliente(string apellido, string nombre, string dni, string? domicilio, string? email, string? telefono)
    {
        Apellido = apellido;
        Nombre = nombre;
        Dni = Normalizacion.Digitos(dni);
        Domicilio = domicilio;
        Email = email;
        Telefono = telefono;
        ApellidoBusqueda = Normalizacion.Texto(apellido);
        NombreBusqueda = Normalizacion.Texto(nombre);
    }

    public void Cerrar() => Estado = EstadoPresupuesto.Final;
}

internal sealed class ConfiguracionPresupuesto : IEntityTypeConfiguration<Presupuesto>
{
    public void Configure(EntityTypeBuilder<Presupuesto> e)
    {
        e.ToTable("Presupuestos");
        e.HasIndex(x => x.Numero).IsUnique();
        e.HasIndex(x => x.Fecha);
        e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(10);
        e.Property(x => x.Apellido).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.Nombre).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.ApellidoBusqueda).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.NombreBusqueda).HasMaxLength(Presupuesto.LargoNombre);
        e.Property(x => x.Dni).HasMaxLength(8);
        e.Property(x => x.Domicilio).HasMaxLength(Presupuesto.LargoDomicilio);
        e.Property(x => x.Email).HasMaxLength(Presupuesto.LargoEmail);
        e.Property(x => x.Telefono).HasMaxLength(Presupuesto.LargoTelefono);
        e.HasMany(x => x.Lineas).WithOne().HasForeignKey(x => x.PresupuestoId).OnDelete(DeleteBehavior.Restrict);
    }
}
