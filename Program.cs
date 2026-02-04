using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "pos.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    await SeedData.Initialize(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
