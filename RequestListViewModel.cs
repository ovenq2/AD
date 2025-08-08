using System;

namespace ADUserManagement.Models.ViewModels
{
    public class RequestListViewModel
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; }
        public string RequestType { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Status { get; set; }
        public string StatusClass { get; set; }
        public DateTime RequestedDate { get; set; }
        public string RequestedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApprovedBy { get; set; }
    }
}
