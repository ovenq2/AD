using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;
using ADUserManagement.Constants;
using System.Diagnostics;

namespace ADUserManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IActiveDirectoryService _adService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IActiveDirectoryService adService, ILogger<AccountController> logger)
        {
            _adService = adService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // AD üzerinden kullanıcı adı ve şifre doğrulama
            var isAuthenticated = await _adService.ValidateCredentialsAsync(model.Username, model.Password);

            if (!isAuthenticated)
            {
                _logger.LogWarning(LogMessages.LoginAttemptFailed, model.Username,
    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", "Invalid credentials");
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                return View(model);
            }

            // Kullanıcı adını normalize et (domain kısmını çıkar)
            var username = model.Username;
            if (username.Contains("\\"))
            {
                username = username.Split('\\')[1];
            }

            // Grup kontrolü yap
            var isHelpDesk = await _adService.IsUserInGroupAsync(username, "HelpDesk");
            var isSysNet = await _adService.IsUserInGroupAsync(username, "SysNet");

            if (!isHelpDesk && !isSysNet)
            {
                _logger.LogWarning(LogMessages.LoginAccessDenied, username);
                ModelState.AddModelError(string.Empty,
                    "Bu uygulamaya erişim yetkiniz bulunmamaktadır. HelpDesk veya SysNet grubunda olmanız gerekmektedir.");
                return View(model);
            }

            // Kullanıcı email bilgisini al
            var userEmail = await _adService.GetUserEmailAsync(username) ?? $"{username}@param.com.tr";

            // Claims oluştur
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, userEmail),
                new Claim("DisplayName", username)
            };

            // Roller ekle
            if (isHelpDesk)
            {
                claims.Add(new Claim(ClaimTypes.Role, "HelpDesk"));
            }
            if (isSysNet)
            {
                claims.Add(new Claim(ClaimTypes.Role, "SysNet"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? System.DateTimeOffset.UtcNow.AddDays(7) : System.DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation(LogMessages.LoginAttemptSuccess, username,
    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation(LogMessages.UserLoggedOut, username ?? "Unknown");

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}