using System.Security.Claims;
using System.Text.Json;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services;

public class AuditLogService(
    ApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    ICurrentCompanyService currentCompany) : IAuditLogService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICurrentCompanyService _currentCompany = currentCompany;

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        string? summary = null,
        int? companyId = null,
        string? actorUserId = null,
        string? actorUserName = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var resolvedCompanyId = companyId ?? await _currentCompany.GetCompanyIdAsync();

        _context.AuditLogs.Add(new AuditLog
        {
            CompanyId = resolvedCompanyId,
            UserId = actorUserId ?? httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = actorUserName ?? httpContext?.User.Identity?.Name,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Summary = summary,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedDate = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task LogChangeAsync<T>(
        string action,
        string entityName,
        string? entityId,
        T? before,
        T? after,
        string? summary = null,
        int? companyId = null,
        string? actorUserId = null,
        string? actorUserName = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var resolvedCompanyId = companyId ?? await _currentCompany.GetCompanyIdAsync();
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        _context.AuditLogs.Add(new AuditLog
        {
            CompanyId = resolvedCompanyId,
            UserId = actorUserId ?? httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = actorUserName ?? httpContext?.User.Identity?.Name,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Summary = summary,
            BeforeJson = before == null ? null : JsonSerializer.Serialize(before, jsonOptions),
            AfterJson = after == null ? null : JsonSerializer.Serialize(after, jsonOptions),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedDate = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }
}
