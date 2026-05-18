using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers;

[Authorize(Roles = "Admin,HR")]
public class HrController(
    ApplicationDbContext context,
    ICurrentCompanyService currentCompany,
    IAuditLogService auditLog,
    IHrPayrollService payrollService,
    ICompanyObligationFinanceService obligationFinance) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;
    private readonly IAuditLogService _auditLog = auditLog;
    private readonly IHrPayrollService _payrollService = payrollService;
    private readonly ICompanyObligationFinanceService _obligationFinance = obligationFinance;

    public async Task<IActionResult> Index()
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        return View(new HrDashboardViewModel
        {
            Employees = await _context.Employees
                .Where(e => e.CompanyId == companyId)
                .OrderByDescending(e => e.IsActive)
                .ThenBy(e => e.FullName)
                .ToListAsync(),
            EmployeeObligations = await _context.PayrollObligations
                .Include(o => o.Employee)
                .Where(o => o.CompanyId == companyId && o.ObligationType == "Payroll")
                .OrderBy(o => o.Status == "Paid")
                .ThenBy(o => o.DueDate ?? DateTime.MaxValue)
                .ThenBy(o => o.PayeeName)
                .Take(100)
                .ToListAsync(),
            PayrollRuns = await _context.PayrollRuns
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Employee)
                .Include(r => r.Lines)
                    .ThenInclude(l => l.PayrollObligation)
                .Include(r => r.PayrollObligation)
                .Where(r => r.CompanyId == companyId)
                .OrderByDescending(r => r.PeriodEnd)
                .ThenByDescending(r => r.Id)
                .Take(50)
                .ToListAsync(),
            FinancialAccounts = await _context.FinancialAccounts
                .Where(a => a.CompanyId == companyId && a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync(),
            PayrollInput = new PayrollRunInput()
        });
    }

    public async Task<IActionResult> Payslip(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var line = await _context.PayrollRunLines
            .Include(l => l.Employee)
            .Include(l => l.PayrollRun)
            .Include(l => l.PayrollObligation)
            .FirstOrDefaultAsync(l => l.Id == id && l.PayrollRun.CompanyId == companyId);
        if (line == null) return NotFound();

        await _auditLog.LogAsync("ViewPayslip", nameof(PayrollRunLine), line.Id.ToString(), $"Viewed payslip for {line.Employee.FullName}", companyId);
        return View(line);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(EmployeeInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FullName) || input.MonthlySalary < 0)
        {
            TempData["Error"] = "Employee name and valid salary are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var employeeNumber = string.IsNullOrWhiteSpace(input.EmployeeNumber) ? null : input.EmployeeNumber.Trim().ToUpperInvariant();
        if (employeeNumber != null)
        {
            var exists = await _context.Employees.AnyAsync(e => e.CompanyId == companyId && e.EmployeeNumber == employeeNumber);
            if (exists)
            {
                TempData["Error"] = "Employee number already exists.";
                return RedirectToAction(nameof(Index));
            }
        }

        var employee = new Employee
        {
            CompanyId = companyId,
            FullName = input.FullName.Trim(),
            EmployeeNumber = employeeNumber,
            JobTitle = string.IsNullOrWhiteSpace(input.JobTitle) ? null : input.JobTitle.Trim(),
            Department = string.IsNullOrWhiteSpace(input.Department) ? null : input.Department.Trim(),
            PersonalNumber = string.IsNullOrWhiteSpace(input.PersonalNumber) ? null : input.PersonalNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
            Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim(),
            EmergencyContact = string.IsNullOrWhiteSpace(input.EmergencyContact) ? null : input.EmergencyContact.Trim(),
            HireDate = input.HireDate.Date,
            MonthlySalary = input.MonthlySalary,
            SalaryDueDay = NormalizeSalaryDueDay(input.SalaryDueDay),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        await CreateOrUpdateEmployeeSalaryObligationAsync(employee, User.Identity?.Name);
        await _auditLog.LogAsync("Create", nameof(Employee), employee.Id.ToString(), $"Created employee {employee.FullName}", companyId);
        TempData["Success"] = "Employee created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmployee(EmployeeInput input)
    {
        if (input.Id <= 0 || string.IsNullOrWhiteSpace(input.FullName) || input.MonthlySalary < 0)
        {
            TempData["Error"] = "Employee name and valid salary are required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == input.Id && e.CompanyId == companyId);
        if (employee == null) return NotFound();

        var employeeNumber = string.IsNullOrWhiteSpace(input.EmployeeNumber) ? null : input.EmployeeNumber.Trim().ToUpperInvariant();
        if (employeeNumber != null)
        {
            var exists = await _context.Employees.AnyAsync(e =>
                e.CompanyId == companyId &&
                e.EmployeeNumber == employeeNumber &&
                e.Id != employee.Id);
            if (exists)
            {
                TempData["Error"] = "Employee number already exists.";
                return RedirectToAction(nameof(Index));
            }
        }

        var before = new
        {
            employee.FullName,
            employee.EmployeeNumber,
            employee.JobTitle,
            employee.Department,
            employee.PersonalNumber,
            employee.Email,
            employee.Phone,
            employee.Address,
            employee.EmergencyContact,
            employee.HireDate,
            employee.MonthlySalary,
            employee.SalaryDueDay,
            employee.Notes
        };

        employee.FullName = input.FullName.Trim();
        employee.EmployeeNumber = employeeNumber;
        employee.JobTitle = string.IsNullOrWhiteSpace(input.JobTitle) ? null : input.JobTitle.Trim();
        employee.Department = string.IsNullOrWhiteSpace(input.Department) ? null : input.Department.Trim();
        employee.PersonalNumber = string.IsNullOrWhiteSpace(input.PersonalNumber) ? null : input.PersonalNumber.Trim();
        employee.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        employee.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
        employee.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
        employee.EmergencyContact = string.IsNullOrWhiteSpace(input.EmergencyContact) ? null : input.EmergencyContact.Trim();
        employee.HireDate = input.HireDate.Date;
        employee.MonthlySalary = input.MonthlySalary;
        employee.SalaryDueDay = NormalizeSalaryDueDay(input.SalaryDueDay);
        employee.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        await _context.SaveChangesAsync();
        if (employee.IsActive)
        {
            await CreateOrUpdateEmployeeSalaryObligationAsync(employee, User.Identity?.Name);
        }
        await _auditLog.LogChangeAsync("Update", nameof(Employee), employee.Id.ToString(), before, new
        {
            employee.FullName,
            employee.EmployeeNumber,
            employee.JobTitle,
            employee.Department,
            employee.PersonalNumber,
            employee.Email,
            employee.Phone,
            employee.Address,
            employee.EmergencyContact,
            employee.HireDate,
            employee.MonthlySalary,
            employee.SalaryDueDay,
            employee.Notes
        }, $"Updated employee {employee.FullName}", companyId);

        TempData["Success"] = "Employee updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployee(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);
        if (employee == null) return NotFound();

        employee.IsActive = !employee.IsActive;
        employee.TerminationDate = employee.IsActive ? null : DateTime.Today;
        await _context.SaveChangesAsync();
        if (employee.IsActive)
        {
            await CreateOrUpdateEmployeeSalaryObligationAsync(employee, User.Identity?.Name);
        }
        else
        {
            await CancelOpenEmployeeSalaryObligationsAsync(employee);
        }
        await _auditLog.LogAsync("Toggle", nameof(Employee), employee.Id.ToString(), $"Changed employee status {employee.FullName}", companyId);
        TempData["Success"] = "Employee status updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayrollRun(PayrollRunInput input)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        try
        {
            var run = await _payrollService.CreatePayrollRunAsync(companyId, input, User.Identity?.Name);
            await _auditLog.LogAsync("Create", nameof(PayrollRun), run.Id.ToString(), $"Created payroll run {run.RunNumber}", companyId);
            TempData["Success"] = $"Payroll run {run.RunNumber} created and obligation posted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayrollLine(PayrollRunEmployeeLineInput input)
    {
        if (input.Id <= 0)
        {
            TempData["Error"] = "Payroll line is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var line = await _context.PayrollRunLines
            .Include(l => l.PayrollRun)
                .ThenInclude(r => r.Lines)
            .Include(l => l.Employee)
            .Include(l => l.PayrollObligation)
            .FirstOrDefaultAsync(l => l.Id == input.Id && l.PayrollRun.CompanyId == companyId);
        if (line == null) return NotFound();

        if (line.PayrollObligation?.IsPaid == true)
        {
            TempData["Error"] = "Paid payroll lines cannot be edited. Reopen or reverse the obligation first.";
            return RedirectToAction(nameof(Index));
        }

        if (input.BaseSalary < 0 || input.BonusAmount < 0 || input.DeductionAmount < 0 || input.TaxPercent < 0)
        {
            TempData["Error"] = "Payroll amounts cannot be negative.";
            return RedirectToAction(nameof(Index));
        }

        var before = new
        {
            line.BaseSalaryAmount,
            line.BonusAmount,
            line.OtherDeductionsAmount,
            line.TaxAmount,
            line.GrossAmount,
            line.DeductionsAmount,
            line.NetAmount,
            line.Notes
        };

        ApplyPayrollLineAmounts(line, input);
        if (line.NetAmount < 0)
        {
            TempData["Error"] = "Payroll net amount cannot be negative.";
            return RedirectToAction(nameof(Index));
        }

        if (line.PayrollObligation != null)
        {
            line.PayrollObligation.Amount = line.NetAmount;
            line.PayrollObligation.Description = line.Employee.FullName;
            line.PayrollObligation.PayeeName = line.Employee.FullName;
            line.PayrollObligation.DueDate = line.PayrollRun.DueDate;
            line.PayrollObligation.Notes = line.Notes;
        }

        RecalculatePayrollRunTotals(line.PayrollRun);
        await _context.SaveChangesAsync();
        await _auditLog.LogChangeAsync("Update", nameof(PayrollRunLine), line.Id.ToString(), before, new
        {
            line.BaseSalaryAmount,
            line.BonusAmount,
            line.OtherDeductionsAmount,
            line.TaxAmount,
            line.GrossAmount,
            line.DeductionsAmount,
            line.NetAmount,
            line.Notes
        }, $"Updated payroll line for {line.Employee.FullName}", companyId);

        TempData["Success"] = "Payroll line updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePayrollLine(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var line = await _context.PayrollRunLines
            .Include(l => l.PayrollRun)
                .ThenInclude(r => r.Lines)
            .Include(l => l.Employee)
            .Include(l => l.PayrollObligation)
            .FirstOrDefaultAsync(l => l.Id == id && l.PayrollRun.CompanyId == companyId);
        if (line == null) return NotFound();

        if (line.PayrollObligation?.IsPaid == true)
        {
            TempData["Error"] = "Paid payroll lines cannot be deleted. Reopen or reverse the obligation first.";
            return RedirectToAction(nameof(Index));
        }

        var run = line.PayrollRun;
        var employeeName = line.Employee.FullName;
        if (line.PayrollObligation != null)
        {
            _context.PayrollObligations.Remove(line.PayrollObligation);
        }
        _context.PayrollRunLines.Remove(line);
        run.Lines.Remove(line);
        RecalculatePayrollRunTotals(run);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", nameof(PayrollRunLine), id.ToString(), $"Deleted payroll line for {employeeName}", companyId);

        TempData["Success"] = "Payroll line deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePayrollRun(int id)
    {
        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
                .ThenInclude(l => l.Employee)
            .Include(r => r.Lines)
                .ThenInclude(l => l.PayrollObligation)
            .Include(r => r.PayrollObligation)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);
        if (run == null) return NotFound();

        var obligationIds = run.Lines
            .Select(l => l.PayrollObligationId)
            .Append(run.PayrollObligationId)
            .Where(obligationId => obligationId.HasValue)
            .Select(obligationId => obligationId!.Value)
            .Distinct()
            .ToList();
        var obligationIdStrings = obligationIds.Select(obligationId => obligationId.ToString()).ToList();

        var hasPostedPayments = await _context.PaymentRecords.AnyAsync(p =>
            p.CompanyId == companyId &&
            p.EntityName == nameof(PayrollObligation) &&
            p.EntityId != null &&
            obligationIdStrings.Contains(p.EntityId) &&
            p.Status == "Completed");
        if (hasPostedPayments || run.Lines.Any(l => l.PayrollObligation?.IsPaid == true) || run.PayrollObligation?.IsPaid == true)
        {
            TempData["Error"] = "Paid payroll runs cannot be deleted. Use a reversal flow instead.";
            return RedirectToAction(nameof(Index));
        }

        var linkedObligations = run.Lines
            .Select(l => l.PayrollObligation)
            .Where(o => o != null)
            .Cast<PayrollObligation>()
            .ToList();
        if (run.PayrollObligation != null)
        {
            linkedObligations.Add(run.PayrollObligation);
        }

        foreach (var obligation in linkedObligations.DistinctBy(o => o.Id))
        {
            _context.PayrollObligations.Remove(obligation);
        }

        _context.PayrollRuns.Remove(run);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync("Delete", nameof(PayrollRun), run.Id.ToString(), $"Deleted payroll run {run.RunNumber}", companyId);

        TempData["Success"] = "Payroll run deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayPayrollRun(PayrollRunPaymentBatchInput input)
    {
        if (input.PayrollRunId <= 0)
        {
            TempData["Error"] = "Payroll run is required.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await _currentCompany.GetRequiredCompanyIdAsync();
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
                .ThenInclude(l => l.Employee)
            .Include(r => r.Lines)
                .ThenInclude(l => l.PayrollObligation)
            .FirstOrDefaultAsync(r => r.Id == input.PayrollRunId && r.CompanyId == companyId);
        if (run == null) return NotFound();

        var payableLines = run.Lines
            .Where(l => l.PayrollObligationId.HasValue && l.PayrollObligation?.IsPaid != true)
            .OrderBy(l => l.Employee.FullName)
            .ToList();
        if (!payableLines.Any())
        {
            TempData["Error"] = "No open payroll obligations are available for this run.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            foreach (var line in payableLines)
            {
                await _obligationFinance.MarkPaidAsync(companyId, new ObligationPaymentInput
                {
                    Id = line.PayrollObligationId!.Value,
                    FinancialAccountId = input.FinancialAccountId,
                    PaymentDate = input.PaymentDate,
                    PaymentMethod = string.IsNullOrWhiteSpace(input.PaymentMethod) ? "Bank" : input.PaymentMethod.Trim(),
                    Reference = input.Reference
                }, User.Identity?.Name);
            }

            run.Status = "Paid";
            await _context.SaveChangesAsync();
            await _auditLog.LogAsync("PayBatch", nameof(PayrollRun), run.Id.ToString(), $"Paid {payableLines.Count} payroll obligation(s) for {run.RunNumber}", companyId);
            TempData["Success"] = $"Payroll batch paid for {payableLines.Count} employee(s).";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static void ApplyPayrollLineAmounts(PayrollRunLine line, PayrollRunEmployeeLineInput input)
    {
        line.BaseSalaryAmount = Math.Round(input.BaseSalary, 2);
        line.BonusAmount = Math.Round(input.BonusAmount, 2);
        line.OtherDeductionsAmount = Math.Round(input.DeductionAmount, 2);
        line.TaxAmount = Math.Round((line.BaseSalaryAmount + line.BonusAmount) * input.TaxPercent / 100, 2);
        line.GrossAmount = line.BaseSalaryAmount + line.BonusAmount;
        line.DeductionsAmount = line.OtherDeductionsAmount + line.TaxAmount;
        line.NetAmount = line.GrossAmount - line.DeductionsAmount;
        line.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
    }

    private static void RecalculatePayrollRunTotals(PayrollRun run)
    {
        run.GrossAmount = run.Lines.Sum(l => l.GrossAmount);
        run.DeductionsAmount = run.Lines.Sum(l => l.DeductionsAmount);
        run.NetAmount = run.Lines.Sum(l => l.NetAmount);
        if (!run.Lines.Any())
        {
            run.Status = "Cancelled";
        }
    }

    private async Task CreateOrUpdateEmployeeSalaryObligationAsync(Employee employee, string? userName)
    {
        if (employee.MonthlySalary <= 0)
        {
            return;
        }

        var periodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var periodEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
        var dueDay = Math.Clamp(employee.SalaryDueDay <= 0 ? 5 : employee.SalaryDueDay, 1, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
        var dueDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, dueDay);

        var obligation = await _context.PayrollObligations.FirstOrDefaultAsync(o =>
            o.CompanyId == employee.CompanyId &&
            o.EmployeeId == employee.Id &&
            o.ObligationType == "Payroll" &&
            o.PeriodStart == periodStart &&
            o.PeriodEnd == periodEnd &&
            o.Status == "Open");

        var notes = BuildEmployeeObligationNotes(employee);
        if (obligation == null)
        {
            _context.PayrollObligations.Add(new PayrollObligation
            {
                CompanyId = employee.CompanyId,
                EmployeeId = employee.Id,
                Description = employee.FullName,
                ObligationType = "Payroll",
                PayeeName = employee.FullName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                DueDate = dueDate,
                Amount = employee.MonthlySalary,
                Status = "Open",
                Notes = notes,
                CreatedDate = DateTime.Now,
                CreatedBy = userName
            });
        }
        else
        {
            obligation.Description = employee.FullName;
            obligation.PayeeName = employee.FullName;
            obligation.DueDate = dueDate;
            obligation.Amount = employee.MonthlySalary;
            obligation.Notes = notes;
        }

        await _context.SaveChangesAsync();
    }

    private async Task CancelOpenEmployeeSalaryObligationsAsync(Employee employee)
    {
        var obligations = await _context.PayrollObligations
            .Where(o =>
                o.CompanyId == employee.CompanyId &&
                o.EmployeeId == employee.Id &&
                o.ObligationType == "Payroll" &&
                o.Status == "Open")
            .ToListAsync();

        foreach (var obligation in obligations)
        {
            obligation.Status = "Cancelled";
            obligation.Notes = string.IsNullOrWhiteSpace(obligation.Notes)
                ? "Cancelled because employee was deactivated."
                : $"{obligation.Notes} | Cancelled because employee was deactivated.";
        }

        await _context.SaveChangesAsync();
    }

    private static int NormalizeSalaryDueDay(int salaryDueDay)
    {
        return Math.Clamp(salaryDueDay <= 0 ? 5 : salaryDueDay, 1, 31);
    }

    private static string? BuildEmployeeObligationNotes(Employee employee)
    {
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(employee.EmployeeNumber)) notes.Add($"No: {employee.EmployeeNumber}");
        if (!string.IsNullOrWhiteSpace(employee.JobTitle)) notes.Add(employee.JobTitle);
        if (!string.IsNullOrWhiteSpace(employee.Department)) notes.Add(employee.Department);
        if (!string.IsNullOrWhiteSpace(employee.Notes)) notes.Add(employee.Notes);
        return notes.Count == 0 ? null : string.Join(" | ", notes);
    }
}
