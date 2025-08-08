using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ADUserManagement.Data;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Controllers
{
    [Authorize(Roles = "HelpDesk,SysNet")]
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveDirectoryService _adService;
        private readonly IGroupManagementService _groupService;
        private readonly ILogger<GroupController> _logger;

        public GroupController(
            ApplicationDbContext context,
            IActiveDirectoryService adService,
            IGroupManagementService groupService,
            ILogger<GroupController> logger)
        {
            _context = context;
            _adService = adService;
            _groupService = groupService;
            _logger = logger;
        }

        // Grup üyelik talebi formu
        [HttpGet]
        public IActionResult MembershipRequest()
        {
            var model = new GroupMembershipViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MembershipRequest(GroupMembershipViewModel model)
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

                // Grubun var olduğunu kontrol et
                var groupDetails = await _adService.GetGroupDetailsAsync(model.GroupName);
                if (groupDetails == null)
                {
                    ModelState.AddModelError("GroupName", "Grup bulunamadı.");
                    return View(model);
                }

                // Built-in grup kontrolü
                if (groupDetails.IsBuiltIn)
                {
                    ModelState.AddModelError("GroupName", "Built-in gruplara üyelik işlemi yapılamaz.");
                    return View(model);
                }

                // Mevcut üyelik durumunu kontrol et
                var isCurrentlyMember = await _adService.IsUserInSpecificGroupAsync(model.Username, model.GroupName);

                if (model.ActionType == "Add" && isCurrentlyMember)
                {
                    ModelState.AddModelError(string.Empty, "Kullanıcı zaten bu grubun üyesidir.");
                    return View(model);
                }

                if (model.ActionType == "Remove" && !isCurrentlyMember)
                {
                    ModelState.AddModelError(string.Empty, "Kullanıcı zaten bu grubun üyesi değildir.");
                    return View(model);
                }

                // Talebi oluştur
                var requestId = await _groupService.CreateGroupMembershipRequestAsync(model, User.Identity.Name);

                _logger.LogInformation($"Group membership request created with ID: {requestId}");

                TempData["SuccessMessage"] = "Grup üyelik talebiniz başarıyla oluşturuldu.";
                return RedirectToAction("MyRequests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group membership request");
                ModelState.AddModelError(string.Empty, $"Talep oluşturulurken bir hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // AJAX - Grup arama
        [HttpGet]
        public async Task<IActionResult> SearchGroups(string term, string username = null)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new { results = new object[0] });
            }

            try
            {
                var groups = await _adService.SearchGroupsAsync(term, username);
                var results = groups.Select(g => new
                {
                    id = g.Name,
                    text = $"{g.DisplayName} ({g.Name})",
                    description = g.Description,
                    groupType = g.GroupType,
                    memberCount = g.MemberCount,
                    isUserMember = g.IsUserMember
                });

                return Json(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching groups");
                return Json(new { results = new object[0] });
            }
        }

        // AJAX - Kullanıcının gruplarını getir
        [HttpGet]
        public async Task<IActionResult> GetUserGroups(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Json(new { success = false, message = "Kullanıcı adı boş olamaz" });
                }

                var groups = await _adService.GetUserGroupsAsync(username);

                return Json(new
                {
                    success = true,
                    groups = groups.Select(g => new { name = g, displayName = g })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                return Json(new { success = false, message = "Hata oluştu" });
            }
        }

        // AJAX - Grup detaylarını getir
        [HttpGet]
        public async Task<IActionResult> GetGroupDetails(string groupName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    return Json(new { success = false, message = "Grup adı boş olamaz" });
                }

                var groupDetails = await _adService.GetGroupDetailsAsync(groupName);
                if (groupDetails == null)
                {
                    return Json(new { success = false, message = "Grup bulunamadı" });
                }

                return Json(new
                {
                    success = true,
                    name = groupDetails.Name,
                    displayName = groupDetails.DisplayName,
                    description = groupDetails.Description,
                    groupType = groupDetails.GroupType,
                    scope = groupDetails.Scope,
                    memberCount = groupDetails.MemberCount,
                    isBuiltIn = groupDetails.IsBuiltIn
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group details");
                return Json(new { success = false, message = "Hata oluştu" });
            }
        }

        // Grup talepleri listesi
        [HttpGet]
        public async Task<IActionResult> MyRequests()
        {
            try
            {
                var requests = await _groupService.GetMyGroupRequestsAsync(User.Identity.Name);
                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group requests");
                TempData["ErrorMessage"] = "Talepler yüklenirken bir hata oluştu.";
                return View(new List<RequestListViewModel>());
            }
        }
    }
}