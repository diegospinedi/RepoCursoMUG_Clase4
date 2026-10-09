namespace Optica.Api.Datos;

/// <summary>Errores de validación por clave de campo, con la acción correctiva (FR-041, RF-35).</summary>
public sealed class Errores
{
    public const string MensajeDecimales = "Usá como máximo 2 decimales.";

    private readonly Dictionary<string, List<string>> _errores = [];

    public bool Hay => _errores.Count > 0;

    public void Agregar(string clave, string mensaje)
    {
        if (!_errores.TryGetValue(clave, out var lista)) _errores[clave] = lista = [];
        lista.Add(mensaje);
    }

    /// <summary>Agrega el error de decimales y devuelve false si el valor tiene más de 2 (FR-010a).</summary>
    public bool DosDecimales(string clave, decimal? valor)
    {
        if (valor is not { } v || decimal.Round(v, 2) == v) return true;
        Agregar(clave, MensajeDecimales);
        return false;
    }

    public IResult Respuesta() => Results.ValidationProblem(_errores.ToDictionary(e => e.Key, e => e.Value.ToArray()));
}
