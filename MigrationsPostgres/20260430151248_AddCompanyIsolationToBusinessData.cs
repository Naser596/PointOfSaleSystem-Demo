using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddCompanyIsolationToBusinessData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_InvoiceNumber",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Products_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_Code",
                table: "Discounts");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SupplierInvoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "StockMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Sales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Discounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_CompanyId",
                table: "SupplierInvoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_CompanyId_InvoiceNumber",
                table: "SupplierInvoices",
                columns: new[] { "CompanyId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CompanyId",
                table: "StockMovements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CompanyId",
                table: "Sales",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId",
                table: "Products",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_SKU",
                table: "Products",
                columns: new[] { "CompanyId", "SKU" },
                unique: true,
                filter: "\"SKU\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_CompanyId",
                table: "Discounts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_CompanyId_Code",
                table: "Discounts",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId",
                table: "Customers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CompanyId",
                table: "Categories",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Companies_CompanyId",
                table: "Categories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Companies_CompanyId",
                table: "Customers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Discounts_Companies_CompanyId",
                table: "Discounts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Companies_CompanyId",
                table: "Products",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Companies_CompanyId",
                table: "Sales",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Companies_CompanyId",
                table: "StockMovements",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierInvoices_Companies_CompanyId",
                table: "SupplierInvoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Companies_CompanyId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Companies_CompanyId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Discounts_Companies_CompanyId",
                table: "Discounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Companies_CompanyId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Companies_CompanyId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Companies_CompanyId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierInvoices_Companies_CompanyId",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_CompanyId",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_CompanyId_InvoiceNumber",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CompanyId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CompanyId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_SKU",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_CompanyId",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_CompanyId_Code",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CompanyId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CompanyId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_InvoiceNumber",
                table: "SupplierInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true,
                filter: "\"SKU\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_Code",
                table: "Discounts",
                column: "Code",
                unique: true);
        }
    }
}
