using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADUserManagement.Models.ViewModels
{
    public class DnsRequestViewModel
    {
        [Required(ErrorMessage = "Kayıt adı zorunludur")]
        [Display(Name = "Kayıt Adı (FQDN)")]
        [RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9-_\.]*$", ErrorMessage = "Geçersiz DNS kayıt adı")]
        public string RecordName { get; set; }

        [Required(ErrorMessage = "Kayıt tipi seçimi zorunludur")]
        [Display(Name = "Kayıt Tipi")]
        public string RecordType { get; set; }

        [Required(ErrorMessage = "Kayıt değeri zorunludur")]
        [Display(Name = "Kayıt Değeri")]
        public string RecordValue { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        public List<SelectListItem> RecordTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "A", Text = "A Kaydı (IP Adresi)" },
            new SelectListItem { Value = "CNAME", Text = "CNAME Kaydı (Alias)" }
        };
    }
}