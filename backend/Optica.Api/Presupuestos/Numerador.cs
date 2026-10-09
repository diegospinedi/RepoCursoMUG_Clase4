using Microsoft.EntityFrameworkCore;
using Optica.Api.Datos;

namespace Optica.Api.Presupuestos;

/// <summary>
/// Último número + 1, empezando en 1 (FR-017). Se llama dentro de una transacción de escritura de SQLite
/// (BEGIN IMMEDIATE, el modo por defecto de Microsoft.Data.Sqlite), así dos sesiones no obtienen el mismo
/// número; la segunda espera gracias a busy_timeout (RNF-13). El índice único de Numero lo respalda.
/// </summary>
public sealed class Numerador(OpticaDbContext db)
{
    public async Task<int> SiguienteAsync()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El número de presupuesto se obtiene dentro de una transacción.");
        return (await db.Set<Presupuesto>().MaxAsync(p => (int?)p.Numero) ?? 0) + 1;
    }
}
