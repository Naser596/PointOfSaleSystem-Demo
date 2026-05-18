using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddTenantBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "CompanySettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTaxRate",
                table: "CompanySettings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "CompanySettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#2563eb");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooterNote",
                table: "CompanySettings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceFooterNote",
                table: "CompanySettings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Companies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTaxRate",
                table: "Companies",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#2563eb");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooterNote",
                table: "Companies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceFooterNote",
                table: "Companies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultTaxRate",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ReceiptFooterNote",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceFooterNote",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DefaultTaxRate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReceiptFooterNote",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceFooterNote",
                table: "Companies");
        }
    }
}
