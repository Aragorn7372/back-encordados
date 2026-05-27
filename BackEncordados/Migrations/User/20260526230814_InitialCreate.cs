using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackEncordados.Migrations.User
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TournamentId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CloudinaryPublicId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Bonos = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "Bonos", "CloudinaryPublicId", "CreatedAt", "Email", "ImageUrl", "Name", "PasswordHash", "Phone", "Role", "TournamentId", "UpdatedAt", "Username", "Version" },
                values: new object[] { "01KS0Q28TD6SAPN0GN0XKRPK5D", 100.0, null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "juan@tenis.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Juan Martínez", "$2a$11$vm89IAT3Kz8i95DTqRg6nuXd8EsUIRdgdFhHvezM7TBvMoidExpt.", "956789012", "USER", "01KS0Q28TEJ0SYA6JJ5H4W4CMP", new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "jugador_juan", 0L });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CloudinaryPublicId", "CreatedAt", "Email", "ImageUrl", "Name", "PasswordHash", "Phone", "Role", "TournamentId", "UpdatedAt", "Username", "Version" },
                values: new object[] { "01KS0Q28TE2EFVQTCW8EN0W0MF", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "owner@encordados.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Propietario", "$2a$11$8fZDZSSFPWUG7wP8PKVGpecehuSNofLupnr6o9eG.9rggszddiUEO", "923456789", "OWNER", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "owner_principal", 0L });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "Bonos", "CloudinaryPublicId", "CreatedAt", "Email", "ImageUrl", "Name", "PasswordHash", "Phone", "Role", "TournamentId", "UpdatedAt", "Username", "Version" },
                values: new object[] { "01KS0Q28TE3RJTW6W35NJRMTZ4", 25.0, null, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ana@tenis.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Ana Pérez", "$2a$11$vm89IAT3Kz8i95DTqRg6nuXd8EsUIRdgdFhHvezM7TBvMoidExpt.", "967890123", "USER", "01KS0Q28TEJ0SYA6JJ5H4W4CMP", new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "jugador_ana", 0L });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CloudinaryPublicId", "CreatedAt", "Email", "ImageUrl", "Name", "PasswordHash", "Phone", "Role", "TournamentId", "UpdatedAt", "Username", "Version" },
                values: new object[,]
                {
                    { "01KS0Q28TE5BA449NS2EVCBTDQ", null, new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "user@example.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "User", "$2a$11$vm89IAT3Kz8i95DTqRg6nuXd8EsUIRdgdFhHvezM7TBvMoidExpt.", "999999000", "USER", null, new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "user", 0L },
                    { "01KS0Q28TE6CVB0NYYANTWEK7B", null, new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "maria@encordados.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "María López", "$2a$11$h4sq7OQFn9BItSjblbZwYe7O.aNbp40eM5A2tXZyLlXhK2WALoI7.", "945678901", "ENCORDER", null, new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "maria_encordadora", 0L },
                    { "01KS0Q28TE7CMWS2D8RVDFA7YJ", null, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "carlos@encordados.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Carlos García", "$2a$11$h4sq7OQFn9BItSjblbZwYe7O.aNbp40eM5A2tXZyLlXhK2WALoI7.", "934567890", "ENCORDER", null, new DateTime(2025, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "carlos_encordador", 0L },
                    { "01KS0Q28TED4PWJPT7DMJ46WBN", null, new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "pedro@tenis.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Pedro Rodríguez", "$2a$11$vm89IAT3Kz8i95DTqRg6nuXd8EsUIRdgdFhHvezM7TBvMoidExpt.", "978901234", "USER", "01KS0Q28TE9N7TG55K98TCB4X0", new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "jugador_pedro", 0L },
                    { "01KS0Q28TEHA2KF5YM3J6QS5Z9", null, new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "luis@encordados.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Luis Fernández", "$2a$11$xl2gKmt6XzEkFpdjbHVpEOHw2sbCo2kFZZhVG38lqst9PxxmR1Dda", "989012345", "SUPERVISOR", null, new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "supervisor_luis", 0L },
                    { "01KS0Q28TESE956013XYJKP6ST", null, new DateTime(2024, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@encordados.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Administrador", "$2a$11$qxEUeoXVUOqGr/HlrrDUZeRsMDcQwoiclUJZOsqEiGI/iCwx1TQDm", "912345678", "ADMIN", null, new DateTime(2024, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin_encordados", 0L },
                    { "01KS0Q28TEXTDY9TQNRAXKAJ81", null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "pablo@encordados.com", "avatar-photo-default-user-icon-600nw-2558759027_y1hcma", "Pablo Sánchez", "$2a$11$xl2gKmt6XzEkFpdjbHVpEOHw2sbCo2kFZZhVG38lqst9PxxmR1Dda", "990123456", "SUPERVISOR", null, new DateTime(2025, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "supervisor_pablo", 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_email_unique",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username_unique",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
