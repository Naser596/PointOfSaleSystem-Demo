using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class LinkPayrollLinesToObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayrollObligationId",
                table: "PayrollRunLines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_PayrollObligationId",
                table: "PayrollRunLines",
                column: "PayrollObligationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollRunLines_PayrollObligations_PayrollObligationId",
                table: "PayrollRunLines",
                column: "PayrollObligationId",
                principalTable: "PayrollObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollRunLines_PayrollObligations_PayrollObligationId",
                table: "PayrollRunLines");

            migrationBuilder.DropIndex(
                name: "IX_PayrollRunLines_PayrollObligationId",
                table: "PayrollRunLines");

            migrationBuilder.DropColumn(
                name: "PayrollObligationId",
                table: "PayrollRunLines");
        }
    }
}
