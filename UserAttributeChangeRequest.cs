using System;
using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Domain
{
    public class UserAttributeChangeRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string AttributeName { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        [Required]
        public string NewValue { get; set; } = string.Empty;

        public string? ChangeReason { get; set; }

        public int RequestedById { get; set; }
        public DateTime RequestedDate { get; set; } = DateTime.Now;
        public int StatusId { get; set; } = 1; // Beklemede
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }

        // Navigation properties
        public virtual SystemUser? RequestedBy { get; set; }
        public virtual SystemUser? ApprovedBy { get; set; }
        public virtual RequestStatus? Status { get; set; }
    }
}