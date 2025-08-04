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
using ADUserManagement.Constants;
using System.Diagnostics;

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
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var pendingRequests = await _requestService.GetPendingRequestsAsync();
                stopwatch.Stop();

                _logger.LogInformation("Admin panel accessed: PendingRequests by {User} - {RequestCount} requests loaded in {ElapsedMs}ms",
                    User.Identity?.Name, pendingRequests.Count, stopwatch.ElapsedMilliseconds);

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

                TempData["ErrorMessage"] = "Geçersiz talep tipi.";
                return RedirectToAction(nameof(PendingRequests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading request details for ID: {Id}, Type: {Type}", id, type);
                TempData["ErrorMessage"] = "Talep detayları yüklenirken hata oluştu.";
                return RedirectToAction(nameof(PendingRequests));
            }
        }

        // Talep onaylama
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id, string type)
        {
            try
            {
                bool result = false;

                if (type == "password")
                {
                    result = await _passwordService.ApprovePasswordResetRequestAsync(id, User.Identity?.Name);
                }
                else
                {
                    result = await _requestService.ApproveRequestAsync(id, type, User.Identity?.Name);
                }

                if (result)
                {
                    TempData["SuccessMessage"] = "Talep başarıyla onaylandı.";
                    _logger.LogInformation(LogMessages.RequestApproved, type, id, User.Identity?.Name);
                }
                else
                {
                    TempData["ErrorMessage"] = "Talep onaylanırken hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {Id} of type {Type}", id, type);
                TempData["ErrorMessage"] = "Talep onaylanırken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(PendingRequests));
        }

        // Talep reddetme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id, string type, string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Red sebebi zorunludur.";
                return RedirectToAction(nameof(RequestDetails), new { id, type });
            }

            try
            {
                bool result = false;

                if (type == "password")
                {
                    result = await _passwordService.RejectPasswordResetRequestAsync(id, rejectionReason, User.Identity?.Name);
                }
                else
                {
                    result = await _requestService.RejectRequestAsync(id, type, rejectionReason, User.Identity?.Name);
                }

                if (result)
                {
                    TempData["SuccessMessage"] = "Talep başarıyla reddedildi.";
                    _logger.LogInformation(LogMessages.RequestRejected, type, id, User.Identity?.Name, rejectionReason);
                }
                else
                {
                    TempData["ErrorMessage"] = "Talep reddedilirken hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {Id} of type {Type}", id, type);
                TempData["ErrorMessage"] = "Talep reddedilirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(PendingRequests));
        }
    }
}