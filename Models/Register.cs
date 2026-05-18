using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApplication3.Models;

public class Register
{
    public int Id { get; set; }
    public int CompanyId { get; set; }

    [ValidateNever]
    public Company Company { get; set; } = null!;

    public int StoreId { get; set; }

    [ValidateNever]
    public Store Store { get; set; } = null!;

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }

    public List<RegisterSession> Sessions { get; set; } = [];
}
