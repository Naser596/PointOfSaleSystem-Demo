using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLog;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IAuditLogService auditLog)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _auditLog = auditLog;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    var isSuperAdmin = user != null && await _userManager.IsInRoleAsync(user, "SuperAdmin");

                    if (user != null && !isSuperAdmin)
                    {
                        var companyActive = user.CompanyId.HasValue &&
                            await _context.Companies.AnyAsync(c => c.Id == user.CompanyId.Value && c.IsActive);

                        if (!companyActive)
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty, "Your company account is inactive. Contact the platform owner.");
                            if (IsAjaxRequest())
                            {
                                return Json(new { success = false, message = "Your company account is inactive. Contact the platform owner." });
                            }

                            return View(model);
                        }
                    }

                    SetAuthSessionMarker();
                    if (user != null)
                    {
                        await _auditLog.LogAsync("Login", "Account", user.Id, $"User {user.Email} logged in", user.CompanyId, user.Id, user.Email);
                    }
                    var destination = GetPostLoginDestination(returnUrl, isSuperAdmin);
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = true, redirectUrl = destination });
                    }

                    return Redirect(destination);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Invalid login attempt." });
                    }

                    return View(model);
                }
            }

            if (IsAjaxRequest())
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                return Json(new { success = false, message = errors.FirstOrDefault() ?? "Please check the login form." });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _auditLog.LogAsync("Logout", "Account", null, $"User {User.Identity?.Name ?? "Unknown"} logged out");
            await _signInManager.SignOutAsync();
            ClearAuthSessionMarker();
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            var loginUrl = Url.Action(nameof(Login))!;
            if (IsAjaxRequest())
            {
                return Json(new { success = true, redirectUrl = loginUrl });
            }

            Response.Headers.Location = loginUrl;
            return StatusCode(StatusCodes.Status303SeeOther);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SessionStatus()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            return Json(new
            {
                authenticated = isAuthenticated,
                session = isAuthenticated ? Request.Cookies["pos_session"] : null
            });
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        private string GetPostLoginDestination(string? returnUrl, bool isSuperAdmin)
        {
            var defaultDestination = isSuperAdmin
                ? Url.Action("Index", "SuperAdmin")!
                : Url.Action(nameof(HomeController.Index), "Home")!;

            if (!Url.IsLocalUrl(returnUrl) ||
                returnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
            {
                return defaultDestination;
            }

            var isPlatformOwnerArea =
                returnUrl.StartsWith("/SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                returnUrl.StartsWith("/Settings", StringComparison.OrdinalIgnoreCase);

            if (isPlatformOwnerArea && !isSuperAdmin)
            {
                return defaultDestination;
            }

            if (isSuperAdmin && !isPlatformOwnerArea)
            {
                return defaultDestination;
            }

            return returnUrl;
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private void SetAuthSessionMarker()
        {
            Response.Cookies.Append("pos_session", Guid.NewGuid().ToString("N"), new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/"
            });
        }

        private void ClearAuthSessionMarker()
        {
            Response.Cookies.Delete("pos_session", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps
            });
        }
    }
}
