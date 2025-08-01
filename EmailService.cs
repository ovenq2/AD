// Services/EmailService.cs - TAM DOSYA
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ADUserManagement.Data;
using ADUserManagement.Models.Dto;
using ADUserManagement.Models.Enums;
using ADUserManagement.Services.Interfaces;

namespace ADUserManagement.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _smtpServer = configuration["SmtpServer"];
            _smtpPort = int.Parse(configuration["SmtpPort"]);
            _smtpUsername = configuration["SmtpUsername"];
            _smtpPassword = configuration["SmtpPassword"];
            _enableSsl = bool.Parse(configuration["SmtpEnableSsl"]);
        }

        public async Task<bool> SendEmailAsync(EmailDto email)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = _enableSsl;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                    var message = new MailMessage
                    {
                        From = new MailAddress(_smtpUsername, "AD Kullanıcı Yönetim Sistemi"),
                        Subject = email.Subject,
                        Body = email.Body,
                        IsBodyHtml = email.IsHtml
                    };

                    message.To.Add(email.To);

                    await client.SendMailAsync(message);
                    _logger.LogInformation($"Email sent successfully to {email.To}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {email.To}");
                return false;
            }
        }

        public async Task SendNewRequestNotificationAsync(string requestType, string requestNumber, int requestId)
        {
            string subject = $"Yeni Talep - {requestNumber}";
            string requestTypeText = "bilinmeyen";
            string requestDetails = "";

            // Talep detaylarını veritabanından al
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    switch (requestType)
                    {
                        case "creation":
                            subject = $"Yeni Kullanıcı Açma Talebi - {requestNumber}";
                            requestTypeText = "kullanıcı açma";

                            var creationRequest = await context.UserCreationRequests
                                .Include(r => r.RequestedBy)
                                .Include(r => r.Company)
                                .FirstOrDefaultAsync(r => r.Id == requestId);

                            if (creationRequest != null)
                            {
                                requestDetails = $@"
                                    <table style='margin: 20px 0; border-collapse: collapse;'>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Açılacak Kullanıcı:</td>
                                            <td style='padding: 8px;'>{creationRequest.FirstName} {creationRequest.LastName} ({creationRequest.Username})</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>E-posta:</td>
                                            <td style='padding: 8px;'>{creationRequest.Email}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Birim:</td>
                                            <td style='padding: 8px;'>{creationRequest.Department}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Unvan:</td>
                                            <td style='padding: 8px;'>{creationRequest.Title}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Şirket:</td>
                                            <td style='padding: 8px;'>{creationRequest.Company?.CompanyName}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Talebi Açan:</td>
                                            <td style='padding: 8px;'>{creationRequest.RequestedBy?.DisplayName} ({creationRequest.RequestedBy?.Username})</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Talep Zamanı:</td>
                                            <td style='padding: 8px;'>{creationRequest.RequestedDate:dd.MM.yyyy HH:mm}</td>
                                        </tr>
                                    </table>";
                            }
                            break;

                        case "group":
                            subject = $"Yeni Grup Üyelik Talebi - {requestNumber}";
                            requestTypeText = "grup üyelik";

                            var groupRequest = await context.GroupMembershipRequests
                                .Include(r => r.RequestedBy)
                                .FirstOrDefaultAsync(r => r.Id == requestId);

                            if (groupRequest != null)
                            {
                                var actionText = groupRequest.ActionType == "Add" ? "Gruba Ekleme" : "Gruptan Çıkarma";
                                var actionIcon = groupRequest.ActionType == "Add" ? "➕" : "➖";

                                requestDetails = $@"
            <table style='margin: 20px 0; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Kullanıcı:</td>
                    <td style='padding: 8px;'><strong>{groupRequest.Username}</strong></td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Grup Adı:</td>
                    <td style='padding: 8px;'><strong>{groupRequest.GroupName}</strong></td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>İşlem Tipi:</td>
                    <td style='padding: 8px;'>{actionIcon} <strong>{actionText}</strong></td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Talebi Açan:</td>
                    <td style='padding: 8px;'>{groupRequest.RequestedBy?.DisplayName} ({groupRequest.RequestedBy?.Username})</td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Talep Zamanı:</td>
                    <td style='padding: 8px;'>{groupRequest.RequestedDate:dd.MM.yyyy HH:mm}</td>
                </tr>
                {(string.IsNullOrEmpty(groupRequest.Reason) ? "" :
                                            $@"<tr>
                        <td style='padding: 8px; font-weight: bold;'>Sebep/Açıklama:</td>
                        <td style='padding: 8px;'>{groupRequest.Reason}</td>
                    </tr>")}
            </table>
            
            <div style='background-color: #e7f3ff; border: 1px solid #b3d9ff; color: #0056b3; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                <p style='margin: 0 0 10px 0;'><strong>👥 ÖNEMLİ BİLGİ:</strong></p>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Bu talep onaylandığında kullanıcı <strong>{groupRequest.Username}</strong> 
                        {(groupRequest.ActionType == "Add" ?
                                                    $"<strong>{groupRequest.GroupName}</strong> grubuna eklenecektir." :
                                                    $"<strong>{groupRequest.GroupName}</strong> grubundan çıkarılacaktır.")}</li>
                    <li>Grup üyelik değişiklikleri anında uygulanır ve kullanıcının yetkileri değişebilir.</li>
                    <li>İşlem Active Directory üzerinde gerçekleştirilecektir.</li>
                    <li>Kullanıcı ve talep sahibi işlem sonucundan haberdar edilecektir.</li>
                </ul>
            </div>";
                            }
                            break;

                        case "deletion":
                            subject = $"Yeni Kullanıcı Kapatma Talebi - {requestNumber}";
                            requestTypeText = "kullanıcı kapatma";

                            var deletionRequest = await context.UserDeletionRequests
                                .Include(r => r.RequestedBy)
                                .FirstOrDefaultAsync(r => r.Id == requestId);

                            if (deletionRequest != null)
                            {
                                requestDetails = $@"
                                    <table style='margin: 20px 0; border-collapse: collapse;'>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Kapatılacak Kullanıcı:</td>
                                            <td style='padding: 8px;'>{deletionRequest.Username}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>E-posta:</td>
                                            <td style='padding: 8px;'>{deletionRequest.Email}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Talebi Açan:</td>
                                            <td style='padding: 8px;'>{deletionRequest.RequestedBy?.DisplayName} ({deletionRequest.RequestedBy?.Username})</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Talep Zamanı:</td>
                                            <td style='padding: 8px;'>{deletionRequest.RequestedDate:dd.MM.yyyy HH:mm}</td>
                                        </tr>
                                    </table>";
                            }
                            break;

                        case "password":
                            subject = $"Yeni Şifre Sıfırlama Talebi - {requestNumber}";
                            requestTypeText = "şifre sıfırlama";

                            var passwordRequest = await context.PasswordResetRequests
                                .Include(r => r.RequestedBy)
                                .FirstOrDefaultAsync(r => r.Id == requestId);

                            if (passwordRequest != null)
                            {
                                requestDetails = $@"
            <table style='margin: 20px 0; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Şifresi Sıfırlanacak Kullanıcı:</td>
                    <td style='padding: 8px;'><strong>{passwordRequest.Username}</strong></td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Kullanıcı E-posta:</td>
                    <td style='padding: 8px;'>{passwordRequest.UserEmail}</td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Talebi Açan:</td>
                    <td style='padding: 8px;'>{passwordRequest.RequestedBy?.DisplayName} ({passwordRequest.RequestedBy?.Username})</td>
                </tr>
                <tr>
                    <td style='padding: 8px; font-weight: bold;'>Talep Zamanı:</td>
                    <td style='padding: 8px;'>{passwordRequest.RequestedDate:dd.MM.yyyy HH:mm}</td>
                </tr>
                {(string.IsNullOrEmpty(passwordRequest.Reason) ? "" :
                                            $@"<tr>
                        <td style='padding: 8px; font-weight: bold;'>Sebep/Açıklama:</td>
                        <td style='padding: 8px;'>{passwordRequest.Reason}</td>
                    </tr>")}
            </table>
            
            <div style='background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                <p style='margin: 0 0 10px 0;'><strong>🔐 ÖNEMLİ BİLGİ:</strong></p>
                <ul style='margin: 5px 0; padding-left: 20px;'>
                    <li>Bu talep onaylandığında kullanıcının şifresi otomatik olarak sıfırlanacaktır.</li>
                    <li>Yeni şifre <strong>{passwordRequest.UserEmail}</strong> adresine gönderilecektir.</li>
                    <li>Kullanıcı ilk girişte yeni şifresini değiştirmeye zorlanacaktır.</li>
                    <li>Güvenlik için şifre sıfırlama işlemi hemen uygulanacaktır.</li>
                </ul>
            </div>";
                            }
                            break;

                        case "attribute":
                            subject = $"Yeni Attribute Değiştirme Talebi - {requestNumber}";
                            requestTypeText = "attribute değiştirme";

                            var attributeRequest = await context.UserAttributeChangeRequests
                                .Include(r => r.RequestedBy)
                                .FirstOrDefaultAsync(r => r.Id == requestId);

                            if (attributeRequest != null)
                            {
                                var friendlyAttributeName = ADAttributes.UserAttributes.ContainsKey(attributeRequest.AttributeName)
                                    ? ADAttributes.UserAttributes[attributeRequest.AttributeName]
                                    : attributeRequest.AttributeName;

                                requestDetails = $@"
                                    <table style='margin: 20px 0; border-collapse: collapse;'>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Kullanıcı:</td>
                                            <td style='padding: 8px;'>{attributeRequest.Username}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Değiştirilecek Alan:</td>
                                            <td style='padding: 8px;'>{friendlyAttributeName}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Mevcut Değer:</td>
                                            <td style='padding: 8px;'>{(string.IsNullOrEmpty(attributeRequest.OldValue) ? "Boş" : attributeRequest.OldValue)}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Yeni Değer:</td>
                                            <td style='padding: 8px;'><strong>{attributeRequest.NewValue}</strong></td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Talebi Açan:</td>
                                            <td style='padding: 8px;'>{attributeRequest.RequestedBy?.DisplayName} ({attributeRequest.RequestedBy?.Username})</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px; font-weight: bold;'>Talep Zamanı:</td>
                                            <td style='padding: 8px;'>{attributeRequest.RequestedDate:dd.MM.yyyy HH:mm}</td>
                                        </tr>
                                        {(string.IsNullOrEmpty(attributeRequest.ChangeReason) ? "" :
                                            $@"<tr>
                                                <td style='padding: 8px; font-weight: bold;'>Değişiklik Sebebi:</td>
                                                <td style='padding: 8px;'>{attributeRequest.ChangeReason}</td>
                                            </tr>")}
                                    </table>";
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request details for email");
            }

            var baseUrl = _configuration["BaseUrl"];
            var approvalUrl = $"{baseUrl}/Admin/RequestDetails/{requestId}?type={requestType}";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 10px;'>
                            <h2 style='color: #333; margin-top: 0;'>{subject}</h2>
                            
                            <div style='background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                <p>Merhaba,</p>
                                <p>Yeni bir <strong>{requestTypeText}</strong> talebi oluşturuldu.</p>
                                
                                <div style='background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                                    <p style='margin: 5px 0;'><strong>Talep Numarası:</strong> <code style='background-color: #fff; padding: 2px 5px; border-radius: 3px;'>{requestNumber}</code></p>
                                </div>
                                
                                <h3 style='color: #495057; margin-top: 25px;'>Talep Detayları:</h3>
                                {requestDetails}
                                
                                <div style='text-align: center; margin: 30px 0;'>
                                    <a href='{approvalUrl}' style='display: inline-block; padding: 12px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                        Talebi Görüntüle ve İşlem Yap
                                    </a>
                                </div>
                                
                                <hr style='border: none; border-top: 1px solid #dee2e6; margin: 20px 0;'>
                                
                                <p style='color: #6c757d; font-size: 14px;'>
                                    Bu e-posta AD Kullanıcı Yönetim Sistemi tarafından otomatik olarak gönderilmiştir.<br>
                                    Talep hakkında detaylı bilgi almak için sistem üzerinden işlem yapabilirsiniz.
                                </p>
                            </div>
                        </div>
                        
                        <p style='text-align: center; color: #6c757d; font-size: 12px; margin-top: 20px;'>
                            AD Kullanıcı Yönetim Sistemi © 2024
                        </p>
                    </div>
                </body>
                </html>";

            var sysnetEmail = _configuration["SysNetEmail"];
            await SendEmailAsync(new EmailDto
            {
                To = sysnetEmail,
                Subject = subject,
                Body = body,
                IsHtml = true
            });
        }

        public async Task SendApprovalNotificationAsync(string toEmail, string username, bool isApproved, string reason = null)
        {
            var subject = isApproved
                ? $"Talebiniz Onaylandı - {username}"
                : $"Talebiniz Reddedildi - {username}";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>{subject}</h2>
                    <p>Merhaba,</p>
                    <p>{username} kullanıcısı için açtığınız talep <strong>{(isApproved ? "onaylanmıştır" : "reddedilmiştir")}</strong>.</p>
                    {(isApproved
                        ? "<p>İşlem başarıyla tamamlanmıştır.</p>"
                        : $"<p>Red Gerekçesi: <strong>{reason}</strong></p>")}
                    <p>Saygılarımızla,<br/>AD Kullanıcı Yönetim Sistemi</p>
                </body>
                </html>";

            await SendEmailAsync(new EmailDto
            {
                To = toEmail,
                Subject = subject,
                Body = body,
                IsHtml = true
            });
        }

        public async Task SendNewUserCredentialsAsync(string toEmail, string username, string password)
        {
            var subject = "Yeni Kullanıcı Hesabınız Oluşturuldu";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>{subject}</h2>
                    <p>Merhaba,</p>
                    <p>Active Directory hesabınız başarıyla oluşturulmuştur. Giriş bilgileriniz aşağıdadır:</p>
                    <table style='border-collapse: collapse; margin: 20px 0; border: 1px solid #ddd;'>
                        <tr>
                            <td style='padding: 10px; font-weight: bold; background-color: #f5f5f5; border: 1px solid #ddd;'>Kullanıcı Adı:</td>
                            <td style='padding: 10px; font-family: monospace; font-size: 14px; border: 1px solid #ddd;'>{username}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; font-weight: bold; background-color: #f5f5f5; border: 1px solid #ddd;'>Geçici Şifre:</td>
                            <td style='padding: 10px; font-family: monospace; font-size: 14px; background-color: #fffbcc; border: 1px solid #ddd;'><strong>{password}</strong></td>
                        </tr>
                    </table>
                    <div style='background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0 0 10px 0;'><strong>⚠️ ÖNEMLİ UYARILAR:</strong></p>
                        <ul style='margin: 5px 0;'>
                            <li>İlk girişinizde şifrenizi değiştirmeniz <strong>zorunludur</strong>.</li>
                            <li>Şifreniz en az 12 karakter uzunluğunda olmalıdır.</li>
                            <li>Şifreniz büyük harf, küçük harf, rakam ve özel karakter içermelidir.</li>
                            <li>Bu e-postayı güvenli bir yerde saklayın veya şifrenizi not alın.</li>
                        </ul>
                    </div>
                    <p>Herhangi bir sorun yaşamanız durumunda IT departmanı ile iletişime geçebilirsiniz.</p>
                    <p>Saygılarımızla,<br/>IT Departmanı</p>
                </body>
                </html>";

            await SendEmailAsync(new EmailDto
            {
                To = toEmail,
                Subject = subject,
                Body = body,
                IsHtml = true
            });
        }

        public async Task SendAttributeChangeNotificationAsync(string toEmail, string username, string attributeName, string newValue, bool isApproved)
        {
            var friendlyAttributeName = ADAttributes.UserAttributes.ContainsKey(attributeName)
                ? ADAttributes.UserAttributes[attributeName]
                : attributeName;

            var subject = isApproved
                ? $"Attribute Değiştirme Talebiniz Onaylandı - {username}"
                : $"Attribute Değiştirme Talebiniz Reddedildi - {username}";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>{subject}</h2>
                    <p>Merhaba,</p>
                    <p>{username} kullanıcısı için açtığınız attribute değiştirme talebi <strong>{(isApproved ? "onaylanmıştır" : "reddedilmiştir")}</strong>.</p>
                    {(isApproved
                        ? $@"<table style='border-collapse: collapse; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 10px; font-weight: bold;'>Kullanıcı:</td>
                                    <td style='padding: 10px;'>{username}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; font-weight: bold;'>Değiştirilen Alan:</td>
                                    <td style='padding: 10px;'>{friendlyAttributeName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; font-weight: bold;'>Yeni Değer:</td>
                                    <td style='padding: 10px;'>{newValue}</td>
                                </tr>
                            </table>
                            <p>Değişiklik Active Directory üzerinde başarıyla uygulanmıştır.</p>"
                        : "")}
                    <p>Saygılarımızla,<br/>AD Kullanıcı Yönetim Sistemi</p>
                </body>
                </html>";

            await SendEmailAsync(new EmailDto
            {
                To = toEmail,
                Subject = subject,
                Body = body,
                IsHtml = true
            });
        }
    }
}