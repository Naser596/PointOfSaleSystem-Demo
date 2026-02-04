using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Discount> Discounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IconClass).HasMaxLength(50);
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SKU).HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.SKU).IsUnique().HasFilter("SKU IS NOT NULL");
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
                entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
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
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
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
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.Code).IsUnique();
            });
        }
    }
}
