using WebApplication3.Models;

namespace WebApplication3.Services;

public interface IErpReportService
{
    Task<ReportBuilderViewModel> BuildReportAsync(int companyId, DateTime? dateFrom, DateTime? dateTo);
}
