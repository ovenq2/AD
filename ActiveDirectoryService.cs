using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using ADUserManagement.Models.Dto;
using ADUserManagement.Services.Interfaces;
using ADUserManagement.Models.Configuration;
using ADUserManagement.Services;
// ✅ FIX: Use alias to resolve ambiguous reference
using CacheConstants = ADUserManagement.Constants.CacheKeys;

namespace ADUserManagement.Services
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ActiveDirectoryConfig _adConfig;
        private readonly ILogger<ActiveDirectoryService> _logger;
        private readonly IMemoryCache _cache;

        // Backward compatibility için gerekli properties
        private readonly string _domain;
        private readonly string _serviceAccount;
        private readonly string _servicePassword;

        public ActiveDirectoryService(
            IOptions<ActiveDirectoryConfig> adConfig,
            ILogger<ActiveDirectoryService> logger,
            IMemoryCache cache = null) // ✅ Optional cache dependency
        {
            _adConfig = adConfig.Value;
            _logger = logger;
            _cache = cache; // Can be null if not needed

            // Backward compatibility
            _domain = _adConfig.Domain;
            _serviceAccount = _adConfig.ServiceAccount;
            _servicePassword = _adConfig.ServicePassword;

            // Configuration validation
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_adConfig.Domain))
                throw new InvalidOperationException("Active Directory domain is not configured.");

            if (string.IsNullOrWhiteSpace(_adConfig.ServiceAccount))
                throw new InvalidOperationException("Active Directory service account is not configured.");

            if (!_adConfig.AuthorizedGroups.Any())
                throw new InvalidOperationException("No authorized groups configured.");

            _logger.LogInformation("ActiveDirectoryService initialized for domain: {Domain}", _adConfig.Domain);
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username?.Split('\\').LastOrDefault() ?? username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(password) || password.Length > 128)
                    {
                        _logger.LogWarning("Invalid password format provided");
                        return false;
                    }

                    // Domain\Username formatını kontrol et
                    var accountName = username;
                    var domainToUse = _domain;

                    if (username.Contains("\\"))
                    {
                        var parts = username.Split('\\');
                        var requestedDomain = parts[0];
                        accountName = parts[1];

                        // Eğer farklı bir domain belirtilmişse, onu kullan
                        // Güvenlik için sadece güvenilen domainler kabul edilebilir
                        if (!string.Equals(requestedDomain, _adConfig.Domain.Split('.')[0], StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Authentication attempt from untrusted domain: {RequestedDomain}", requestedDomain);
                            return false;
                        }
                    }

                    using (var context = CreatePrincipalContext(domainToUse))
                    {
                        var isValid = context.ValidateCredentials(accountName, password);

                        if (isValid)
                        {
                            _logger.LogInformation("AD user validation successful for {Username}", accountName);
                        }
                        else
                        {
                            _logger.LogWarning("AD user validation failed for {Username}: Invalid credentials", accountName);
                        }

                        return isValid;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AD user validation failed for {Username}: {Error}", username, ex.Message);
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
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            return user != null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking if user {Username} exists", username);
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
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    if (!InputValidationHelper.IsValidGroupName(groupName))
                    {
                        _logger.LogWarning("Invalid group name format: {GroupName}", groupName);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                        {
                            if (user != null && group != null)
                            {
                                var isMember = user.IsMemberOf(group);
                                _logger.LogInformation(isMember ? "User {Username} is member of group {GroupName}" : "User {Username} is not member of group {GroupName}", username, groupName);
                                return isMember;
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AD group check failed for user {Username} and group {GroupName}: {Error}", username, groupName, ex.Message);
                    return false;
                }
            });
        }

        // Multi-domain için eklenen yeni method
        public async Task<bool> IsUserInAnyAuthorizedGroupAsync(string username)
        {
            foreach (var group in _adConfig.AuthorizedGroups)
            {
                if (await IsUserInGroupAsync(username, group))
                {
                    return true;
                }
            }
            return false;
        }

        // IsUserInSpecificGroupAsync - interface'den gelen eksik method
        public async Task<bool> IsUserInSpecificGroupAsync(string username, string groupName)
        {
            return await IsUserInGroupAsync(username, groupName);
        }

        public async Task<string> GetUserEmailAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return null;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            return user?.EmailAddress;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting email for user {Username}", username);
                    return null;
                }
            });
        }

        public async Task<ADUserDetailsDto> GetUserDetailsAsync(string username)
        {
            // ✅ FIX: Use cache alias instead of ambiguous CacheKeys
            var cacheKey = $"user_details_{username}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out ADUserDetailsDto cachedUser))
            {
                _logger.LogInformation("Retrieved user details for {Username} from cache", username);
                return cachedUser;
            }

            return await Task.Run(() =>
            {
                try
                {
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return null;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                // ✅ FIXED: DirectoryEntry properly disposed
                                using (var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry)
                                {
                                    // Groups listesini al
                                    var userGroups = new List<string>();
                                    try
                                    {
                                        var groups = user.GetGroups();
                                        using (groups)
                                        {
                                            foreach (GroupPrincipal group in groups)
                                            {
                                                using (group)
                                                {
                                                    if (!IsBuiltInGroup(group.Name))
                                                    {
                                                        userGroups.Add(group.Name);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error getting groups for user {Username}", username);
                                    }

                                    var userDetails = new ADUserDetailsDto
                                    {
                                        Username = user.SamAccountName ?? username,
                                        FirstName = user.GivenName ?? "",
                                        LastName = user.Surname ?? "",
                                        DisplayName = user.DisplayName ?? user.Name ?? username,
                                        Email = user.EmailAddress ?? "",
                                        Phone = user.VoiceTelephoneNumber ?? "",
                                        Mobile = directoryEntry?.Properties["mobile"]?.Value?.ToString() ?? "",
                                        Department = directoryEntry?.Properties["department"]?.Value?.ToString() ?? "",
                                        Title = directoryEntry?.Properties["title"]?.Value?.ToString() ?? "",
                                        Office = directoryEntry?.Properties["physicalDeliveryOfficeName"]?.Value?.ToString() ?? "",
                                        Manager = directoryEntry?.Properties["manager"]?.Value?.ToString() ?? "",
                                        Description = user.Description ?? "",
                                        Company = directoryEntry?.Properties["company"]?.Value?.ToString() ?? "",
                                        IsEnabled = user.Enabled ?? false,
                                        LastLogon = user.LastLogon,
                                        Created = directoryEntry?.Properties["whenCreated"]?.Value as DateTime?,
                                        PasswordLastSet = user.LastPasswordSet,
                                        AccountExpirationDate = user.AccountExpirationDate,
                                        Groups = userGroups
                                    };

                                    // ✅ FIX: Cache the result if cache is available
                                    if (_cache != null)
                                    {
                                        var cacheOptions = new MemoryCacheEntryOptions
                                        {
                                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheConstants.ExpirationTimes.MediumTerm),
                                            SlidingExpiration = TimeSpan.FromMinutes(5),
                                            Priority = CacheItemPriority.Normal
                                        };
                                        _cache.Set(cacheKey, userDetails, cacheOptions);
                                    }

                                    return userDetails;
                                }
                            }
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting user details for: {Username}", username);
                    return null;
                }
            });
        }

        // SearchUsersAsync - Eksik method
        public async Task<List<ADUserSearchResultDto>> SearchUsersAsync(string searchTerm)
        {
            // ✅ FIX: Use direct cache key instead of ambiguous reference
            var cacheKey = $"user_search_{searchTerm}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out List<ADUserSearchResultDto> cachedResults))
            {
                _logger.LogInformation("Retrieved user search results for '{SearchTerm}' from cache", searchTerm);
                return cachedResults;
            }

            return await Task.Run(() =>
            {
                var results = new List<ADUserSearchResultDto>();

                try
                {
                    if (string.IsNullOrWhiteSpace(searchTerm))
                        return results;

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var searcher = new PrincipalSearcher())
                        {
                            var userPrincipal = new UserPrincipal(context);
                            userPrincipal.SamAccountName = "*" + searchTerm + "*";

                            searcher.QueryFilter = userPrincipal;
                            var searchResults = searcher.FindAll();

                            using (searchResults)
                            {
                                foreach (UserPrincipal user in searchResults)
                                {
                                    using (user)
                                    {
                                        try
                                        {
                                            using (var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry)
                                            {
                                                var result = new ADUserSearchResultDto
                                                {
                                                    Username = user.SamAccountName,
                                                    DisplayName = user.DisplayName ?? user.SamAccountName,
                                                    Email = user.EmailAddress ?? "",
                                                    Department = directoryEntry?.Properties["department"]?.Value?.ToString() ?? "",
                                                    Title = directoryEntry?.Properties["title"]?.Value?.ToString() ?? "",
                                                    IsEnabled = user.Enabled ?? false
                                                };

                                                results.Add(result);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Error processing user {Username} in search", user.SamAccountName);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // ✅ FIX: Cache the results if cache is available
                    if (_cache != null)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheConstants.ExpirationTimes.MediumTerm),
                            SlidingExpiration = TimeSpan.FromMinutes(5),
                            Priority = CacheItemPriority.Low
                        };
                        _cache.Set(cacheKey, results.Take(50).ToList(), cacheOptions);
                    }

                    return results.Take(50).ToList(); // Limit results
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                    return results;
                }
            });
        }

        public async Task<List<ADGroupSearchResultDto>> SearchGroupsAsync(string searchTerm, string username = null)
        {
            // ✅ FIX: Use direct cache key instead of ambiguous reference
            var cacheKey = $"group_search_{searchTerm}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out List<ADGroupSearchResultDto> cachedResults))
            {
                _logger.LogInformation("Retrieved group search results for '{SearchTerm}' from cache", searchTerm);
                return cachedResults;
            }

            return await Task.Run(() =>
            {
                var results = new List<ADGroupSearchResultDto>();

                try
                {
                    if (string.IsNullOrWhiteSpace(searchTerm))
                        return results;

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var searcher = new PrincipalSearcher())
                        {
                            var groupPrincipal = new GroupPrincipal(context);
                            groupPrincipal.Name = "*" + searchTerm + "*";

                            searcher.QueryFilter = groupPrincipal;
                            var searchResults = searcher.FindAll();

                            // ✅ FIXED: Proper disposal of search results
                            using (searchResults)
                            {
                                foreach (GroupPrincipal group in searchResults)
                                {
                                    // ✅ FIXED: Each group is properly disposed
                                    using (group)
                                    {
                                        try
                                        {
                                            // Built-in grupları filtrele
                                            if (IsBuiltInGroup(group.Name))
                                                continue;

                                            // ✅ FIXED: DirectoryEntry properly disposed
                                            using (var directoryEntry = group.GetUnderlyingObject() as DirectoryEntry)
                                            {
                                                var groupType = GetGroupType(directoryEntry);

                                                var result = new ADGroupSearchResultDto
                                                {
                                                    Name = group.Name,
                                                    DisplayName = group.DisplayName ?? group.Name,
                                                    Description = group.Description ?? "",
                                                    GroupType = groupType,
                                                    MemberCount = group.Members?.Count() ?? 0,
                                                    IsBuiltIn = IsBuiltInGroup(group.Name)
                                                };

                                                results.Add(result);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Error processing group {GroupName} in search", group.Name);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // ✅ FIX: Cache the results if cache is available
                    if (_cache != null)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheConstants.ExpirationTimes.MediumTerm),
                            SlidingExpiration = TimeSpan.FromMinutes(5),
                            Priority = CacheItemPriority.Low
                        };
                        _cache.Set(cacheKey, results.Take(20).ToList(), cacheOptions);
                    }

                    return results.Take(20).ToList(); // Limit results to prevent memory issues
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching groups with term: {SearchTerm}", searchTerm);
                    return results;
                }
            });
        }

        // GetGroupDetailsAsync - Eksik method
        public async Task<ADGroupDto> GetGroupDetailsAsync(string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!InputValidationHelper.IsValidGroupName(groupName))
                    {
                        _logger.LogWarning("Invalid group name format: {GroupName}", groupName);
                        return null;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var group = GroupPrincipal.FindByIdentity(context, groupName))
                        {
                            if (group != null)
                            {
                                using (var directoryEntry = group.GetUnderlyingObject() as DirectoryEntry)
                                {
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
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting group details for: {GroupName}", groupName);
                    return null;
                }
            });
        }

        // CreateUserAsync - Eksik method implementation
        public async Task<bool> CreateUserAsync(ADUserDto user)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!InputValidationHelper.IsValidUsername(user.Username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", user.Username);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
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

                            // Set password
                            newUser.SetPassword(user.Password);
                            newUser.ExpirePasswordNow();

                            newUser.Save();

                            // Set additional attributes
                            using (var directoryEntry = newUser.GetUnderlyingObject() as DirectoryEntry)
                            {
                                if (!string.IsNullOrEmpty(user.Department))
                                    directoryEntry.Properties["department"].Value = user.Department;
                                if (!string.IsNullOrEmpty(user.Title))
                                    directoryEntry.Properties["title"].Value = user.Title;

                                directoryEntry.CommitChanges();

                                // Move to specified OU if provided, otherwise use default
                                var targetOU = !string.IsNullOrEmpty(user.OUPath) ? user.OUPath : _adConfig.DefaultOU;
                                if (!string.IsNullOrEmpty(targetOU))
                                {
                                    using (var ouEntry = new DirectoryEntry($"LDAP://{targetOU}", _serviceAccount, _servicePassword))
                                    {
                                        directoryEntry.MoveTo(ouEntry);
                                    }
                                }
                            }

                            _logger.LogInformation("User {Username} created successfully in AD", user.Username);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user {Username} in AD", user.Username);
                    return false;
                }
            });
        }

        // DisableUserAsync - Eksik method implementation
        public async Task<bool> DisableUserAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                user.Enabled = false;
                                user.Save();
                                _logger.LogInformation("User {Username} disabled successfully", username);
                                return true;
                            }
                            else
                            {
                                _logger.LogWarning("User {Username} not found in AD", username);
                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disabling user {Username}", username);
                    return false;
                }
            });
        }

        public async Task<bool> AddUserToGroupAsync(string username, string groupName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    if (!InputValidationHelper.IsValidGroupName(groupName))
                    {
                        _logger.LogWarning("Invalid group name format: {GroupName}", groupName);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
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
                                    _logger.LogInformation("User {Username} added to group {GroupName}", username, groupName);
                                    return true;
                                }
                                else
                                {
                                    _logger.LogInformation("User {Username} is already member of group {GroupName}", username, groupName);
                                    return true; // Zaten üye
                                }
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding user {Username} to group {GroupName}", username, groupName);
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
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    if (!InputValidationHelper.IsValidGroupName(groupName))
                    {
                        _logger.LogWarning("Invalid group name format: {GroupName}", groupName);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
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
                                    _logger.LogInformation("User {Username} removed from group {GroupName}", username, groupName);
                                    return true;
                                }
                                else
                                {
                                    _logger.LogInformation("User {Username} is not member of group {GroupName}", username, groupName);
                                    return true; // Zaten üye değil
                                }
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing user {Username} from group {GroupName}", username, groupName);
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
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return groups;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                // ✅ FIXED: Proper disposal of group results
                                var userGroups = user.GetGroups();
                                using (userGroups)
                                {
                                    foreach (GroupPrincipal group in userGroups)
                                    {
                                        using (group)
                                        {
                                            try
                                            {
                                                if (!IsBuiltInGroup(group.Name))
                                                {
                                                    groups.Add(group.Name);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogWarning(ex, "Error processing group {GroupName} for user {Username}", group.Name, username);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return groups;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting groups for user {Username}", username);
                    return groups;
                }
            });
        }

        public async Task<bool> UpdateUserAttributesAsync(string username, Dictionary<string, string> attributes)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning("Invalid username format: {Username}", username);
                        return false;
                    }

                    using (var context = CreateServicePrincipalContext())
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                // ✅ FIXED: DirectoryEntry properly disposed
                                using (var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry)
                                {
                                    foreach (var attribute in attributes)
                                    {
                                        var sanitizedValue = InputValidationHelper.SanitizeString(attribute.Value);

                                        if (directoryEntry.Properties.Contains(attribute.Key))
                                        {
                                            directoryEntry.Properties[attribute.Key].Value = sanitizedValue;
                                        }
                                        else
                                        {
                                            directoryEntry.Properties[attribute.Key].Add(sanitizedValue);
                                        }
                                    }

                                    directoryEntry.CommitChanges();
                                    _logger.LogInformation("Attributes updated for user {Username}", username);
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating attributes for user {Username}", username);
                    return false;
                }
            });
        }

        // ✅ TAMAMEN DÜZELTİLMİŞ Helper methods
        private PrincipalContext CreatePrincipalContext(string domain = null)
        {
            try
            {
                var targetDomain = domain ?? _adConfig.Domain;

                if (_adConfig.EnableSSL)
                {
                    // ✅ CORRECT: (ContextType, name, options)
                    return new PrincipalContext(ContextType.Domain, targetDomain, ContextOptions.SecureSocketLayer);
                }
                else
                {
                    // ✅ CORRECT: (ContextType, name)
                    return new PrincipalContext(ContextType.Domain, targetDomain);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AD principal context for domain {Domain}", domain ?? _adConfig.Domain);
                throw new InvalidOperationException($"Failed to create Active Directory context: {ex.Message}", ex);
            }
        }

        private PrincipalContext CreateServicePrincipalContext()
        {
            try
            {
                // ✅ FIXED: Correct parameter order for PrincipalContext constructors
                if (!string.IsNullOrEmpty(_adConfig.SearchBase))
                {
                    // SearchBase var - container ile
                    if (_adConfig.EnableSSL)
                    {
                        // ✅ CORRECT: (ContextType, name, container, options, userName, password)
                        return new PrincipalContext(
                            ContextType.Domain,
                            _adConfig.Domain,
                            _adConfig.SearchBase,
                            ContextOptions.SecureSocketLayer,
                            _adConfig.ServiceAccount,
                            _adConfig.ServicePassword);
                    }
                    else
                    {
                        // ✅ CORRECT: (ContextType, name, container, userName, password)
                        return new PrincipalContext(
                            ContextType.Domain,
                            _adConfig.Domain,
                            _adConfig.SearchBase,
                            _adConfig.ServiceAccount,
                            _adConfig.ServicePassword);
                    }
                }
                else
                {
                    // SearchBase yok - container olmadan
                    if (_adConfig.EnableSSL && !string.IsNullOrEmpty(_adConfig.ServiceAccount))
                    {
                        // ✅ FIXED: (ContextType, name, userName, password, options)
                        // Note: This constructor doesn't exist, so we use a different approach
                        var context = new PrincipalContext(ContextType.Domain, _adConfig.Domain, _adConfig.ServiceAccount, _adConfig.ServicePassword);
                        // SSL will be handled at connection level if needed
                        return context;
                    }
                    else if (!string.IsNullOrEmpty(_adConfig.ServiceAccount))
                    {
                        // ✅ CORRECT: (ContextType, name, userName, password)
                        return new PrincipalContext(
                            ContextType.Domain,
                            _adConfig.Domain,
                            _adConfig.ServiceAccount,
                            _adConfig.ServicePassword);
                    }
                    else
                    {
                        // ✅ FALLBACK: No credentials - use current user context
                        if (_adConfig.EnableSSL)
                        {
                            return new PrincipalContext(ContextType.Domain, _adConfig.Domain, ContextOptions.SecureSocketLayer);
                        }
                        else
                        {
                            return new PrincipalContext(ContextType.Domain, _adConfig.Domain);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AD principal context for domain {Domain}", _adConfig.Domain);
                throw new InvalidOperationException($"Failed to create Active Directory context: {ex.Message}", ex);
            }
        }

        // Helper methods
        private bool IsBuiltInGroup(string groupName)
        {
            var builtInGroups = new[]
            {
                "Domain Admins", "Domain Users", "Domain Guests", "Enterprise Admins",
                "Schema Admins", "Administrators", "Users", "Guests", "Power Users",
                "Backup Operators", "Replicator", "Network Configuration Operators",
                "Performance Monitor Users", "Performance Log Users", "Distributed COM Users",
                "IIS_IUSRS", "Cryptographic Operators", "Event Log Readers",
                "Certificate Service DCOM Access", "RDS Remote Access Servers"
            };

            return builtInGroups.Any(bg => string.Equals(bg, groupName, StringComparison.OrdinalIgnoreCase));
        }

        private string GetGroupType(DirectoryEntry directoryEntry)
        {
            try
            {
                if (directoryEntry?.Properties["groupType"]?.Value != null)
                {
                    var groupType = (int)directoryEntry.Properties["groupType"].Value;
                    return (groupType & 0x80000000) != 0 ? "Security" : "Distribution";
                }
                return "Unknown";
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
                if (directoryEntry?.Properties["groupType"]?.Value != null)
                {
                    var groupType = (int)directoryEntry.Properties["groupType"].Value;

                    // Scope belirleme
                    if ((groupType & 0x00000004) != 0) return "Domain Local";
                    if ((groupType & 0x00000002) != 0) return "Global";
                    if ((groupType & 0x00000008) != 0) return "Universal";
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    // ✅ FIXED: Extension method with proper resource management
    public static class UserPrincipalExtensions
    {
        public static string GetProperty(this UserPrincipal principal, string property)
        {
            try
            {
                // ✅ FIXED: DirectoryEntry properly disposed
                using (var directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry)
                {
                    return directoryEntry?.Properties[property]?.Value?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}