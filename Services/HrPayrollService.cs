using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class HrPayrollService(ApplicationDbContext context) : IHrPayrollService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PayrollRun> CreatePayrollRunAsync(int companyId, PayrollRunInput input, string? userName = null)
    {
        if (input.PeriodEnd.Date < input.PeriodStart.Date)
        {
            throw new InvalidOperationException("Payroll period end must be after the start date.");
        }
        if (input.BonusPercent < 0 || input.DeductionPercent < 0 || input.TaxPercent < 0)
        {
            throw new InvalidOperationException("Payroll percentages cannot be negative.");
        }

        var periodStart = input.PeriodStart.Date;
        var periodEnd = input.PeriodEnd.Date;
        var employees = await _context.Employees
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .OrderBy(e => e.FullName)
            .ToListAsync();
        if (!employees.Any())
        {
            throw new InvalidOperationException("No active employees found.");
        }

        var run = new PayrollRun
        {
            CompanyId = companyId,
            RunNumber = await GenerateRunNumberAsync(companyId),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            DueDate = input.DueDate?.Date,
            Status = "Posted",
            CreatedBy = userName,
            CreatedDate = DateTime.Now
        };

        var selectedLines = BuildSelectedLines(input, employees);
        if (!selectedLines.Any())
        {
            throw new InvalidOperationException("Select at least one active employee for payroll.");
        }

        await EnsurePayrollPeriodIsNotDuplicatedAsync(companyId, periodStart, periodEnd, selectedLines, input);

        foreach (var selectedLine in selectedLines)
        {
            var employee = selectedLine.Employee;
            var baseSalary = selectedLine.BaseSalary;
            var bonus = selectedLine.BonusAmount;
            var otherDeductions = selectedLine.DeductionAmount;
            var tax = Math.Round((baseSalary + bonus) * selectedLine.TaxPercent / 100, 2);
            var gross = baseSalary + bonus;
            var deductions = otherDeductions + tax;
            var net = gross - deductions;
            if (net < 0)
            {
                throw new InvalidOperationException($"Payroll net amount cannot be negative for {employee.FullName}.");
            }

            run.Lines.Add(new PayrollRunLine
            {
                EmployeeId = employee.Id,
                BaseSalaryAmount = baseSalary,
                BonusAmount = bonus,
                OtherDeductionsAmount = otherDeductions,
                TaxAmount = tax,
                GrossAmount = gross,
                DeductionsAmount = deductions,
                NetAmount = net,
                Notes = BuildPayrollLineNotes(employee.JobTitle, bonus, otherDeductions, selectedLine.TaxPercent, selectedLine.Notes, input.OverrideReason),
                PayrollObligation = new PayrollObligation
                {
                    CompanyId = companyId,
                    Description = employee.FullName,
                    ObligationType = "Payroll",
                    PayeeName = employee.FullName,
                    PeriodStart = run.PeriodStart,
                    PeriodEnd = run.PeriodEnd,
                    DueDate = run.DueDate,
                    Amount = net,
                    Status = "Open",
                    Notes = BuildObligationNotes(selectedLine.Notes, input.OverrideReason),
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName
                }
            });
            run.GrossAmount += gross;
            run.DeductionsAmount += deductions;
            run.NetAmount += net;
        }

        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync();
        return run;
    }

    private static List<SelectedPayrollLine> BuildSelectedLines(PayrollRunInput input, List<Employee> employees)
    {
        if (input.Lines.Any())
        {
            var employeesById = employees.ToDictionary(e => e.Id);
            return input.Lines
                .Where(l => l.Include)
                .Select(l =>
                {
                    if (!employeesById.TryGetValue(l.EmployeeId, out var employee))
                    {
                        throw new InvalidOperationException("Payroll contains an employee that is not active for this company.");
                    }

                    if (l.BaseSalary < 0 || l.BonusAmount < 0 || l.DeductionAmount < 0 || l.TaxPercent < 0)
                    {
                        throw new InvalidOperationException($"Payroll amounts cannot be negative for {employee.FullName}.");
                    }

                    return new SelectedPayrollLine(
                        employee,
                        Math.Round(l.BaseSalary > 0 ? l.BaseSalary : employee.MonthlySalary, 2),
                        Math.Round(l.BonusAmount, 2),
                        Math.Round(l.DeductionAmount, 2),
                        Math.Round(l.TaxPercent, 2),
                        l.Notes?.Trim());
                })
                .ToList();
        }

        return employees.Select(employee =>
        {
            var bonus = Math.Round(employee.MonthlySalary * input.BonusPercent / 100, 2);
            var gross = employee.MonthlySalary + bonus;
            var deductionAmount = Math.Round(gross * input.DeductionPercent / 100, 2);
            return new SelectedPayrollLine(
                employee,
                employee.MonthlySalary,
                bonus,
                deductionAmount,
                input.TaxPercent,
                null);
        }).ToList();
    }

    private async Task EnsurePayrollPeriodIsNotDuplicatedAsync(
        int companyId,
        DateTime periodStart,
        DateTime periodEnd,
        List<SelectedPayrollLine> selectedLines,
        PayrollRunInput input)
    {
        var employeeIds = selectedLines.Select(l => l.Employee.Id).ToList();
        var duplicateNames = await _context.PayrollRunLines
            .Include(l => l.Employee)
            .Include(l => l.PayrollRun)
            .Where(l =>
                l.PayrollRun.CompanyId == companyId &&
                l.PayrollRun.Status != "Cancelled" &&
                employeeIds.Contains(l.EmployeeId) &&
                l.PayrollRun.PeriodStart <= periodEnd &&
                l.PayrollRun.PeriodEnd >= periodStart)
            .Select(l => l.Employee.FullName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        if (!duplicateNames.Any())
        {
            return;
        }

        if (!input.AllowDuplicateOverride)
        {
            throw new InvalidOperationException($"Payroll already exists for this period for: {string.Join(", ", duplicateNames)}.");
        }

        if (string.IsNullOrWhiteSpace(input.OverrideReason))
        {
            throw new InvalidOperationException("Duplicate payroll override requires a reason.");
        }
    }

    private static string? BuildPayrollLineNotes(string? jobTitle, decimal bonusAmount, decimal deductionAmount, decimal taxPercent, string? customNotes, string? overrideReason)
    {
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(jobTitle)) notes.Add(jobTitle);
        if (bonusAmount > 0) notes.Add($"Bonus {bonusAmount:N2}");
        if (deductionAmount > 0) notes.Add($"Deductions {deductionAmount:N2}");
        if (taxPercent > 0) notes.Add($"Tax {taxPercent:N2}%");
        if (!string.IsNullOrWhiteSpace(customNotes)) notes.Add(customNotes.Trim());
        if (!string.IsNullOrWhiteSpace(overrideReason)) notes.Add($"Duplicate override: {overrideReason.Trim()}");
        return notes.Count == 0 ? null : string.Join(" | ", notes);
    }

    private static string? BuildObligationNotes(string? customNotes, string? overrideReason)
    {
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(customNotes)) notes.Add(customNotes.Trim());
        if (!string.IsNullOrWhiteSpace(overrideReason)) notes.Add($"Duplicate override: {overrideReason.Trim()}");
        return notes.Count == 0 ? null : string.Join(" | ", notes);
    }

    private async Task<string> GenerateRunNumberAsync(int companyId)
    {
        var datePart = DateTime.Today.ToString("yyyyMMdd");
        var startsWith = $"PR-{datePart}-";
        var count = await _context.PayrollRuns
            .CountAsync(r => r.CompanyId == companyId && r.RunNumber.StartsWith(startsWith));
        return $"{startsWith}{count + 1:000}";
    }

    private sealed record SelectedPayrollLine(
        Employee Employee,
        decimal BaseSalary,
        decimal BonusAmount,
        decimal DeductionAmount,
        decimal TaxPercent,
        string? Notes);
}
