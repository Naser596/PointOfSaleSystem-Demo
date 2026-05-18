using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models;

public class PayrollObligation
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ObligationType { get; set; } = "Payroll";

    [MaxLength(200)]
    public string? PayeeName { get; set; }

    public int? EmployeeId { get; set; }

    [ValidateNever]
    public Employee? Employee { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Amount { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Open";

    public DateTime? PaidDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    [MaxLength(256)]
    public string? CreatedBy { get; set; }

    [NotMapped]
    public bool IsPaid => string.Equals(Status, "Paid", StringComparison.OrdinalIgnoreCase);

    [NotMapped]
    public bool IsOverdue => !IsPaid && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;
}
