using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class NormalizePayrollObligationDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "PayrollObligations" o
                SET
                    "Description" = e."FullName",
                    "PayeeName" = e."FullName",
                    "Notes" = CASE
                        WHEN o."Notes" LIKE 'Payroll run %' THEN NULL
                        ELSE o."Notes"
                    END
                FROM "PayrollRunLines" l
                JOIN "Employees" e ON e."Id" = l."EmployeeId"
                WHERE l."PayrollObligationId" = o."Id"
                  AND o."ObligationType" = 'Payroll';

                UPDATE "PayrollObligations"
                SET "Notes" = NULL
                WHERE "ObligationType" = 'Payroll'
                  AND "Notes" LIKE 'Payroll run %';
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
