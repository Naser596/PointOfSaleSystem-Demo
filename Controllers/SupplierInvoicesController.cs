using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Roles = "Admin,Accountant,Manager")]
    public class SupplierInvoicesController : Controller
    {
        private readonly ISupplierInvoiceService _invoiceService;
        private readonly ICompanySettingsService _settingsService;
        private readonly IAuditLogService _auditLog;
        private readonly IErpAccountingService _accounting;

        public SupplierInvoicesController(
            ISupplierInvoiceService invoiceService,
            ICompanySettingsService settingsService,
            IAuditLogService auditLog,
            IErpAccountingService accounting)
        {
            _invoiceService = invoiceService;
            _settingsService = settingsService;
            _auditLog = auditLog;
            _accounting = accounting;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllAsync();
            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            var model = new SupplierInvoiceDetailsViewModel
            {
                Company = await _settingsService.GetSettingsAsync(),
                Invoice = invoice
            };

            return View(model);
        }

        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            return View(new SupplierInvoiceDetailsViewModel
            {
                Company = await _settingsService.GetSettingsAsync(),
                Invoice = invoice
            });
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            var company = await _settingsService.GetSettingsAsync();
            var pdf = SimplePdfService.BuildSupplierInvoice(invoice, company);
            await _auditLog.LogAsync("ExportPdf", nameof(SupplierInvoice), invoice.Id.ToString(), $"Downloaded PDF for supplier invoice {invoice.InvoiceNumber}", invoice.CompanyId);
            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }

        public async Task<IActionResult> Create()
        {
            return View(await _invoiceService.BuildCreateModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierInvoiceCreateViewModel model)
        {
            model.Items = model.Items
                .Where(i =>
                    !string.IsNullOrWhiteSpace(i.Description) ||
                    !string.IsNullOrWhiteSpace(i.NewProductName) ||
                    i.ProductId.HasValue ||
                    i.CreateNewProduct ||
                    i.Quantity != 0 ||
                    i.UnitCost != 0 ||
                    i.TaxRate != 0)
                .ToList();

            if (model.Items.Count == 0)
            {
                model.Items.Add(new SupplierInvoiceItemInput());
                ModelState.AddModelError("", "At least one invoice item is required.");
            }

            if (!ModelState.IsValid)
            {
                if (model.Items.Count == 0) model.Items.Add(new SupplierInvoiceItemInput());
                await _invoiceService.PopulateMatchingOptionsAsync(model);
                return View(model);
            }

            try
            {
                var invoice = await _invoiceService.CreateAsync(model, User.Identity?.Name);
                await _auditLog.LogAsync("Create", nameof(SupplierInvoice), invoice.Id.ToString(), $"Created supplier invoice {invoice.InvoiceNumber} for {invoice.SupplierName}", invoice.CompanyId);
                TempData["Success"] = "Supplier invoice created successfully.";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                if (model.Items.Count == 0) model.Items.Add(new SupplierInvoiceItemInput());
                await _invoiceService.PopulateMatchingOptionsAsync(model);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            try
            {
                var entry = await _accounting.PostSupplierInvoiceAsync(id, User.Identity?.Name);
                if (entry == null)
                {
                    TempData["Error"] = "Supplier invoice could not be posted.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _auditLog.LogAsync(
                    "Post",
                    nameof(SupplierInvoice),
                    invoice.Id.ToString(),
                    $"Posted supplier invoice {invoice.InvoiceNumber} to journal {entry.EntryNumber}",
                    invoice.CompanyId);
                TempData["Success"] = $"Supplier invoice posted to journal {entry.EntryNumber}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
