using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models;

public class SalesReturnViewModel
{
    public int SaleId { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RefundedAmount { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public List<SalesReturnItemViewModel> Items { get; set; } = [];
}

public class SalesReturnItemViewModel
{
    public int SaleItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int ReturnableQuantity { get; set; }
    public decimal UnitPrice { get; set; }

    [Range(0, 100000)]
    public int ReturnQuantity { get; set; }
}

