using WebApplication3.Models;

namespace WebApplication3.Services;

public interface INotificationService
{
    Task<int> GenerateInvoiceAndOverdueRemindersAsync(int companyId);
    Task<int> GenerateSubscriptionExpiryNotificationsAsync();
    Task MarkSentAsync(int notificationId, int? companyId = null);
    Task CancelAsync(int notificationId, int? companyId = null);
}
