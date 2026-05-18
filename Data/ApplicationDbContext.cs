using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Company> Companies { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<Sale> Sales { get; set; } = default!;
        public DbSet<SaleItem> SaleItems { get; set; } = default!;
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<StockMovement> StockMovements { get; set; } = default!;
        public DbSet<Customer> Customers { get; set; } = default!;
        public DbSet<Discount> Discounts { get; set; } = default!;
        public DbSet<CompanySettings> CompanySettings { get; set; } = default!;
        public DbSet<SupplierInvoice> SupplierInvoices { get; set; } = default!;
        public DbSet<SupplierInvoiceItem> SupplierInvoiceItems { get; set; } = default!;
        public DbSet<PayrollObligation> PayrollObligations { get; set; } = default!;
        public DbSet<AuditLog> AuditLogs { get; set; } = default!;
        public DbSet<Store> Stores { get; set; } = default!;
        public DbSet<Register> Registers { get; set; } = default!;
        public DbSet<RegisterSession> RegisterSessions { get; set; } = default!;
        public DbSet<ApprovalRequest> ApprovalRequests { get; set; } = default!;
        public DbSet<ApprovalRule> ApprovalRules { get; set; } = default!;
        public DbSet<OfflineSyncRecord> OfflineSyncRecords { get; set; } = default!;
        public DbSet<DocumentAttachment> DocumentAttachments { get; set; } = default!;
        public DbSet<FiscalPeriod> FiscalPeriods { get; set; } = default!;
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; } = default!;
        public DbSet<JournalEntry> JournalEntries { get; set; } = default!;
        public DbSet<JournalEntryLine> JournalEntryLines { get; set; } = default!;
        public DbSet<SalesDocument> SalesDocuments { get; set; } = default!;
        public DbSet<SalesDocumentLine> SalesDocumentLines { get; set; } = default!;
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = default!;
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; } = default!;
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = default!;
        public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; } = default!;
        public DbSet<Warehouse> Warehouses { get; set; } = default!;
        public DbSet<StockLocation> StockLocations { get; set; } = default!;
        public DbSet<WarehouseStock> WarehouseStocks { get; set; } = default!;
        public DbSet<StockTransfer> StockTransfers { get; set; } = default!;
        public DbSet<StockTransferLine> StockTransferLines { get; set; } = default!;
        public DbSet<ProductTraceLot> ProductTraceLots { get; set; } = default!;
        public DbSet<StockCount> StockCounts { get; set; } = default!;
        public DbSet<StockCountLine> StockCountLines { get; set; } = default!;
        public DbSet<FinancialAccount> FinancialAccounts { get; set; } = default!;
        public DbSet<BankTransaction> BankTransactions { get; set; } = default!;
        public DbSet<PaymentRecord> PaymentRecords { get; set; } = default!;
        public DbSet<NotificationMessage> NotificationMessages { get; set; } = default!;
        public DbSet<Employee> Employees { get; set; } = default!;
        public DbSet<PayrollRun> PayrollRuns { get; set; } = default!;
        public DbSet<PayrollRunLine> PayrollRunLines { get; set; } = default!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await ValidateCompanyIsolationAsync(cancellationToken);
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ValidateCompanyIsolationAsync(CancellationToken.None).GetAwaiter().GetResult();
            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Company
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LegalName).HasMaxLength(200);
                entity.Property(e => e.TaxNumber).HasMaxLength(80);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(40);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.PrimaryColor).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.DefaultTaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.InvoicePrefix).IsRequired().HasMaxLength(20);
                entity.Property(e => e.InvoiceFooterNote).HasMaxLength(1000);
                entity.Property(e => e.ReceiptFooterNote).HasMaxLength(1000);
                entity.Property(e => e.SupplierInvoiceFooterNote).HasMaxLength(1000);
                entity.Property(e => e.AutoDisableGraceDays).HasDefaultValue(3);
                entity.Property(e => e.PlatformDisabledReason).HasMaxLength(500);
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Users)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.CompanyId);
            });

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IconClass).HasMaxLength(50);
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SKU).HasMaxLength(50);
                entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => new { e.CompanyId, e.SKU }).IsUnique().HasFilter("\"SKU\" IS NOT NULL");
                entity.HasQueryFilter(e => !e.IsDeleted);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Sale
            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RefundedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ReturnedBy).HasMaxLength(256);
                entity.Property(e => e.ReturnReason).HasMaxLength(500);
                entity.HasIndex(e => e.SaleDate);
                entity.HasIndex(e => e.CashierId);
                entity.HasIndex(e => e.PaymentMethod);
                entity.HasIndex(e => e.Status);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Sales)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure SaleItem
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductNameSnapshot).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ProductSkuSnapshot).HasMaxLength(50);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RefundedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Sale)
                    .WithMany(s => s.SaleItems)
                    .HasForeignKey(e => e.SaleId);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId);
            });

            // Configure StockMovement
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.Property(e => e.MovementType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ReferenceType).HasMaxLength(20);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CreatedDate);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.StockMovements)
                    .HasForeignKey(e => e.ProductId);
            });

            // Configure Customer
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.TotalPurchases).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.Phone);
                entity.HasIndex(e => e.Email);
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure Discount
            modelBuilder.Entity<Discount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
            });

            // Configure Company Settings
            modelBuilder.Entity<CompanySettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.LegalName).HasMaxLength(200);
                entity.Property(e => e.TaxNumber).HasMaxLength(80);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(40);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.PrimaryColor).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.DefaultTaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.InvoicePrefix).IsRequired().HasMaxLength(20);
                entity.Property(e => e.InvoiceFooterNote).HasMaxLength(1000);
                entity.Property(e => e.ReceiptFooterNote).HasMaxLength(1000);
                entity.Property(e => e.SupplierInvoiceFooterNote).HasMaxLength(1000);
            });

            // Configure Supplier Invoice
            modelBuilder.Entity<SupplierInvoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.PurchaseOrder)
                    .WithMany()
                    .HasForeignKey(e => e.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.GoodsReceipt)
                    .WithMany()
                    .HasForeignKey(e => e.GoodsReceiptId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.PurchaseOrderId);
                entity.HasIndex(e => e.GoodsReceiptId);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SupplierInvoiceNumber).HasMaxLength(100);
                entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SupplierTaxNumber).HasMaxLength(80);
                entity.Property(e => e.SupplierAddress).HasMaxLength(500);
                entity.Property(e => e.SupplierPhone).HasMaxLength(40);
                entity.Property(e => e.SupplierEmail).HasMaxLength(256);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.MatchStatus).IsRequired().HasMaxLength(30);
                entity.HasIndex(e => new { e.CompanyId, e.InvoiceNumber }).IsUnique();
                entity.HasIndex(e => e.InvoiceDate);
            });

            // Configure Supplier Invoice Item
            modelBuilder.Entity<SupplierInvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LineTotal).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.SupplierInvoice)
                    .WithMany(i => i.Items)
                    .HasForeignKey(e => e.SupplierInvoiceId);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Payroll Obligation
            modelBuilder.Entity<PayrollObligation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.HasOne(e => e.Employee).WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.PeriodStart);
                entity.HasIndex(e => e.PeriodEnd);
                entity.HasIndex(e => new { e.CompanyId, e.EmployeeId, e.Status });
                entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ObligationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PayeeName).HasMaxLength(200);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            // Configure Audit Log
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.Action);
                entity.Property(e => e.UserId).HasMaxLength(256);
                entity.Property(e => e.UserName).HasMaxLength(256);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(80);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.EntityId).HasMaxLength(80);
                entity.Property(e => e.Summary).HasMaxLength(1000);
                entity.Property(e => e.BeforeJson);
                entity.Property(e => e.AfterJson);
                entity.Property(e => e.IpAddress).HasMaxLength(80);
            });

            // Configure Store
            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique().HasFilter("\"Code\" IS NOT NULL");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
                entity.Property(e => e.Code).HasMaxLength(40);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            // Configure Register
            modelBuilder.Entity<Register>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Store)
                    .WithMany(s => s.Registers)
                    .HasForeignKey(e => e.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique().HasFilter("\"Code\" IS NOT NULL");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
                entity.Property(e => e.Code).HasMaxLength(40);
            });

            // Configure Register Session
            modelBuilder.Entity<RegisterSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Register)
                    .WithMany(r => r.Sessions)
                    .HasForeignKey(e => e.RegisterId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.RegisterId);
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.OpenedBy).IsRequired().HasMaxLength(256);
                entity.Property(e => e.ClosedBy).HasMaxLength(256);
                entity.Property(e => e.OpeningCash).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ExpectedCash).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ClosingCash).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Difference).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Notes).HasMaxLength(1000);
            });

            ConfigureErpCore(modelBuilder);
            ConfigureHr(modelBuilder);

            // The app currently uses local DateTime values throughout the POS flow.
            // PostgreSQL "timestamp without time zone" avoids Npgsql UTC-kind errors.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp without time zone");
                    }
                }
            }
        }

        private static void ConfigureErpCore(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApprovalRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.Status });
                entity.HasIndex(e => e.RequestedDate);
                entity.Property(e => e.RequestType).IsRequired().HasMaxLength(80);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.EntityId).HasMaxLength(80);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.RequestedBy).HasMaxLength(256);
                entity.Property(e => e.ReviewedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<ApprovalRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.EntityName, e.ActionName, e.IsActive });
                entity.Property(e => e.RuleName).IsRequired().HasMaxLength(80);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.ActionName).IsRequired().HasMaxLength(80);
                entity.Property(e => e.AmountThreshold).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(1000);
            });

            modelBuilder.Entity<OfflineSyncRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Sale).WithMany().HasForeignKey(e => e.SaleId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.ClientId }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.Status, e.QueuedAt });
                entity.Property(e => e.ClientId).IsRequired().HasMaxLength(120);
                entity.Property(e => e.SyncType).IsRequired().HasMaxLength(40);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                entity.Property(e => e.PayloadJson).IsRequired();
            });

            modelBuilder.Entity<DocumentAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.EntityName, e.EntityId });
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.EntityId).HasMaxLength(80);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(260);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).HasMaxLength(120);
                entity.Property(e => e.UploadedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<FiscalPeriod>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.StartDate, e.EndDate });
                entity.Property(e => e.Name).IsRequired().HasMaxLength(80);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            });

            modelBuilder.Entity<ChartOfAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ParentAccount).WithMany().HasForeignKey(e => e.ParentAccountId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(40);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(180);
                entity.Property(e => e.AccountType).IsRequired().HasMaxLength(40);
            });

            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.FiscalPeriod).WithMany().HasForeignKey(e => e.FiscalPeriodId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.EntryNumber }).IsUnique();
                entity.HasIndex(e => e.EntryDate);
                entity.Property(e => e.EntryNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.SourceType).HasMaxLength(120);
                entity.Property(e => e.SourceId).HasMaxLength(80);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<JournalEntryLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.JournalEntry).WithMany(e => e.Lines).HasForeignKey(e => e.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Account).WithMany(e => e.JournalEntryLines).HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Memo).HasMaxLength(300);
                entity.Property(e => e.Debit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Credit).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<SalesDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.ConvertedFromDocument).WithMany().HasForeignKey(e => e.ConvertedFromDocumentId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.DocumentNumber }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.DocumentType, e.Status });
                entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(40);
                entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(30);
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<SalesDocumentLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.SalesDocument).WithMany(e => e.Lines).HasForeignKey(e => e.SalesDocumentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LineTotal).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.OrderNumber }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.Status });
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SupplierTaxNumber).HasMaxLength(80);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<PurchaseOrderLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.PurchaseOrder).WithMany(e => e.Lines).HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.LineTotal).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<GoodsReceipt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.PurchaseOrder).WithMany().HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.ReceiptNumber }).IsUnique();
                entity.Property(e => e.ReceiptNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.ReceivedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<GoodsReceiptLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.GoodsReceipt).WithMany(e => e.Lines).HasForeignKey(e => e.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique().HasFilter("\"Code\" IS NOT NULL");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
                entity.Property(e => e.Code).HasMaxLength(40);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            modelBuilder.Entity<StockLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Warehouse).WithMany(e => e.Locations).HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.WarehouseId, e.Code }).IsUnique().HasFilter("\"Code\" IS NOT NULL");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
                entity.Property(e => e.Code).HasMaxLength(40);
            });

            modelBuilder.Entity<WarehouseStock>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.StockLocation).WithMany().HasForeignKey(e => e.StockLocationId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.WarehouseId, e.StockLocationId, e.ProductId }).IsUnique();
                entity.Property(e => e.QuantityOnHand).HasColumnType("decimal(18,3)");
                entity.Property(e => e.QuantityReserved).HasColumnType("decimal(18,3)");
            });

            modelBuilder.Entity<StockTransfer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.TransferNumber }).IsUnique();
                entity.Property(e => e.TransferNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<StockTransferLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.StockTransfer).WithMany(e => e.Lines).HasForeignKey(e => e.StockTransferId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            });

            modelBuilder.Entity<ProductTraceLot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.ProductId, e.TraceNumber }).IsUnique();
                entity.Property(e => e.TraceType).IsRequired().HasMaxLength(80);
                entity.Property(e => e.TraceNumber).IsRequired().HasMaxLength(120);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<StockCount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.CountNumber }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.Status, e.CountDate });
                entity.Property(e => e.CountNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<StockCountLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.StockCount).WithMany(e => e.Lines).HasForeignKey(e => e.StockCountId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.StockLocation).WithMany().HasForeignKey(e => e.StockLocationId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.SystemQuantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.CountedQuantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Reason).HasMaxLength(500);
            });

            modelBuilder.Entity<NotificationMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.Status, e.CreatedDate });
                entity.HasIndex(e => new { e.CompanyId, e.NotificationType, e.EntityName, e.EntityId, e.Channel, e.Recipient });
                entity.Property(e => e.Channel).IsRequired().HasMaxLength(40);
                entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(80);
                entity.Property(e => e.EntityName).HasMaxLength(120);
                entity.Property(e => e.EntityId).HasMaxLength(80);
                entity.Property(e => e.Recipient).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Body).IsRequired().HasMaxLength(4000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            });

            modelBuilder.Entity<FinancialAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
                entity.Property(e => e.AccountType).IsRequired().HasMaxLength(30);
                entity.Property(e => e.AccountNumber).HasMaxLength(80);
                entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10);
                entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<BankTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.FinancialAccount).WithMany().HasForeignKey(e => e.FinancialAccountId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.TransactionDate });
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Reference).HasMaxLength(120);
            });

            modelBuilder.Entity<PaymentRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.FinancialAccount).WithMany().HasForeignKey(e => e.FinancialAccountId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.PaymentDate });
                entity.Property(e => e.Direction).IsRequired().HasMaxLength(30);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(40);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.EntityName).HasMaxLength(120);
                entity.Property(e => e.EntityId).HasMaxLength(80);
                entity.Property(e => e.ProviderName).HasMaxLength(80);
                entity.Property(e => e.ProviderTransactionId).HasMaxLength(160);
                entity.Property(e => e.ProviderStatus).HasMaxLength(80);
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });
        }

        private static void ConfigureHr(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.EmployeeNumber }).IsUnique().HasFilter("\"EmployeeNumber\" IS NOT NULL");
                entity.HasIndex(e => new { e.CompanyId, e.IsActive });
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(160);
                entity.Property(e => e.EmployeeNumber).HasMaxLength(80);
                entity.Property(e => e.JobTitle).HasMaxLength(160);
                entity.Property(e => e.Department).HasMaxLength(160);
                entity.Property(e => e.PersonalNumber).HasMaxLength(80);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.Phone).HasMaxLength(40);
                entity.Property(e => e.Address).HasMaxLength(300);
                entity.Property(e => e.EmergencyContact).HasMaxLength(160);
                entity.Property(e => e.MonthlySalary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(1000);
            });

            modelBuilder.Entity<PayrollRun>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.PayrollObligation).WithMany().HasForeignKey(e => e.PayrollObligationId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.CompanyId, e.RunNumber }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.PeriodStart, e.PeriodEnd });
                entity.Property(e => e.RunNumber).IsRequired().HasMaxLength(60);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.GrossAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DeductionsAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedBy).HasMaxLength(256);
            });

            modelBuilder.Entity<PayrollRunLine>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.PayrollRun).WithMany(e => e.Lines).HasForeignKey(e => e.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Employee).WithMany(e => e.PayrollLines).HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PayrollObligation).WithMany().HasForeignKey(e => e.PayrollObligationId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.BaseSalaryAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BonusAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OtherDeductionsAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GrossAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DeductionsAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(300);
            });
        }

        private async Task ValidateCompanyIsolationAsync(CancellationToken cancellationToken)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                switch (entry.Entity)
                {
                    case Category category:
                        RequireCompany(category.CompanyId, nameof(Category));
                        break;
                    case Customer customer:
                        RequireCompany(customer.CompanyId, nameof(Customer));
                        break;
                    case Discount discount:
                        RequireCompany(discount.CompanyId, nameof(Discount));
                        break;
                    case SupplierInvoice invoice:
                        RequireCompany(invoice.CompanyId, nameof(SupplierInvoice));
                        if (invoice.PurchaseOrderId.HasValue)
                        {
                            var invoicePurchaseOrderCompanyId = await PurchaseOrders
                                .Where(p => p.Id == invoice.PurchaseOrderId.Value)
                                .Select(p => p.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(invoice.CompanyId, invoicePurchaseOrderCompanyId, "Supplier invoice purchase order belongs to another company.");
                        }
                        if (invoice.GoodsReceiptId.HasValue)
                        {
                            var invoiceGoodsReceiptCompanyId = await GoodsReceipts
                                .Where(r => r.Id == invoice.GoodsReceiptId.Value)
                                .Select(r => r.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(invoice.CompanyId, invoiceGoodsReceiptCompanyId, "Supplier invoice goods receipt belongs to another company.");
                        }
                        break;
                    case SupplierInvoiceItem invoiceItem:
                        if (invoiceItem.ProductId.HasValue)
                        {
                            var invoiceItemInvoiceCompanyId = invoiceItem.SupplierInvoice?.CompanyId;
                            if (invoiceItemInvoiceCompanyId <= 0 && invoiceItem.SupplierInvoiceId > 0)
                            {
                                invoiceItemInvoiceCompanyId = await SupplierInvoices
                                    .Where(i => i.Id == invoiceItem.SupplierInvoiceId)
                                    .Select(i => i.CompanyId)
                                    .FirstOrDefaultAsync(cancellationToken);
                            }

                            var invoiceItemProductCompanyId = await Products
                                .IgnoreQueryFilters()
                                .Where(p => p.Id == invoiceItem.ProductId.Value)
                                .Select(p => p.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(invoiceItemInvoiceCompanyId.GetValueOrDefault(), invoiceItemProductCompanyId, "Supplier invoice item product belongs to another company.");
                        }
                        break;
                    case PayrollObligation payroll:
                        RequireCompany(payroll.CompanyId, nameof(PayrollObligation));
                        break;
                    case Employee employee:
                        RequireCompany(employee.CompanyId, nameof(Employee));
                        break;
                    case PayrollRun payrollRun:
                        RequireCompany(payrollRun.CompanyId, nameof(PayrollRun));
                        if (payrollRun.PayrollObligationId.HasValue)
                        {
                            var payrollObligationCompanyId = payrollRun.PayrollObligation?.CompanyId;
                            if (payrollObligationCompanyId <= 0)
                            {
                                payrollObligationCompanyId = await PayrollObligations
                                    .Where(o => o.Id == payrollRun.PayrollObligationId.Value)
                                    .Select(o => o.CompanyId)
                                    .FirstOrDefaultAsync(cancellationToken);
                            }
                            EnsureSameCompany(payrollRun.CompanyId, payrollObligationCompanyId.GetValueOrDefault(), "Payroll run obligation belongs to another company.");
                        }
                        break;
                    case PayrollRunLine payrollLine:
                        var payrollLineRunCompanyId = payrollLine.PayrollRun?.CompanyId;
                        if (payrollLineRunCompanyId <= 0 && payrollLine.PayrollRunId > 0)
                        {
                            payrollLineRunCompanyId = await PayrollRuns
                                .Where(r => r.Id == payrollLine.PayrollRunId)
                                .Select(r => r.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                        }
                        var payrollLineEmployeeCompanyId = await Employees
                            .Where(e => e.Id == payrollLine.EmployeeId)
                            .Select(e => e.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        RequireCompany(payrollLineRunCompanyId.GetValueOrDefault(), nameof(PayrollRunLine));
                        EnsureSameCompany(payrollLineRunCompanyId.GetValueOrDefault(), payrollLineEmployeeCompanyId, "Payroll employee belongs to another company.");
                        break;
                    case Store store:
                        RequireCompany(store.CompanyId, nameof(Store));
                        break;
                    case Register register:
                        RequireCompany(register.CompanyId, nameof(Register));
                        var storeCompanyId = await Stores
                            .Where(s => s.Id == register.StoreId)
                            .Select(s => s.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(register.CompanyId, storeCompanyId, "Register store belongs to another company.");
                        break;
                    case RegisterSession session:
                        RequireCompany(session.CompanyId, nameof(RegisterSession));
                        var registerCompanyId = await Registers
                            .Where(r => r.Id == session.RegisterId)
                            .Select(r => r.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(session.CompanyId, registerCompanyId, "Register session belongs to another company.");
                        break;
                    case ApprovalRequest approval:
                        RequireCompany(approval.CompanyId, nameof(ApprovalRequest));
                        break;
                    case ApprovalRule approvalRule:
                        RequireCompany(approvalRule.CompanyId, nameof(ApprovalRule));
                        break;
                    case OfflineSyncRecord syncRecord:
                        RequireCompany(syncRecord.CompanyId, nameof(OfflineSyncRecord));
                        if (syncRecord.SaleId.HasValue)
                        {
                            var syncSaleCompanyId = await Sales
                                .Where(s => s.Id == syncRecord.SaleId.Value)
                                .Select(s => s.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(syncRecord.CompanyId, syncSaleCompanyId, "Offline sync sale belongs to another company.");
                        }
                        break;
                    case DocumentAttachment attachment:
                        RequireCompany(attachment.CompanyId, nameof(DocumentAttachment));
                        break;
                    case FiscalPeriod fiscalPeriod:
                        RequireCompany(fiscalPeriod.CompanyId, nameof(FiscalPeriod));
                        break;
                    case ChartOfAccount account:
                        RequireCompany(account.CompanyId, nameof(ChartOfAccount));
                        break;
                    case JournalEntry journalEntry:
                        RequireCompany(journalEntry.CompanyId, nameof(JournalEntry));
                        break;
                    case SalesDocument salesDocument:
                        RequireCompany(salesDocument.CompanyId, nameof(SalesDocument));
                        if (salesDocument.CustomerId.HasValue)
                        {
                            var salesDocumentCustomerCompanyId = await Customers
                                .IgnoreQueryFilters()
                                .Where(c => c.Id == salesDocument.CustomerId.Value)
                                .Select(c => c.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(salesDocument.CompanyId, salesDocumentCustomerCompanyId, "Sales document customer belongs to another company.");
                        }
                        if (salesDocument.ConvertedFromDocumentId.HasValue)
                        {
                            var sourceSalesDocumentCompanyId = await SalesDocuments
                                .Where(d => d.Id == salesDocument.ConvertedFromDocumentId.Value)
                                .Select(d => d.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(salesDocument.CompanyId, sourceSalesDocumentCompanyId, "Converted source document belongs to another company.");
                        }
                        break;
                    case PurchaseOrder purchaseOrder:
                        RequireCompany(purchaseOrder.CompanyId, nameof(PurchaseOrder));
                        break;
                    case GoodsReceipt goodsReceipt:
                        RequireCompany(goodsReceipt.CompanyId, nameof(GoodsReceipt));
                        break;
                    case Warehouse warehouse:
                        RequireCompany(warehouse.CompanyId, nameof(Warehouse));
                        break;
                    case StockLocation stockLocation:
                        RequireCompany(stockLocation.CompanyId, nameof(StockLocation));
                        var stockLocationWarehouseCompanyId = await Warehouses
                            .Where(w => w.Id == stockLocation.WarehouseId)
                            .Select(w => w.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(stockLocation.CompanyId, stockLocationWarehouseCompanyId, "Stock location warehouse belongs to another company.");
                        break;
                    case WarehouseStock warehouseStock:
                        RequireCompany(warehouseStock.CompanyId, nameof(WarehouseStock));
                        var warehouseStockWarehouseCompanyId = await Warehouses
                            .Where(w => w.Id == warehouseStock.WarehouseId)
                            .Select(w => w.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(warehouseStock.CompanyId, warehouseStockWarehouseCompanyId, "Warehouse stock warehouse belongs to another company.");
                        if (warehouseStock.StockLocationId.HasValue)
                        {
                            var warehouseStockLocationCompanyId = await StockLocations
                                .Where(l => l.Id == warehouseStock.StockLocationId.Value)
                                .Select(l => l.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(warehouseStock.CompanyId, warehouseStockLocationCompanyId, "Warehouse stock location belongs to another company.");
                        }
                        var warehouseStockProductCompanyId = await Products
                            .IgnoreQueryFilters()
                            .Where(p => p.Id == warehouseStock.ProductId)
                            .Select(p => p.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(warehouseStock.CompanyId, warehouseStockProductCompanyId, "Warehouse stock product belongs to another company.");
                        break;
                    case StockTransfer transfer:
                        RequireCompany(transfer.CompanyId, nameof(StockTransfer));
                        var transferWarehouseCompanies = await Warehouses
                            .Where(w => w.Id == transfer.FromWarehouseId || w.Id == transfer.ToWarehouseId)
                            .Select(w => w.CompanyId)
                            .ToListAsync(cancellationToken);
                        if (transferWarehouseCompanies.Count != 2 || transferWarehouseCompanies.Any(id => id != transfer.CompanyId))
                        {
                            throw new InvalidOperationException("Stock transfer warehouses must belong to the same company.");
                        }
                        break;
                    case ProductTraceLot traceLot:
                        RequireCompany(traceLot.CompanyId, nameof(ProductTraceLot));
                        var traceProductCompanyId = await Products
                            .IgnoreQueryFilters()
                            .Where(p => p.Id == traceLot.ProductId)
                            .Select(p => p.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(traceLot.CompanyId, traceProductCompanyId, "Trace lot product belongs to another company.");
                        if (traceLot.WarehouseId.HasValue)
                        {
                            var traceWarehouseCompanyId = await Warehouses
                                .Where(w => w.Id == traceLot.WarehouseId.Value)
                                .Select(w => w.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(traceLot.CompanyId, traceWarehouseCompanyId, "Trace lot warehouse belongs to another company.");
                        }
                        break;
                    case StockCount stockCount:
                        RequireCompany(stockCount.CompanyId, nameof(StockCount));
                        var stockCountWarehouseCompanyId = await Warehouses
                            .Where(w => w.Id == stockCount.WarehouseId)
                            .Select(w => w.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(stockCount.CompanyId, stockCountWarehouseCompanyId, "Stock count warehouse belongs to another company.");
                        break;
                    case StockCountLine stockCountLine:
                        var stockCountCompanyId = stockCountLine.StockCount?.CompanyId;
                        if (stockCountCompanyId <= 0 && stockCountLine.StockCountId > 0)
                        {
                            stockCountCompanyId = await StockCounts
                                .Where(c => c.Id == stockCountLine.StockCountId)
                                .Select(c => c.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                        }
                        var stockCountLineProductCompanyId = await Products
                            .IgnoreQueryFilters()
                            .Where(p => p.Id == stockCountLine.ProductId)
                            .Select(p => p.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        RequireCompany(stockCountCompanyId.GetValueOrDefault(), nameof(StockCountLine));
                        EnsureSameCompany(stockCountCompanyId.GetValueOrDefault(), stockCountLineProductCompanyId, "Stock count line product belongs to another company.");
                        break;
                    case NotificationMessage notification:
                        if (notification.CompanyId.HasValue)
                        {
                            RequireCompany(notification.CompanyId.Value, nameof(NotificationMessage));
                        }
                        break;
                    case FinancialAccount financialAccount:
                        RequireCompany(financialAccount.CompanyId, nameof(FinancialAccount));
                        break;
                    case BankTransaction bankTransaction:
                        RequireCompany(bankTransaction.CompanyId, nameof(BankTransaction));
                        var bankTransactionAccountCompanyId = await FinancialAccounts
                            .Where(a => a.Id == bankTransaction.FinancialAccountId)
                            .Select(a => a.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(bankTransaction.CompanyId, bankTransactionAccountCompanyId, "Bank transaction account belongs to another company.");
                        break;
                    case PaymentRecord paymentRecord:
                        RequireCompany(paymentRecord.CompanyId, nameof(PaymentRecord));
                        if (paymentRecord.FinancialAccountId.HasValue)
                        {
                            var paymentAccountCompanyId = await FinancialAccounts
                                .Where(a => a.Id == paymentRecord.FinancialAccountId.Value)
                                .Select(a => a.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(paymentRecord.CompanyId, paymentAccountCompanyId, "Payment financial account belongs to another company.");
                        }
                        break;
                    case Product product:
                        RequireCompany(product.CompanyId, nameof(Product));
                        if (product.CategoryId.HasValue)
                        {
                            var categoryCompanyId = await Categories
                                .IgnoreQueryFilters()
                                .Where(c => c.Id == product.CategoryId.Value)
                                .Select(c => c.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(product.CompanyId, categoryCompanyId, "Product category belongs to another company.");
                        }
                        break;
                    case Sale sale:
                        RequireCompany(sale.CompanyId, nameof(Sale));
                        if (sale.CustomerId.HasValue)
                        {
                            var customerCompanyId = await Customers
                                .IgnoreQueryFilters()
                                .Where(c => c.Id == sale.CustomerId.Value)
                                .Select(c => c.CompanyId)
                                .FirstOrDefaultAsync(cancellationToken);
                            EnsureSameCompany(sale.CompanyId, customerCompanyId, "Sale customer belongs to another company.");
                        }
                        break;
                    case SaleItem saleItem:
                        await ValidateSaleItemCompanyAsync(saleItem, cancellationToken);
                        break;
                    case StockMovement movement:
                        RequireCompany(movement.CompanyId, nameof(StockMovement));
                        var productCompanyId = await Products
                            .IgnoreQueryFilters()
                            .Where(p => p.Id == movement.ProductId)
                            .Select(p => p.CompanyId)
                            .FirstOrDefaultAsync(cancellationToken);
                        EnsureSameCompany(movement.CompanyId, productCompanyId, "Stock movement product belongs to another company.");
                        break;
                }
            }
        }

        private async Task ValidateSaleItemCompanyAsync(SaleItem saleItem, CancellationToken cancellationToken)
        {
            var productCompanyId = await Products
                .IgnoreQueryFilters()
                .Where(p => p.Id == saleItem.ProductId)
                .Select(p => p.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);

            var saleCompanyId = saleItem.Sale?.CompanyId;
            if (saleCompanyId <= 0 && saleItem.SaleId > 0)
            {
                saleCompanyId = await Sales
                    .Where(s => s.Id == saleItem.SaleId)
                    .Select(s => s.CompanyId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var resolvedSaleCompanyId = saleCompanyId.GetValueOrDefault();
            RequireCompany(resolvedSaleCompanyId, nameof(SaleItem));
            EnsureSameCompany(resolvedSaleCompanyId, productCompanyId, "Sale item product belongs to another company.");
        }

        private static void RequireCompany(int companyId, string entityName)
        {
            if (companyId <= 0)
            {
                throw new InvalidOperationException($"{entityName} must belong to a company.");
            }
        }

        private static void EnsureSameCompany(int expectedCompanyId, int actualCompanyId, string message)
        {
            if (actualCompanyId <= 0 || expectedCompanyId != actualCompanyId)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
