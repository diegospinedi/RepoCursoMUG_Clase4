using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class CierrePresupuestos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Un presupuesto Final no se modifica, no se borra y no vuelve a Borrador (RF-08, RF-67, FR-018).
            // Defensa en profundidad: ninguna vía (ni un endpoint mal hecho) puede tocarlo. No borrar.
            migrationBuilder.Sql(@"
CREATE TRIGGER Presupuestos_Final_SinCambios BEFORE UPDATE ON Presupuestos
WHEN OLD.Estado = 'Final'
BEGIN SELECT RAISE(ABORT, 'El presupuesto está en estado Final y no se puede modificar.'); END;");
            migrationBuilder.Sql(@"
CREATE TRIGGER Presupuestos_Final_SinBorrar BEFORE DELETE ON Presupuestos
WHEN OLD.Estado = 'Final'
BEGIN SELECT RAISE(ABORT, 'El presupuesto está en estado Final y no se puede eliminar.'); END;");
            migrationBuilder.Sql(@"
CREATE TRIGGER LineasPresupuesto_Final_SinCambios BEFORE UPDATE ON LineasPresupuesto
WHEN (SELECT Estado FROM Presupuestos WHERE Id = OLD.PresupuestoId) = 'Final'
BEGIN SELECT RAISE(ABORT, 'El presupuesto está en estado Final y no se puede modificar.'); END;");
            migrationBuilder.Sql(@"
CREATE TRIGGER LineasPresupuesto_Final_SinBorrar BEFORE DELETE ON LineasPresupuesto
WHEN (SELECT Estado FROM Presupuestos WHERE Id = OLD.PresupuestoId) = 'Final'
BEGIN SELECT RAISE(ABORT, 'El presupuesto está en estado Final y no se puede modificar.'); END;");
            migrationBuilder.Sql(@"
CREATE TRIGGER LineasPresupuesto_Final_SinAgregar BEFORE INSERT ON LineasPresupuesto
WHEN (SELECT Estado FROM Presupuestos WHERE Id = NEW.PresupuestoId) = 'Final'
BEGIN SELECT RAISE(ABORT, 'El presupuesto está en estado Final y no se puede modificar.'); END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var trigger in new[]
            {
                "Presupuestos_Final_SinCambios", "Presupuestos_Final_SinBorrar", "LineasPresupuesto_Final_SinCambios",
                "LineasPresupuesto_Final_SinBorrar", "LineasPresupuesto_Final_SinAgregar",
            })
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
        }
    }
}
