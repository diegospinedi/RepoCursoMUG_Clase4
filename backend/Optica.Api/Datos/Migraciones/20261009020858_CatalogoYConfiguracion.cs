using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class CatalogoYConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuracion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AlicuotaIva = table.Column<long>(type: "INTEGER", nullable: false),
                    CondicionFiscal = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TopeIdentificacion = table.Column<long>(type: "INTEGER", nullable: false),
                    MultiploRedondeo = table.Column<long>(type: "INTEGER", nullable: false),
                    MargenPredeterminado = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion", x => x.Id);
                    table.CheckConstraint("CK_Configuracion_Unica", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NombreClave = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articulos",
                columns: table => new
                {
                    Codigo = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoProveedor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "BINARY"),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PrecioCosto = table.Column<long>(type: "INTEGER", nullable: false),
                    Margen = table.Column<long>(type: "INTEGER", nullable: false),
                    PrecioVenta = table.Column<long>(type: "INTEGER", nullable: false),
                    DescripcionBusqueda = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_Articulos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Configuracion",
                columns: new[] { "Id", "AlicuotaIva", "CondicionFiscal", "MargenPredeterminado", "MultiploRedondeo", "TopeIdentificacion" },
                values: new object[] { 1, 2100L, "ResponsableInscripto", null, 1L, 1000000000L });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_DescripcionBusqueda",
                table: "Articulos",
                column: "DescripcionBusqueda");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_ProveedorId_CodigoProveedor",
                table: "Articulos",
                columns: new[] { "ProveedorId", "CodigoProveedor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_NombreClave",
                table: "Proveedores",
                column: "NombreClave",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articulos");

            migrationBuilder.DropTable(
                name: "Configuracion");

            migrationBuilder.DropTable(
                name: "Proveedores");
        }
    }
}
