using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Optica.Api.Datos;

public sealed class OpticaDbContext(DbContextOptions<OpticaDbContext> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configuracion)
    {
        // Importes y porcentajes como enteros (research R2).
        configuracion.Properties<decimal>().HaveConversion<ConversorCentesimos>();
        configuracion.Properties<decimal?>().HaveConversion<ConversorCentesimosNulable>();
    }

    protected override void OnModelCreating(ModelBuilder modelo) =>
        modelo.ApplyConfigurationsFromAssembly(typeof(OpticaDbContext).Assembly);
}

/// <summary>
/// WAL para lecturas sin bloquear escrituras y espera de 5 s ante un bloqueo, para que dos sesiones
/// graben sin errores (RNF-13). Claves foráneas activas.
/// </summary>
public sealed class PragmasSqlite : DbConnectionInterceptor
{
    private const string Pragmas = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken ct = default)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = Pragmas;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
