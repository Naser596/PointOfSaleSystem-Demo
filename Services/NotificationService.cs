using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class NotificationService(ApplicationDbContext context) : INotificationService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<int> GenerateInvoiceAndOverdueRemindersAsync(int companyId)
    {
        var today = DateTime.Today;
        var reminderHorizon = today.AddDays(7);
        var invoices = await _context.SalesDocuments
            .Include(d => d.Customer)
            .Where(d =>
                d.CompanyId == companyId &&
                d.DocumentType == "Invoice" &&
                d.Status != "Cancelled" &&
                d.TotalAmount > d.PaidAmount &&
                d.Customer != null &&
                (d.DueDate ?? d.DocumentDate) < reminderHorizon.AddDays(1))
            .ToListAsync();

        var created = 0;
        foreach (var invoice in invoices)
        {
            var balance = invoice.TotalAmount - invoice.PaidAmount;
            var reminderDate = (invoice.DueDate ?? invoice.DocumentDate).Date;
            var isOverdue = reminderDate < today;
            var notificationType = isOverdue ? "OverdueCustomerReminder" : "InvoiceReminder";
            var subject = isOverdue
                ? $"Overdue invoice {invoice.DocumentNumber}"
                : $"Invoice reminder {invoice.DocumentNumber}";
            var body = isOverdue
                ? $"Invoice {invoice.DocumentNumber} is overdue. Balance due: {balance:N2}."
                : $"Invoice {invoice.DocumentNumber} is due on {reminderDate:yyyy-MM-dd}. Balance due: {balance:N2}.";

            if (!string.IsNullOrWhiteSpace(invoice.Customer!.Email))
            {
                created += await QueueOnceAsync(new NotificationMessage
                {
                    CompanyId = companyId,
                    Channel = "Email",
                    NotificationType = notificationType,
                    EntityName = nameof(SalesDocument),
                    EntityId = invoice.Id.ToString(),
                    Recipient = invoice.Customer.Email.Trim(),
                    Subject = subject,
                    Body = body,
                    Status = "Queued",
                    CreatedDate = DateTime.Now
                });
            }

            if (isOverdue && !string.IsNullOrWhiteSpace(invoice.Customer.Phone))
            {
                created += await QueueOnceAsync(new NotificationMessage
                {
                    CompanyId = companyId,
                    Channel = "SMS",
                    NotificationType = notificationType,
                    EntityName = nameof(SalesDocument),
                    EntityId = invoice.Id.ToString(),
                    Recipient = invoice.Customer.Phone.Trim(),
                    Subject = subject,
                    Body = body,
                    Status = "Queued",
                    CreatedDate = DateTime.Now
                });
            }
        }

        return created;
    }

    public async Task<int> GenerateSubscriptionExpiryNotificationsAsync()
    {
        var horizon = DateTime.Today.AddDays(7);
        var companies = await _context.Companies
            .Where(c =>
                c.PlatformAccessEndDate.HasValue &&
                c.PlatformAccessEndDate.Value.Date <= horizon &&
                c.IsActive &&
                !string.IsNullOrWhiteSpace(c.Email))
            .ToListAsync();

        var created = 0;
        foreach (var company in companies)
        {
            var days = (company.PlatformAccessEndDate!.Value.Date - DateTime.Today).Days;
            var subject = days < 0
                ? $"Platform access expired for {company.DisplayName}"
                : $"Platform access expires in {days} day(s)";
            var body = days < 0
                ? $"Platform access for {company.DisplayName} expired on {company.PlatformAccessEndDate:yyyy-MM-dd}."
                : $"Platform access for {company.DisplayName} expires on {company.PlatformAccessEndDate:yyyy-MM-dd}.";

            created += await QueueOnceAsync(new NotificationMessage
            {
                CompanyId = company.Id,
                Channel = "Email",
                NotificationType = "SubscriptionExpiry",
                EntityName = nameof(Company),
                EntityId = company.Id.ToString(),
                Recipient = company.Email!.Trim(),
                Subject = subject,
                Body = body,
                Status = "Queued",
                CreatedDate = DateTime.Now
            });
        }

        return created;
    }

    public async Task MarkSentAsync(int notificationId, int? companyId = null)
    {
        var notification = await FindNotificationAsync(notificationId, companyId);
        notification.Status = "Sent";
        notification.SentDate = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int notificationId, int? companyId = null)
    {
        var notification = await FindNotificationAsync(notificationId, companyId);
        notification.Status = "Cancelled";
        await _context.SaveChangesAsync();
    }

    private async Task<int> QueueOnceAsync(NotificationMessage message)
    {
        var exists = await _context.NotificationMessages.AnyAsync(n =>
            n.CompanyId == message.CompanyId &&
            n.NotificationType == message.NotificationType &&
            n.EntityName == message.EntityName &&
            n.EntityId == message.EntityId &&
            n.Channel == message.Channel &&
            n.Recipient == message.Recipient &&
            n.Status != "Cancelled");
        if (exists)
        {
            return 0;
        }

        _context.NotificationMessages.Add(message);
        await _context.SaveChangesAsync();
        return 1;
    }

    private async Task<NotificationMessage> FindNotificationAsync(int notificationId, int? companyId)
    {
        var query = _context.NotificationMessages.Where(n => n.Id == notificationId);
        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        return await query.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Notification was not found.");
    }
}
