using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ApprovalsController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IAuditLogService auditLog) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var model = new ApprovalDashboardViewModel
        {
            Requests = await _context.ApprovalRequests
                .Where(r => r.CompanyId == companyId)
                .OrderBy(r => r.Status == "Completed")
                .ThenByDescending(r => r.RequestedDate)
                .Take(100)
                .ToListAsync(),
            Rules = await _context.ApprovalRules
                .Where(r => r.CompanyId == companyId)
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.EntityName)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApprovalRequestInput input)
    {
        if (string.IsNullOrWhiteSpace(input.RequestType) || string.IsNullOrWhiteSpace(input.EntityName))
        {
            TempData["Error"] = "Request type and entity are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var request = new ApprovalRequest
        {
            CompanyId = companyId,
            RequestType = input.RequestType.Trim(),
            EntityName = input.EntityName.Trim(),
            EntityId = string.IsNullOrWhiteSpace(input.EntityId) ? null : input.EntityId.Trim(),
            Status = "Pending",
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            RequestedBy = User.Identity?.Name,
            RequestedDate = DateTime.Now
        };

        _context.ApprovalRequests.Add(request);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(ApprovalRequest), request.Id.ToString(), $"Created approval request {request.RequestType}", companyId);

        TempData["Success"] = "Approval request created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRule(ApprovalRuleInput input)
    {
        if (string.IsNullOrWhiteSpace(input.RuleName) || string.IsNullOrWhiteSpace(input.EntityName))
        {
            TempData["Error"] = "Rule name and entity are required.";
            return RedirectToAction(nameof(Index));
        }

        if (input.AmountThreshold < 0)
        {
            TempData["Error"] = "Threshold cannot be negative.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        _context.ApprovalRules.Add(new ApprovalRule
        {
            CompanyId = companyId,
            RuleName = input.RuleName.Trim(),
            EntityName = input.EntityName.Trim(),
            ActionName = string.IsNullOrWhiteSpace(input.ActionName) ? "Create" : input.ActionName.Trim(),
            AmountThreshold = input.AmountThreshold,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(ApprovalRule), input.EntityName, $"Created approval rule {input.RuleName}", companyId);

        TempData["Success"] = "Approval rule created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRule(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var rule = await _context.ApprovalRules.FirstOrDefaultAsync(r => r.CompanyId == companyId && r.Id == id);
        if (rule == null) return NotFound();

        rule.IsActive = !rule.IsActive;
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Toggle", nameof(ApprovalRule), rule.Id.ToString(), $"Toggled approval rule {rule.RuleName}", companyId);

        TempData["Success"] = "Approval rule updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string status)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var request = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.CompanyId == companyId && r.Id == id);
        if (request == null) return NotFound();

        request.Status = string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase) ? "Rejected" : "Approved";
        request.ReviewedBy = User.Identity?.Name;
        request.ReviewedDate = DateTime.Now;

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(request.Status, nameof(ApprovalRequest), request.Id.ToString(), $"{request.Status} approval request {request.RequestType}", companyId);

        TempData["Success"] = $"Request {request.Status.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Index));
    }
}
