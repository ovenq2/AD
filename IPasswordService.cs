using System.Threading.Tasks;
using ADUserManagement.Models.ViewModels;

namespace ADUserManagement.Services.Interfaces
{
    public interface IPasswordService
    {
        Task<int> CreatePasswordResetRequestAsync(PasswordResetViewModel model, string requestedByUsername);
        Task<bool> ApprovePasswordResetRequestAsync(int requestId, string approvedByUsername);
        Task<bool> RejectPasswordResetRequestAsync(int requestId, string rejectionReason, string rejectedByUsername);

        // AD Password İşlemleri
        Task<bool> ResetUserPasswordAsync(string username, string newPassword);
        Task<bool> ForcePasswordChangeAtNextLogonAsync(string username);
    }
}