using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            customer.CreatedDate = DateTime.Now;
            customer.UpdatedDate = DateTime.Now;
            
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            var existing = await _context.Customers.FindAsync(customer.Id);
            if (existing == null) throw new KeyNotFoundException("Customer not found");

            existing.Name = customer.Name;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.Address = customer.Address;
            existing.Notes = customer.Notes;
            existing.UpdatedDate = DateTime.Now;

            _context.Customers.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteCustomerAsync(int id, string? deletedBy = null)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return false;

            // Soft Delete
            customer.IsDeleted = true;
            customer.DeletedDate = DateTime.Now;
            customer.DeletedBy = deletedBy ?? "System";
            customer.UpdatedDate = DateTime.Now;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Customer>> SearchCustomersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Customer>();

            query = query.ToLower();
            return await _context.Customers
                .Where(c => c.Name.ToLower().Contains(query) || 
                            (c.Phone != null && c.Phone.Contains(query)) ||
                            (c.Email != null && c.Email.ToLower().Contains(query)))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task UpdateCustomerStatsAsync(int customerId, decimal purchaseAmount)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null)
            {
                customer.TotalPurchases += purchaseAmount;
                customer.VisitCount += 1;
                customer.LoyaltyPoints += (int)purchaseAmount; // Simple 1 point per $1 logic
                customer.UpdatedDate = DateTime.Now;
                
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }
        }
    }
}
