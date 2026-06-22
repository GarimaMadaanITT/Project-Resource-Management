using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prm.Infrastructure.Migrations;

public partial class RemoveUnusedLookupTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "system_configurations");
        migrationBuilder.DropTable(name: "activity_tags");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "activity_tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TagCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                TagName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TagCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_activity_tags", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_activity_tags_TagCode",
            table: "activity_tags",
            column: "TagCode",
            unique: true);

        migrationBuilder.CreateTable(
            name: "system_configurations",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ConfigKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ConfigValue = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_system_configurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_system_configurations_users_UpdatedByUserId",
                    column: x => x.UpdatedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_system_configurations_ConfigKey",
            table: "system_configurations",
            column: "ConfigKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_system_configurations_UpdatedByUserId",
            table: "system_configurations",
            column: "UpdatedByUserId");
    }
}
