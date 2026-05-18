using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class CustomersController(
        ICustomerService customerService,
        IAuditLogService auditLog,
        ApplicationDbContext context,
        ICurrentCompanyService currentCompany) : Controller
    {
        private readonly ICustomerService _customerService = customerService;
        private readonly IAuditLogService _auditLog = auditLog;
        private readonly ApplicationDbContext _context = context;
        private readonly ICurrentCompanyService _currentCompany = currentCompany;

        // GET: Customers
        public async Task<IActionResult> Index(string searchString)
        {
            var customers = string.IsNullOrEmpty(searchString)
                ? await _customerService.GetAllCustomersAsync()
                : await _customerService.SearchCustomersAsync(searchString);

            ViewData["CurrentFilter"] = searchString;
            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [Authorize(Roles = "Admin,Manager,Accountant")]
        public async Task<IActionResult> Statement(int id)
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == id);
            if (customer == null) return NotFound();

            var sales = await _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.CompanyId == companyId && s.CustomerId == id)
                .OrderByDescending(s => s.SaleDate)
                .Take(100)
                .ToListAsync();
            var invoices = await _context.SalesDocuments
                .Where(d => d.CompanyId == companyId && d.CustomerId == id && d.DocumentType == "Invoice")
                .OrderByDescending(d => d.DocumentDate)
                .Take(100)
                .ToListAsync();

            return View(new CustomerStatementDetailsViewModel
            {
                Customer = customer,
                Sales = sales,
                Invoices = invoices,
                PosSalesTotal = sales.Sum(s => s.TotalAmount - s.RefundedAmount),
                InvoiceTotal = invoices.Sum(i => i.TotalAmount),
                PaidInvoiceTotal = invoices.Sum(i => i.PaidAmount),
                BalanceDue = invoices.Sum(i => Math.Max(i.TotalAmount - i.PaidAmount, 0))
            });
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address,Notes")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.CreateCustomerAsync(customer);
                await _auditLog.LogAsync("Create", nameof(Customer), customer.Id.ToString(), $"Created customer {customer.Name}");
                TempData["Success"] = "Customer created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Phone,Address,Notes")] Customer customer)
        {
            if (id != customer.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _customerService.UpdateCustomerAsync(customer);
                    await _auditLog.LogAsync("Update", nameof(Customer), customer.Id.ToString(), $"Updated customer {customer.Name}");
                    TempData["Success"] = "Customer updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }
            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            var success = await _customerService.DeleteCustomerAsync(id, User.Identity?.Name);
            if (success)
            {
                await _auditLog.LogAsync("Delete", nameof(Customer), id.ToString(), $"Deleted customer {customer?.Name ?? id.ToString()}");
                TempData["Success"] = "Customer deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Error deleting customer.";
            }
            return RedirectToAction(nameof(Index));
        }

        // API: Customers/Search?q=term
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());

            var customers = await _customerService.SearchCustomersAsync(q);
            return Json(customers.Select(c => new { id = c.Id, text = $"{c.Name} ({c.Phone ?? "No Phone"})", email = c.Email }));
        }
    }
}
