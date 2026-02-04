using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
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

            var (success, errors) = await _userService.CreateUserAsync(dto);
            if (success)
            {
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
            var user = await _userService.GetUserByIdAsync(id);
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

            var (success, errors) = await _userService.UpdateUserAsync(dto);
            if (success)
            {
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
            var (success, errors) = await _userService.DeleteUserAsync(id);
            if (success)
            {
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

            var (success, errors) = await _userService.ResetPasswordAsync(id, newPassword);
            if (success)
            {
                TempData["Success"] = "Password reset successfully!";
            }
            else
            {
                TempData["Error"] = string.Join(", ", errors);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
