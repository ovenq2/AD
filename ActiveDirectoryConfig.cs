using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Configuration
{
    public class ActiveDirectoryConfig
    {
        [Required]
        public string Domain { get; set; } = string.Empty;

        [Required]
        public string ServiceAccount { get; set; } = string.Empty;

        public string ServicePassword { get; set; } = string.Empty;

        public string LdapPath { get; set; } = string.Empty;

        public string SearchBase { get; set; } = string.Empty;

        [Required]
        public List<string> AuthorizedGroups { get; set; } = new();

        public string DefaultOU { get; set; } = string.Empty;

        public int ConnectionTimeout { get; set; } = 30;

        public bool EnableSSL { get; set; } = false;
    }
}
