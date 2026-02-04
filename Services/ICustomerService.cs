using WebApplication3.Models;

namespace WebApplication3.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(int id, string? deletedBy = null);
        Task<List<Customer>> SearchCustomersAsync(string query);
        Task UpdateCustomerStatsAsync(int customerId, decimal purchaseAmount);
    }
}
