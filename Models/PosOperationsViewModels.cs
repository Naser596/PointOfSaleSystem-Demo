namespace WebApplication3.Models;

public class PosSaleInput
{
    public List<PosSaleItemInput> Items { get; set; } = [];
    public string PaymentMethod { get; set; } = "Cash";
    public int? CustomerId { get; set; }
    public string? DiscountCode { get; set; }
}

public class PosSaleItemInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OfflineSaleSyncInput : PosSaleInput
{
    public string ClientId { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public string? LastError { get; set; }
}

public class OfflineSaleSyncResult
{
    public string ClientId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int? SaleId { get; set; }
    public string? SaleNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class OfflineSyncDashboardViewModel
{
    public List<OfflineSyncRecord> Records { get; set; } = [];
    public int PendingCount { get; set; }
    public int SyncedCount { get; set; }
    public int FailedCount { get; set; }
    public int ConflictCount { get; set; }
    public int CancelledCount { get; set; }
}
