using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddSupplierInvoiceMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoodsReceiptId",
                table: "SupplierInvoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatchStatus",
                table: "SupplierInvoices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unmatched");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId",
                table: "SupplierInvoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_GoodsReceiptId",
                table: "SupplierInvoices",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_PurchaseOrderId",
                table: "SupplierInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierInvoices_GoodsReceipts_GoodsReceiptId",
                table: "SupplierInvoices",
                column: "GoodsReceiptId",
                principalTable: "GoodsReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierInvoices_PurchaseOrders_PurchaseOrderId",
                table: "SupplierInvoices",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierInvoices_GoodsReceipts_GoodsReceiptId",
                table: "SupplierInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierInvoices_PurchaseOrders_PurchaseOrderId",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_GoodsReceiptId",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_PurchaseOrderId",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptId",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "MatchStatus",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "SupplierInvoices");
        }
    }
}
