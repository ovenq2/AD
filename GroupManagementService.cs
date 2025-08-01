using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.Domain;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Services
{
    public class GroupManagementService : IGroupManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveDirectoryService _adService;
        private readonly IEmailService _emailService;
        private readonly ILogger<GroupManagementService> _logger;

        public GroupManagementService(
            ApplicationDbContext context,
            IActiveDirectoryService adService,
            IEmailService emailService,
            ILogger<GroupManagementService> logger)
        {
            _context = context;
            _adService = adService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<int> CreateGroupMembershipRequestAsync(GroupMembershipViewModel model, string requestedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var requestedBy = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == requestedByUsername);

                if (requestedBy == null)
                {
                    requestedBy = new SystemUser
                    {
                        Username = requestedByUsername,
                        DisplayName = requestedByUsername,
                        Email = await _adService.GetUserEmailAsync(requestedByUsername) ?? $"{requestedByUsername}@param.com.tr"
                    };
                    _context.SystemUsers.Add(requestedBy);
                    await _context.SaveChangesAsync();
                }

                var requestNumber = await GenerateRequestNumberAsync();

                var request = new GroupMembershipRequest
                {
                    RequestNumber = requestNumber,
                    Username = model.Username,
                    GroupName = model.GroupName,
                    ActionType = model.ActionType,
                    Reason = model.Reason,
                    RequestedById = requestedBy.Id,
                    StatusId = 1 // Beklemede
                };

                _context.GroupMembershipRequests.Add(request);
                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Grup üyelik talebi oluşturuldu",
                    EntityType = "GroupMembershipRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}, Grup: {model.GroupName}, İşlem: {model.ActionType}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _emailService.SendNewRequestNotificationAsync("group", requestNumber, request.Id);

                return request.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating group membership request");
                throw;
            }
        }

        public async Task<bool> ApproveGroupMembershipRequestAsync(int requestId, string approvedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.GroupMembershipRequests
                    .Include(r => r.RequestedBy)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null || request.StatusId != 1)
                    return false;

                var approvedBy = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == approvedByUsername);

                if (approvedBy == null)
                {
                    approvedBy = new SystemUser
                    {
                        Username = approvedByUsername,
                        DisplayName = approvedByUsername,
                        Email = await _adService.GetUserEmailAsync(approvedByUsername) ?? $"{approvedByUsername}@param.com.tr"
                    };
                    _context.SystemUsers.Add(approvedBy);
                    await _context.SaveChangesAsync();
                }

                bool adResult = false;
                if (request.ActionType == "Add")
                {
                    adResult = await AddUserToGroupAsync(request.Username, request.GroupName);
                }
                else
                {
                    adResult = await RemoveUserFromGroupAsync(request.Username, request.GroupName);
                }

                if (!adResult)
                {
                    _logger.LogError($"Failed to {request.ActionType} user {request.Username} to/from group {request.GroupName}");
                    return false;
                }

                request.StatusId = 2; // Onaylandı
                request.ApprovedById = approvedBy.Id;
                request.ApprovedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = approvedBy.Id,
                    Action = "Grup üyelik talebi onaylandı",
                    EntityType = "GroupMembershipRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}, Grup: {request.GroupName}, İşlem: {request.ActionType}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error approving group membership request {requestId}");
                throw;
            }
        }

        public async Task<bool> RejectGroupMembershipRequestAsync(int requestId, string rejectionReason, string rejectedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.GroupMembershipRequests
                    .Include(r => r.RequestedBy)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null || request.StatusId != 1)
                    return false;

                var rejectedBy = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == rejectedByUsername);

                if (rejectedBy == null)
                {
                    rejectedBy = new SystemUser
                    {
                        Username = rejectedByUsername,
                        DisplayName = rejectedByUsername,
                        Email = await _adService.GetUserEmailAsync(rejectedByUsername) ?? $"{rejectedByUsername}@param.com.tr"
                    };
                    _context.SystemUsers.Add(rejectedBy);
                    await _context.SaveChangesAsync();
                }

                request.StatusId = 3; // Reddedildi
                request.ApprovedById = rejectedBy.Id;
                request.ApprovedDate = DateTime.Now;
                request.RejectionReason = rejectionReason;
                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = rejectedBy.Id,
                    Action = "Grup üyelik talebi reddedildi",
                    EntityType = "GroupMembershipRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}, Grup: {request.GroupName}, Sebep: {rejectionReason}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error rejecting group membership request {requestId}");
                throw;
            }
        }

        public async Task<List<string>> SearchGroupsAsync(string searchTerm)
        {
            // Bu method AD'den grup arayacak - şimdilik örnek implementasyon
            return new List<string>
            {
                "Domain Admins",
                "Domain Users",
                "HelpDesk",
                "SysNet",
                "Developers",
                "Managers"
            }.Where(g => g.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<bool> AddUserToGroupAsync(string username, string groupName)
        {
            // AD'de grup işlemleri için implementasyon gerekli
            _logger.LogInformation($"Adding user {username} to group {groupName}");
            return true; // Şimdilik true döndürüyor
        }

        public async Task<bool> RemoveUserFromGroupAsync(string username, string groupName)
        {
            // AD'de grup işlemleri için implementasyon gerekli
            _logger.LogInformation($"Removing user {username} from group {groupName}");
            return true; // Şimdilik true döndürüyor
        }

        public async Task<List<string>> GetUserGroupsAsync(string username)
        {
            // AD'den kullanıcının gruplarını getir
            return new List<string>(); // Şimdilik boş liste
        }

        public async Task<List<RequestListViewModel>> GetMyGroupRequestsAsync(string username)
        {
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new List<RequestListViewModel>();

            return await _context.GroupMembershipRequests
                .Include(r => r.Status)
                .Where(r => r.RequestedById == user.Id)
                .OrderByDescending(r => r.RequestedDate)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "group",
                    DisplayName = r.Username + " - " + r.GroupName,
                    Username = r.Username,
                    Email = "",
                    Company = r.ActionType == "Add" ? "Gruba Ekle" : "Gruptan Çıkar",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var year = DateTime.Now.Year.ToString();
            var lastRequest = await _context.GroupMembershipRequests
                .Where(r => r.RequestNumber.StartsWith($"GRP-{year}"))
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
            {
                return $"GRP-{year}-00001";
            }

            var parts = lastRequest.RequestNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastCounter))
            {
                return $"GRP-{year}-{(lastCounter + 1):D5}";
            }

            return $"GRP-{year}-00001";
        }
    }
}