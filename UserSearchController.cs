using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Controllers
{
    [Authorize(Roles = "HelpDesk,SysNet")]
    public class UserSearchController : Controller
    {
        private readonly IActiveDirectoryService _adService;
        private readonly ILogger<UserSearchController> _logger;

        public UserSearchController(IActiveDirectoryService adService, ILogger<UserSearchController> logger)
        {
            _adService = adService;
            _logger = logger;
        }

        // Kullanıcı arama sayfası
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // AJAX ile kullanıcı arama
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new { results = new object[0] });
            }

            try
            {
                var users = await _adService.SearchUsersAsync(term);
                var results = users.Select(u => new
                {
                    id = u.Username,
                    text = $"{u.DisplayName} ({u.Username})",
                    email = u.Email,
                    department = u.Department,
                    title = u.Title
                });

                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return Json(new { results = new object[0] });
            }
        }

        // Kullanıcı detayları
        [HttpGet]
        public async Task<IActionResult> UserDetails(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return NotFound();
            }

            try
            {
                var user = await _adService.GetUserDetailsAsync(username);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user details for {Username}", username);
                TempData["ErrorMessage"] = "Kullanıcı bilgileri yüklenirken hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}