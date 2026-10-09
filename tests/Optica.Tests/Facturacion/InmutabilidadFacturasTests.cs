using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Datos;

namespace Optica.Tests.Facturacion;

public class InmutabilidadFacturasTests
{
    [Fact]
    public async Task No_hay_forma_de_modificar_ni_eliminar_una_factura_por_la_API()
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var f = await cliente.FacturarBienAsync((await cliente.PresupuestoFinalAsync()).Id);

        var put = await cliente.PutAsync($"/api/facturas/{f.FacturaId}", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var delete = await cliente.DeleteAsync($"/api/facturas/{f.FacturaId}");

        Assert.Contains(put.StatusCode, new[] { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound });
        Assert.Contains(delete.StatusCode, new[] { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound });
        Assert.Equal("Autorizada", (await cliente.FacturaAsync(f.FacturaId)).Estado);
    }

    [Theory]
    [InlineData("UPDATE Facturas SET Total = 1 WHERE Id = {0}")]
    [InlineData("UPDATE Facturas SET Estado = 'Pendiente' WHERE Id = {0}")]
    [InlineData("DELETE FROM Facturas WHERE Id = {0}")]
    public async Task La_base_impide_modificar_una_Autorizada_por_SQL_directo(string sql)
    {
        await using var app = new AppDePrueba();
        var cliente = await app.IngresarAsync();
        var f = await cliente.FacturarBienAsync((await cliente.PresupuestoFinalAsync()).Id);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(sql, f.FacturaId));
        Assert.Equal(1815m, (await cliente.FacturaAsync(f.FacturaId)).Total);
    }
}
