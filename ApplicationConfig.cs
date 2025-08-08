using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Configuration
{
    public class ApplicationConfig
    {
        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string DefaultPassword { get; set; } = string.Empty;

        public List<string> AdminEmails { get; set; } = new();

        public PasswordPolicyConfig PasswordPolicy { get; set; } = new();
    }

    public class PasswordPolicyConfig
    {
        public int MinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireNumbers { get; set; } = true;
        public bool RequireSpecialCharacters { get; set; } = false;
    }
}