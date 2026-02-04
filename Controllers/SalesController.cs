using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ISaleService _saleService;

        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var sales = await _saleService.GetAllSalesAsync();
            return View(sales);
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            return View(sale);
        }

        // GET: Sales/Receipt/5
        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null)
            {
                return NotFound();
            }
            return View(sale);
        }
        // GET: Sales/ExportDailyReport
        public async Task<IActionResult> ExportDailyReport()
        {
            var sales = await _saleService.GetAllSalesAsync();
            var todaySales = sales.Where(s => s.SaleDate.Date == DateTime.Today).ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Today's Sales");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Sale Number";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Payment Method";
                worksheet.Cell(1, 4).Value = "Total Amount";
                worksheet.Cell(1, 5).Value = "Status";

                // Data
                int row = 2;
                foreach (var sale in todaySales)
                {
                    worksheet.Cell(row, 1).Value = sale.SaleNumber;
                    worksheet.Cell(row, 2).Value = sale.SaleDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 3).Value = sale.PaymentMethod ?? "Cash";
                    worksheet.Cell(row, 4).Value = sale.TotalAmount;
                    worksheet.Cell(row, 5).Value = sale.Status;
                    row++;
                }

                // Styling
                var headerRange = worksheet.Range("A1:E1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"MiniPOS_Report_{DateTime.Today:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}
