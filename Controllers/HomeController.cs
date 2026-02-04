using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, IInventoryService inventoryService, ILogger<HomeController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
                var totalSales = await _context.Sales.CountAsync();
                
                // Get today's date boundaries for SQLite compatibility
                var todayStart = DateTime.Today;
                var todayEnd = todayStart.AddDays(1);
                
                var todayRevenue = await _context.Sales
                    .Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd)
                    .Select(s => (double)s.TotalAmount) // Cast to double for SQLite aggregation
                    .SumAsync();
                
                var totalRevenue = await _context.Sales
                    .Select(s => (double)s.TotalAmount) // Cast to double for SQLite aggregation
                    .SumAsync();

                var todaySalesCount = await _context.Sales
                    .Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd)
                    .CountAsync();

                // Low stock alerts
                var lowStockCount = await _inventoryService.GetLowStockCountAsync();
                var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();

                ViewBag.TotalProducts = totalProducts;
                ViewBag.TodaySales = todaySalesCount;
                ViewBag.TodayRevenue = (decimal)todayRevenue;
                ViewBag.AllSalesHistory = totalSales;
                ViewBag.AllRevenue = (decimal)totalRevenue;
                ViewBag.LowStockCount = lowStockCount;
                ViewBag.LowStockProducts = lowStockProducts;

                // Recent sales for the dashboard
                var recentSales = await _context.Sales
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync();
                ViewBag.RecentSales = recentSales;

                // Weekly Sales Data for Chart (Last 7 Days)
                var weekStart = DateTime.Today.AddDays(-6);
                var weeklySalesData = await _context.Sales
                    .Where(s => s.SaleDate >= weekStart)
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new { Date = g.Key, Total = g.Sum(s => (double)s.TotalAmount) })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Create arrays for chart
                var chartLabels = new List<string>();
                var chartValues = new List<double>();
                for (int i = 0; i < 7; i++)
                {
                    var date = weekStart.AddDays(i);
                    chartLabels.Add(date.ToString("ddd"));
                    var dayData = weeklySalesData.FirstOrDefault(x => x.Date == date);
                    chartValues.Add(dayData?.Total ?? 0);
                }
                ViewBag.ChartLabels = chartLabels;
                ViewBag.ChartValues = chartValues;

                // Top Selling Products (Top 5 by Quantity)
                var topSellers = await _context.SaleItems
                    .Include(si => si.Product)
                    .GroupBy(si => si.ProductId)
                    .Select(g => new
                    {
                        ProductName = g.First().Product.Name,
                        TotalQty = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => (double)(si.Quantity * si.UnitPrice))
                    })
                    .OrderByDescending(x => x.TotalQty)
                    .Take(5)
                    .ToListAsync();
                ViewBag.TopSellers = topSellers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard Error: {ex}");
                _logger.LogError(ex, "Error getting dashboard statistics");
                // Set default values if there's an error
                ViewBag.TotalProducts = 0;
                ViewBag.TotalSales = 0;
                ViewBag.TodayRevenue = 0m;
                ViewBag.TotalRevenue = 0m;
                ViewBag.LowStockCount = 0;
                ViewBag.LowStockProducts = new List<Product>();
                ViewBag.RecentSales = new List<Sale>();
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
