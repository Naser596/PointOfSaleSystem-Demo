using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApplication3.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddSalesHrReportsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConvertedFromDocumentId",
                table: "SalesDocuments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "SalesDocuments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "SalesDocuments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unpaid");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MonthlySalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    RunNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeductionsAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PayrollObligationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_PayrollObligations_PayrollObligationId",
                        column: x => x.PayrollObligationId,
                        principalTable: "PayrollObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRunLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PayrollRunId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DeductionsAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRunLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRunLines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRunLines_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocuments_ConvertedFromDocumentId",
                table: "SalesDocuments",
                column: "ConvertedFromDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_EmployeeNumber",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeNumber" },
                unique: true,
                filter: "\"EmployeeNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_IsActive",
                table: "Employees",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_EmployeeId",
                table: "PayrollRunLines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRunLines_PayrollRunId",
                table: "PayrollRunLines",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_CompanyId_PeriodStart_PeriodEnd",
                table: "PayrollRuns",
                columns: new[] { "CompanyId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_CompanyId_RunNumber",
                table: "PayrollRuns",
                columns: new[] { "CompanyId", "RunNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayrollObligationId",
                table: "PayrollRuns",
                column: "PayrollObligationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDocuments_SalesDocuments_ConvertedFromDocumentId",
                table: "SalesDocuments",
                column: "ConvertedFromDocumentId",
                principalTable: "SalesDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesDocuments_SalesDocuments_ConvertedFromDocumentId",
                table: "SalesDocuments");

            migrationBuilder.DropTable(
                name: "PayrollRunLines");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropIndex(
                name: "IX_SalesDocuments_ConvertedFromDocumentId",
                table: "SalesDocuments");

            migrationBuilder.DropColumn(
                name: "ConvertedFromDocumentId",
                table: "SalesDocuments");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "SalesDocuments");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "SalesDocuments");
        }
    }
}
