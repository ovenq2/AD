using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Models.Enums;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Controllers
{
    [Authorize(Roles = "HelpDesk,SysNet")]
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRequestService _requestService;
        private readonly IActiveDirectoryService _adService;
        private readonly ILogger<RequestController> _logger;

        public RequestController(
            ApplicationDbContext context,
            IRequestService requestService,
            IActiveDirectoryService adService,
            ILogger<RequestController> logger)
        {
            _context = context;
            _requestService = requestService;
            _adService = adService;
            _logger = logger;
        }

        // Kullanıcı açma talebi formu
        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                _logger.LogInformation("CreateUser GET method called by {User}", User.Identity?.Name);

                // Duplicate kontrolü için distinct kullan
                var companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .Distinct()
                    .ToListAsync();

                _logger.LogInformation("Found {Count} active companies", companies.Count);

                var model = new UserCreationRequestViewModel
                {
                    Companies = companies
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = c.CompanyName
                        })
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateUser GET");
                TempData["ErrorMessage"] = "Form yüklenirken bir hata oluştu.";
                return RedirectToAction("MyRequests");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreationRequestViewModel model)
        {
            _logger.LogInformation("CreateUser POST method called by {User}", User.Identity?.Name);

            try
            {
                // Companies listesini model state'ten çıkar
                ModelState.Remove("Companies");

                // Model state'i kontrol et
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Model error: {Error}", modelError.ErrorMessage);
                    }

                    // Companies listesini yeniden yükle
                    model.Companies = await _context.Companies
                        .Where(c => c.IsActive)
                        .Distinct()
                        .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CompanyName })
                        .ToListAsync();

                    return View(model);
                }

                _logger.LogInformation("Creating user request for username: {Username}", model.Username);

                var requestId = await _requestService.CreateUserCreationRequestAsync(model, User.Identity.Name);

                _logger.LogInformation("User creation request created successfully with ID: {RequestId}", requestId);

                TempData["SuccessMessage"] = "Kullanıcı açma talebiniz başarıyla oluşturuldu.";
                return RedirectToAction("MyRequests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user creation request for username: {Username}", model.Username);

                ModelState.AddModelError(string.Empty, $"Talep oluşturulurken bir hata oluştu: {ex.Message}");

                // Companies listesini yeniden yükle
                model.Companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .Distinct()
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CompanyName })
                    .ToListAsync();

                return View(model);
            }
        }

        // Kullanıcı kapatma talebi formu
        [HttpGet]
        public IActionResult DeleteUser()
        {
            _logger.LogInformation("DeleteUser GET method called by {User}", User.Identity?.Name);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(UserDeletionRequestViewModel model)
        {
            _logger.LogInformation("DeleteUser POST method called by {User}", User.Identity?.Name);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var requestId = await _requestService.CreateUserDeletionRequestAsync(model, User.Identity.Name);

                _logger.LogInformation("User deletion request created successfully with ID: {RequestId}", requestId);

                TempData["SuccessMessage"] = "Kullanıcı kapatma talebiniz başarıyla oluşturuldu.";
                return RedirectToAction("MyRequests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user deletion request for username: {Username}", model.Username);
                ModelState.AddModelError(string.Empty, $"Talep oluşturulurken bir hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // Attribute değiştirme talebi formu
        [HttpGet]
        public async Task<IActionResult> ChangeAttribute(string username = null)
        {
            var model = new UserAttributeChangeViewModel
            {
                Username = username,
                AttributeOptions = ADAttributes.UserAttributes
                    .Select(a => new SelectListItem
                    {
                        Value = a.Key,
                        Text = a.Value
                    })
                    .ToList()
            };

            // Eğer username parametresi varsa, mevcut değerleri getir
            if (!string.IsNullOrEmpty(username))
            {
                ViewBag.PreselectedUser = username;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAttribute(UserAttributeChangeViewModel model)
        {
            _logger.LogInformation("ChangeAttribute POST method called by {User}", User.Identity?.Name);

            if (!ModelState.IsValid)
            {
                model.AttributeOptions = ADAttributes.UserAttributes
                    .Select(a => new SelectListItem { Value = a.Key, Text = a.Value })
                    .ToList();
                return View(model);
            }

            try
            {
                var requestId = await _requestService.CreateAttributeChangeRequestAsync(model, User.Identity.Name);

                _logger.LogInformation("Attribute change request created successfully with ID: {RequestId}", requestId);

                TempData["SuccessMessage"] = "Attribute değiştirme talebiniz başarıyla oluşturuldu.";
                return RedirectToAction("MyRequests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attribute change request");
                ModelState.AddModelError(string.Empty, $"Talep oluşturulurken bir hata oluştu: {ex.Message}");

                model.AttributeOptions = ADAttributes.UserAttributes
                    .Select(a => new SelectListItem { Value = a.Key, Text = a.Value })
                    .ToList();

                return View(model);
            }
        }

        // AJAX - Kullanıcının mevcut attribute değerini getir
        [HttpGet]
        public async Task<IActionResult> GetUserAttributeValue(string username, string attributeName)
        {
            try
            {
                var userDetails = await _adService.GetUserDetailsAsync(username);
                if (userDetails == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                string currentValue = attributeName switch
                {
                    "telephoneNumber" => userDetails.Phone,
                    "mobile" => userDetails.Mobile,
                    "department" => userDetails.Department,
                    "title" => userDetails.Title,
                    "physicalDeliveryOfficeName" => userDetails.Office,
                    "description" => userDetails.Description,
                    _ => null
                };

                return Json(new { success = true, value = currentValue ?? "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user attribute value");
                return Json(new { success = false, message = "Hata oluştu" });
            }
        }

        // Taleplerim
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            try
            {
                _logger.LogInformation("MyRequests accessed by {User}", User.Identity?.Name);

                var requests = await _requestService.GetMyRequestsAsync(User.Identity.Name);

                _logger.LogInformation("Found {Count} requests for user {User}", requests.Count, User.Identity?.Name);

                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting requests for user {User}", User.Identity?.Name);
                TempData["ErrorMessage"] = "Talepler yüklenirken bir hata oluştu.";
                return View(new List<RequestListViewModel>());
            }
        }

        // Talep detayları
        [HttpGet]
        public async Task<IActionResult> Details(int id, string type)
        {
            try
            {
                if (type == "creation")
                {
                    var request = await _requestService.GetUserCreationRequestAsync(id);
                    if (request == null)
                        return NotFound();

                    return View("CreationRequestDetails", request);
                }
                else if (type == "deletion")
                {
                    var request = await _requestService.GetUserDeletionRequestAsync(id);
                    if (request == null)
                        return NotFound();

                    return View("DeletionRequestDetails", request);
                }
                else if (type == "attribute")
                {
                    var request = await _requestService.GetAttributeChangeRequestAsync(id);
                    if (request == null)
                        return NotFound();

                    return View("AttributeRequestDetails", request);
                }
                else if (type == "password")
                {
                    var request = await _requestService.GetPasswordResetRequestAsync(id);
                    if (request == null)
                        return NotFound();

                    return View("PasswordRequestDetails", request);
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request details for ID: {Id}, Type: {Type}", id, type);
                TempData["ErrorMessage"] = "Talep detayları yüklenirken bir hata oluştu.";
                return RedirectToAction("MyRequests");
            }
        }

    }
}