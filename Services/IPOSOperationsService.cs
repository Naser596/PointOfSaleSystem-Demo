using WebApplication3.Models;

namespace WebApplication3.Services;

public interface IPOSOperationsService
{
    Task<Sale> CreateSaleAsync(int companyId, PosSaleInput input, string userName);
    Task<List<OfflineSaleSyncResult>> SyncOfflineSalesAsync(int companyId, IEnumerable<OfflineSaleSyncInput> requests, string userName);
    Task<OfflineSaleSyncResult> RetryOfflineSyncRecordAsync(int companyId, int recordId, string userName);
    Task CancelOfflineSyncRecordAsync(int companyId, int recordId, string userName);
}
