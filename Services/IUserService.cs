using Microsoft.AspNetCore.Identity;

namespace WebApplication3.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync(int? companyId = null);
        Task<UserDto?> GetUserByIdAsync(string id, int? companyId = null);
        Task<(bool Success, string[] Errors)> CreateUserAsync(CreateUserDto dto, int? companyId = null);
        Task<(bool Success, string[] Errors)> UpdateUserAsync(UpdateUserDto dto, int? companyId = null);
        Task<(bool Success, string[] Errors)> DeleteUserAsync(string id, int? companyId = null);
        Task<(bool Success, string[] Errors)> ResetPasswordAsync(string userId, string newPassword, int? companyId = null);
        Task<List<string>> GetUserRolesAsync(string userId);
        Task<List<string>> GetAllRolesAsync();
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
    }

    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier";
    }

    public class UpdateUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool LockoutEnabled { get; set; }
    }
}
