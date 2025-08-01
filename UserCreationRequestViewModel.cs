using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADUserManagement.Models.ViewModels
{
    public class UserCreationRequestViewModel
    {
        [Required(ErrorMessage = "İsim alanı zorunludur")]
        [Display(Name = "İsim")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyisim alanı zorunludur")]
        [Display(Name = "Soyisim")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Lokasyon bilgisi zorunludur")]
        [Display(Name = "Lokasyon")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Birim bilgisi zorunludur")]
        [Display(Name = "Birim")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Unvan bilgisi zorunludur")]
        [Display(Name = "Unvan")]
        public string Title { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Şirket seçimi zorunludur")]
        [Display(Name = "Şirket")]
        public int CompanyId { get; set; }

        // Bu property POST işleminde bind edilmeyecek, sadece view için
        public List<SelectListItem>? Companies { get; set; }
    }
}