using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<UserDto>> GetAllUsersAsync(int? companyId = null)
        {
            var query = _userManager.Users.Include(u => u.Company).AsQueryable();
            if (companyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == companyId.Value);
            }

            var users = await query.ToListAsync();
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
                    Roles = roles.ToList(),
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.DisplayName
                });
            }

            return userDtos;
        }

        public async Task<UserDto?> GetUserByIdAsync(string id, int? companyId = null)
        {
            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;
            if (companyId.HasValue && user.CompanyId != companyId.Value) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                Roles = roles.ToList(),
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.DisplayName
            };
        }

        public async Task<(bool Success, string[] Errors)> CreateUserAsync(CreateUserDto dto, int? companyId = null)
        {
            var role = NormalizeAssignableRole(dto.Role, companyId);
            if (!string.IsNullOrEmpty(role) && !await _roleManager.RoleExistsAsync(role))
            {
                return (false, new[] { "Selected role is not available" });
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true,
                CompanyId = companyId
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            if (!string.IsNullOrEmpty(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> UpdateUserAsync(UpdateUserDto dto, int? companyId = null)
        {
            var user = await _userManager.FindByIdAsync(dto.Id);
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            if (companyId.HasValue && user.CompanyId != companyId.Value)
            {
                return (false, new[] { "User does not belong to your company" });
            }

            var role = NormalizeAssignableRole(dto.Role, companyId);
            if (!string.IsNullOrEmpty(role) && !await _roleManager.RoleExistsAsync(role))
            {
                return (false, new[] { "Selected role is not available" });
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
            if (!string.IsNullOrEmpty(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> DeleteUserAsync(string id, int? companyId = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            if (companyId.HasValue && user.CompanyId != companyId.Value)
            {
                return (false, new[] { "User does not belong to your company" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }

            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> ResetPasswordAsync(string userId, string newPassword, int? companyId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            if (companyId.HasValue && user.CompanyId != companyId.Value)
            {
                return (false, new[] { "User does not belong to your company" });
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
            return await _roleManager.Roles
                .Where(r => r.Name != "SuperAdmin")
                .Select(r => r.Name!)
                .OrderBy(r => r)
                .ToListAsync();
        }

        private static string? NormalizeAssignableRole(string? requestedRole, int? companyId)
        {
            if (string.IsNullOrWhiteSpace(requestedRole))
            {
                return null;
            }

            if (!companyId.HasValue)
            {
                return requestedRole;
            }

            var allowedCompanyRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Admin",
                "Manager",
                "Accountant",
                "Warehouse",
                "HR",
                "Cashier"
            };

            return allowedCompanyRoles.Contains(requestedRole) ? requestedRole : "Cashier";
        }
    }
}
