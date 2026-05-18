using WebApplication3.Models;

namespace WebApplication3.Services;

public interface IHrPayrollService
{
    Task<PayrollRun> CreatePayrollRunAsync(int companyId, PayrollRunInput input, string? userName = null);
}
