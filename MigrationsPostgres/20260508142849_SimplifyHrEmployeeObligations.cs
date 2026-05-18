using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class SimplifyHrEmployeeObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "PayrollObligations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Employees",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Employees",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "Employees",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Employees",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalNumber",
                table: "Employees",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryDueDay",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollObligations_CompanyId_EmployeeId_Status",
                table: "PayrollObligations",
                columns: new[] { "CompanyId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollObligations_EmployeeId",
                table: "PayrollObligations",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollObligations_Employees_EmployeeId",
                table: "PayrollObligations",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE "PayrollObligations" o
                SET "EmployeeId" = l."EmployeeId"
                FROM "PayrollRunLines" l
                WHERE l."PayrollObligationId" = o."Id"
                  AND o."EmployeeId" IS NULL;

                UPDATE "Employees"
                SET "SalaryDueDay" = 5
                WHERE "SalaryDueDay" <= 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollObligations_Employees_EmployeeId",
                table: "PayrollObligations");

            migrationBuilder.DropIndex(
                name: "IX_PayrollObligations_CompanyId_EmployeeId_Status",
                table: "PayrollObligations");

            migrationBuilder.DropIndex(
                name: "IX_PayrollObligations_EmployeeId",
                table: "PayrollObligations");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "PayrollObligations");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PersonalNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SalaryDueDay",
                table: "Employees");
        }
    }
}
