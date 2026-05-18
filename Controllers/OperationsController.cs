using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin")]
public class OperationsController(
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
        var model = new OperationsDashboardViewModel
        {
            Stores = await _context.Stores
                .Include(s => s.Registers)
                .Where(s => s.CompanyId == companyId)
                .OrderBy(s => s.Name)
                .ToListAsync(),
            OpenSessions = await _context.RegisterSessions
                .Include(s => s.Register)
                .ThenInclude(r => r.Store)
                .Where(s => s.CompanyId == companyId && s.Status == "Open")
                .OrderByDescending(s => s.OpenedAt)
                .ToListAsync(),
            RecentSessions = await _context.RegisterSessions
                .Include(s => s.Register)
                .ThenInclude(r => r.Store)
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.OpenedAt)
                .Take(20)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStore(StoreInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["Error"] = "Store name is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var store = new Store
        {
            CompanyId = companyId,
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim().ToUpperInvariant(),
            Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim(),
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(Store), store.Id.ToString(), $"Created store {store.Name}", companyId);
        TempData["Success"] = "Store created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRegister(RegisterInput input)
    {
        if (input.StoreId <= 0 || string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["Error"] = "Store and register name are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var storeExists = await _context.Stores.AnyAsync(s => s.Id == input.StoreId && s.CompanyId == companyId);
        if (!storeExists) return NotFound();

        var register = new Register
        {
            CompanyId = companyId,
            StoreId = input.StoreId,
            Name = input.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim().ToUpperInvariant(),
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _context.Registers.Add(register);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Create", nameof(Register), register.Id.ToString(), $"Created register {register.Name}", companyId);
        TempData["Success"] = "Register created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenShift(RegisterSessionOpenInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var register = await _context.Registers
            .FirstOrDefaultAsync(r => r.Id == input.RegisterId && r.CompanyId == companyId);
        if (register == null) return NotFound();

        var alreadyOpen = await _context.RegisterSessions
            .AnyAsync(s => s.CompanyId == companyId && s.RegisterId == input.RegisterId && s.Status == "Open");
        if (alreadyOpen)
        {
            TempData["Error"] = "This register already has an open shift.";
            return RedirectToAction(nameof(Index));
        }

        var session = new RegisterSession
        {
            CompanyId = companyId,
            RegisterId = input.RegisterId,
            OpenedAt = DateTime.Now,
            OpenedBy = User.Identity?.Name ?? "Unknown",
            OpeningCash = input.OpeningCash,
            Status = "Open",
            Notes = input.Notes
        };

        _context.RegisterSessions.Add(session);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("OpenShift", nameof(RegisterSession), session.Id.ToString(), $"Opened shift for {register.Name}", companyId);
        TempData["Success"] = "Shift opened.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseShift(RegisterSessionCloseInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var session = await _context.RegisterSessions
            .Include(s => s.Register)
            .FirstOrDefaultAsync(s => s.Id == input.SessionId && s.CompanyId == companyId);
        if (session == null) return NotFound();

        if (session.Status == "Closed")
        {
            TempData["Error"] = "This shift is already closed.";
            return RedirectToAction(nameof(Index));
        }

        var cashSales = await _context.Sales
            .Where(s => s.CompanyId == companyId &&
                        s.CashierName == session.OpenedBy &&
                        s.PaymentMethod == "Cash" &&
                        s.SaleDate >= session.OpenedAt &&
                        s.SaleDate <= DateTime.Now)
            .SumAsync(s => s.TotalAmount - s.RefundedAmount);

        session.ClosedAt = DateTime.Now;
        session.ClosedBy = User.Identity?.Name ?? "Unknown";
        session.ExpectedCash = session.OpeningCash + cashSales;
        session.ClosingCash = input.ClosingCash;
        session.Difference = input.ClosingCash - session.ExpectedCash.Value;
        session.Status = "Closed";
        session.Notes = string.IsNullOrWhiteSpace(input.Notes)
            ? session.Notes
            : $"{session.Notes}\nClose: {input.Notes}".Trim();

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("CloseShift", nameof(RegisterSession), session.Id.ToString(), $"Closed shift for {session.Register.Name}. Difference: {session.Difference:N2}", companyId);
        TempData["Success"] = "Shift closed.";
        return RedirectToAction(nameof(Index));
    }
}
