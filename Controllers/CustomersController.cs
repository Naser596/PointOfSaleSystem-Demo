using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

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
            var success = await _customerService.DeleteCustomerAsync(id, User.Identity?.Name);
            if (success)
            {
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
