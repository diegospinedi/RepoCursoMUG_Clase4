using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
app.UseStatusCodePages();

// En producción la API sirve el frontend compilado (research R4). Sin ForwardedHeaders: la IP remota
// decide qué pedidos son locales.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
