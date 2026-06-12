using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prm.Infrastructure.Persistence;

#nullable disable

namespace Prm.Infrastructure.Migrations;

[DbContext(typeof(PrmDbContext))]
[Migration("20260611104000_EnsureSkillsCustomCategoryLabel")]
public partial class EnsureSkillsCustomCategoryLabel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE skills ADD COLUMN IF NOT EXISTS "CustomCategoryLabel" character varying(100);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CustomCategoryLabel",
            table: "skills");
    }
}
