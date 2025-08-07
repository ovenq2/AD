using System.Collections.Generic;
using System.Threading.Tasks;
using ADUserManagement.Models.Domain;
using ADUserManagement.Models.ViewModels;

namespace ADUserManagement.Services.Interfaces
{
    public interface IRequestService
    {
        Task<int> CreateUserCreationRequestAsync(UserCreationRequestViewModel model, string requestedByUsername);
        Task<int> CreateUserDeletionRequestAsync(UserDeletionRequestViewModel model, string requestedByUsername);
        Task<int> CreateAttributeChangeRequestAsync(UserAttributeChangeViewModel model, string requestedByUsername);
        Task<bool> ApproveRequestAsync(int requestId, string requestType, string approvedByUsername);
        Task<bool> RejectRequestAsync(int requestId, string requestType, string rejectionReason, string rejectedByUsername);
        Task<List<RequestListViewModel>> GetMyRequestsAsync(string username);
        Task<List<RequestListViewModel>> GetPendingRequestsAsync();
        Task<UserCreationRequest> GetUserCreationRequestAsync(int id);
        Task<UserDeletionRequest> GetUserDeletionRequestAsync(int id);
        Task<UserAttributeChangeRequest> GetAttributeChangeRequestAsync(int id);
        Task<PasswordResetRequest> GetPasswordResetRequestAsync(int id);
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}