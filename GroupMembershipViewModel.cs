using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADUserManagement.Models.ViewModels
{
    public class GroupMembershipViewModel
    {
        [Required(ErrorMessage = "Kullanıcı seçimi zorunludur")]
        [Display(Name = "Kullanıcı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Grup seçimi zorunludur")]
        [Display(Name = "Grup Adı")]
        public string GroupName { get; set; }

        [Required(ErrorMessage = "İşlem tipi seçimi zorunludur")]
        [Display(Name = "İşlem Tipi")]
        public string ActionType { get; set; } = "Add";

        [Display(Name = "Sebep/Açıklama")]
        public string? Reason { get; set; }

        // UI için ek bilgiler
        public string? UserDisplayName { get; set; }
        public string? UserEmail { get; set; }
        public string? GroupDescription { get; set; }
        public string? GroupType { get; set; }

        public List<SelectListItem> ActionTypes { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Add", Text = "Gruba Ekle" },
            new SelectListItem { Value = "Remove", Text = "Gruptan Çıkar" }
        };
    }
}
