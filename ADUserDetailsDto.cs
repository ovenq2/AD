using System;

namespace ADUserManagement.Models.Dto
{
    public class ADUserDetailsDto
    {
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public string Office { get; set; }
        public string Manager { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime? LastLogon { get; set; }
        public DateTime? Created { get; set; }
    }
}