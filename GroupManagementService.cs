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
                        Email = await _adService.GetUserEmailAsync(requestedByUsername) ?? $"{requestedByUsername}@param.com.tr",
                        CreatedDate = DateTime.Now,
                        IsActive = true
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
                    StatusId = 1, // Beklemede
                    RequestedDate = DateTime.Now
                };

                _context.GroupMembershipRequests.Add(request);
                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = $"Grup üyelik talebi oluşturuldu - {model.ActionType}",
                    EntityType = "GroupMembershipRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}, Grup: {model.GroupName}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                try
                {
                    await _emailService.SendNewRequestNotificationAsync("group", requestNumber, request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email notification failed for request {RequestId}", request.Id);
                }

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
                    .FirstOrDefaultAsync(r => r.Id == requestId && r.StatusId == 1);

                if (request == null)
                {
                    _logger.LogWarning($"Group membership request {requestId} not found or already processed");
                    return false;
                }

                var approvedBy = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == approvedByUsername);

                if (approvedBy == null)
                {
                    approvedBy = new SystemUser
                    {
                        Username = approvedByUsername,
                        DisplayName = approvedByUsername,
                        Email = await _adService.GetUserEmailAsync(approvedByUsername) ?? $"{approvedByUsername}@param.com.tr",
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };
                    _context.SystemUsers.Add(approvedBy);
                    await _context.SaveChangesAsync();
                }

                // AD'de grup işlemini gerçekleştir
                bool adResult = false;
                if (request.ActionType == "Add")
                {
                    adResult = await _adService.AddUserToGroupAsync(request.Username, request.GroupName);
                }
                else if (request.ActionType == "Remove")
                {
                    adResult = await _adService.RemoveUserFromGroupAsync(request.Username, request.GroupName);
                }

                if (!adResult)
                {
                    throw new Exception($"Active Directory grup işlemi başarısız oldu");
                }

                request.StatusId = 2; // Onaylandı
                request.ApprovedById = approvedBy.Id;
                request.ApprovedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = approvedBy.Id,
                    Action = $"Grup üyelik talebi onaylandı - {request.ActionType}",
                    EntityType = "GroupMembershipRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}, Grup: {request.GroupName}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"Group membership request approved for user {request.Username}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving group membership request");
                return false;
            }
        }

        public async Task<bool> RejectGroupMembershipRequestAsync(int requestId, string rejectionReason, string rejectedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.GroupMembershipRequests
                    .FirstOrDefaultAsync(r => r.Id == requestId && r.StatusId == 1);

                if (request == null)
                {
                    _logger.LogWarning($"Group membership request {requestId} not found or already processed");
                    return false;
                }

                var rejectedBy = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == rejectedByUsername);

                if (rejectedBy == null)
                {
                    rejectedBy = new SystemUser
                    {
                        Username = rejectedByUsername,
                        DisplayName = rejectedByUsername,
                        Email = await _adService.GetUserEmailAsync(rejectedByUsername) ?? $"{rejectedByUsername}@param.com.tr",
                        CreatedDate = DateTime.Now,
                        IsActive = true
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
                    Action = $"Grup üyelik talebi reddedildi - {request.ActionType}",
                    EntityType = "GroupMembershipRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}, Grup: {request.GroupName}, Sebep: {rejectionReason}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"Group membership request rejected for user {request.Username}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting group membership request");
                return false;
            }
        }

        public async Task<List<RequestListViewModel>> GetMyGroupRequestsAsync(string username)
        {
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new List<RequestListViewModel>();

            var groupRequests = await _context.GroupMembershipRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == user.Id)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "group",
                    DisplayName = r.Username + " - " + r.GroupName,
                    Username = r.Username,
                    Email = null,
                    Company = r.ActionType == "Add" ? "Gruba Ekle" : "Gruptan Çıkar",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .OrderByDescending(r => r.RequestedDate)
                .ToListAsync();

            return groupRequests;
        }

        public async Task<List<string>> SearchGroupsAsync(string searchTerm)
        {
            try
            {
                var groups = await _adService.SearchGroupsAsync(searchTerm);
                return groups.Select(g => g.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching groups with term: {SearchTerm}", searchTerm);
                return new List<string>();
            }
        }

        public async Task<bool> AddUserToGroupAsync(string username, string groupName)
        {
            try
            {
                // Input validation
                if (!InputValidationHelper.IsValidUsername(username))
                {
                    _logger.LogWarning($"Invalid username format: {username}");
                    return false;
                }

                if (!InputValidationHelper.IsValidGroupName(groupName))
                {
                    _logger.LogWarning($"Invalid group name format: {groupName}");
                    return false;
                }

                var result = await _adService.AddUserToGroupAsync(username, groupName);

                if (result)
                {
                    _logger.LogInformation($"User {username} successfully added to group {groupName}");
                }
                else
                {
                    _logger.LogWarning($"Failed to add user {username} to group {groupName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", username, groupName);
                return false;
            }
        }

        public async Task<bool> RemoveUserFromGroupAsync(string username, string groupName)
        {
            try
            {
                // Input validation
                if (!InputValidationHelper.IsValidUsername(username))
                {
                    _logger.LogWarning($"Invalid username format: {username}");
                    return false;
                }

                if (!InputValidationHelper.IsValidGroupName(groupName))
                {
                    _logger.LogWarning($"Invalid group name format: {groupName}");
                    return false;
                }

                var result = await _adService.RemoveUserFromGroupAsync(username, groupName);

                if (result)
                {
                    _logger.LogInformation($"User {username} successfully removed from group {groupName}");
                }
                else
                {
                    _logger.LogWarning($"Failed to remove user {username} from group {groupName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", username, groupName);
                return false;
            }
        }

        public async Task<List<string>> GetUserGroupsAsync(string username)
        {
            try
            {
                // Input validation
                if (!InputValidationHelper.IsValidUsername(username))
                {
                    _logger.LogWarning($"Invalid username format: {username}");
                    return new List<string>();
                }

                var groups = await _adService.GetUserGroupsAsync(username);

                _logger.LogInformation($"Retrieved {groups.Count} groups for user {username}");
                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for user {Username}", username);
                return new List<string>();
            }
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var prefix = "GRP";
            var date = DateTime.Now.ToString("yyyyMMdd");

            var lastRequest = await _context.GroupMembershipRequests
                .Where(r => r.RequestNumber.StartsWith($"{prefix}{date}"))
                .OrderBy(r => r.RequestNumber)
                .LastOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var lastSequence = lastRequest.RequestNumber.Substring(11); // GRPyyyyMMdd sonrası
                if (int.TryParse(lastSequence, out int parsed))
                {
                    sequence = parsed + 1;
                }
            }

            return $"{prefix}{date}{sequence:D3}";
        }
    }
}