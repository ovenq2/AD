using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;
using ADUserManagement.Constants;
using ADUserManagement.Models.Configuration;
using System.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;

namespace ADUserManagement.Controllers
{
    public class AccountController : Controller

    {
        private readonly IActiveDirectoryService _adService;
        private readonly ILogger<AccountController> _logger;
        private readonly ActiveDirectoryConfig _adConfig;
        private readonly ApplicationConfig _appConfig;

        public AccountController(
            IActiveDirectoryService adService,
            ILogger<AccountController> logger,
            IOptions<ActiveDirectoryConfig> adConfig,
            IOptions<ApplicationConfig> appConfig)
        {
            _adService = adService;
            _logger = logger;
            _adConfig = adConfig.Value;
            _appConfig = appConfig.Value;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["CompanyName"] = _appConfig.CompanyName;
            ViewData["DomainName"] = _adConfig.Domain.Split('.')[0]; // Sadece domain adını göster
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginPolicy")]  // ← Login için rate limit
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["CompanyName"] = _appConfig.CompanyName;
            ViewData["DomainName"] = _adConfig.Domain.Split('.')[0];

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

            // Yetkili gruplardan herhangi birinde olup olmadığını kontrol et
            var hasAuthorization = await _adService.IsUserInAnyAuthorizedGroupAsync(username);

            if (!hasAuthorization)
            {
                _logger.LogWarning(LogMessages.LoginAccessDenied, username);

                var authorizedGroupsText = string.Join(", ", _adConfig.AuthorizedGroups);
                ModelState.AddModelError(string.Empty,
                    $"Bu uygulamaya erişim yetkiniz bulunmamaktadır. Şu gruplardan birinde olmanız gerekmektedir: {authorizedGroupsText}");
                return View(model);
            }

            // Kullanıcı detaylarını al
            var userDetails = await _adService.GetUserDetailsAsync(username);
            var userEmail = userDetails?.Email ?? await _adService.GetUserEmailAsync(username) ?? $"{username}@{_adConfig.Domain}";
            var displayName = userDetails?.DisplayName ?? username;

            // Kullanıcının hangi yetkili gruplarda olduğunu belirle
            var userGroups = new List<string>();
            foreach (var group in _adConfig.AuthorizedGroups)
            {
                if (await _adService.IsUserInGroupAsync(username, group))
                {
                    userGroups.Add(group);
                }
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, userEmail),
                new("DisplayName", displayName),
                new("Domain", _adConfig.Domain)
            };

            // Grup bilgilerini claim olarak ekle
            foreach (var group in userGroups)
            {
                claims.Add(new Claim(ClaimTypes.Role, group));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

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
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation(LogMessages.UserLoggedOut, username);

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            ViewData["CompanyName"] = _appConfig.CompanyName;
            ViewData["AuthorizedGroups"] = _adConfig.AuthorizedGroups;
            return View();
        }
    }
}