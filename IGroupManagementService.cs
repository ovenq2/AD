using System.Collections.Generic;
using System.Threading.Tasks;
using ADUserManagement.Models.ViewModels;

namespace ADUserManagement.Services.Interfaces
{
    public interface IGroupManagementService
    {
        Task<int> CreateGroupMembershipRequestAsync(GroupMembershipViewModel model, string requestedByUsername);
        Task<bool> ApproveGroupMembershipRequestAsync(int requestId, string approvedByUsername);
        Task<bool> RejectGroupMembershipRequestAsync(int requestId, string rejectionReason, string rejectedByUsername);
        Task<List<RequestListViewModel>> GetMyGroupRequestsAsync(string username);

        // AD Group İşlemleri
        Task<List<string>> SearchGroupsAsync(string searchTerm);
        Task<bool> AddUserToGroupAsync(string username, string groupName);
        Task<bool> RemoveUserFromGroupAsync(string username, string groupName);
        Task<List<string>> GetUserGroupsAsync(string username);
    }
}