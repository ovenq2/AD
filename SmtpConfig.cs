using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Configuration
{
    public class SmtpConfig
    {
        [Required]
        public string Server { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool EnableSsl { get; set; } = true;

        [Required]
        public string FromAddress { get; set; } = string.Empty;

        public string FromDisplayName { get; set; } = "AD User Management System";
    }
}