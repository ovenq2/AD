using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADUserManagement.Models.ViewModels
{
    public class UserAttributeChangeViewModel
    {
        [Required(ErrorMessage = "Kullanıcı seçimi zorunludur")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Değiştirilecek alan seçimi zorunludur")]
        [Display(Name = "Değiştirilecek Alan")]
        public string AttributeName { get; set; }

        [Display(Name = "Mevcut Değer")]
        public string? OldValue { get; set; }

        [Required(ErrorMessage = "Yeni değer zorunludur")]
        [Display(Name = "Yeni Değer")]
        public string NewValue { get; set; }

        [Display(Name = "Değişiklik Sebebi")]
        public string? ChangeReason { get; set; }

        public List<SelectListItem>? AttributeOptions { get; set; }
    }
}