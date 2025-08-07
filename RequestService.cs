using ADUserManagement.Data;
using ADUserManagement.Models.Domain;
using ADUserManagement.Models.Dto;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ADUserManagement.Constants;
using System.Diagnostics;

namespace ADUserManagement.Services
{
    public class RequestService : IRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveDirectoryService _adService;
        private readonly IEmailService _emailService;
        private readonly IPasswordGeneratorService _passwordGenerator;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RequestService> _logger;

        public RequestService(
            ApplicationDbContext context,
            IActiveDirectoryService adService,
            IEmailService emailService,
            IPasswordGeneratorService passwordGenerator,
            IConfiguration configuration,
            ILogger<RequestService> logger)
        {
            _context = context;
            _adService = adService;
            _emailService = emailService;
            _passwordGenerator = passwordGenerator;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<int> CreateUserCreationRequestAsync(UserCreationRequestViewModel model, string requestedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Kullanıcı bilgilerini al veya oluştur
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

                // Talep numarası oluştur
                var requestNumber = await GenerateRequestNumberAsync("CRT");

                // Talebi oluştur
                var request = new UserCreationRequest
                {
                    RequestNumber = requestNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Username = model.Username,
                    Email = model.Email,
                    CompanyId = model.CompanyId,
                    Department = model.Department,
                    RequestedById = requestedBy.Id,
                    StatusId = 1, // Beklemede
                    RequestedDate = DateTime.Now
                };

                _context.UserCreationRequests.Add(request);
                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Kullanıcı açma talebi oluşturuldu",
                    EntityType = "UserCreationRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // E-posta bildirimi gönder
                try
                {
                    await _emailService.SendNewRequestNotificationAsync("creation", requestNumber, request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email notification failed for request {RequestId}", request.Id);
                }

                _logger.LogInformation(LogMessages.UserCreationRequestCreated, request.Id, model.Username, requestedByUsername);
                return request.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating user creation request");
                throw;
            }
        }

        public async Task<int> CreateUserDeletionRequestAsync(UserDeletionRequestViewModel model, string requestedByUsername)
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

                var requestNumber = await GenerateRequestNumberAsync("DEL");

                var request = new UserDeletionRequest
                {
                    RequestNumber = requestNumber,
                    Username = model.Username,
                    Email = model.Email,
                    RequestedById = requestedBy.Id,
                    StatusId = 1, // Beklemede
                    RequestedDate = DateTime.Now
                };

                _context.UserDeletionRequests.Add(request);
                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Kullanıcı kapatma talebi oluşturuldu",
                    EntityType = "UserDeletionRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                try
                {
                    await _emailService.SendNewRequestNotificationAsync("deletion", requestNumber, request.Id);
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
                _logger.LogError(ex, "Error creating user deletion request");
                throw;
            }
        }

        public async Task<int> CreateAttributeChangeRequestAsync(UserAttributeChangeViewModel model, string requestedByUsername)
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

                var requestNumber = await GenerateRequestNumberAsync("ATR");

                var request = new UserAttributeChangeRequest
                {
                    RequestNumber = requestNumber,
                    Username = model.Username,
                    AttributeName = model.AttributeName,
                    OldValue = model.OldValue,
                    NewValue = model.NewValue,
                    ChangeReason = model.ChangeReason,
                    RequestedById = requestedBy.Id,
                    StatusId = 1, // Beklemede
                    RequestedDate = DateTime.Now
                };

