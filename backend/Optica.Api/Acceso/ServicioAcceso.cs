using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;

namespace Optica.Api.Acceso;

public abstract record ResultadoIngreso
{
    public sealed record Correcto(string Sello) : ResultadoIngreso;
    public sealed record Incorrecta : ResultadoIngreso;
    public sealed record Bloqueado(DateTimeOffset Hasta) : ResultadoIngreso;
    public sealed record NoDefinida : ResultadoIngreso;
}

/// <summary>Contraseña única, bloqueo temporal y sello de seguridad (RNF-04, RNF-08, RNF-14, FR-005a–d).</summary>
public sealed class ServicioAcceso(OpticaDbContext db, TimeProvider reloj, RegistroSeguridad registro)
{
    public const int LongitudMinima = 8;
    public const int IntentosMaximos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(5);

    private static readonly PasswordHasher<EstadoAcceso> Hasher = new();

    public async Task<EstadoAcceso> ObtenerAsync()
    {
        var estado = await db.Set<EstadoAcceso>().SingleOrDefaultAsync();
        if (estado is not null) return estado;
        estado = new EstadoAcceso();
        db.Add(estado);
        await db.SaveChangesAsync();
        return estado;
    }

    public DateTimeOffset? BloqueoVigente(EstadoAcceso estado) =>
        estado.BloqueadoHasta is { } hasta && hasta > reloj.GetUtcNow() ? hasta : null;

    /// <summary>Define la contraseña la primera vez. Devuelve false si ya estaba definida.</summary>
    public async Task<bool> DefinirAsync(string contrasena)
    {
        var estado = await ObtenerAsync();
        if (estado.HashContrasena is not null) return false;
        estado.HashContrasena = Hasher.HashPassword(estado, contrasena);
        await db.SaveChangesAsync();
        registro.Registrar("Contraseña definida por primera vez");
        return true;
    }

    public async Task<ResultadoIngreso> IngresarAsync(string contrasena)
    {
        var estado = await ObtenerAsync();
        if (estado.HashContrasena is null) return new ResultadoIngreso.NoDefinida();
        if (BloqueoVigente(estado) is { } hasta) return new ResultadoIngreso.Bloqueado(hasta);

        if (Hasher.VerifyHashedPassword(estado, estado.HashContrasena, contrasena) == PasswordVerificationResult.Failed)
        {
            estado.IntentosFallidos++;
            registro.Registrar($"Ingreso fallido (intento {estado.IntentosFallidos} de {IntentosMaximos})");
            if (estado.IntentosFallidos >= IntentosMaximos)
            {
                estado.IntentosFallidos = 0;
                estado.BloqueadoHasta = reloj.GetUtcNow() + DuracionBloqueo;
                registro.Registrar($"Acceso bloqueado hasta {estado.BloqueadoHasta.Value.ToOffset(TimeSpan.FromHours(-3)):HH:mm}");
            }
            await db.SaveChangesAsync();
            return new ResultadoIngreso.Incorrecta();
        }

        estado.IntentosFallidos = 0;
        estado.BloqueadoHasta = null;
        await db.SaveChangesAsync();
        return new ResultadoIngreso.Correcto(estado.SelloSeguridad);
    }

    /// <summary>Cambia la contraseña verificando la actual. Devuelve el sello nuevo o null si la actual no coincide.</summary>
    public async Task<string?> CambiarAsync(string actual, string nueva)
    {
        var estado = await ObtenerAsync();
        if (estado.HashContrasena is null ||
            Hasher.VerifyHashedPassword(estado, estado.HashContrasena, actual) == PasswordVerificationResult.Failed)
            return null;
        Reemplazar(estado, nueva);
        await db.SaveChangesAsync();
        registro.Registrar("Contraseña cambiada");
        return estado.SelloSeguridad;
    }

    /// <summary>Restablece sin conocer la anterior; solo se llama desde la PC del sistema (FR-005b).</summary>
    public async Task RestablecerAsync(string nueva)
    {
        var estado = await ObtenerAsync();
        Reemplazar(estado, nueva);
        await db.SaveChangesAsync();
        registro.Registrar("Contraseña restablecida desde la PC del sistema");
    }

    public async Task<string> SelloActualAsync() => (await ObtenerAsync()).SelloSeguridad;

    private static void Reemplazar(EstadoAcceso estado, string nueva)
    {
        estado.HashContrasena = Hasher.HashPassword(estado, nueva);
        estado.SelloSeguridad = Guid.NewGuid().ToString("N");
        estado.IntentosFallidos = 0;
        estado.BloqueadoHasta = null;
    }
}
