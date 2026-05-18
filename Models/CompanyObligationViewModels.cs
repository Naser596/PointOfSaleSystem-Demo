namespace WebApplication3.Models;

public class CompanyObligationsViewModel
{
    public string? Status { get; set; }
    public string? Type { get; set; }
    public List<PayrollObligation> Obligations { get; set; } = [];
    public List<FinancialAccount> FinancialAccounts { get; set; } = [];
    public CompanyObligationInput Input { get; set; } = new();
    public decimal OpenTotal { get; set; }
    public decimal OverdueTotal { get; set; }
    public decimal DueThisMonthTotal { get; set; }
    public decimal PaidThisMonthTotal { get; set; }
}

public class CompanyObligationInput
{
    public string ObligationType { get; set; } = "Payroll";
    public string Description { get; set; } = string.Empty;
    public string? PayeeName { get; set; }
    public DateTime? DueDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Open";
    public string? Notes { get; set; }
}

public class ObligationPaymentInput
{
    public int Id { get; set; }
    public int? FinancialAccountId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
}
