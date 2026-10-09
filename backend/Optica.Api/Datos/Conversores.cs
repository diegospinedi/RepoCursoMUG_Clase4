using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Optica.Api.Datos;

/// <summary>
/// Importes en centavos y porcentajes en centésimos como INTEGER (research R2): SQLite guarda decimal como
/// TEXT, que no se puede sumar ni comparar. Ambos se escalan × 100; la validación limita a 2 decimales.
/// </summary>
public sealed class ConversorCentesimos() : ValueConverter<decimal, long>(
    v => (long)decimal.Round(v * 100m, 0, MidpointRounding.AwayFromZero),
    v => v / 100m);

public sealed class ConversorCentesimosNulable() : ValueConverter<decimal?, long?>(
    v => v.HasValue ? (long)decimal.Round(v.Value * 100m, 0, MidpointRounding.AwayFromZero) : null,
    v => v.HasValue ? v.Value / 100m : null);
