using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ADUserManagement.Models.Dto;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Services
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ActiveDirectoryService> _logger;
        private readonly string _domain;
        private readonly string _serviceAccount;
        private readonly string _servicePassword;

        public ActiveDirectoryService(IConfiguration configuration, ILogger<ActiveDirectoryService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _domain = "paramtech.local";
            _serviceAccount = configuration["ADServiceAccount"];
            _servicePassword = configuration["ADServicePassword"];
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Domain\Username formatını kontrol et
                    var accountName = username;
                    var domainToUse = _domain;

                    if (username.Contains("\\"))
                    {
                        var parts = username.Split('\\');
                        domainToUse = parts[0];
                        accountName = parts[1];
                    }

                    using (var context = new PrincipalContext(ContextType.Domain, domainToUse))
                    {
                        // AD'ye karşı kullanıcı adı ve şifre doğrula
                        var isValid = context.ValidateCredentials(accountName, password);

                        if (isValid)
                        {
                            _logger.LogInformation($"User {accountName} authenticated successfully against AD");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed authentication attempt for user {accountName}");
                        }

                        return isValid;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error validating credentials for user {username}");
                    return false;
                }
            });
        }

        public async Task<bool> CreateUserAsync(ADUserDto user)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var newUser = new UserPrincipal(context))
                        {
                            newUser.SamAccountName = user.Username;
                            newUser.UserPrincipalName = $"{user.Username}@{_domain}";
                            newUser.GivenName = user.FirstName;
                            newUser.Surname = user.LastName;
                            newUser.DisplayName = $"{user.FirstName} {user.LastName}";
                            newUser.EmailAddress = user.Email;
                            newUser.VoiceTelephoneNumber = user.Phone;
                            newUser.Description = user.Description;
                            newUser.Enabled = true;
                            newUser.PasswordNeverExpires = false;
                            newUser.SetPassword(user.Password);
                            newUser.ExpirePasswordNow();

                            newUser.Save();

                            // Kullanıcıyı belirtilen OU'ya taşı
                            using (var de = newUser.GetUnderlyingObject() as DirectoryEntry)
                            {
                                using (var ouEntry = new DirectoryEntry($"LDAP://{user.OUPath}", _serviceAccount, _servicePassword))
                                {
                                    de.MoveTo(ouEntry);
                                }
                            }

                            _logger.LogInformation($"User {user.Username} created successfully in AD");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating user {user.Username} in AD");
                    return false;
                }
            });
        }

        public async Task<bool> DisableUserAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                user.Enabled = false;
                                user.Save();
                                _logger.LogInformation($"User {username} disabled successfully");
                                return true;
                            }
                            else
                            {
                                _logger.LogWarning($"User {username} not found in AD");
                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error disabling user {username}");
                    return false;
                }
            });
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            return user != null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error checking if user {username} exists");
                    return false;
                }
            });
        }

        public async Task<bool> IsUserInGroupAsync(string username, string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                                {
                                    if (group != null)
                                    {
                                        return user.IsMemberOf(group);
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error checking if user {username} is in group {groupName}");
                    return false;
                }
            });
        }

        public async Task<string> GetUserEmailAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            return user?.EmailAddress;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting email for user {username}");
                    return null;
                }
            });
        }

        public async Task<List<ADUserSearchResultDto>> SearchUsersAsync(string searchTerm)
        {
            return await Task.Run(() =>
            {
                var results = new List<ADUserSearchResultDto>();

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var searcher = new PrincipalSearcher())
                        {
                            var userPrincipal = new UserPrincipal(context);
                            userPrincipal.DisplayName = searchTerm + "*"; // Wildcard search

                            searcher.QueryFilter = userPrincipal;
                            var searchResults = searcher.FindAll();

                            foreach (UserPrincipal user in searchResults)
                            {
                                if (user.Enabled == true) // Sadece aktif kullanıcılar
                                {
                                    results.Add(new ADUserSearchResultDto
                                    {
                                        Username = user.SamAccountName,
                                        DisplayName = user.DisplayName ?? $"{user.GivenName} {user.Surname}",
                                        Email = user.EmailAddress,
                                        Department = user.GetProperty("department"),
                                        Title = user.GetProperty("title"),
                                        IsEnabled = user.Enabled ?? false
                                    });
                                }
                            }
                        }

                        // İsim ile de ara
                        if (results.Count < 10) // Maksimum 10 sonuç için
                        {
                            using (var searcher = new PrincipalSearcher())
                            {
                                var userPrincipal = new UserPrincipal(context);
                                userPrincipal.GivenName = searchTerm + "*";

                                searcher.QueryFilter = userPrincipal;
                                var searchResults = searcher.FindAll();

                                foreach (UserPrincipal user in searchResults)
                                {
                                    if (user.Enabled == true && !results.Any(r => r.Username == user.SamAccountName))
                                    {
                                        results.Add(new ADUserSearchResultDto
                                        {
                                            Username = user.SamAccountName,
                                            DisplayName = user.DisplayName ?? $"{user.GivenName} {user.Surname}",
                                            Email = user.EmailAddress,
                                            Department = user.GetProperty("department"),
                                            Title = user.GetProperty("title"),
                                            IsEnabled = user.Enabled ?? false
                                        });
                                    }
                                }
                            }
                        }
                    }

                    return results.Take(10).ToList(); // En fazla 10 sonuç döndür
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error searching users with term: {searchTerm}");
                    return results;
                }
            });
        }

        public async Task<ADUserDetailsDto> GetUserDetailsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry;

                                return new ADUserDetailsDto
                                {
                                    Username = user.SamAccountName,
                                    FirstName = user.GivenName,
                                    LastName = user.Surname,
                                    DisplayName = user.DisplayName,
                                    Email = user.EmailAddress,
                                    Phone = user.VoiceTelephoneNumber,
                                    Mobile = directoryEntry?.Properties["mobile"]?.Value?.ToString(),
                                    Department = directoryEntry?.Properties["department"]?.Value?.ToString(),
                                    Title = directoryEntry?.Properties["title"]?.Value?.ToString(),
                                    Office = directoryEntry?.Properties["physicalDeliveryOfficeName"]?.Value?.ToString(),
                                    Manager = directoryEntry?.Properties["manager"]?.Value?.ToString(),
                                    Description = user.Description,
                                    IsEnabled = user.Enabled ?? false,
                                    LastLogon = user.LastLogon,
                                    Created = directoryEntry?.Properties["whenCreated"]?.Value as DateTime?
                                };
                            }
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting user details for: {username}");
                    return null;
                }
            });
        }

        public async Task<List<ADGroupSearchResultDto>> SearchGroupsAsync(string searchTerm, string username = null)
        {
            return await Task.Run(() =>
            {
                var results = new List<ADGroupSearchResultDto>();

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var searcher = new PrincipalSearcher())
                        {
                            var groupPrincipal = new GroupPrincipal(context);
                            groupPrincipal.Name = "*" + searchTerm + "*";

                            searcher.QueryFilter = groupPrincipal;
                            var searchResults = searcher.FindAll();

                            foreach (GroupPrincipal group in searchResults)
                            {
                                try
                                {
                                    // Built-in grupları filtrele
                                    if (IsBuiltInGroup(group.Name))
                                        continue;

                                    var directoryEntry = group.GetUnderlyingObject() as DirectoryEntry;
                                    var groupType = GetGroupType(directoryEntry);

                                    var result = new ADGroupSearchResultDto
                                    {
                                        Name = group.Name,
                                        DisplayName = group.DisplayName ?? group.Name,
                                        Description = group.Description ?? "",
                                        GroupType = groupType,
                                        MemberCount = group.Members?.Count() ?? 0
                                    };

                                    // Eğer username verilmişse, kullanıcının bu grupta olup olmadığını kontrol et
                                    if (!string.IsNullOrEmpty(username))
                                    {
                                        result.IsUserMember = IsUserInGroup(username, group.Name, context);
                                    }

                                    results.Add(result);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Error processing group {group.Name}");
                                }
                            }
                        }
                    }

                    return results.Take(20).ToList(); // En fazla 20 sonuç
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error searching groups with term: {searchTerm}");
                    return results;
                }
            });
        }

        public async Task<ADGroupDto> GetGroupDetailsAsync(string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                        {
                            if (group != null)
                            {
                                var directoryEntry = group.GetUnderlyingObject() as DirectoryEntry;

                                return new ADGroupDto
                                {
                                    Name = group.Name,
                                    DisplayName = group.DisplayName ?? group.Name,
                                    Description = group.Description ?? "",
                                    GroupType = GetGroupType(directoryEntry),
                                    Scope = GetGroupScope(directoryEntry),
                                    MemberCount = group.Members?.Count() ?? 0,
                                    IsBuiltIn = IsBuiltInGroup(group.Name)
                                };
                            }
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting group details for: {groupName}");
                    return null;
                }
            });
        }

        public async Task<bool> AddUserToGroupAsync(string username, string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                        {
                            if (user != null && group != null)
                            {
                                if (!user.IsMemberOf(group))
                                {
                                    group.Members.Add(user);
                                    group.Save();
                                    _logger.LogInformation($"User {username} added to group {groupName}");
                                    return true;
                                }
                                else
                                {
                                    _logger.LogInformation($"User {username} is already member of group {groupName}");
                                    return true; // Zaten üye
                                }
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error adding user {username} to group {groupName}");
                    return false;
                }
            });
        }

        public async Task<bool> RemoveUserFromGroupAsync(string username, string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                        {
                            if (user != null && group != null)
                            {
                                if (user.IsMemberOf(group))
                                {
                                    group.Members.Remove(user);
                                    group.Save();
                                    _logger.LogInformation($"User {username} removed from group {groupName}");
                                    return true;
                                }
                                else
                                {
                                    _logger.LogInformation($"User {username} is not member of group {groupName}");
                                    return true; // Zaten üye değil
                                }
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error removing user {username} from group {groupName}");
                    return false;
                }
            });
        }

        public async Task<List<string>> GetUserGroupsAsync(string username)
        {
            return await Task.Run(() =>
            {
                var groups = new List<string>();

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                var userGroups = user.GetAuthorizationGroups();
                                foreach (GroupPrincipal group in userGroups)
                                {
                                    try
                                    {
                                        if (!IsBuiltInGroup(group.Name))
                                        {
                                            groups.Add(group.Name);
                                        }
                                    }
                                    catch
                                    {
                                        // Bazı gruplar erişilemez olabilir, atla
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting groups for user {username}");
                }

                return groups.Distinct().OrderBy(g => g).ToList();
            });
        }

        public async Task<bool> IsUserInSpecificGroupAsync(string username, string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                        {
                            if (user != null && group != null)
                            {
                                return user.IsMemberOf(group);
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error checking if user {username} is in group {groupName}");
                    return false;
                }
            });
        }

        // Yardımcı metodlar
        private bool IsBuiltInGroup(string groupName)
        {
            var builtInGroups = new[]
            {
        "Domain Admins", "Domain Users", "Domain Guests", "Domain Controllers",
        "Schema Admins", "Enterprise Admins", "Administrators", "Users", "Guests",
        "Account Operators", "Server Operators", "Print Operators", "Backup Operators",
        "Replicator", "Remote Desktop Users", "Network Configuration Operators",
        "Performance Monitor Users", "Performance Log Users", "Distributed COM Users",
        "IIS_IUSRS", "Cryptographic Operators", "Event Log Readers",
        "Certificate Service DCOM Access", "RDS Remote Access Servers",
        "RDS Endpoint Servers", "RDS Management Servers", "Hyper-V Administrators",
        "Access Control Assistance Operators", "Remote Management Users"
    };

            return builtInGroups.Contains(groupName, StringComparer.OrdinalIgnoreCase) ||
                   groupName.StartsWith("DnsAdmins", StringComparison.OrdinalIgnoreCase) ||
                   groupName.StartsWith("DnsUpdateProxy", StringComparison.OrdinalIgnoreCase);
        }

        private string GetGroupType(DirectoryEntry directoryEntry)
        {
            try
            {
                var groupType = (int)directoryEntry.Properties["groupType"].Value;

                // Security vs Distribution
                bool isSecurity = (groupType & 0x80000000) != 0;
                return isSecurity ? "Security" : "Distribution";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetGroupScope(DirectoryEntry directoryEntry)
        {
            try
            {
                var groupType = (int)directoryEntry.Properties["groupType"].Value;

                // Scope belirleme
                if ((groupType & 0x00000004) != 0) return "Domain Local";
                if ((groupType & 0x00000002) != 0) return "Global";
                if ((groupType & 0x00000008) != 0) return "Universal";

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private bool IsUserInGroup(string username, string groupName, PrincipalContext context)
        {
            try
            {
                using (var user = UserPrincipal.FindByIdentity(context, username))
                using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                {
                    return user != null && group != null && user.IsMemberOf(group);
                }
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> UpdateUserAttributesAsync(string username, Dictionary<string, string> attributes)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry;

                                foreach (var attribute in attributes)
                                {
                                    if (directoryEntry.Properties.Contains(attribute.Key))
                                    {
                                        directoryEntry.Properties[attribute.Key].Value = attribute.Value;
                                    }
                                    else
                                    {
                                        directoryEntry.Properties[attribute.Key].Add(attribute.Value);
                                    }
                                }

                                directoryEntry.CommitChanges();
                                _logger.LogInformation($"Attributes updated for user {username}");
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating attributes for user {username}");
                    return false;
                }
            });
        }
    }

    // Extension method for UserPrincipal
    public static class UserPrincipalExtensions
    {
        public static string GetProperty(this UserPrincipal principal, string property)
        {
            try
            {
                var directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;
                return directoryEntry?.Properties[property]?.Value?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}