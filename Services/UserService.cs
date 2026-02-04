using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication3.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    Roles = roles.ToList()
                });
            }

            return userDtos;
        }

        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = roles.ToList()
            };
        }

        public async Task<(bool Success, string[] Errors)> CreateUserAsync(CreateUserDto dto)
        {
            var user = new IdentityUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            if (!string.IsNullOrEmpty(dto.Role))
            {
                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> UpdateUserAsync(UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.LockoutEnabled = dto.LockoutEnabled;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    return (false, passwordResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!string.IsNullOrEmpty(dto.Role))
            {
                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            return (true, Array.Empty<string>());
        }

        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();
            
            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<List<string>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        }
    }
}
