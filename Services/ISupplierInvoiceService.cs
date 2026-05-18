using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface ISupplierInvoiceService
    {
        Task<List<SupplierInvoice>> GetAllAsync();
        Task<SupplierInvoice?> GetByIdAsync(int id);
        Task<SupplierInvoiceCreateViewModel> BuildCreateModelAsync();
        Task PopulateMatchingOptionsAsync(SupplierInvoiceCreateViewModel model);
        Task<SupplierInvoice> CreateAsync(SupplierInvoiceCreateViewModel model, string? createdBy = null);
    }
}
