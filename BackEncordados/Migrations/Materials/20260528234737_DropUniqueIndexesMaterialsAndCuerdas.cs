using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackEncordados.Migrations.Materials
{
    /// <inheritdoc />
    public partial class DropUniqueIndexesMaterialsAndCuerdas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Materiales_Tournament_Marca_Modelo_unique\"");

            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS \"IX_Cuerdas_Tournament_Marca_Modelo_unique\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
