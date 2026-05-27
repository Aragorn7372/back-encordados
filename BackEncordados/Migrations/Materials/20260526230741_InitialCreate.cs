using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackEncordados.Migrations.Materials
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cuerdas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<string>(type: "text", nullable: false),
                    Marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<double>(type: "double precision", nullable: false),
                    StringFormat = table.Column<string>(type: "text", nullable: false),
                    StringsType = table.Column<string>(type: "text", nullable: false),
                    Calibre = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuerdas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materiales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<string>(type: "text", nullable: false),
                    Marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<double>(type: "double precision", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiales", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Cuerdas",
                columns: new[] { "Id", "Calibre", "CloudinaryPublicId", "CreatedAt", "ImageUrl", "IsDeleted", "Marca", "Modelo", "Precio", "Stock", "StringFormat", "StringsType", "TournamentId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, 1.25, null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Babolat", "RPM Blast", 19.989999999999998, 50, "Reel", "Polyester", "01KS0Q28TEJ0SYA6JJ5H4W4CMP", new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, 1.3, null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Wilson", "Champion's Choice", 24.989999999999998, 20, "Set", "NaturalGut", "01KS0Q28TEJ0SYA6JJ5H4W4CMP", new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, 1.25, null, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Luxilon", "ALU Power", 21.989999999999998, 45, "Reel", "Polyester", "01KS0Q28TE9N7TG55K98TCB4X0", new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, 1.3, null, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Head", "Intellitour", 18.5, 30, "Set", "Multifilament", "01KS0Q28TE9N7TG55K98TCB4X0", new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, 1.28, null, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Signum Pro", "Plasma", 17.989999999999998, 25, "Reel", "Hybrid", "01KS0Q28TEVEYS4303TXP202N4", new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6L, 1.25, null, new DateTime(2025, 5, 2, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Kirschbaum", "Pro Line", 15.99, 35, "Set", "SyntheticGut", "01KS0Q28TET0JHJV4T5YFDJWBW", new DateTime(2025, 5, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7L, 1.3, null, new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Babolat", "VS Team", 22.0, 40, "Reel", "NaturalGut", "01KS0Q28TE5BA449NS2EVCBTDQ", new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8L, 1.3, null, new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Technifibre", "XR2", 18.5, 60, "Set", "Polyester", "01KS0Q28TE5BA449NS2EVCBTDQ", new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Materiales",
                columns: new[] { "Id", "CloudinaryPublicId", "CreatedAt", "ImageUrl", "IsDeleted", "Marca", "Modelo", "Precio", "Stock", "TournamentId", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Head", "Hydrosorb Comfort", 8.9900000000000002, 100, "01KS0Q28TEJ0SYA6JJ5H4W4CMP", "Grip", new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Babolat", "Pro Overgrip", 3.9900000000000002, 200, "01KS0Q28TEJ0SYA6JJ5H4W4CMP", "Overgrip", new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, null, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Tourna", "Lead Tape 1/2\"", 12.99, 80, "01KS0Q28TE9N7TG55K98TCB4X0", "LeadTape", new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, null, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Wilson", "ShockShield", 9.9900000000000002, 50, "01KS0Q28TE9N7TG55K98TCB4X0", "Silicone", new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, null, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Head", "Graphene 360+", 199.99000000000001, 10, "01KS0Q28TEVEYS4303TXP202N4", "Otro", new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6L, null, new DateTime(2025, 5, 2, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Babolat", "Syn Pro", 6.9900000000000002, 75, "01KS0Q28TET0JHJV4T5YFDJWBW", "Grip", new DateTime(2025, 5, 2, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7L, null, new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Head", "Super Soft", 7.5, 150, "01KS0Q28TE5BA449NS2EVCBTDQ", "Grip", new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8L, null, new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "default-680x600_kw5ji6", false, "Tourna", "Grip Boost", 4.9900000000000002, 100, "01KS0Q28TE5BA449NS2EVCBTDQ", "Overgrip", new DateTime(2025, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cuerdas");

            migrationBuilder.DropTable(
                name: "Materiales");
        }
    }
}
