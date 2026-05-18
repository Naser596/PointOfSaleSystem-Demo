using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class ExpandCompanyObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PayrollObligations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObligationType",
                table: "PayrollObligations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Payroll");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidDate",
                table: "PayrollObligations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayeeName",
                table: "PayrollObligations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PayrollObligations");

            migrationBuilder.DropColumn(
                name: "ObligationType",
                table: "PayrollObligations");

            migrationBuilder.DropColumn(
                name: "PaidDate",
                table: "PayrollObligations");

            migrationBuilder.DropColumn(
                name: "PayeeName",
                table: "PayrollObligations");
        }
    }
}
