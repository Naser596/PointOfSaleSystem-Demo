namespace WebApplication3.Models;

public class FinanceDashboardViewModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal PayrollLiability { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal SupplierSpend { get; set; }
    public decimal PaidObligationExpense { get; set; }
    public decimal OpenObligationLiability { get; set; }
    public decimal NetProfitAfterPayroll { get; set; }
    public decimal NetProfitAfterPaidObligations { get; set; }
    public int SaleCount { get; set; }
    public int ReturnCount { get; set; }
    public decimal AverageTicket { get; set; }
    public List<FinanceDailyPoint> DailyPoints { get; set; } = [];
    public List<FinanceUserPerformance> UserPerformance { get; set; } = [];
    public List<FinanceMonthlyProfitPoint> MonthlyProfitReport { get; set; } = [];
    public List<PayrollObligation> PayrollObligations { get; set; } = [];
    public PayrollObligationInput PayrollInput { get; set; } = new();
}

public class FinanceDailyPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
}

public class FinanceUserPerformance
{
    public string CashierName { get; set; } = string.Empty;
    public int SaleCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
}

public class FinanceMonthlyProfitPoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal Payroll { get; set; }
    public decimal PaidObligations { get; set; }
    public decimal SupplierSpend { get; set; }
    public decimal NetProfit { get; set; }
}

public class PayrollObligationInput
{
    public string Description { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime PeriodEnd { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Open";
}
