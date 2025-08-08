using System;
using System.Collections.Generic;

namespace ADUserManagement.Models.Dto
{
    public class ADUserDetailsDto
    {
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Office { get; set; } = string.Empty;
        public string Manager { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime? LastLogon { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? PasswordLastSet { get; set; }
        public DateTime? AccountExpirationDate { get; set; }
        public List<string> Groups { get; set; } = new List<string>();
    }
}