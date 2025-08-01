// Services/RequestService.cs - TAM DOSYA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.Domain;
using ADUserManagement.Models.Dto;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;

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
                // Kullanıcı bilgilerini al
                var requestedBy = await _context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Username == requestedByUsername);

                if (requestedBy == null)
                {
                    // Eğer sistemde yoksa ekle
                    requestedBy = new SystemUser
                    {
                        Username = requestedByUsername,
                        DisplayName = requestedByUsername,
                        Email = await _adService.GetUserEmailAsync(requestedByUsername) ?? $"{requestedByUsername}@param.com.tr"
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
                    Phone = model.Phone,
                    Location = model.Location,
                    Department = model.Department,
                    Title = model.Title,
                    Description = model.Description,
                    CompanyId = model.CompanyId,
                    RequestedById = requestedBy.Id,
                    StatusId = 1 // Beklemede
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
                    Details = $"Kullanıcı: {model.Username}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // E-posta bildirimi gönder
                await _emailService.SendNewRequestNotificationAsync("creation", requestNumber, request.Id);

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
                // Kullanıcı bilgilerini al
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

                // Talep numarası oluştur
                var requestNumber = await GenerateRequestNumberAsync("DEL");

                // Talebi oluştur
                var request = new UserDeletionRequest
                {
                    RequestNumber = requestNumber,
                    Username = model.Username,
                    Email = model.Email,
                    RequestedById = requestedBy.Id,
                    StatusId = 1 // Beklemede
                };

                _context.UserDeletionRequests.Add(request);
                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Kullanıcı kapatma talebi oluşturuldu",
                    EntityType = "UserDeletionRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // E-posta bildirimi gönder
                await _emailService.SendNewRequestNotificationAsync("deletion", requestNumber, request.Id);

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
                // Kullanıcı bilgilerini al
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

                // Talep numarası oluştur
                var requestNumber = await GenerateRequestNumberAsync("ATR");

                // Talebi oluştur
                var request = new UserAttributeChangeRequest
                {
                    RequestNumber = requestNumber,
                    Username = model.Username,
                    AttributeName = model.AttributeName,
                    OldValue = model.OldValue,
                    NewValue = model.NewValue,
                    ChangeReason = model.ChangeReason,
                    RequestedById = requestedBy.Id,
                    StatusId = 1 // Beklemede
                };

                _context.UserAttributeChangeRequests.Add(request);
                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Attribute değiştirme talebi oluşturuldu",
                    EntityType = "UserAttributeChangeRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}, Attribute: {model.AttributeName}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // E-posta bildirimi gönder
                await _emailService.SendNewRequestNotificationAsync("attribute", requestNumber, request.Id);

                return request.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating attribute change request");
                throw;
            }
        }

        public async Task<bool> ApproveRequestAsync(int requestId, string requestType, string approvedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
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

                if (requestType == "creation")
                {
                    var request = await _context.UserCreationRequests
                        .Include(r => r.Company)
                        .Include(r => r.RequestedBy)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null || request.StatusId != 1)
                        return false;

                    // AD'de kullanıcı oluştur
                    var generatedPassword = _passwordGenerator.GeneratePassword(12);
                    _logger.LogInformation("Generated password for user {Username}", request.Username);

                    var adUser = new ADUserDto
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Username = request.Username,
                        Email = request.Email,
                        Phone = request.Phone,
                        Department = request.Department,
                        Title = request.Title,
                        Description = request.Description,
                        OUPath = request.Company.OUPath,
                        Password = generatedPassword
                    };

                    var adResult = await _adService.CreateUserAsync(adUser);
                    if (!adResult)
                    {
                        _logger.LogError($"Failed to create user {request.Username} in AD");
                        return false;
                    }

                    // Talebi güncelle
                    request.StatusId = 2; // Onaylandı
                    request.ApprovedById = approvedBy.Id;
                    request.ApprovedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Log
                    var log = new ActivityLog
                    {
                        UserId = approvedBy.Id,
                        Action = "Kullanıcı açma talebi onaylandı",
                        EntityType = "UserCreationRequest",
                        EntityId = request.Id,
                        Details = $"Kullanıcı: {request.Username}"
                    };
                    _context.ActivityLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // E-posta bildirimleri
                    await _emailService.SendApprovalNotificationAsync(request.RequestedBy.Email, request.Username, true);
                    await _emailService.SendNewUserCredentialsAsync(request.Email, request.Username, generatedPassword);

                    return true;
                }
                else if (requestType == "deletion")
                {
                    var request = await _context.UserDeletionRequests
                        .Include(r => r.RequestedBy)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null || request.StatusId != 1)
                        return false;

                    // AD'de kullanıcıyı devre dışı bırak
                    var adResult = await _adService.DisableUserAsync(request.Username);
                    if (!adResult)
                    {
                        _logger.LogError($"Failed to disable user {request.Username} in AD");
                        return false;
                    }

                    // Talebi güncelle
                    request.StatusId = 2; // Onaylandı
                    request.ApprovedById = approvedBy.Id;
                    request.ApprovedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Log
                    var log = new ActivityLog
                    {
                        UserId = approvedBy.Id,
                        Action = "Kullanıcı kapatma talebi onaylandı",
                        EntityType = "UserDeletionRequest",
                        EntityId = request.Id,
                        Details = $"Kullanıcı: {request.Username}"
                    };
                    _context.ActivityLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // E-posta bildirimi
                    await _emailService.SendApprovalNotificationAsync(request.RequestedBy.Email, request.Username, true);

                    return true;
                }
                else if (requestType == "attribute")
                {
                    var request = await _context.UserAttributeChangeRequests
                        .Include(r => r.RequestedBy)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null || request.StatusId != 1)
                        return false;

                    // AD'de attribute değiştir
                    var attributes = new Dictionary<string, string>
                    {
                        { request.AttributeName, request.NewValue }
                    };

                    var adResult = await _adService.UpdateUserAttributesAsync(request.Username, attributes);
                    if (!adResult)
                    {
                        _logger.LogError($"Failed to update attribute {request.AttributeName} for user {request.Username} in AD");
                        return false;
                    }

                    // Talebi güncelle
                    request.StatusId = 2; // Onaylandı
                    request.ApprovedById = approvedBy.Id;
                    request.ApprovedDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Log
                    var log = new ActivityLog
                    {
                        UserId = approvedBy.Id,
                        Action = "Attribute değiştirme talebi onaylandı",
                        EntityType = "UserAttributeChangeRequest",
                        EntityId = request.Id,
                        Details = $"Kullanıcı: {request.Username}, Attribute: {request.AttributeName}"
                    };
                    _context.ActivityLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // E-posta bildirimi
                    await _emailService.SendAttributeChangeNotificationAsync(
                        request.RequestedBy.Email,
                        request.Username,
                        request.AttributeName,
                        request.NewValue,
                        true);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error approving {requestType} request {requestId}");
                throw;
            }
        }

        public async Task<bool> RejectRequestAsync(int requestId, string requestType, string rejectionReason, string rejectedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
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

                if (requestType == "creation")
                {
                    var request = await _context.UserCreationRequests
                        .Include(r => r.RequestedBy)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null || request.StatusId != 1)
                        return false;

                    request.StatusId = 3; // Reddedildi
                    request.ApprovedById = rejectedBy.Id;
                    request.ApprovedDate = DateTime.Now;
                    request.RejectionReason = rejectionReason;
                    await _context.SaveChangesAsync();

                    // Log
                    var log = new ActivityLog
                    {
                        UserId = rejectedBy.Id,
                        Action = "Kullanıcı açma talebi reddedildi",
                        EntityType = "UserCreationRequest",
                        EntityId = request.Id,
                        Details = $"Kullanıcı: {request.Username}, Sebep: {rejectionReason}"
                    };
                    _context.ActivityLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // E-posta bildirimi
                    await _emailService.SendApprovalNotificationAsync(request.RequestedBy.Email, request.Username, false, rejectionReason);

                    return true;
                }
                else if (requestType == "deletion")
                {
                    var request = await _context.UserDeletionRequests
                        .Include(r => r.RequestedBy)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null || request.StatusId != 1)
                        return false;

                    request.StatusId = 3; // Reddedildi
                    request.ApprovedById = rejectedBy.Id;
                    request.ApprovedDate = DateTime.Now;
                    request.RejectionReason = rejectionReason;
                    await _context.SaveChangesAsync();

                    // Log
                    var log = new ActivityLog
                    {
                        UserId = rejectedBy.Id,
                        Action = "Kullanıcı kapatma talebi reddedildi",
                        EntityType = "UserDeletionRequest",
                        EntityId = request.Id,
                        Details = $"Kullanıcı: {request.Username}, Sebep: {rejectionReason}"
                    };
                    _context.ActivityLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // E-posta bildirimi
                    await _emailService.SendApprovalNotificationAsync(request.RequestedBy.Email, request.Username, false, rejectionReason);

                    return true;
                }
                else if (requestType == "attribute")
                {
                    var request = await _context.UserAttributeChangeRequests
                        .Include(r => r.RequestedBy)
                        .FirstOrDefaultAsync(r => r.Id == requestId);

                    if (request == null || request.StatusId != 1)
                        return false;

                    request.StatusId = 3; // Reddedildi
                    request.ApprovedById = rejectedBy.Id;
                    request.ApprovedDate = DateTime.Now;
                    request.RejectionReason = rejectionReason;
                    await _context.SaveChangesAsync();

                    // Log
                    var log = new ActivityLog
                    {
                        UserId = rejectedBy.Id,
                        Action = "Attribute değiştirme talebi reddedildi",
                        EntityType = "UserAttributeChangeRequest",
                        EntityId = request.Id,
                        Details = $"Kullanıcı: {request.Username}, Attribute: {request.AttributeName}, Sebep: {rejectionReason}"
                    };
                    _context.ActivityLogs.Add(log);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // E-posta bildirimi
                    await _emailService.SendAttributeChangeNotificationAsync(
                        request.RequestedBy.Email,
                        request.Username,
                        request.AttributeName,
                        request.NewValue,
                        false);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error rejecting {requestType} request {requestId}");
                throw;
            }
        }

        public async Task<PasswordResetRequest> GetPasswordResetRequestAsync(int id)
        {
            return await _context.PasswordResetRequests
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.Status)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        // RequestService.cs dosyasındaki GetMyRequestsAsync metodunu güncelle:

        public async Task<List<RequestListViewModel>> GetMyRequestsAsync(string username)
        {
            var user = await _context.SystemUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return new List<RequestListViewModel>();

            var creationRequests = await _context.UserCreationRequests
                .Include(r => r.Company)
                .Include(r => r.Status)
                .Where(r => r.RequestedById == user.Id)
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
                .Where(r => r.RequestedById == user.Id)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "deletion",
                    DisplayName = r.Username,
                    Username = r.Username,
                    Email = r.Email,
                    Company = null,
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            var attributeRequests = await _context.UserAttributeChangeRequests
                .Include(r => r.Status)
                .Where(r => r.RequestedById == user.Id)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "attribute",
                    DisplayName = r.Username,
                    Username = r.Username,
                    Email = null,
                    Company = r.AttributeName,
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            // Password reset requests
            var passwordRequests = await _context.PasswordResetRequests
                .Include(r => r.Status)
                .Where(r => r.RequestedById == user.Id)
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "password",
                    DisplayName = r.Username,
                    Username = r.Username,
                    Email = r.UserEmail,
                    Company = "Şifre Sıfırlama",
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            // Group membership requests - BUNU EKLEYİN
            var groupRequests = await _context.GroupMembershipRequests
                .Include(r => r.Status)
                .Where(r => r.RequestedById == user.Id)
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

            var allRequests = creationRequests
                .Concat(deletionRequests)
                .Concat(attributeRequests)
                .Concat(passwordRequests)
                .Concat(groupRequests)
                .OrderByDescending(r => r.RequestedDate)
                .ToList();

            return allRequests;
        }

        // GetPendingRequestsAsync metodunu da güncelle:

        public async Task<List<RequestListViewModel>> GetPendingRequestsAsync()
        {
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

            var deletionRequests = await _context.UserDeletionRequests
                .Include(r => r.Status)
                .Include(r => r.RequestedBy)
                .Where(r => r.StatusId == 1) // Beklemede
                .Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    RequestType = "deletion",
                    DisplayName = r.Username,
                    Username = r.Username,
                    Email = r.Email,
                    Company = null,
                    Status = r.Status.StatusName,
                    RequestedDate = r.RequestedDate,
                    RequestedBy = r.RequestedBy.DisplayName
                })
                .ToListAsync();

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
                    Company = null,
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

            
            var groupPendingRequests = await _context.GroupMembershipRequests
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
                .Concat(groupPendingRequests)
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
                    var parts = lastRequest.RequestNumber.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var lastCounter))
                    {
                        counter = lastCounter + 1;
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
                    var parts = lastRequest.RequestNumber.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var lastCounter))
                    {
                        counter = lastCounter + 1;
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
                    var parts = lastRequest.RequestNumber.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var lastCounter))
                    {
                        counter = lastCounter + 1;
                    }
                }
            }

            return $"{requestType}-{year}-{counter:D5}";
        }
    }
}