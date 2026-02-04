using Microsoft.AspNetCore.Identity;

namespace WebApplication3.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(string id);
        Task<(bool Success, string[] Errors)> CreateUserAsync(CreateUserDto dto);
        Task<(bool Success, string[] Errors)> UpdateUserAsync(UpdateUserDto dto);
        Task<(bool Success, string[] Errors)> DeleteUserAsync(string id);
        Task<(bool Success, string[] Errors)> ResetPasswordAsync(string userId, string newPassword);
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
