using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prm.Infrastructure.Persistence;

#nullable disable

namespace Prm.Infrastructure.Migrations;

[DbContext(typeof(PrmDbContext))]
[Migration("20260611103000_FixMigratedDepartmentValues")]
public partial class FixMigratedDepartmentValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE users
            SET "Department" = CASE
                WHEN "Department" IN ('Delivery', 'Engineering', 'Backend', 'Frontend', 'QA', 'DevOps', 'IT', 'HR')
                    THEN "Department"
                WHEN "Department" IN ('Automation Testing', 'Testing') THEN 'QA'
                ELSE 'Engineering'
            END
            WHERE "Department" IS NOT NULL
              AND "Department" NOT IN ('Delivery', 'Engineering', 'Backend', 'Frontend', 'QA', 'DevOps', 'IT', 'HR');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
