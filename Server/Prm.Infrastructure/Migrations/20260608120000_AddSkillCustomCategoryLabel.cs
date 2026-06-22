using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prm.Infrastructure.Migrations;

public partial class AddSkillCustomCategoryLabel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CustomCategoryLabel",
            table: "skills",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CustomCategoryLabel",
            table: "skills");
    }
}
