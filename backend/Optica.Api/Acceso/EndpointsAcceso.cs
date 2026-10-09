using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Optica.Api.Acceso;

public static class EndpointsAcceso
{
    public const string ClaimSello = "sello";
    private const string MensajeLongitud = "La contraseña debe tener al menos 8 caracteres.";

    public sealed record PedidoContrasena(string? Contrasena);
    public sealed record PedidoCambio(string? Actual, string? Nueva);
    public sealed record PedidoRestablecer(string? Nueva);
    public sealed record Estado(bool Definida, bool SesionIniciada, DateTimeOffset? BloqueadoHasta);

    public static void MapAcceso(this WebApplication app)
    {
        var acceso = app.MapGroup("/api/acceso").AllowAnonymous();

        acceso.MapGet("/estado", async (HttpContext ctx, ServicioAcceso servicio) =>
        {
            var estado = await servicio.ObtenerAsync();
            return new Estado(estado.HashContrasena is not null, ctx.User.Identity?.IsAuthenticated == true, servicio.BloqueoVigente(estado));
        });

        acceso.MapPost("/definir", async (PedidoContrasena pedido, HttpContext ctx, ServicioAcceso servicio) =>
        {
            if (!EsPedidoLocal.Desde(ctx)) return SoloLocal();
            if (Corta(pedido.Contrasena)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["contrasena"] = [MensajeLongitud] });
            return await servicio.DefinirAsync(pedido.Contrasena!)
                ? Results.NoContent()
                : Results.Problem(statusCode: 409, type: "contrasena-definida", title: "La contraseña ya está definida.");
        });

        acceso.MapPost("/ingresar", async (PedidoContrasena pedido, HttpContext ctx, ServicioAcceso servicio) =>
            await servicio.IngresarAsync(pedido.Contrasena ?? "") switch
            {
                ResultadoIngreso.Correcto c => await IniciarSesionAsync(ctx, c.Sello),
                ResultadoIngreso.Bloqueado b => Results.Problem(
                    statusCode: 423, type: "acceso-bloqueado",
                    title: "El acceso está bloqueado por 5 intentos fallidos.",
                    extensions: new Dictionary<string, object?> { ["bloqueadoHasta"] = b.Hasta.ToOffset(TimeSpan.FromHours(-3)) }),
                ResultadoIngreso.NoDefinida => Results.Problem(statusCode: 409, type: "contrasena-no-definida", title: "Primero hay que definir la contraseña."),
                _ => Results.Unauthorized(),
            });

        acceso.MapPost("/salir", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        acceso.MapPost("/cambiar", async (PedidoCambio pedido, HttpContext ctx, ServicioAcceso servicio) =>
        {
            if (Corta(pedido.Nueva)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["nueva"] = [MensajeLongitud] });
            var sello = await servicio.CambiarAsync(pedido.Actual ?? "", pedido.Nueva!);
            if (sello is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["actual"] = ["La contraseña actual no es correcta."] });
            return await IniciarSesionAsync(ctx, sello);
        }).RequireAuthorization();

        acceso.MapPost("/restablecer", async (PedidoRestablecer pedido, HttpContext ctx, ServicioAcceso servicio) =>
        {
            if (!EsPedidoLocal.Desde(ctx)) return SoloLocal();
            if (Corta(pedido.Nueva)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["nueva"] = [MensajeLongitud] });
            await servicio.RestablecerAsync(pedido.Nueva!);
            return Results.NoContent();
        });
    }

    private static bool Corta(string? contrasena) => (contrasena?.Length ?? 0) < ServicioAcceso.LongitudMinima;

    private static IResult SoloLocal() => Results.Problem(
        statusCode: 403, type: "solo-pc-local",
        title: "Esta acción solo se puede hacer desde la PC donde corre el sistema.");

    private static async Task<IResult> IniciarSesionAsync(HttpContext ctx, string sello)
    {
        var identidad = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "operadora"), new Claim(ClaimSello, sello)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
        return Results.NoContent();
    }
}
