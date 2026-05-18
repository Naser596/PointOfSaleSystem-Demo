using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddCompanyPlatformAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoDisableGraceDays",
                table: "Companies",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlatformAccessEndDate",
                table: "Companies",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlatformAccessStartDate",
                table: "Companies",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlatformDisabledDate",
                table: "Companies",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlatformDisabledReason",
                table: "Companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoDisableGraceDays",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlatformAccessEndDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlatformAccessStartDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlatformDisabledDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlatformDisabledReason",
                table: "Companies");
        }
    }
}
