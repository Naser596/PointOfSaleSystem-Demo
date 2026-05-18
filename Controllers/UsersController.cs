using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _auditLog;

        public UsersController(IUserService userService, UserManager<ApplicationUser> userManager, IAuditLogService auditLog)
        {
            _userService = userService;
            _userManager = userManager;
            _auditLog = auditLog;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync(await GetCurrentCompanyIdAsync());
            ViewBag.Roles = await _userService.GetAllRolesAsync();
            return View(users);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _userService.GetAllRolesAsync();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                ViewBag.Roles = await _userService.GetAllRolesAsync();
                return View(dto);
            }

            var (success, errors) = await _userService.CreateUserAsync(dto, await GetCurrentCompanyIdAsync());
            if (success)
            {
                await _auditLog.LogAsync("Create", nameof(ApplicationUser), dto.Email, $"Created user {dto.Email} with role {dto.Role}", await GetCurrentCompanyIdAsync());
                TempData["Success"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }
            ViewBag.Roles = await _userService.GetAllRolesAsync();
            return View(dto);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userService.GetUserByIdAsync(id, await GetCurrentCompanyIdAsync());
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Roles = await _userService.GetAllRolesAsync();
            var dto = new UpdateUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Roles.FirstOrDefault() ?? "",
                LockoutEnabled = user.LockoutEnabled
            };
            return View(dto);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UpdateUserDto dto)
        {
            if (id != dto.Id)
            {
                return NotFound();
            }

            var (success, errors) = await _userService.UpdateUserAsync(dto, await GetCurrentCompanyIdAsync());
            if (success)
            {
                await _auditLog.LogAsync("Update", nameof(ApplicationUser), dto.Id, $"Updated user {dto.Email} with role {dto.Role}", await GetCurrentCompanyIdAsync());
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }
            ViewBag.Roles = await _userService.GetAllRolesAsync();
            return View(dto);
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var companyId = await GetCurrentCompanyIdAsync();
            var user = await _userService.GetUserByIdAsync(id, companyId);
            var (success, errors) = await _userService.DeleteUserAsync(id, companyId);
            if (success)
            {
                await _auditLog.LogAsync("Delete", nameof(ApplicationUser), id, $"Deleted user {user?.Email ?? id}", companyId);
                TempData["Success"] = "User deleted successfully!";
            }
            else
            {
                TempData["Error"] = string.Join(", ", errors);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var companyId = await GetCurrentCompanyIdAsync();
            var user = await _userService.GetUserByIdAsync(id, companyId);
            var (success, errors) = await _userService.ResetPasswordAsync(id, newPassword, companyId);
            if (success)
            {
                await _auditLog.LogAsync("ResetPassword", nameof(ApplicationUser), id, $"Reset password for user {user?.Email ?? id}", companyId);
                TempData["Success"] = "Password reset successfully!";
            }
            else
            {
                TempData["Error"] = string.Join(", ", errors);
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetCurrentCompanyIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.CompanyId;
        }
    }
}
