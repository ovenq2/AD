using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace ADUserManagement.Helpers
{
    public static class InputValidationHelper
    {
        // Username validation patterns
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);
        private static readonly Regex GroupNameRegex = new Regex(@"^[a-zA-Z0-9\s._-]+$", RegexOptions.Compiled);

        // Dangerous characters for LDAP injection prevention
        private static readonly char[] DangerousChars = { '(', ')', '*', '\\', '/', '\0' };

        /// <summary>
        /// Validates username format
        /// </summary>
        /// <param name="username">Username to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            // Remove domain part if exists (domain\username)
            var actualUsername = username.Contains("\\") ? username.Split('\\').LastOrDefault() : username;

            if (string.IsNullOrWhiteSpace(actualUsername))
                return false;

            // Check length constraints
            if (actualUsername.Length < 2 || actualUsername.Length > 20)
                return false;

            // Check for valid characters
            if (!UsernameRegex.IsMatch(actualUsername))
                return false;

            // Check for dangerous characters
            if (actualUsername.Any(c => DangerousChars.Contains(c)))
                return false;

            // Cannot start or end with dots or hyphens
            if (actualUsername.StartsWith(".") || actualUsername.EndsWith(".") ||
                actualUsername.StartsWith("-") || actualUsername.EndsWith("-"))
                return false;

            return true;
        }

        /// <summary>
        /// Validates group name format
        /// </summary>
        /// <param name="groupName">Group name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidGroupName(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return false;

            // Check length constraints
            if (groupName.Length < 2 || groupName.Length > 64)
                return false;

            // Check for valid characters (groups can have spaces)
            if (!GroupNameRegex.IsMatch(groupName))
                return false;

            // Check for dangerous characters
            if (groupName.Any(c => DangerousChars.Contains(c)))
                return false;

            // Cannot start or end with spaces
            if (groupName.StartsWith(" ") || groupName.EndsWith(" "))
                return false;

            return true;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Length check
            if (password.Length < 8 || password.Length > 128)
                return false;

            // Must contain at least one uppercase letter
            if (!password.Any(char.IsUpper))
                return false;

            // Must contain at least one lowercase letter
            if (!password.Any(char.IsLower))
                return false;

            // Must contain at least one digit
            if (!password.Any(char.IsDigit))
                return false;

            // Must contain at least one special character
            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
                return false;

            return true;
        }

        /// <summary>
        /// Sanitizes string input to prevent injection attacks
        /// </summary>
        /// <param name="input">Input string to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove null characters
            input = input.Replace("\0", "");

            // Remove or escape dangerous LDAP characters
            input = input.Replace("(", "\\(")
                        .Replace(")", "\\)")
                        .Replace("*", "\\*")
                        .Replace("\\", "\\\\");

            // Trim whitespace
            input = input.Trim();

            // Remove control characters
            input = new string(input.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());

            return input;
        }

        /// <summary>
        /// Validates phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true; // Phone is optional

            // Remove common formatting characters
            var cleanPhone = Regex.Replace(phoneNumber, @"[\s\-\(\)\+]", "");

            // Check if contains only digits
            if (!Regex.IsMatch(cleanPhone, @"^\d+$"))
                return false;

            // Check length (between 7 and 15 digits)
            if (cleanPhone.Length < 7 || cleanPhone.Length > 15)
                return false;

            return true;
        }

        /// <summary>
        /// Validates department name
        /// </summary>
        /// <param name="department">Department name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidDepartment(string department)
        {
            if (string.IsNullOrWhiteSpace(department))
                return true; // Department is optional

            // Check length
            if (department.Length > 100)
                return false;

            // Check for dangerous characters
            if (department.Any(c => DangerousChars.Contains(c)))
                return false;

            return true;
        }

        /// <summary>
        /// Validates title/job title
        /// </summary>
        /// <param name="title">Title to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return true; // Title is optional

            // Check length
            if (title.Length > 100)
                return false;

            // Check for dangerous characters
            if (title.Any(c => DangerousChars.Contains(c)))
                return false;

            return true;
        }

        /// <summary>
        /// Validates distinguished name (DN) format
        /// </summary>
        /// <param name="dn">Distinguished name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidDistinguishedName(string dn)
        {
            if (string.IsNullOrWhiteSpace(dn))
                return false;

            // Basic DN format check (should contain = and ,)
            if (!dn.Contains("="))
                return false;

            // Should not contain dangerous characters
            if (dn.Any(c => new char[] { '\0', '\n', '\r' }.Contains(c)))
                return false;

            return true;
        }

        /// <summary>
        /// Validates organizational unit path
        /// </summary>
        /// <param name="ouPath">OU path to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidOUPath(string ouPath)
        {
            if (string.IsNullOrWhiteSpace(ouPath))
                return true; // OU path is optional

            // Should start with OU= or CN=
            if (!ouPath.StartsWith("OU=", StringComparison.OrdinalIgnoreCase) &&
                !ouPath.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                return false;

            return IsValidDistinguishedName(ouPath);
        }
    }
}