using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }

    [ValidateNever]
    public Company? Company { get; set; }

    [MaxLength(256)]
    public string? UserId { get; set; }

    [MaxLength(256)]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? EntityId { get; set; }

    [MaxLength(1000)]
    public string? Summary { get; set; }

    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }

    [MaxLength(80)]
    public string? IpAddress { get; set; }

    public DateTime CreatedDate { get; set; }
}
