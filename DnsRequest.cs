using System;
using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Domain
{
    public class DnsRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string RecordName { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string RecordType { get; set; } = string.Empty; // A veya CNAME

        [Required]
        [StringLength(255)]
        public string RecordValue { get; set; } = string.Empty; // IP adresi veya CNAME hedefi

        [StringLength(500)]
        public string? Description { get; set; }

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