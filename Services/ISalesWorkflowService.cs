using WebApplication3.Models;

namespace WebApplication3.Services;

public interface ISalesWorkflowService
{
    Task<SalesDocument> ConvertDocumentAsync(int sourceDocumentId, string targetDocumentType, string? userName = null);
    Task<PaymentRecord> RecordPaymentAsync(SalesDocumentPaymentInput input, string? userName = null);
}
