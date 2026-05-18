using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using WebApplication3.Models;

var builder = WebApplication.CreateBuilder(args);

// Avoid EventLog provider permission failures in local/dev runs.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Configure database. PostgreSQL is the only runtime database provider.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var postgresConnection = builder.Configuration.GetConnectionString("Postgres")
        ?? builder.Configuration["POSTGRES_CONNECTION_STRING"];

    if (string.IsNullOrWhiteSpace(postgresConnection))
    {
        throw new InvalidOperationException("PostgreSQL connection string is required. Configure ConnectionStrings:Postgres.");
    }

    options.UseNpgsql(postgresConnection);
});

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

const string platformOwnerEmail = "nasermustafi@gmail.com";
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/SuperAdmin") ||
            context.Request.Path.StartsWithSegments("/Settings"))
        {
            context.Response.Redirect("/Account/Login");
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        if (context.Principal == null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var user = await userManager.GetUserAsync(context.Principal);

        if (user == null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        var isSuperAdmin = await userManager.IsInRoleAsync(user, "SuperAdmin");
        var isPlatformOwner = isSuperAdmin &&
            string.Equals(user.Email, platformOwnerEmail, StringComparison.OrdinalIgnoreCase);

        if (isSuperAdmin && !isPlatformOwner)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        if (!isSuperAdmin)
        {
            if (!user.CompanyId.HasValue)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return;
            }

            var companyActive = await db.Companies.AnyAsync(c => c.Id == user.CompanyId.Value && c.IsActive);
            if (!companyActive)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            }
        }
    };
});

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IPOSOperationsService, POSOperationsService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<ICompanySettingsService, CompanySettingsService>();
builder.Services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();
builder.Services.AddScoped<ICurrentCompanyService, CurrentCompanyService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IErpAccountingService, ErpAccountingService>();
builder.Services.AddScoped<IWarehouseWorkflowService, WarehouseWorkflowService>();
builder.Services.AddScoped<ISalesWorkflowService, SalesWorkflowService>();
builder.Services.AddScoped<IHrPayrollService, HrPayrollService>();
builder.Services.AddScoped<ICompanyObligationFinanceService, CompanyObligationFinanceService>();
builder.Services.AddScoped<IErpReportService, ErpReportService>();
builder.Services.AddScoped<ICompanySubscriptionService, CompanySubscriptionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<CompanySubscriptionMonitorService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformOwner", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("SuperAdmin") &&
            string.Equals(context.User.Identity?.Name, platformOwnerEmail, StringComparison.OrdinalIgnoreCase)));
});

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (context.Database.IsRelational())
    {
        context.Database.Migrate();
    }
    else
    {
        context.Database.EnsureCreated();
    }
    await SeedData.Initialize(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var isUnsafeMethod =
        HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method);

    context.Response.OnStarting(() =>
    {
        var path = context.Request.Path;
        var isAccountPage = path.StartsWithSegments("/Account");
        var isAuthenticatedPage = context.User.Identity?.IsAuthenticated == true;

        if (isAccountPage || isAuthenticatedPage)
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
        }

        if (!isAccountPage &&
            isUnsafeMethod &&
            context.Response.Headers.ContainsKey("Location") &&
            (context.Response.StatusCode == StatusCodes.Status301MovedPermanently ||
             context.Response.StatusCode == StatusCodes.Status302Found ||
             context.Response.StatusCode == StatusCodes.Status307TemporaryRedirect ||
             context.Response.StatusCode == StatusCodes.Status308PermanentRedirect))
        {
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
        }

        return Task.CompletedTask;
    });

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
}
