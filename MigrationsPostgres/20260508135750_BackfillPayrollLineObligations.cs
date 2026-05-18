using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class BackfillPayrollLineObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "PayrollObligations"
                    ("CompanyId", "Description", "ObligationType", "PayeeName", "PeriodStart", "PeriodEnd", "DueDate", "Amount", "Status", "PaidDate", "Notes", "CreatedDate", "CreatedBy")
                SELECT
                    r."CompanyId",
                    'Payroll ' || e."FullName" || ' ' || to_char(r."PeriodStart", 'Mon DD, YYYY') || ' - ' || to_char(r."PeriodEnd", 'Mon DD, YYYY'),
                    'Payroll',
                    e."FullName",
                    r."PeriodStart",
                    r."PeriodEnd",
                    r."DueDate",
                    l."NetAmount",
                    'Open',
                    NULL,
                    'Payroll run ' || r."RunNumber" || ' | Payroll line ' || l."Id" || COALESCE(' | ' || NULLIF(l."Notes", ''), ''),
                    NOW(),
                    r."CreatedBy"
                FROM "PayrollRunLines" l
                JOIN "PayrollRuns" r ON r."Id" = l."PayrollRunId"
                JOIN "Employees" e ON e."Id" = l."EmployeeId"
                LEFT JOIN "PayrollObligations" aggregate_obligation ON aggregate_obligation."Id" = r."PayrollObligationId"
                WHERE l."PayrollObligationId" IS NULL
                  AND COALESCE(aggregate_obligation."Status", '') <> 'Paid';

                UPDATE "PayrollRunLines" l
                SET "PayrollObligationId" = o."Id"
                FROM "PayrollRuns" r, "PayrollObligations" o
                WHERE r."Id" = l."PayrollRunId"
                  AND l."PayrollObligationId" IS NULL
                  AND o."CompanyId" = r."CompanyId"
                  AND o."Notes" LIKE '%Payroll line ' || l."Id" || '%';

                UPDATE "PayrollObligations" aggregate_obligation
                SET
                    "Status" = 'Cancelled',
                    "Amount" = 0,
                    "Notes" = COALESCE(NULLIF(aggregate_obligation."Notes", '') || ' | ', '') || 'Replaced by employee payroll obligations'
                FROM "PayrollRuns" r
                WHERE r."PayrollObligationId" = aggregate_obligation."Id"
                  AND aggregate_obligation."Status" = 'Open'
                  AND EXISTS (
                      SELECT 1
                      FROM "PayrollRunLines" l
                      WHERE l."PayrollRunId" = r."Id"
                        AND l."PayrollObligationId" IS NOT NULL
                  );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PayrollObligations"
                WHERE "Notes" LIKE '%Payroll line %';
                """);
        }
    }
}
