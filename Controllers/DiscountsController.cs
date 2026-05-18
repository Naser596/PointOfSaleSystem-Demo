using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class DiscountsController : Controller
    {
        private readonly IDiscountService _discountService;
        private readonly IAuditLogService _auditLog;

        public DiscountsController(IDiscountService discountService, IAuditLogService auditLog)
        {
            _discountService = discountService;
            _auditLog = auditLog;
        }

        // GET: Discounts
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var discounts = await _discountService.GetAllDiscountsAsync();
            return View(discounts);
        }

        // GET: Discounts/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Discounts/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Discount discount)
        {
            if (ModelState.IsValid)
            {
                // Basic validation
                if (discount.StartDate > discount.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date.");
                    return View(discount);
                }

                await _discountService.CreateDiscountAsync(discount);
                await _auditLog.LogAsync("Create", nameof(Discount), discount.Id.ToString(), $"Created discount {discount.Code}");
                TempData["Success"] = "Discount created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(discount);
        }

        // GET: Discounts/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var discount = await _discountService.GetDiscountByIdAsync(id);
            if (discount == null) return NotFound();
            return View(discount);
        }

        // POST: Discounts/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Discount discount)
        {
            if (id != discount.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (discount.StartDate > discount.EndDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date.");
                        return View(discount);
                    }

                    await _discountService.UpdateDiscountAsync(discount);
                    await _auditLog.LogAsync("Update", nameof(Discount), discount.Id.ToString(), $"Updated discount {discount.Code}");
                    TempData["Success"] = "Discount updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (KeyNotFoundException)
                {
                    return NotFound();
                }
            }
            return View(discount);
        }

        // POST: Discounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var discount = await _discountService.GetDiscountByIdAsync(id);
            var success = await _discountService.DeleteDiscountAsync(id);
            if (success)
            {
                await _auditLog.LogAsync("Delete", nameof(Discount), id.ToString(), $"Deleted discount {discount?.Code ?? id.ToString()}");
                TempData["Success"] = "Discount deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Error deleting discount.";
            }
            return RedirectToAction(nameof(Index));
        }

        // API: Verify Code for POS
        [HttpGet]
        public async Task<IActionResult> VerifyCode(string code)
        {
            var discount = await _discountService.GetValidDiscountByCodeAsync(code);
            if (discount == null)
            {
                return Json(new { valid = false, message = "Invalid or expired discount code." });
            }
            return Json(new
            {
                valid = true,
                code = discount.Code,
                type = discount.DiscountType.ToString(), // Percentage or FixedAmount
                value = discount.Value,
                minOrder = discount.MinOrderAmount
            });
        }
    }
}
