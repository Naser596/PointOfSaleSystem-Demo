using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddPayrollEmployeeAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalaryAmount",
                table: "PayrollRunLines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BonusAmount",
                table: "PayrollRunLines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherDeductionsAmount",
                table: "PayrollRunLines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PayrollRunLines",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseSalaryAmount",
                table: "PayrollRunLines");

            migrationBuilder.DropColumn(
                name: "BonusAmount",
                table: "PayrollRunLines");

            migrationBuilder.DropColumn(
                name: "OtherDeductionsAmount",
                table: "PayrollRunLines");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PayrollRunLines");
        }
    }
}
