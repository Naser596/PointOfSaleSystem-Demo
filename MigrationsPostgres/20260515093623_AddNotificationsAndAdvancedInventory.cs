using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddNotificationsAndAdvancedInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: true),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Recipient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationMessages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTraceLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: true),
                    TraceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TraceNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTraceLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTraceLots_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTraceLots_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTraceLots_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StockCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    CountNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    WarehouseId = table.Column<int>(type: "integer", nullable: false),
                    CountDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCounts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockCounts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockCountId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    StockLocationId = table.Column<int>(type: "integer", nullable: true),
                    SystemQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCounts_StockCountId",
                        column: x => x.StockCountId,
                        principalTable: "StockCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_CompanyId_NotificationType_EntityName_~",
                table: "NotificationMessages",
                columns: new[] { "CompanyId", "NotificationType", "EntityName", "EntityId", "Channel", "Recipient" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessages_CompanyId_Status_CreatedDate",
                table: "NotificationMessages",
                columns: new[] { "CompanyId", "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductTraceLots_CompanyId_ProductId_TraceNumber",
                table: "ProductTraceLots",
                columns: new[] { "CompanyId", "ProductId", "TraceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTraceLots_ProductId",
                table: "ProductTraceLots",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTraceLots_WarehouseId",
                table: "ProductTraceLots",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_ProductId",
                table: "StockCountLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountId",
                table: "StockCountLines",
                column: "StockCountId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockLocationId",
                table: "StockCountLines",
                column: "StockLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_CompanyId_CountNumber",
                table: "StockCounts",
                columns: new[] { "CompanyId", "CountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_CompanyId_Status_CountDate",
                table: "StockCounts",
                columns: new[] { "CompanyId", "Status", "CountDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_WarehouseId",
                table: "StockCounts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationMessages");

            migrationBuilder.DropTable(
                name: "ProductTraceLots");

            migrationBuilder.DropTable(
                name: "StockCountLines");

            migrationBuilder.DropTable(
                name: "StockCounts");
        }
    }
}
