using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Optica.Api.Acceso;
using Optica.Api.Arca;
using Optica.Api.Datos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PragmasSqlite>();
builder.Services.AddDbContext<OpticaDbContext>((sp, o) => o
    .UseSqlite(builder.Configuration.GetConnectionString("Optica"))
    .AddInterceptors(sp.GetRequiredService<PragmasSqlite>()));

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Acceso: contraseña única, sesión por cookie de 60 minutos deslizantes (RNF-04, RNF-09).
builder.Services.AddSingleton<RegistroSeguridad>();
builder.Services.AddScoped<ServicioAcceso>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "optica.sesion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        o.SlidingExpiration = true;
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
        // Cambiar o restablecer la contraseña rota el sello y cierra las demás sesiones (FR-005c).
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var sello = ctx.Principal?.FindFirst(EndpointsAcceso.ClaimSello)?.Value;
            var vigente = await ctx.HttpContext.RequestServices.GetRequiredService<ServicioAcceso>().SelloActualAsync();
            if (sello != vigente)
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
// El vencimiento usa el reloj inyectado, así los tests lo controlan con FakeTimeProvider.
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<TimeProvider>((o, reloj) => o.TimeProvider = reloj);
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ARCA: solo el simulador hasta tener certificado de homologación (AGENTS.md). Nunca producción.
builder.Services.Configure<OpcionesArca>(builder.Configuration.GetSection("Arca"));
var entornoArca = builder.Configuration["Arca:Entorno"];
if (entornoArca != "Simulado")
    throw new InvalidOperationException(
        $"Arca:Entorno = '{entornoArca}' no está soportado: solo se admite 'Simulado' hasta tener certificado de homologación.");
builder.Services.AddSingleton<IServicioArca, ArcaSimulado>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<OpticaDbContext>().Database.Migrate();

app.UseExceptionHandler();

// En producción la API sirve el frontend compilado (research R4). Sin ForwardedHeaders: la IP remota
// decide qué pedidos son locales.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAcceso();

// Una ruta /api desconocida no cae en el frontend; sin sesión responde 401 como el resto de la API.
app.MapFallback("/api/{**resto}", () => Results.NotFound());
// La pantalla de ingreso tiene que cargar sin sesión.
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

public partial class Program;
