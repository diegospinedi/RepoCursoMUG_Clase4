using System.Net;

namespace Optica.Api.Acceso;

/// <summary>
/// "Desde la PC donde corre el sistema" = pedido originado en esa misma computadora (FR-001). Se decide por
/// la IP remota de la conexión, sin encabezados reenviados, para que no se pueda falsificar (research R4).
/// </summary>
public static class EsPedidoLocal
{
    public static bool Desde(HttpContext contexto) =>
        contexto.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip);
}
