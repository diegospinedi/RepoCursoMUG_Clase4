using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Optica.Tests.Catalogo;

public class ProveedoresTests
{
    [Fact]
    public async Task Da_de_alta_y_lista_proveedores()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.CrearProveedorAsync("Lentes SA");
        Assert.Contains(p, (await cliente.GetFromJsonAsync<ProveedorDto[]>("/api/proveedores"))!);
    }

    [Fact]
    public async Task Rechaza_un_nombre_vacio()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var r = await cliente.PostAsJsonAsync("/api/proveedores", new { nombre = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.True((await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ContainsKey("nombre"));
    }

    [Fact]
    public async Task Rechaza_un_nombre_repetido_sin_distinguir_mayusculas()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        await cliente.CrearProveedorAsync("Lentes SA");
        var r = await cliente.PostAsJsonAsync("/api/proveedores", new { nombre = "lentes sa" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.True((await r.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.ContainsKey("nombre"));
    }

    [Fact]
    public async Task Modifica_el_nombre()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var p = await cliente.CrearProveedorAsync("Lentes SA");
        var r = await cliente.PutAsJsonAsync($"/api/proveedores/{p.Id}", new { nombre = "Lentes Sur SA" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Lentes Sur SA", (await r.Content.ReadFromJsonAsync<ProveedorDto>())!.Nombre);
    }

    [Fact]
    public async Task Modificar_un_proveedor_inexistente_responde_404()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync("/api/proveedores/99", new { nombre = "X" })).StatusCode);
    }
}
