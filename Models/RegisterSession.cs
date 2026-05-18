using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models;

public class RegisterSession
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int RegisterId { get; set; }

    [ValidateNever]
    public Register Register { get; set; } = null!;

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    [MaxLength(256)]
    public string OpenedBy { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? ClosedBy { get; set; }

    public decimal OpeningCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? Difference { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Open";

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
