using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // Seed Roles
                string[] roleNames = { "Admin", "Cashier" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Seed Admin User
                var adminEmail = "nasermustafi@gmail.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(adminUser, "Admin123");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // Seed Cashier User
                var cashierEmail = "puntor1@gmail.com";
                var cashierUser = await userManager.FindByEmailAsync(cashierEmail);
                if (cashierUser == null)
                {
                    cashierUser = new IdentityUser
                    {
                        UserName = cashierEmail,
                        Email = cashierEmail,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(cashierUser, "puntor123");
                    await userManager.AddToRoleAsync(cashierUser, "Cashier");
                }

                // Seed Categories
                if (!context.Categories.Any())
                {
                    var categories = new Category[]
                    {
                        new Category
                        {
                            Name = "Smartphones",
                            Description = "Mobile phones and accessories",
                            IconClass = "fas fa-mobile-alt",
                            DisplayOrder = 1,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new Category
                        {
                            Name = "Gaming",
                            Description = "Gaming consoles and accessories",
                            IconClass = "fas fa-gamepad",
                            DisplayOrder = 2,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new Category
                        {
                            Name = "TVs & Displays",
                            Description = "Televisions and monitors",
                            IconClass = "fas fa-tv",
                            DisplayOrder = 3,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new Category
                        {
                            Name = "Computers",
                            Description = "Laptops and desktop computers",
                            IconClass = "fas fa-laptop",
                            DisplayOrder = 4,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        },
                        new Category
                        {
                            Name = "Accessories",
                            Description = "Cables, chargers and other accessories",
                            IconClass = "fas fa-keyboard",
                            DisplayOrder = 5,
                            IsActive = true,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        }
                    };

                    foreach (var category in categories)
                    {
                        context.Categories.Add(category);
                    }
                    await context.SaveChangesAsync();
                }

                // Get category IDs for products
                var smartphonesCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Smartphones");
                var gamingCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Gaming");
                var tvsCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "TVs & Displays");

                // Check if database has been seeded with products
                if (context.Products.Any())
                {
                    return;   // DB has been seeded
                }

                // Seed Products with Images and Categories
                var products = new Product[]
                {
                    new Product
                    {
                        Name = "iPhone 16 Pro",
                        Description = "Latest Apple iPhone with advanced camera system",
                        SKU = "IPHONE-16-PRO",
                        Price = 1199.99m,
                        Stock = 25,
                        MinStock = 5,
                        CategoryId = smartphonesCategory?.Id,
                        ImagePath = "/images/products/c69beb6b-1375-4b2b-93e5-68bc8b3dc6f4.png",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Xbox Series S",
                        Description = "Next-gen gaming console - Digital Edition",
                        SKU = "XBOX-SERIES-S",
                        Price = 299.99m,
                        Stock = 40,
                        MinStock = 10,
                        CategoryId = gamingCategory?.Id,
                        ImagePath = "/images/products/c9640b0b-1fee-466e-88f2-71690206330b.png",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Samsung Neo QLED 8K TV",
                        Description = "65-inch Neo QLED 8K Smart TV",
                        SKU = "SAMSUNG-NEO-8K",
                        Price = 2499.99m,
                        Stock = 3,
                        MinStock = 5,
                        CategoryId = tvsCategory?.Id,
                        ImagePath = "/images/products/dd8a65b0-5ddf-4199-8a6c-b685acdb2c20.jpg",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Name = "Samsung Odyssey G3 Monitor",
                        Description = "27-inch Gaming Monitor 165Hz",
                        SKU = "SAMSUNG-G3-27",
                        Price = 349.99m,
                        Stock = 2,
                        MinStock = 5,
                        CategoryId = tvsCategory?.Id,
                        ImagePath = "/images/products/fb020bad-3636-43d0-b891-34a2bb869ed6.jpeg",
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    }
                };

                foreach (Product p in products)
                {
                    context.Products.Add(p);
                }

                await context.SaveChangesAsync();

                // Seed Sample Sales
                var adminUserId = adminUser?.Id;
                var sales = new Sale[]
                {
                    new Sale
                    {
                        SaleNumber = $"SALE-{DateTime.Now:yyyyMMddHHmmss}",
                        SaleDate = DateTime.Now,
                        SubTotal = 1039.97m,
                        TotalAmount = 1039.97m,
                        Status = "Completed",
                        PaymentMethod = "Cash",
                        CashierId = adminUserId,
                        CashierName = adminEmail
                    }
                };

                foreach (Sale s in sales)
                {
                    context.Sales.Add(s);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
