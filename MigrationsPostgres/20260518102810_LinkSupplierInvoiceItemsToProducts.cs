using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class LinkSupplierInvoiceItemsToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SupplierInvoiceItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceItems_ProductId",
                table: "SupplierInvoiceItems",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierInvoiceItems_Products_ProductId",
                table: "SupplierInvoiceItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierInvoiceItems_Products_ProductId",
                table: "SupplierInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoiceItems_ProductId",
                table: "SupplierInvoiceItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SupplierInvoiceItems");
        }
    }
}
