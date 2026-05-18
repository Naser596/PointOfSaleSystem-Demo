using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using WebApplication3.Data;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogsController(ApplicationDbContext context, ICurrentCompanyService currentCompany) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;

    public async Task<IActionResult> Index(string? auditAction = null, string? entity = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var query = _context.AuditLogs
            .Where(l => l.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(auditAction))
        {
            query = query.Where(l => l.Action == auditAction);
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            query = query.Where(l => l.EntityName == entity);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(l => l.CreatedDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var exclusiveTo = dateTo.Value.Date.AddDays(1);
            query = query.Where(l => l.CreatedDate < exclusiveTo);
        }

        ViewBag.AuditAction = auditAction;
        ViewBag.Entity = entity;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.TodayCount = await _context.AuditLogs
            .Where(l => l.CompanyId == companyId && l.CreatedDate >= DateTime.Today)
            .CountAsync();
        ViewBag.ChangeCount = await _context.AuditLogs
            .Where(l => l.CompanyId == companyId && l.Action != "Login" && l.Action != "Logout")
            .CountAsync();
        ViewBag.LastActivity = await _context.AuditLogs
            .Where(l => l.CompanyId == companyId)
            .OrderByDescending(l => l.CreatedDate)
            .Select(l => (DateTime?)l.CreatedDate)
            .FirstOrDefaultAsync();
        ViewBag.Actions = await _context.AuditLogs
            .Where(l => l.CompanyId == companyId)
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        ViewBag.Entities = await _context.AuditLogs
            .Where(l => l.CompanyId == companyId)
            .Select(l => l.EntityName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedDate)
            .Take(250)
            .ToListAsync();

        return View(logs);
    }

    public async Task<IActionResult> Export(string? auditAction = null, string? entity = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var query = _context.AuditLogs.Where(l => l.CompanyId == companyId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(auditAction)) query = query.Where(l => l.Action == auditAction);
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(l => l.EntityName == entity);
        if (dateFrom.HasValue) query = query.Where(l => l.CreatedDate >= dateFrom.Value.Date);
        if (dateTo.HasValue) query = query.Where(l => l.CreatedDate < dateTo.Value.Date.AddDays(1));

        var logs = await query.OrderByDescending(l => l.CreatedDate).Take(5000).ToListAsync();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Audit Logs");
        var headers = new[] { "Date", "User", "Action", "Entity", "Entity ID", "Summary", "IP", "Before", "After" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var row = 0; row < logs.Count; row++)
        {
            var log = logs[row];
            var excelRow = row + 2;
            sheet.Cell(excelRow, 1).Value = log.CreatedDate;
            sheet.Cell(excelRow, 2).Value = log.UserName ?? "System";
            sheet.Cell(excelRow, 3).Value = log.Action;
            sheet.Cell(excelRow, 4).Value = log.EntityName;
            sheet.Cell(excelRow, 5).Value = log.EntityId;
            sheet.Cell(excelRow, 6).Value = log.Summary;
            sheet.Cell(excelRow, 7).Value = log.IpAddress;
            sheet.Cell(excelRow, 8).Value = log.BeforeJson;
            sheet.Cell(excelRow, 9).Value = log.AfterJson;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"audit-logs-{DateTime.Today:yyyyMMdd}.xlsx");
    }
}