                _context.UserAttributeChangeRequests.Add(request);
                await _context.SaveChangesAsync();

                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Attribute değiştirme talebi oluşturuldu",
                    EntityType = "UserAttributeChangeRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}, Attribute: {model.AttributeName}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                try
                {
                    await _emailService.SendNewRequestNotificationAsync("attribute", requestNumber, request.Id);
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
                _logger.LogError(ex, "Error creating attribute change request");
                throw;
            }
        }

        public async Task<List<RequestListViewModel>> GetPendingRequestsAsync()
        {
            try
            {
                // ✅ OPTIMIZED: Tek sorgu ile tüm pending requestleri getir
                var pendingRequests = new List<RequestListViewModel>();
                var stopwatch = Stopwatch.StartNew(); // ✅ DOĞRU YER: Query'den ÖNCE

                // Raw SQL ile UNION ALL - En hızlı yöntem
                var sql = @"
                    -- User Creation Requests
                    SELECT 
                        ucr.Id,
                        ucr.RequestNumber,
                        'creation' as RequestType,
                        (ucr.FirstName + ' ' + ucr.LastName) as DisplayName,
                        ucr.Username,
                        ucr.Email,
                        c.CompanyName as Company,
                        rs.StatusName as Status,
                        ucr.RequestedDate,
                        su.DisplayName as RequestedBy
                    FROM UserCreationRequests ucr
                    INNER JOIN Companies c ON ucr.CompanyId = c.Id  
                    INNER JOIN RequestStatuses rs ON ucr.StatusId = rs.Id
                    INNER JOIN SystemUsers su ON ucr.RequestedById = su.Id
                    WHERE ucr.StatusId = 1

                    UNION ALL

                    -- User Deletion Requests  
                    SELECT
                        udr.Id,
                        udr.RequestNumber,
                        'deletion' as RequestType,
                        (udr.Username + ' - Kullanıcı Kapatma') as DisplayName,
                        udr.Username,
                        udr.Email,
                        'Kapatma İşlemi' as Company,
                        rs.StatusName as Status,
                        udr.RequestedDate,
                        su.DisplayName as RequestedBy
                    FROM UserDeletionRequests udr
                    INNER JOIN RequestStatuses rs ON udr.StatusId = rs.Id
                    INNER JOIN SystemUsers su ON udr.RequestedById = su.Id
                    WHERE udr.StatusId = 1

                    UNION ALL

                    -- Attribute Change Requests
                    SELECT
                        uacr.Id,
                        uacr.RequestNumber, 
                        'attribute' as RequestType,
                        (uacr.Username + ' - ' + uacr.AttributeName) as DisplayName,
                        uacr.Username,
                        NULL as Email,
                        (uacr.AttributeName + ' Değişikliği') as Company,
                        rs.StatusName as Status,
                        uacr.RequestedDate,
                        su.DisplayName as RequestedBy
                    FROM UserAttributeChangeRequests uacr
                    INNER JOIN RequestStatuses rs ON uacr.StatusId = rs.Id
                    INNER JOIN SystemUsers su ON uacr.RequestedById = su.Id
                    WHERE uacr.StatusId = 1

                    UNION ALL

                    -- Password Reset Requests
                    SELECT
                        prr.Id,
                        prr.RequestNumber,
                        'password' as RequestType,
                        (prr.Username + ' - Şifre Sıfırlama') as DisplayName,
                        prr.Username,
                        prr.UserEmail as Email,
                        'Şifre İşlemi' as Company,
                        rs.StatusName as Status,
                        prr.RequestedDate,
                        su.DisplayName as RequestedBy
                    FROM PasswordResetRequests prr
                    INNER JOIN RequestStatuses rs ON prr.StatusId = rs.Id
                    INNER JOIN SystemUsers su ON prr.RequestedById = su.Id
                    WHERE prr.StatusId = 1

                    UNION ALL

                    -- Group Membership Requests
                    SELECT
                        gmr.Id,
                        gmr.RequestNumber,
                        'group' as RequestType,
                        (gmr.Username + ' - ' + gmr.GroupName) as DisplayName,
                        gmr.Username,
                        NULL as Email,
                        CASE 
                            WHEN gmr.ActionType = 'Add' THEN 'Gruba Ekle'
                            ELSE 'Gruptan Çıkar'
                        END as Company,
                        rs.StatusName as Status,
                        gmr.RequestedDate,
                        su.DisplayName as RequestedBy
                    FROM GroupMembershipRequests gmr
                    INNER JOIN RequestStatuses rs ON gmr.StatusId = rs.Id
                    INNER JOIN SystemUsers su ON gmr.RequestedById = su.Id
                    WHERE gmr.StatusId = 1

                    ORDER BY RequestedDate DESC";

                // Execute raw SQL
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    await _context.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            pendingRequests.Add(new RequestListViewModel
                            {
                                Id = reader.GetInt32("Id"),
                                RequestNumber = reader.GetString("RequestNumber"),
                                RequestType = reader.GetString("RequestType"),
                                DisplayName = reader.GetString("DisplayName"),
                                Username = reader.GetString("Username"),
                                Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                                Company = reader.GetString("Company"),
                                Status = reader.GetString("Status"),
                                RequestedDate = reader.GetDateTime("RequestedDate"),
                                RequestedBy = reader.GetString("RequestedBy")
                            });
                        }
                    }
                }

                stopwatch.Stop(); // ✅ DOĞRU YER: Query'den SONRA

                // Performance logging with threshold check
                if (stopwatch.ElapsedMilliseconds > PerformanceThresholds.SlowQueryMs)
                {
                    _logger.LogWarning("🐌 Slow query detected: GetPendingRequests took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                }

                _logger.LogInformation(LogMessages.PendingRequestsRetrieved, pendingRequests.Count, stopwatch.ElapsedMilliseconds);

                return pendingRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimized GetPendingRequestsAsync");

                // Fallback to old method if SQL fails
                _logger.LogWarning("Falling back to original method");
                return await GetPendingRequestsAsync_Fallback();
            }
        }

        // Fallback method (original logic)
        private async Task<List<RequestListViewModel>> GetPendingRequestsAsync_Fallback()
        {
            // User creation requests
            var creationRequests = await _context.UserCreationRequests
                .Include(r => r.Company)
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.StatusId == 1) // Beklemede
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "creation",
                    DisplayName = r.FirstName + " " + r.LastName,
                    Username = r.Username,
                    Email = r.Email,
                    Company = r.Company.CompanyName,
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    RequestedBy = r.RequestedBy.DisplayName
                })
                .ToListAsync();

            // User deletion requests
            var deletionRequests = await _context.UserDeletionRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.StatusId == 1) // Beklemede
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "deletion",
                    DisplayName = r.Username + " - Kullanıcı Kapatma",
                    Username = r.Username,
                    Email = r.Email,
                    Company = "Kapatma İşlemi",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    RequestedBy = r.RequestedBy.DisplayName
                })
                .ToListAsync();

            // Attribute change requests
            var attributeRequests = await _context.UserAttributeChangeRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.StatusId == 1) // Beklemede
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "attribute",
                    DisplayName = r.Username + " - " + r.AttributeName,
                    Username = r.Username,
                    Email = null,
                    Company = r.AttributeName + " Değişikliği",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    RequestedBy = r.RequestedBy.DisplayName
                })
                .ToListAsync();

            // Password reset requests
            var passwordRequests = await _context.PasswordResetRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.StatusId == 1) // Beklemede
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "password",
                    DisplayName = r.Username + " - Şifre Sıfırlama",
                    Username = r.Username,
                    Email = r.UserEmail,
                    Company = "Şifre İşlemi",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    RequestedBy = r.RequestedBy.DisplayName
                })
                .ToListAsync();

            // Group membership requests
            var groupRequests = await _context.GroupMembershipRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.StatusId == 1) // Beklemede
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
                    RequestedBy = r.RequestedBy.DisplayName
                })
                .ToListAsync();

            var allRequests = creationRequests
                .Concat(deletionRequests)
                .Concat(attributeRequests)
                .Concat(passwordRequests)
                .Concat(groupRequests)
                .OrderByDescending(r => r.RequestedDate)
                .ToList();

            _logger.LogInformation("Fallback method returned {RequestCount} pending requests", allRequests.Count);
            return allRequests;
        }
        public async Task<List<RequestListViewModel>> GetMyRequestsAsync(string username)
        {
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new List<RequestListViewModel>();

            try
            {
                // ✅ OPTIMIZED: Tek sorgu ile kullanıcının tüm requestlerini getir
                var myRequests = new List<RequestListViewModel>();

                // Raw SQL ile UNION ALL - User ID parametreli
                var sql = @"
                    -- User Creation Requests
                    SELECT 
                        ucr.Id,
                        ucr.RequestNumber,
                        'creation' as RequestType,
                        (ucr.FirstName + ' ' + ucr.LastName) as DisplayName,
                        ucr.Username,
                        ucr.Email,
                        c.CompanyName as Company,
                        rs.StatusName as Status,
                        ucr.RequestedDate,
                        ucr.ApprovedDate
                    FROM UserCreationRequests ucr
                    INNER JOIN Companies c ON ucr.CompanyId = c.Id  
                    INNER JOIN RequestStatuses rs ON ucr.StatusId = rs.Id
                    WHERE ucr.RequestedById = @userId

                    UNION ALL

                    -- User Deletion Requests  
                    SELECT
                        udr.Id,
                        udr.RequestNumber,
                        'deletion' as RequestType,
                        (udr.Username + ' - Kullanıcı Kapatma') as DisplayName,
                        udr.Username,
                        udr.Email,
                        'Kapatma İşlemi' as Company,
                        rs.StatusName as Status,
                        udr.RequestedDate,
                        udr.ApprovedDate
                    FROM UserDeletionRequests udr
                    INNER JOIN RequestStatuses rs ON udr.StatusId = rs.Id
                    WHERE udr.RequestedById = @userId

                    UNION ALL

                    -- Attribute Change Requests
                    SELECT
                        uacr.Id,
                        uacr.RequestNumber, 
                        'attribute' as RequestType,
                        (uacr.Username + ' - ' + uacr.AttributeName) as DisplayName,
                        uacr.Username,
                        NULL as Email,
                        (uacr.AttributeName + ' Değişikliği') as Company,
                        rs.StatusName as Status,
                        uacr.RequestedDate,
                        uacr.ApprovedDate
                    FROM UserAttributeChangeRequests uacr
                    INNER JOIN RequestStatuses rs ON uacr.StatusId = rs.Id
                    WHERE uacr.RequestedById = @userId

                    UNION ALL

                    -- Password Reset Requests
                    SELECT
                        prr.Id,
                        prr.RequestNumber,
                        'password' as RequestType,
                        (prr.Username + ' - Şifre Sıfırlama') as DisplayName,
                        prr.Username,
                        prr.UserEmail as Email,
                        'Şifre İşlemi' as Company,
                        rs.StatusName as Status,
                        prr.RequestedDate,
                        prr.ApprovedDate
                    FROM PasswordResetRequests prr
                    INNER JOIN RequestStatuses rs ON prr.StatusId = rs.Id
                    WHERE prr.RequestedById = @userId

                    UNION ALL

                    -- Group Membership Requests
                    SELECT
                        gmr.Id,
                        gmr.RequestNumber,
                        'group' as RequestType,
                        (gmr.Username + ' - ' + gmr.GroupName) as DisplayName,
                        gmr.Username,
                        NULL as Email,
                        CASE 
                            WHEN gmr.ActionType = 'Add' THEN 'Gruba Ekle'
                            ELSE 'Gruptan Çıkar'
                        END as Company,
                        rs.StatusName as Status,
                        gmr.RequestedDate,
                        gmr.ApprovedDate
                    FROM GroupMembershipRequests gmr
                    INNER JOIN RequestStatuses rs ON gmr.StatusId = rs.Id
                    WHERE gmr.RequestedById = @userId

                    ORDER BY RequestedDate DESC";

                // Execute raw SQL with parameter
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;

                    // Add parameter for user ID
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@userId";
                    parameter.Value = user.Id;
                    command.Parameters.Add(parameter);

                    await _context.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            myRequests.Add(new RequestListViewModel
                            {
                                Id = reader.GetInt32("Id"),
                                RequestNumber = reader.GetString("RequestNumber"),
                                RequestType = reader.GetString("RequestType"),
                                DisplayName = reader.GetString("DisplayName"),
                                Username = reader.GetString("Username"),
                                Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                                Company = reader.GetString("Company"),
                                Status = reader.GetString("Status"),
                                RequestedDate = reader.GetDateTime("RequestedDate"),
                                ApprovedDate = reader.IsDBNull("ApprovedDate") ? null : reader.GetDateTime("ApprovedDate")
                            });
                        }
                    }
                }

                _logger.LogInformation($"Retrieved {myRequests.Count} requests for user {username} with optimized query");
                return myRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in optimized GetMyRequestsAsync for user {Username}", username);

                // Fallback to old method if SQL fails
                _logger.LogWarning("Falling back to original method for user {Username}", username);
                return await GetMyRequestsAsync_Fallback(user.Id);
            }
        }

        // Fallback method for GetMyRequestsAsync
        private async Task<List<RequestListViewModel>> GetMyRequestsAsync_Fallback(int userId)
        {
            var creationRequests = await _context.UserCreationRequests
                .Include(r => r.Company)
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == userId)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "creation",
                    DisplayName = r.FirstName + " " + r.LastName,
                    Username = r.Username,
                    Email = r.Email,
                    Company = r.Company.CompanyName,
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            var deletionRequests = await _context.UserDeletionRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == userId)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "deletion",
                    DisplayName = r.Username + " - Kullanıcı Kapatma",
                    Username = r.Username,
                    Email = r.Email,
                    Company = "Kapatma İşlemi",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            var attributeRequests = await _context.UserAttributeChangeRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == userId)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "attribute",
                    DisplayName = r.Username + " - " + r.AttributeName,
                    Username = r.Username,
                    Email = null,
                    Company = r.AttributeName + " Değişikliği",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            var passwordRequests = await _context.PasswordResetRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == userId)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "password",
                    DisplayName = r.Username + " - Şifre Sıfırlama",
                    Username = r.Username,
                    Email = r.UserEmail,
                    Company = "Şifre İşlemi",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            var groupRequests = await _context.GroupMembershipRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.RequestedById == userId)
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
                .ToListAsync();

            var allRequests = creationRequests
                .Concat(deletionRequests)
                .Concat(attributeRequests)
                .Concat(passwordRequests)
                .Concat(groupRequests)
                .OrderByDescending(r => r.RequestedDate)
                .ToList();

            return allRequests;
        }

        public async Task<UserCreationRequest> GetUserCreationRequestAsync(int id)
        {
            return await _context.UserCreationRequests
                .Include(r => r.Company)
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.Status)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<UserDeletionRequest> GetUserDeletionRequestAsync(int id)
        {
            return await _context.UserDeletionRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.Status)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<UserAttributeChangeRequest> GetAttributeChangeRequestAsync(int id)
        {
            return await _context.UserAttributeChangeRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.Status)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PasswordResetRequest> GetPasswordResetRequestAsync(int id)
        {
            return await _context.PasswordResetRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.Status)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var dashboard = new DashboardViewModel
            {
                PendingCreationRequests = await _context.UserCreationRequests.CountAsync(r => r.StatusId == 1),
                PendingDeletionRequests = await _context.UserDeletionRequests.CountAsync(r => r.StatusId == 1),
                ApprovedCreationRequests = await _context.UserCreationRequests.CountAsync(r => r.StatusId == 2),
                ApprovedDeletionRequests = await _context.UserDeletionRequests.CountAsync(r => r.StatusId == 2),
                RejectedCreationRequests = await _context.UserCreationRequests.CountAsync(r => r.StatusId == 3),
                RejectedDeletionRequests = await _context.UserDeletionRequests.CountAsync(r => r.StatusId == 3)
            };

            dashboard.TotalRequests = await _context.UserCreationRequests.CountAsync() +
                                     await _context.UserDeletionRequests.CountAsync() +
                                     await _context.UserAttributeChangeRequests.CountAsync();

            return dashboard;
        }

        public async Task<bool> ApproveRequestAsync(int requestId, string requestType, string approvedByUsername)
        {
            // Bu metod implementation'ı ayrı bir dosyada olacak
            throw new NotImplementedException("ApproveRequestAsync method needs implementation");
        }

        public async Task<bool> RejectRequestAsync(int requestId, string requestType, string rejectionReason, string rejectedByUsername)
        {
            // Bu metod implementation'ı ayrı bir dosyada olacak
            throw new NotImplementedException("RejectRequestAsync method needs implementation");
        }

        private async Task<string> GenerateRequestNumberAsync(string requestType)
        {
            var year = DateTime.Now.Year.ToString();
            var counter = 1;

            if (requestType == "CRT")
            {
                var lastRequest = await _context.UserCreationRequests
                    .Where(r => r.RequestNumber.StartsWith($"CRT-{year}"))
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                if (lastRequest != null)
                {
                    var lastNumber = lastRequest.RequestNumber.Substring(8); // "CRT-2024" sonrası
                    if (int.TryParse(lastNumber, out int parsed))
                    {
                        counter = parsed + 1;
                    }
                }
            }
            else if (requestType == "DEL")
            {
                var lastRequest = await _context.UserDeletionRequests
                    .Where(r => r.RequestNumber.StartsWith($"DEL-{year}"))
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                if (lastRequest != null)
                {
                    var lastNumber = lastRequest.RequestNumber.Substring(8);
                    if (int.TryParse(lastNumber, out int parsed))
                    {
                        counter = parsed + 1;
                    }
                }
            }
            else if (requestType == "ATR")
            {
                var lastRequest = await _context.UserAttributeChangeRequests
                    .Where(r => r.RequestNumber.StartsWith($"ATR-{year}"))
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                if (lastRequest != null)
                {
                    var lastNumber = lastRequest.RequestNumber.Substring(8);
                    if (int.TryParse(lastNumber, out int parsed))
                    {
                        counter = parsed + 1;
                    }
                }
            }

            return $"{requestType}-{year}{counter:D4}";
        }
    }
}