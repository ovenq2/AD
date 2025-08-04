using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.ViewModels
{
    public class PasswordResetViewModel
    {
        [Required(ErrorMessage = "Kullanıcı seçimi zorunludur")]
        [Display(Name = "Kullanıcı")]
        public string Username { get; set; }

        [Display(Name = "Kullanıcı E-posta")]
        public string UserEmail { get; set; }

        [Display(Name = "Sebep/Açıklama")]
        public string? Reason { get; set; }
    }
}