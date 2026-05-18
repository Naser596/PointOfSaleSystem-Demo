using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize]
public class NotificationsController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    INotificationService notificationService,
    IAuditLogService auditLog) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<IActionResult> Index(string? status = "Queued")
    {
        IQueryable<NotificationMessage> query = _context.NotificationMessages
            .Include(n => n.Company)
            .OrderByDescending(n => n.CreatedDate);

        if (!User.IsInRole("SuperAdmin"))
        {
            var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
            query = query.Where(n => n.CompanyId == companyId);
        }

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.Status == status);
        }

        ViewBag.Status = status;
        return View(await query.Take(200).ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager,Accountant")]
    public async Task<IActionResult> GenerateCustomerReminders()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var created = await _notificationService.GenerateInvoiceAndOverdueRemindersAsync(companyId);
        await _auditLog.LogAsync("Generate", nameof(NotificationMessage), null, $"Generated {created} invoice/customer reminder(s)", companyId);
        TempData["Success"] = $"{created} reminder(s) queued.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PlatformOwner")]
    public async Task<IActionResult> GenerateSubscriptionReminders()
    {
        var created = await _notificationService.GenerateSubscriptionExpiryNotificationsAsync();
        await _auditLog.LogAsync("Generate", nameof(NotificationMessage), null, $"Generated {created} subscription expiry notification(s)");
        TempData["Success"] = $"{created} subscription notification(s) queued.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSent(int id)
    {
        try
        {
            int? companyId = User.IsInRole("SuperAdmin") ? null : await _currentCompany.GetRequiredCompanyIdAsync();
            await _notificationService.MarkSentAsync(id, companyId);
            TempData["Success"] = "Notification marked as sent.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            int? companyId = User.IsInRole("SuperAdmin") ? null : await _currentCompany.GetRequiredCompanyIdAsync();
            await _notificationService.CancelAsync(id, companyId);
            TempData["Success"] = "Notification cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
