using System;
using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.Domain
{
    public class UserCreationRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "İsim alanı zorunludur")]
        [StringLength(50)]
        [Display(Name = "İsim")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyisim alanı zorunludur")]
        [StringLength(50)]
        [Display(Name = "Soyisim")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [StringLength(50)]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [StringLength(100)]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarası zorunludur")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lokasyon bilgisi zorunludur")]
        [StringLength(100)]
        [Display(Name = "Lokasyon")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birim bilgisi zorunludur")]
        [StringLength(100)]
        [Display(Name = "Birim")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unvan bilgisi zorunludur")]
        [StringLength(100)]
        [Display(Name = "Unvan")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; } // Nullable

        [Required(ErrorMessage = "Şirket seçimi zorunludur")]
        [Display(Name = "Şirket")]
        public int CompanyId { get; set; }

        public int RequestedById { get; set; }
        public DateTime RequestedDate { get; set; } = DateTime.Now;
        public int StatusId { get; set; } = 1; // Beklemede
        public int? ApprovedById { get; set; } // Nullable
        public DateTime? ApprovedDate { get; set; } // Nullable
        public string? RejectionReason { get; set; } // Nullable

        // Navigation properties
        public virtual Company? Company { get; set; }
        public virtual SystemUser? RequestedBy { get; set; }
        public virtual SystemUser? ApprovedBy { get; set; }
        public virtual RequestStatus? Status { get; set; }
    }
}