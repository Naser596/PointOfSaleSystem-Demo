namespace WebApplication3.Services;

public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? summary = null,
        int? companyId = null,
        string? actorUserId = null,
        string? actorUserName = null);

    Task LogChangeAsync<T>(
        string action,
        string entityName,
        string? entityId,
        T? before,
        T? after,
        string? summary = null,
        int? companyId = null,
        string? actorUserId = null,
        string? actorUserName = null);
}
