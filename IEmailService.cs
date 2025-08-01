using System.Threading.Tasks;
using ADUserManagement.Models.Dto;

namespace ADUserManagement.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailDto email);
        Task SendNewRequestNotificationAsync(string requestType, string requestNumber, int requestId);
        Task SendApprovalNotificationAsync(string toEmail, string username, bool isApproved, string reason = null);
        Task SendNewUserCredentialsAsync(string toEmail, string username, string password);
        Task SendAttributeChangeNotificationAsync(string toEmail, string username, string attributeName, string newValue, bool isApproved);
    }
}