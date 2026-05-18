using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class HomeController(ApplicationDbContext context, IInventoryService inventoryService, ICurrentCompanyService currentCompany, ILogger<HomeController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IInventoryService _inventoryService = inventoryService;
        private readonly ICurrentCompanyService _currentCompany = currentCompany;
        private readonly ILogger<HomeController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return RedirectToAction("Index", "SuperAdmin");
            }

            try
            {
                // Determine if we should filter by cashier
                var isAdmin = User.IsInRole("Admin");
                var cashierName = User.Identity?.Name ?? "";
                var companyId = await _currentCompany.GetRequiredCompanyIdAsync();

                // Base query for sales
                var salesQuery = _context.Sales.Where(s => s.CompanyId == companyId);
                if (!isAdmin)
                {
                    salesQuery = salesQuery.Where(s => s.CashierName == cashierName);
                }

                var totalProducts = await _context.Products.CountAsync(p => p.CompanyId == companyId && p.IsActive);

                // Use the filtered query for stats
                var totalSales = await salesQuery.CountAsync();

                // Get today's date boundaries
                var todayStart = DateTime.Today;
                var todayEnd = todayStart.AddDays(1);

                var todayRevenue = await salesQuery
                    .Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd)
                    .Select(s => (double)s.TotalAmount)
                    .SumAsync();

                var totalRevenue = await salesQuery
                    .Select(s => (double)s.TotalAmount)
                    .SumAsync();

                var todaySalesCount = await salesQuery
                    .Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd)
                    .CountAsync();
                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var dueSoonCutoff = DateTime.Today.AddDays(7);
                var monthRevenue = await salesQuery
                    .Where(s => s.SaleDate >= monthStart)
                    .SumAsync(s => s.TotalAmount - s.RefundedAmount);
                var refundedToday = await salesQuery
                    .Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd)
                    .SumAsync(s => s.RefundedAmount);
                var netTodayRevenue = (decimal)todayRevenue - refundedToday;
                var averageTicket = todaySalesCount == 0 ? 0 : netTodayRevenue / todaySalesCount;

                // Low stock alerts (global for everyone)
                var lowStockCount = await _inventoryService.GetLowStockCountAsync();
                var lowStockProducts = await _inventoryService.GetLowStockProductsAsync();

                ViewBag.TotalProducts = totalProducts;
                ViewBag.TodaySales = todaySalesCount;
                ViewBag.TodayRevenue = (decimal)todayRevenue;
                ViewBag.TodayNetRevenue = netTodayRevenue;
                ViewBag.MonthRevenue = monthRevenue;
                ViewBag.TodayRefunds = refundedToday;
                ViewBag.AverageTicket = averageTicket;
                ViewBag.AllSalesHistory = totalSales;
                ViewBag.AllRevenue = (decimal)totalRevenue;
                ViewBag.LowStockCount = lowStockCount;
                ViewBag.LowStockProducts = lowStockProducts;

                // Recent sales
                var recentSales = await salesQuery
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync();
                ViewBag.RecentSales = recentSales;

                // Weekly Sales Data for Chart
                var weekStart = DateTime.Today.AddDays(-6);
                var weeklySalesData = await salesQuery
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

                // Top Selling Products (Filtered by visible sales)
                // Note: SaleItems query needs to be joined with filtered Sales if not Admin
                IQueryable<SaleItem> saleItemsQuery = _context.SaleItems.Include(si => si.Product);
                saleItemsQuery = saleItemsQuery.Where(si => si.Sale.CompanyId == companyId);

                if (!isAdmin)
                {
                    saleItemsQuery = saleItemsQuery.Where(si => si.Sale.CashierName == cashierName);
                }

                var todayProfit = await saleItemsQuery
                    .Where(si => si.Sale.SaleDate >= todayStart && si.Sale.SaleDate < todayEnd)
                    .SumAsync(si => (si.UnitPrice - si.UnitCost) * (si.Quantity - si.ReturnedQuantity));
                var monthProfit = await saleItemsQuery
                    .Where(si => si.Sale.SaleDate >= monthStart)
                    .SumAsync(si => (si.UnitPrice - si.UnitCost) * (si.Quantity - si.ReturnedQuantity));
                ViewBag.TodayProfit = todayProfit;
                ViewBag.MonthProfit = monthProfit;

                var paymentSplit = await salesQuery
                    .Where(s => s.SaleDate >= monthStart)
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new { Method = g.Key, Total = g.Sum(s => (double)(s.TotalAmount - s.RefundedAmount)) })
                    .OrderByDescending(x => x.Total)
                    .ToListAsync();
                ViewBag.PaymentLabels = paymentSplit.Select(x => x.Method).ToList();
                ViewBag.PaymentValues = paymentSplit.Select(x => x.Total).ToList();

                var dueObligations = await _context.PayrollObligations
                    .Where(o => o.CompanyId == companyId &&
                                o.Status != "Paid" &&
                                o.DueDate.HasValue &&
                                o.DueDate.Value.Date <= dueSoonCutoff)
                    .OrderBy(o => o.DueDate)
                    .ThenByDescending(o => o.Amount)
                    .Take(8)
                    .ToListAsync();
                ViewBag.DueObligations = dueObligations;
                ViewBag.DueObligationCount = dueObligations.Count;
                ViewBag.DueObligationTotal = dueObligations.Sum(o => o.Amount);
                ViewBag.OverdueObligationCount = dueObligations.Count(o => o.IsOverdue);

                var userPerformance = await salesQuery
                    .Where(s => s.SaleDate >= monthStart)
                    .GroupBy(s => string.IsNullOrEmpty(s.CashierName) ? "Unknown" : s.CashierName)
                    .Select(g => new { Cashier = g.Key, Sales = g.Count(), Revenue = g.Sum(s => (double)(s.TotalAmount - s.RefundedAmount)) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(6)
                    .ToListAsync();
                ViewBag.UserPerformance = userPerformance;

                var topSellers = await saleItemsQuery
                    .GroupBy(si => si.ProductId)
                    .Select(g => new
                    {
                        ProductName = g.First().Product.Name,
                        ImagePath = g.First().Product.ImagePath,
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
