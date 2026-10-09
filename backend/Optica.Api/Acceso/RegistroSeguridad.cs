using Optica.Api.Datos;

namespace Optica.Api.Acceso;

/// <summary>
/// Registro local de eventos de seguridad con fecha y hora (FR-005d). Nunca recibe contraseñas ni datos
/// de clientes: los mensajes son fijos.
/// </summary>
public sealed class RegistroSeguridad(IConfiguration configuracion, TimeProvider reloj)
{
    private static readonly Lock Candado = new();
    private readonly string _archivo = configuracion["RegistroSeguridad:Archivo"] ?? "logs/seguridad.log";

    public void Registrar(string evento)
    {
        var linea = $"{reloj.Ahora():yyyy-MM-dd HH:mm:ss zzz} {evento}{Environment.NewLine}";
        lock (Candado)
        {
            var carpeta = Path.GetDirectoryName(Path.GetFullPath(_archivo));
            if (carpeta is not null) Directory.CreateDirectory(carpeta);
            File.AppendAllText(_archivo, linea);
        }
    }
}
