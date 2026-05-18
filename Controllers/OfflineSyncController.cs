using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class OfflineSyncController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IPOSOperationsService posOperations,
    IAuditLogService auditLog) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IPOSOperationsService _posOperations = posOperations;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var records = await _context.OfflineSyncRecords
            .Include(r => r.Sale)
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.ReceivedAt)
            .Take(100)
            .ToListAsync();

        return View(new OfflineSyncDashboardViewModel
        {
            Records = records,
            PendingCount = records.Count(r => r.Status == "Pending"),
            SyncedCount = records.Count(r => r.Status == "Synced"),
            FailedCount = records.Count(r => r.Status == "Failed"),
            ConflictCount = records.Count(r => r.Status == "Conflict"),
            CancelledCount = records.Count(r => r.Status == "Cancelled")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var result = await _posOperations.RetryOfflineSyncRecordAsync(companyId, id, User.Identity?.Name ?? "Unknown");
            await _auditLog.LogAsync("Retry", nameof(OfflineSyncRecord), id.ToString(), $"Retry result: {result.Status} {result.Message}", companyId);
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? $"Offline sale synced as {result.SaleNumber}."
                : $"Retry failed: {result.Message}";
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
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            await _posOperations.CancelOfflineSyncRecordAsync(companyId, id, User.Identity?.Name ?? "Unknown");
            await _auditLog.LogAsync("Cancel", nameof(OfflineSyncRecord), id.ToString(), "Cancelled offline sync record.", companyId);
            TempData["Success"] = "Offline sync record cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
