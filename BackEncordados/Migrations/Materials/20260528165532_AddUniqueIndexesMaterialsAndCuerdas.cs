using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackEncordados.Migrations.Materials
{
    /// <inheritdoc />
    public partial class AddUniqueIndexesMaterialsAndCuerdas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Materiales_Tournament_Marca_Modelo_unique",
                table: "Materiales",
                columns: new[] { "TournamentId", "Marca", "Modelo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuerdas_Tournament_Marca_Modelo_unique",
                table: "Cuerdas",
                columns: new[] { "TournamentId", "Marca", "Modelo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Materiales_Tournament_Marca_Modelo_unique",
                table: "Materiales");

            migrationBuilder.DropIndex(
                name: "IX_Cuerdas_Tournament_Marca_Modelo_unique",
                table: "Cuerdas");
        }
    }
}
