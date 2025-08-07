using System.Text.RegularExpressions;

namespace ADUserManagement.Services
{
    public static class InputValidationHelper
    {
        // AD Username validation - LDAP injection koruması
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            // Tehlikeli karakterleri kontrol et
            var dangerousChars = new[] { "(", ")", "*", "\\", "/", "+", "<", ">", "\"", "'", ";", "=", ",", "#" };
            if (dangerousChars.Any(username.Contains))
                return false;

            // Regex pattern: alfanumerik, nokta, tire, underscore
            var regex = new Regex(@"^[a-zA-Z0-9._-]+$");
            return regex.IsMatch(username) && username.Length <= 64;
        }

        // Email validation güçlendirilmiş
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Length <= 254;
            }
            catch
            {
                return false;
            }
        }

        // Group name validation
        public static bool IsValidGroupName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return false;

            // LDAP injection karakterlerini kontrol et
            var dangerousChars = new[] { "(", ")", "*", "\\", "/", "+", "<", ">", "\"", "'", ";", "=", ",", "#", "|" };
            if (dangerousChars.Any(groupName.Contains))
                return false;

            var regex = new Regex(@"^[a-zA-Z0-9\s._-]+$");
            return regex.IsMatch(groupName) && groupName.Length <= 64;
        }

        // Genel string temizleme
        public static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Tehlikeli karakterleri kaldır
            var dangerous = new[] { "<", ">", "\"", "'", "&", ";", "(", ")", "*", "+", "=", "%", "#" };
            var sanitized = input;

            foreach (var character in dangerous)
            {
                sanitized = sanitized.Replace(character, "");
            }

            return sanitized.Trim();
        }

        // LDAP özel karakterleri escape et
        public static string EscapeLdapString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input
                .Replace("\\", "\\5c")
                .Replace("*", "\\2a")
                .Replace("(", "\\28")
                .Replace(")", "\\29")
                .Replace("\0", "\\00");
        }
    }
}