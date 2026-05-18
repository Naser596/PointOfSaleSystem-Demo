using WebApplication3.Models;

namespace WebApplication3.Services;

public interface ICompanyObligationFinanceService
{
    Task<PaymentRecord> MarkPaidAsync(int companyId, ObligationPaymentInput input, string? userName = null);
}
