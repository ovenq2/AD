using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.Domain;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActiveDirectoryService _adService;
        private readonly IEmailService _emailService;
        private readonly IPasswordGeneratorService _passwordGenerator;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordService> _logger;
        private readonly string _domain;
        private readonly string _serviceAccount;
        private readonly string _servicePassword;

        public PasswordService(
            ApplicationDbContext context,
            IActiveDirectoryService adService,
            IEmailService emailService,
            IPasswordGeneratorService passwordGenerator,
            IConfiguration configuration,
            ILogger<PasswordService> logger)
        {
            _context = context;
            _adService = adService;
            _emailService = emailService;
            _passwordGenerator = passwordGenerator;
            _configuration = configuration;
            _logger = logger;
            _domain = "paramtech.local";
            _serviceAccount = configuration["ADServiceAccount"];
            _servicePassword = configuration["ADServicePassword"];
        }

        public async Task<int> CreatePasswordResetRequestAsync(PasswordResetViewModel model, string requestedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Kullanıcının var olduğunu kontrol et
                var userExists = await _adService.UserExistsAsync(model.Username);
                if (!userExists)
                {
                    throw new Exception($"Kullanıcı '{model.Username}' Active Directory'de bulunamadı.");
                }

                // Kullanıcı bilgilerini AD'den al
                var userDetails = await _adService.GetUserDetailsAsync(model.Username);
                if (userDetails == null || string.IsNullOrEmpty(userDetails.Email))
                {
                    throw new Exception($"Kullanıcının e-posta adresi bulunamadı.");
                }

                // Talep eden kullanıcıyı al veya oluştur
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
                var requestNumber = await GenerateRequestNumberAsync();

                // Talebi oluştur
                var request = new PasswordResetRequest
                {
                    RequestNumber = requestNumber,
                    Username = model.Username,
                    UserEmail = userDetails.Email,
                    Reason = model.Reason,
                    RequestedById = requestedBy.Id,
                    StatusId = 1 // Beklemede
                };

                _context.PasswordResetRequests.Add(request);
                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = requestedBy.Id,
                    Action = "Şifre sıfırlama talebi oluşturuldu",
                    EntityType = "PasswordResetRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {model.Username}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // E-posta bildirimi gönder
                await _emailService.SendNewRequestNotificationAsync("password", requestNumber, request.Id);

                _logger.LogInformation($"Password reset request created for user {model.Username}");
                return request.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating password reset request");
                throw;
            }
        }

        public async Task<bool> ApprovePasswordResetRequestAsync(int requestId, string approvedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.PasswordResetRequests
                    .Include(r => r.RequestedBy)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null || request.StatusId != 1)
                    return false;

                // Onaylayan kullanıcıyı al
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

                // Yeni şifre oluştur
                var newPassword = _passwordGenerator.GeneratePassword(12);
                _logger.LogInformation($"Generated new password for user {request.Username}");

                // AD'de şifreyi sıfırla
                var resetResult = await ResetUserPasswordAsync(request.Username, newPassword);
                if (!resetResult)
                {
                    _logger.LogError($"Failed to reset password for user {request.Username} in AD");
                    return false;
                }

                // İlk girişte şifre değiştirmeye zorla
                await ForcePasswordChangeAtNextLogonAsync(request.Username);

                // Talebi güncelle
                request.StatusId = 2; // Onaylandı
                request.ApprovedById = approvedBy.Id;
                request.ApprovedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = approvedBy.Id,
                    Action = "Şifre sıfırlama talebi onaylandı",
                    EntityType = "PasswordResetRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Kullanıcıya yeni şifreyi e-posta ile gönder
                await _emailService.SendNewUserCredentialsAsync(request.UserEmail, request.Username, newPassword);

                // Talep edene bildirim gönder
                await _emailService.SendApprovalNotificationAsync(
                    request.RequestedBy.Email,
                    request.Username,
                    true,
                    "Şifre sıfırlama işlemi tamamlandı. Yeni şifre kullanıcıya e-posta ile gönderildi.");

                _logger.LogInformation($"Password reset approved for user {request.Username}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error approving password reset request {requestId}");
                throw;
            }
        }

        public async Task<bool> RejectPasswordResetRequestAsync(int requestId, string rejectionReason, string rejectedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.PasswordResetRequests
                    .Include(r => r.RequestedBy)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null || request.StatusId != 1)
                    return false;

                // Reddeden kullanıcıyı al
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

                // Talebi güncelle
                request.StatusId = 3; // Reddedildi
                request.ApprovedById = rejectedBy.Id;
                request.ApprovedDate = DateTime.Now;
                request.RejectionReason = rejectionReason;
                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = rejectedBy.Id,
                    Action = "Şifre sıfırlama talebi reddedildi",
                    EntityType = "PasswordResetRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}, Sebep: {rejectionReason}"
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Talep edene bildirim gönder
                await _emailService.SendApprovalNotificationAsync(
                    request.RequestedBy.Email,
                    request.Username,
                    false,
                    rejectionReason);

                _logger.LogInformation($"Password reset rejected for user {request.Username}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error rejecting password reset request {requestId}");
                throw;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(string username, string newPassword)
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
                                user.SetPassword(newPassword);
                                user.Save();
                                _logger.LogInformation($"Password reset successfully for user {username}");
                                return true;
                            }
                            else
                            {
                                _logger.LogWarning($"User {username} not found in AD for password reset");
                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error resetting password for user {username}");
                    return false;
                }
            });
        }

        public async Task<bool> ForcePasswordChangeAtNextLogonAsync(string username)
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
                                user.ExpirePasswordNow();
                                user.Save();
                                _logger.LogInformation($"User {username} will be forced to change password at next logon");
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
                    _logger.LogError(ex, $"Error forcing password change for user {username}");
                    return false;
                }
            });
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var year = DateTime.Now.Year.ToString();
            var lastRequest = await _context.PasswordResetRequests
                .Where(r => r.RequestNumber.StartsWith($"PWD-{year}"))
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
            {
                return $"PWD-{year}-00001";
            }

            var parts = lastRequest.RequestNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastCounter))
            {
                return $"PWD-{year}-{(lastCounter + 1):D5}";
            }

            return $"PWD-{year}-00001";
        }
    }
}