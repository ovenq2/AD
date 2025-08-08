using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Controllers
{
    [Authorize(Roles = "HelpDesk,SysNet")]
    public class PasswordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IActiveDirectoryService _adService;
        private readonly ILogger<PasswordController> _logger;

        public PasswordController(
            ApplicationDbContext context,
            IPasswordService passwordService,
            IActiveDirectoryService adService,
            ILogger<PasswordController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _adService = adService;
            _logger = logger;
        }

        // Şifre sıfırlama talebi formu
        [HttpGet]
        public IActionResult ResetRequest()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetRequest(PasswordResetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kullanıcının var olduğunu kontrol et
                var userExists = await _adService.UserExistsAsync(model.Username);
                if (!userExists)
                {
                    ModelState.AddModelError("Username", "Kullanıcı bulunamadı.");
                    return View(model);
                }

                // Kullanıcı detaylarını al (email için)
                var userDetails = await _adService.GetUserDetailsAsync(model.Username);
                if (userDetails == null || string.IsNullOrEmpty(userDetails.Email))
                {
                    ModelState.AddModelError(string.Empty, "Kullanıcının e-posta adresi tanımlı değil. IT departmanı ile iletişime geçin.");
                    return View(model);
                }

                model.UserEmail = userDetails.Email;

                // Talebi oluştur
                var requestId = await _passwordService.CreatePasswordResetRequestAsync(model, User.Identity.Name);

                _logger.LogInformation($"Password reset request created for user {model.Username} by {User.Identity.Name}");

                TempData["SuccessMessage"] = $"Şifre sıfırlama talebiniz başarıyla oluşturuldu. Kullanıcı: {model.Username}";
                return RedirectToAction(nameof(MyRequests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating password reset request for user {model.Username}");
                ModelState.AddModelError(string.Empty, $"Talep oluşturulurken bir hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // Taleplerim
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            try
            {
                var user = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

                if (user == null)
                {
                    return View(new List<RequestListViewModel>());
                }

                var requests = await _context.PasswordResetRequests
                    .Include(r => r.Status)
                    .Where(r => r.RequestedById == user.Id)
                    .OrderByDescending(r => r.RequestedDate)
                    .Select(r => new RequestListViewModel
                    {
                        Id = r.Id,
                        RequestNumber = r.RequestNumber,
                        RequestType = "password",
                        DisplayName = r.Username,
                        Username = r.Username,
                        Email = r.UserEmail,
                        Company = "-",
                        Status = r.Status.StatusName,
                        RequestedDate = r.RequestedDate,
                        ApprovedDate = r.ApprovedDate
                    })
                    .ToListAsync();

                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting password reset requests");
                TempData["ErrorMessage"] = "Talepler yüklenirken bir hata oluştu.";
                return View(new List<RequestListViewModel>());
            }
        }

        // Talep detayları
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var request = await _context.PasswordResetRequests
                    .Include(r => r.RequestedBy)
                    .Include(r => r.ApprovedBy)
                    .Include(r => r.Status)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    return NotFound();
                }

                // Sadece kendi talebini veya SysNet ise tüm talepleri görebilir
                var user = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

                if (user != null && request.RequestedById != user.Id && !User.IsInRole("SysNet"))
                {
                    return Forbid();
                }

                return View(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting password reset request details for ID: {id}");
                TempData["ErrorMessage"] = "Talep detayları yüklenirken bir hata oluştu.";
                return RedirectToAction(nameof(MyRequests));
            }
        }

        // AJAX - Kullanıcı bilgilerini getir
        [HttpGet]
        public async Task<IActionResult> GetUserInfo(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Kullanıcı adı boş olamaz" });
                }

                var userDetails = await _adService.GetUserDetailsAsync(username);
                if (userDetails == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                return Json(new
                {
                    success = true,
                    displayName = userDetails.DisplayName,
                    email = userDetails.Email,
                    department = userDetails.Department,
                    title = userDetails.Title,
                    isEnabled = userDetails.IsEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return Json(new { success = false, message = "Hata oluştu" });
            }
        }
    }
}