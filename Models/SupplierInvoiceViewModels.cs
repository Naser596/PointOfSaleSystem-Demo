using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class SupplierInvoiceCreateViewModel
    {
        [Display(Name = "Match Purchase Order")]
        public int? PurchaseOrderId { get; set; }

        [Display(Name = "Match Goods Receipt")]
        public int? GoodsReceiptId { get; set; }

        public List<PurchaseOrder> AvailablePurchaseOrders { get; set; } = [];
        public List<GoodsReceipt> AvailableGoodsReceipts { get; set; } = [];
        public List<Product> AvailableProducts { get; set; } = [];
        public List<Warehouse> AvailableWarehouses { get; set; } = [];
        public List<StockLocation> AvailableStockLocations { get; set; } = [];

        [Display(Name = "Receive Stock To Warehouse")]
        public int? WarehouseId { get; set; }

        [Display(Name = "Stock Location")]
        public int? StockLocationId { get; set; }

        [Display(Name = "Supplier Invoice No.")]
        public string? SupplierInvoiceNumber { get; set; }

        [Display(Name = "Supplier Name")]
        public string? SupplierName { get; set; }

        [Display(Name = "Supplier Tax / VAT No.")]
        public string? SupplierTaxNumber { get; set; }

        [Display(Name = "Supplier Address")]
        public string? SupplierAddress { get; set; }

        [Display(Name = "Supplier Phone")]
        public string? SupplierPhone { get; set; }

        [EmailAddress]
        [Display(Name = "Supplier Email")]
        public string? SupplierEmail { get; set; }

        [Required]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        public string? Notes { get; set; }

        public List<SupplierInvoiceItemInput> Items { get; set; } = new()
        {
            new SupplierInvoiceItemInput()
        };
    }

    public class SupplierInvoiceItemInput
    {
        [Display(Name = "Product")]
        public int? ProductId { get; set; }

        public string Description { get; set; } = string.Empty;

        public bool CreateNewProduct { get; set; }

        [Display(Name = "New Product Name")]
        public string? NewProductName { get; set; }

        [Display(Name = "New Product SKU")]
        public string? NewProductSku { get; set; }

        [Display(Name = "Sale Price")]
        [Range(0, 1000000)]
        public decimal? NewProductSalePrice { get; set; }

        [Display(Name = "Minimum Stock")]
        [Range(0, 1000)]
        public int NewProductMinStock { get; set; } = 5;

        [Range(0.01, 100000)]
        public decimal Quantity { get; set; } = 1;

        [Range(0, 1000000)]
        public decimal UnitCost { get; set; }

        [Range(0, 100)]
        public decimal TaxRate { get; set; }
    }

    public class SupplierInvoiceDetailsViewModel
    {
        public CompanySettings Company { get; set; } = new();
        public SupplierInvoice Invoice { get; set; } = new();
    }
}
