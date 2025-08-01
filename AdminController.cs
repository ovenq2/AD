using System;
using System.Collections.Generic;
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
    [Authorize(Roles = "SysNet")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRequestService _requestService;
        private readonly IPasswordService _passwordService;
        private readonly IGroupManagementService _groupService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IRequestService requestService,
            IPasswordService passwordService,
            IGroupManagementService groupService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _requestService = requestService;
            _passwordService = passwordService;
            _groupService = groupService;
            _logger = logger;
        }

        // Bekleyen talepler listesi
        [HttpGet]
        public async Task<IActionResult> PendingRequests()
        {
            _logger.LogInformation("PendingRequests accessed by {User}", User.Identity?.Name);

            try
            {
                var pendingRequests = await _requestService.GetPendingRequestsAsync();
                _logger.LogInformation("Found {Count} pending requests", pendingRequests.Count);
                return View(pendingRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending requests");
                TempData["ErrorMessage"] = "Talepler yüklenirken hata oluştu.";
                return View(new List<RequestListViewModel>());
            }
        }

        // Talep detayları ve onay/red işlemi
        [HttpGet]
        public async Task<IActionResult> RequestDetails(int id, string type)
        {
            _logger.LogInformation("RequestDetails accessed for ID: {Id}, Type: {Type} by {User}",
                id, type, User.Identity?.Name);

            try
            {
                if (type == "creation")
                {
                    var request = await _requestService.GetUserCreationRequestAsync(id);
                    if (request == null || request.StatusId != 1)
                    {
                        TempData["ErrorMessage"] = "Talep bulunamadı veya zaten işlem görmüş.";
                        return RedirectToAction(nameof(PendingRequests));
                    }

                    ViewBag.RequestType = "creation";
                    return View("RequestDetails", request);
                }
                else if (type == "deletion")
                {
                    var request = await _requestService.GetUserDeletionRequestAsync(id);
                    if (request == null || request.StatusId != 1)
                    {
                        TempData["ErrorMessage"] = "Talep bulunamadı veya zaten işlem görmüş.";
                        return RedirectToAction(nameof(PendingRequests));
                    }

                    ViewBag.RequestType = "deletion";
                    return View("RequestDetails", request);
                }
                else if (type == "attribute")
                {
                    var request = await _requestService.GetAttributeChangeRequestAsync(id);
                    if (request == null || request.StatusId != 1)
                    {
                        TempData["ErrorMessage"] = "Talep bulunamadı veya zaten işlem görmüş.";
                        return RedirectToAction(nameof(PendingRequests));
                    }

                    ViewBag.RequestType = "attribute";
                    return View("RequestDetails", request);
                }
                else if (type == "password")
                {
                    var request = await _requestService.GetPasswordResetRequestAsync(id);
                    if (request == null || request.StatusId != 1)
                    {
                        TempData["ErrorMessage"] = "Talep bulunamadı veya zaten işlem görmüş.";
                        return RedirectToAction(nameof(PendingRequests));
                    }

                    ViewBag.RequestType = "password";
                    return View("RequestDetails", request);
                }
                else if (type == "group")
                {
                    var request = await _context.GroupMembershipRequests
                        .Include(r => r.RequestedBy)
                        .Include(r => r.ApprovedBy)
                        .Include(r => r.Status)
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (request == null || request.StatusId != 1)
                    {
                        TempData["ErrorMessage"] = "Talep bulunamadı veya zaten işlem görmüş.";
                        return RedirectToAction(nameof(PendingRequests));
                    }

                    ViewBag.RequestType = "group";
                    return View("RequestDetails", request);
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request details");
                TempData["ErrorMessage"] = "Talep detayları yüklenirken hata oluştu.";
                return RedirectToAction(nameof(PendingRequests));
            }
        }

        // Talep onaylama
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int requestId, string requestType)
        {
            _logger.LogInformation("ApproveRequest called for ID: {Id}, Type: {Type} by {User}",
                requestId, requestType, User.Identity?.Name);

            try
            {
                bool result = false;

                if (requestType == "password")
                {
                    result = await _passwordService.ApprovePasswordResetRequestAsync(requestId, User.Identity.Name);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Şifre sıfırlama talebi onaylandı. Yeni şifre kullanıcıya e-posta ile gönderildi.";
                    }
                }
                else if (requestType == "group")
                {
                    result = await _groupService.ApproveGroupMembershipRequestAsync(requestId, User.Identity.Name);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Grup üyelik talebi onaylandı ve işlem başarıyla uygulandı.";
                    }
                }
                else
                {
                    result = await _requestService.ApproveRequestAsync(requestId, requestType, User.Identity.Name);
                    if (result)
                    {
                        TempData["SuccessMessage"] = requestType switch
                        {
                            "creation" => "Kullanıcı başarıyla oluşturuldu.",
                            "deletion" => "Kullanıcı başarıyla devre dışı bırakıldı.",
                            "attribute" => "Attribute değişikliği başarıyla uygulandı.",
                            _ => "Talep başarıyla onaylandı."
                        };
                    }
                }

                if (!result)
                {
                    TempData["ErrorMessage"] = "Talep onaylanırken bir hata oluştu.";
                    _logger.LogWarning("Request {Id} approval failed", requestId);
                }
                else
                {
                    _logger.LogInformation("Request {Id} approved successfully", requestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {RequestId}", requestId);
                TempData["ErrorMessage"] = $"Talep onaylanırken hata: {ex.Message}";
            }

            return RedirectToAction(nameof(PendingRequests));
        }

        // Talep reddetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(ApprovalViewModel model)
        {
            _logger.LogInformation("RejectRequest called for ID: {Id}, Type: {Type} by {User}",
                model.RequestId, model.RequestType, User.Identity?.Name);

            if (string.IsNullOrWhiteSpace(model.RejectionReason))
            {
                TempData["ErrorMessage"] = "Red gerekçesi belirtmelisiniz.";
                return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId, type = model.RequestType });
            }

            try
            {
                bool result = false;

                if (model.RequestType == "password")
                {
                    result = await _passwordService.RejectPasswordResetRequestAsync(
                        model.RequestId,
                        model.RejectionReason,
                        User.Identity.Name);
                }
                else if (model.RequestType == "group")
                {
                    result = await _groupService.RejectGroupMembershipRequestAsync(
                        model.RequestId,
                        model.RejectionReason,
                        User.Identity.Name);
                }
                else
                {
                    result = await _requestService.RejectRequestAsync(
                        model.RequestId,
                        model.RequestType,
                        model.RejectionReason,
                        User.Identity.Name);
                }

                if (result)
                {
                    TempData["SuccessMessage"] = "Talep reddedildi ve talep sahibine bildirim gönderildi.";
                    _logger.LogInformation("Request {Id} rejected successfully", model.RequestId);
                }
                else
                {
                    TempData["ErrorMessage"] = "Talep reddedilirken bir hata oluştu.";
                    _logger.LogWarning("Request {Id} rejection failed", model.RequestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {RequestId}", model.RequestId);
                TempData["ErrorMessage"] = $"Talep reddedilirken hata: {ex.Message}";
            }

            return RedirectToAction(nameof(PendingRequests));
        }
    }
}