using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class FacturasInmutablesYBusqueda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PresupuestoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    PuntoVenta = table.Column<int>(type: "INTEGER", nullable: false),
                    Numero = table.Column<long>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ReceptorDocTipo = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceptorDocNro = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Total = table.Column<long>(type: "INTEGER", nullable: false),
                    Neto = table.Column<long>(type: "INTEGER", nullable: true),
                    Iva = table.Column<long>(type: "INTEGER", nullable: true),
                    AlicuotaIva = table.Column<long>(type: "INTEGER", nullable: true),
                    Cae = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    VencimientoCae = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Apellido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Dni = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    ApellidoBusqueda = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NombreBusqueda = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NumeroBusqueda = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facturas_Presupuestos_PresupuestoId",
                        column: x => x.PresupuestoId,
                        principalTable: "Presupuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Fecha",
                table: "Facturas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_PresupuestoId",
                table: "Facturas",
                column: "PresupuestoId",
                unique: true,
                filter: "Estado IN ('Pendiente', 'Bloqueada', 'Autorizada')");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_PuntoVenta_Tipo_Numero",
                table: "Facturas",
                columns: new[] { "PuntoVenta", "Tipo", "Numero" },
                unique: true);

            // Una factura con CAE (o descartada) no se modifica ni se elimina (RF-32, FR-031). No borrar.
            migrationBuilder.Sql(@"
CREATE TRIGGER Facturas_Cerradas_SinCambios BEFORE UPDATE ON Facturas
WHEN OLD.Estado IN ('Autorizada', 'Descartada')
BEGIN SELECT RAISE(ABORT, 'La factura está cerrada y no se puede modificar.'); END;");
            migrationBuilder.Sql(@"
CREATE TRIGGER Facturas_Cerradas_SinBorrar BEFORE DELETE ON Facturas
WHEN OLD.Estado IN ('Autorizada', 'Descartada')
BEGIN SELECT RAISE(ABORT, 'La factura está cerrada y no se puede eliminar.'); END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Facturas_Cerradas_SinCambios;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Facturas_Cerradas_SinBorrar;");

            migrationBuilder.DropTable(
                name: "Facturas");
        }
    }
}
