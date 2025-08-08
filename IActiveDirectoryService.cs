using System.Collections.Generic;
using System.Threading.Tasks;
using ADUserManagement.Models.Dto;

namespace ADUserManagement.Services.Interfaces
{
    public interface IActiveDirectoryService
    {
        Task<bool> CreateUserAsync(ADUserDto user);
        Task<bool> DisableUserAsync(string username);
        Task<bool> UserExistsAsync(string username);
        Task<bool> IsUserInGroupAsync(string username, string groupName);
        Task<bool> IsUserInAnyAuthorizedGroupAsync(string username); // ✅ EKLENEN METHOD
        Task<string> GetUserEmailAsync(string username);
        Task<bool> ValidateCredentialsAsync(string username, string password);

        // Yeni metodlar
        Task<List<ADUserSearchResultDto>> SearchUsersAsync(string searchTerm);
        Task<List<ADGroupSearchResultDto>> SearchGroupsAsync(string searchTerm, string username = null);
        Task<ADUserDetailsDto> GetUserDetailsAsync(string username);
        Task<ADGroupDto> GetGroupDetailsAsync(string groupName);
        Task<bool> UpdateUserAttributesAsync(string username, Dictionary<string, string> attributes);
        Task<bool> AddUserToGroupAsync(string username, string groupName);
        Task<bool> RemoveUserFromGroupAsync(string username, string groupName);
        Task<List<string>> GetUserGroupsAsync(string username);
        Task<bool> IsUserInSpecificGroupAsync(string username, string groupName);
    }
}