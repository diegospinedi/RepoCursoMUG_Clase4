using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Importaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Importaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProveedorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    NombreArchivo = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Creados = table.Column<int>(type: "INTEGER", nullable: false),
                    Actualizados = table.Column<int>(type: "INTEGER", nullable: false),
                    NoProcesados = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Importaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Importaciones_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FilasImportacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroFila = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoProveedor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Razon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilasImportacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FilasImportacion_Importaciones_ImportacionId",
                        column: x => x.ImportacionId,
                        principalTable: "Importaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilasImportacion_ImportacionId",
                table: "FilasImportacion",
                column: "ImportacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Importaciones_ProveedorId",
                table: "Importaciones",
                column: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilasImportacion");

            migrationBuilder.DropTable(
                name: "Importaciones");
        }
    }
}
