using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Optica.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Acceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Acceso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    HashContrasena = table.Column<string>(type: "TEXT", nullable: true),
                    IntentosFallidos = table.Column<int>(type: "INTEGER", nullable: false),
                    BloqueadoHasta = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SelloSeguridad = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acceso", x => x.Id);
                    table.CheckConstraint("CK_Acceso_Unica", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Acceso");
        }
    }
}
