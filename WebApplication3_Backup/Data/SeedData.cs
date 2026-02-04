using WebApplication3.Data;
using WebApplication3.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplication3.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Check if database has been seeded
                if (context.Products.Any())
                {
                    return;   // DB has been seeded
                }

                // Seed Products
                var products = new Product[]
                {
                    new Product
                    {
                        Name = "Laptop",
                        Description = "High-performance laptop",
                        Price = 999.99m,
                        Stock = 50,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Mouse",
                        Description = "Wireless mouse",
                        Price = 29.99m,
                        Stock = 200,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Keyboard",
                        Description = "Mechanical keyboard",
                        Price = 89.99m,
                        Stock = 100,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Monitor",
                        Description = "27-inch 4K monitor",
                        Price = 399.99m,
                        Stock = 30,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "USB Cable",
                        Description = "USB-C charging cable",
                        Price = 9.99m,
                        Stock = 500,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    }
                };

                foreach (Product p in products)
                {
                    context.Products.Add(p);
                }

                context.SaveChanges();

                // Seed Sample Sales (optional)
                var sales = new Sale[]
                {
                    new Sale
                    {
                        SaleNumber = $"SALE-{DateTime.Now:yyyyMMddHHmmss}",
                        SaleDate = DateTime.Now,
                        TotalAmount = 1039.97m,
                        Status = "Completed"
                    }
                };

                foreach (Sale s in sales)
                {
                    context.Sales.Add(s);
                }

                context.SaveChanges();
            }
        }
    }
}
