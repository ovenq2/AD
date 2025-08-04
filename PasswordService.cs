using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.Domain;
using ADUserManagement.Models.ViewModels;
using ADUserManagement.Models.Dto;
using ADUserManagement.Services.Interfaces;
using ADUserManagement.Constants;

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
                // Input validation
                if (!InputValidationHelper.IsValidUsername(model.Username))
                {
                    throw new ArgumentException("Geçersiz kullanıcı adı formatı");
                }

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
                    Reason = InputValidationHelper.SanitizeString(model.Reason),
                    RequestedById = requestedBy.Id,
                    StatusId = 1, // Beklemede
                    RequestedDate = DateTime.Now
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
                    Details = $"Kullanıcı: {model.Username}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // E-posta bildirimi gönder
                try
                {
                    await _emailService.SendNewRequestNotificationAsync("password", requestNumber, request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email notification failed for request {RequestId}", request.Id);
                }

                _logger.LogInformation(LogMessages.PasswordResetRequestCreated, request.Id, model.Username, requestedByUsername);
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
                    .FirstOrDefaultAsync(r => r.Id == requestId && r.StatusId == 1);

                if (request == null)
                {
                    _logger.LogWarning($"Password reset request {requestId} not found or already processed");
                    return false;
                }

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

                // AD'de şifreyi sıfırla
                var resetSuccess = await ResetUserPasswordAsync(request.Username, newPassword);
                if (!resetSuccess)
                {
                    throw new Exception("Active Directory şifre sıfırlama işlemi başarısız oldu");
                }

                // Bir sonraki girişte şifre değiştirmeye zorla
                await ForcePasswordChangeAtNextLogonAsync(request.Username);

                // Talebi onayla
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
                    Details = $"Kullanıcı: {request.Username}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Kullanıcıya yeni şifre ile e-posta gönder
                try
                {
                    var emailDto = new EmailDto
                    {
                        To = request.UserEmail,
                        Subject = "Şifreniz Sıfırlandı - AD Kullanıcı Yönetim Sistemi",
                        Body = $@"
                            <html>
                            <body style='font-family: Arial, sans-serif;'>
                                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                    <h2 style='color: #28a745;'>✅ Şifreniz Başarıyla Sıfırlandı</h2>
                                    
                                    <p>Merhaba <strong>{request.Username}</strong>,</p>
                                    
                                    <p>Sistem yöneticisi tarafından şifreniz sıfırlanmıştır. Yeni şifreniz:</p>
                                    
                                    <div style='background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                        <strong style='font-family: monospace; font-size: 16px; color: #dc3545;'>{newPassword}</strong>
                                    </div>
                                    
                                    <p><strong>⚠️ UYARI:</strong> İlk giriş yaptığınızda şifrenizi değiştirmeniz istenecektir.</p>
                                    
                                    <p>Sisteme giriş: <a href='{_configuration["BaseUrl"]}/Account/Login'>Buraya Tıklayın</a></p>
                                </div>
                            </body>
                            </html>",
                        IsHtml = true
                    };

                    await _emailService.SendEmailAsync(emailDto);
                    _logger.LogInformation($"Password reset confirmation email sent to {request.Username} at {request.UserEmail}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send password reset email to user {Username}", request.Username);
                }

                _logger.LogInformation(LogMessages.PasswordResetRequestApproved, requestId, request.Username, approvedByUsername);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving password reset request");
                return false;
            }
        }

        public async Task<bool> RejectPasswordResetRequestAsync(int requestId, string rejectionReason, string rejectedByUsername)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.PasswordResetRequests
                    .FirstOrDefaultAsync(r => r.Id == requestId && r.StatusId == 1);

                if (request == null)
                {
                    _logger.LogWarning($"Password reset request {requestId} not found or already processed");
                    return false;
                }

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

                // Talebi reddet
                request.StatusId = 3; // Reddedildi
                request.ApprovedById = rejectedBy.Id;
                request.ApprovedDate = DateTime.Now;
                request.RejectionReason = InputValidationHelper.SanitizeString(rejectionReason);

                await _context.SaveChangesAsync();

                // Activity log
                var log = new ActivityLog
                {
                    UserId = rejectedBy.Id,
                    Action = "Şifre sıfırlama talebi reddedildi",
                    EntityType = "PasswordResetRequest",
                    EntityId = request.Id,
                    Details = $"Kullanıcı: {request.Username}, Sebep: {rejectionReason}",
                    CreatedDate = DateTime.Now
                };
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation($"Password reset rejected for user {request.Username}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting password reset request");
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(string username, string newPassword)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning($"Invalid username format: {username}");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length > 128)
                    {
                        _logger.LogWarning("Invalid password format");
                        return false;
                    }

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
                    // Input validation
                    if (!InputValidationHelper.IsValidUsername(username))
                    {
                        _logger.LogWarning($"Invalid username format: {username}");
                        return false;
                    }

                    using (var context = new PrincipalContext(ContextType.Domain, _domain, _serviceAccount, _servicePassword))
                    {
                        using (var user = UserPrincipal.FindByIdentity(context, username))
                        {
                            if (user != null)
                            {
                                user.ExpirePasswordNow();
                                user.Save();
                                _logger.LogInformation($"Password expiry set for user {username}");
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
                    _logger.LogError(ex, $"Error setting password expiry for user {username}");
                    return false;
                }
            });
        }

        private async Task<string> GenerateRequestNumberAsync()
        {
            var prefix = "PWD";
            var date = DateTime.Now.ToString("yyyyMMdd");

            var lastRequest = await _context.PasswordResetRequests
                .Where(r => r.RequestNumber.StartsWith($"{prefix}{date}"))
                .OrderBy(r => r.RequestNumber)
                .LastOrDefaultAsync();

            int sequence = 1;
            if (lastRequest != null)
            {
                var lastSequence = lastRequest.RequestNumber.Substring(11); // PWDyyyyMMdd sonrası
                if (int.TryParse(lastSequence, out int parsed))
                {
                    sequence = parsed + 1;
                }
            }

            return $"{prefix}{date}{sequence:D3}";
        }
    }
}