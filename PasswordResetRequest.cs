using System;
using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Domain
{
    public class PasswordResetRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string UserEmail { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Reason { get; set; }

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