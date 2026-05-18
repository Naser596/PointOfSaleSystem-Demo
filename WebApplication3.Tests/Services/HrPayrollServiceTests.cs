using WebApplication3.Models;
using WebApplication3.Services;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Services;

public sealed class HrPayrollServiceTests
{
    [Fact]
    public async Task CreatePayrollRunAsync_CreatesLinesAndPayrollObligation()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Employees.AddRange(
            Employee(1, "A", 1000),
            Employee(2, "B", 1200));
        await database.Context.SaveChangesAsync();
        var service = new HrPayrollService(database.Context);

        var run = await service.CreatePayrollRunAsync(1, new PayrollRunInput
        {
            PeriodStart = new DateTime(2026, 5, 1),
            PeriodEnd = new DateTime(2026, 5, 31),
            DueDate = new DateTime(2026, 6, 5)
        }, "tester");

        Assert.Equal("Posted", run.Status);
        Assert.Equal(2, run.Lines.Count);
        Assert.Equal(2200, run.NetAmount);
        Assert.Equal(2, run.Lines.Count(l => l.PayrollObligation != null));
        Assert.Equal(2200, run.Lines.Sum(l => l.PayrollObligation?.Amount ?? 0));
        Assert.Contains("A", run.Lines.Select(l => l.PayrollObligation?.PayeeName));
        Assert.Contains("B", run.Lines.Select(l => l.PayrollObligation?.PayeeName));
    }

    [Fact]
    public async Task CreatePayrollRunAsync_RejectsWhenNoEmployees()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        await database.Context.SaveChangesAsync();
        var service = new HrPayrollService(database.Context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePayrollRunAsync(1, new PayrollRunInput()));

        Assert.Contains("No active employees", error.Message);
    }

    [Fact]
    public async Task CreatePayrollRunAsync_RejectsDuplicateEmployeePeriodWithoutOverride()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Employees.Add(Employee(1, "A", 1000));
        await database.Context.SaveChangesAsync();
        var service = new HrPayrollService(database.Context);
        var input = new PayrollRunInput
        {
            PeriodStart = new DateTime(2026, 5, 1),
            PeriodEnd = new DateTime(2026, 5, 31),
            DueDate = new DateTime(2026, 6, 5)
        };

        await service.CreatePayrollRunAsync(1, input, "tester");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePayrollRunAsync(1, input, "tester"));

        Assert.Contains("Payroll already exists", error.Message);
        Assert.Single(database.Context.PayrollRuns.Where(r => r.PeriodStart == input.PeriodStart && r.PeriodEnd == input.PeriodEnd));
    }

    [Fact]
    public async Task CreatePayrollRunAsync_AllowsDuplicatePeriodWithOverrideReason()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Employees.Add(Employee(1, "A", 1000));
        await database.Context.SaveChangesAsync();
        var service = new HrPayrollService(database.Context);
        var input = new PayrollRunInput
        {
            PeriodStart = new DateTime(2026, 5, 1),
            PeriodEnd = new DateTime(2026, 5, 31),
            DueDate = new DateTime(2026, 6, 5)
        };

        var firstRun = await service.CreatePayrollRunAsync(1, input, "tester");
        var secondRun = await service.CreatePayrollRunAsync(1, new PayrollRunInput
        {
            PeriodStart = input.PeriodStart,
            PeriodEnd = input.PeriodEnd,
            DueDate = input.DueDate,
            AllowDuplicateOverride = true,
            OverrideReason = "Correction payment"
        }, "tester");

        Assert.NotEqual(firstRun.RunNumber, secondRun.RunNumber);
        Assert.Contains("Duplicate override", secondRun.Lines.Single().Notes);
        Assert.Equal(2, database.Context.PayrollRuns.Count(r => r.PeriodStart == input.PeriodStart && r.PeriodEnd == input.PeriodEnd));
    }

    [Fact]
    public async Task CreatePayrollRunAsync_AppliesBonusDeductionsAndTax()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Employees.Add(Employee(1, "A", 1000));
        await database.Context.SaveChangesAsync();
        var service = new HrPayrollService(database.Context);

        var run = await service.CreatePayrollRunAsync(1, new PayrollRunInput
        {
            PeriodStart = new DateTime(2026, 6, 1),
            PeriodEnd = new DateTime(2026, 6, 30),
            BonusPercent = 10,
            DeductionPercent = 5,
            TaxPercent = 10
        }, "tester");

        Assert.Equal(1100, run.GrossAmount);
        Assert.Equal(165, run.DeductionsAmount);
        Assert.Equal(935, run.NetAmount);
        Assert.Equal(935, run.Lines.Single().PayrollObligation?.Amount);
    }

    [Fact]
    public async Task CreatePayrollRunAsync_UsesSelectedEmployeeLineAdjustments()
    {
        using var database = new TestDatabase();
        database.Context.Companies.Add(Company(1));
        database.Context.Employees.AddRange(
            Employee(1, "A", 1000),
            Employee(2, "B", 1200));
        await database.Context.SaveChangesAsync();
        var service = new HrPayrollService(database.Context);

        var run = await service.CreatePayrollRunAsync(1, new PayrollRunInput
        {
            PeriodStart = new DateTime(2026, 7, 1),
            PeriodEnd = new DateTime(2026, 7, 31),
            Lines =
            [
                new PayrollRunEmployeeLineInput
                {
                    EmployeeId = 1,
                    Include = true,
                    BaseSalary = 1000,
                    BonusAmount = 100,
                    DeductionAmount = 25,
                    TaxPercent = 10,
                    Notes = "Performance bonus"
                },
                new PayrollRunEmployeeLineInput
                {
                    EmployeeId = 2,
                    Include = false,
                    BaseSalary = 1200
                }
            ]
        }, "tester");

        var line = Assert.Single(run.Lines);
        Assert.Equal(1000, line.BaseSalaryAmount);
        Assert.Equal(100, line.BonusAmount);
        Assert.Equal(110, line.TaxAmount);
        Assert.Equal(25, line.OtherDeductionsAmount);
        Assert.Equal(135, line.DeductionsAmount);
        Assert.Equal(965, line.NetAmount);
        Assert.Equal(965, line.PayrollObligation?.Amount);
        Assert.Equal("A", line.PayrollObligation?.PayeeName);
        Assert.Contains("Performance bonus", line.Notes);
    }

    private static Employee Employee(int id, string name, decimal salary)
    {
        return new Employee
        {
            Id = id,
            CompanyId = 1,
            FullName = name,
            HireDate = DateTime.Today,
            MonthlySalary = salary,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
    }

    private static Company Company(int id)
    {
        return new Company
        {
            Id = id,
            DisplayName = $"Company {id}",
            InvoicePrefix = "INV",
            PrimaryColor = "#2563eb",
            CurrencyCode = "USD",
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
    }
}
