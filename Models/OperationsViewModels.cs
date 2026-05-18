namespace WebApplication3.Models;

public class OperationsDashboardViewModel
{
    public List<Store> Stores { get; set; } = [];
    public List<RegisterSession> OpenSessions { get; set; } = [];
    public List<RegisterSession> RecentSessions { get; set; } = [];
    public StoreInput StoreInput { get; set; } = new();
    public RegisterInput RegisterInput { get; set; } = new();
    public RegisterSessionOpenInput OpenInput { get; set; } = new();
    public RegisterSessionCloseInput CloseInput { get; set; } = new();
}

public class StoreInput
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
}

public class RegisterInput
{
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}

public class RegisterSessionOpenInput
{
    public int RegisterId { get; set; }
    public decimal OpeningCash { get; set; }
    public string? Notes { get; set; }
}

public class RegisterSessionCloseInput
{
    public int SessionId { get; set; }
    public decimal ClosingCash { get; set; }
    public string? Notes { get; set; }
}
