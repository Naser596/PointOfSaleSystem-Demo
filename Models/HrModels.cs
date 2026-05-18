using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models;

public class Employee
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(160)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EmployeeNumber { get; set; }

    [MaxLength(160)]
    public string? JobTitle { get; set; }

    [MaxLength(160)]
    public string? Department { get; set; }

    [MaxLength(80)]
    public string? PersonalNumber { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(160)]
    public string? EmergencyContact { get; set; }

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public decimal MonthlySalary { get; set; }
    public int SalaryDueDay { get; set; } = 5;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }

    public List<PayrollRunLine> PayrollLines { get; set; } = [];
}

public class PayrollRun
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required, MaxLength(60)]
    public string RunNumber { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? DueDate { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft";

    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }

    public int? PayrollObligationId { get; set; }

    [ValidateNever]
    public PayrollObligation? PayrollObligation { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }
    public List<PayrollRunLine> Lines { get; set; } = [];
}

public class PayrollRunLine
{
    public int Id { get; set; }
    public int PayrollRunId { get; set; }

    [ValidateNever]
    public PayrollRun PayrollRun { get; set; } = null!;

    public int EmployeeId { get; set; }

    [ValidateNever]
    public Employee Employee { get; set; } = null!;

    public int? PayrollObligationId { get; set; }

    [ValidateNever]
    public PayrollObligation? PayrollObligation { get; set; }

    public decimal BaseSalaryAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal OtherDeductionsAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }
}
