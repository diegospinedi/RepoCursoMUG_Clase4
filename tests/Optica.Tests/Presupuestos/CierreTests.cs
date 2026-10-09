using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Datos;
using static Optica.Tests.Presupuestos.AyudasPresupuestos;

namespace Optica.Tests.Presupuestos;

public class CierreTests
{
    [Fact]
    public async Task Un_Final_no_se_modifica_y_queda_igual()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1));

        var r = await cliente.PutPresupuestoAsync(p.Id, "Final", Cliente(domicilio: "Otro"), Linea(a.Codigo, 5, id: p.Lineas[0].Id));

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("presupuesto-cerrado", (await r.Content.ReadFromJsonAsync<ProblemDetails>())!.Type);
        Assert.Equal(JsonSerializer.Serialize(p), JsonSerializer.Serialize(await cliente.PresupuestoAsync(p.Id)));
    }

    [Fact]
    public async Task Un_Final_no_vuelve_a_Borrador()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1));

        var r = await cliente.PutPresupuestoAsync(p.Id, "Borrador", Cliente(), Linea(a.Codigo, 1, id: p.Lineas[0].Id));

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("Final", (await cliente.PresupuestoAsync(p.Id)).Estado);
    }

    [Theory]
    [InlineData("UPDATE Presupuestos SET Apellido = 'X' WHERE Id = {0}")]
    [InlineData("UPDATE Presupuestos SET Estado = 'Borrador' WHERE Id = {0}")]
    [InlineData("DELETE FROM Presupuestos WHERE Id = {0}")]
    [InlineData("UPDATE LineasPresupuesto SET Cantidad = 9 WHERE PresupuestoId = {0}")]
    [InlineData("DELETE FROM LineasPresupuesto WHERE PresupuestoId = {0}")]
    [InlineData("INSERT INTO LineasPresupuesto (PresupuestoId, Orden, ArticuloCodigo, Descripcion, PrecioUnitario, Cantidad, Descuento, PrecioConDescuento, PrecioFinal) VALUES ({0}, 9, 1, 'x', 1, 1, 0, 1, 1)")]
    public async Task La_base_impide_modificar_un_Final_por_SQL_directo(string sql)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var a = await cliente.PrepararCatalogoAsync();
        var p = await cliente.CrearFinalAsync(Cliente(), Linea(a.Codigo, 1));

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(sql, p.Id));
        Assert.Equal(JsonSerializer.Serialize(p), JsonSerializer.Serialize(await cliente.PresupuestoAsync(p.Id)));
    }
}
