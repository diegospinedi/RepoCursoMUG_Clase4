namespace Optica.Api.Datos;

/// <summary>Fechas en hora de Argentina (UTC−3, sin horario de verano), sin depender de la zona de la PC.</summary>
public static class HoraArgentina
{
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-3);

    public static DateTimeOffset Ahora(this TimeProvider reloj) => reloj.GetUtcNow().ToOffset(Desfase);

    public static DateOnly Hoy(this TimeProvider reloj) => DateOnly.FromDateTime(reloj.Ahora().DateTime);
}
