using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.ViewModels
{
    public class DhcpReservationViewModel
    {
        [Required(ErrorMessage = "Cihaz adı zorunludur")]
        [Display(Name = "Cihaz Adı")]
        public string DeviceName { get; set; }

        [Required(ErrorMessage = "MAC adresi zorunludur")]
        [Display(Name = "MAC Adresi")]
        [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
            ErrorMessage = "Geçerli bir MAC adresi giriniz (örn: 00:11:22:33:44:55)")]
        public string MacAddress { get; set; }

        [Required(ErrorMessage = "IP adresi zorunludur")]
        [Display(Name = "Talep Edilen IP Adresi")]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            ErrorMessage = "Geçerli bir IP adresi giriniz")]
        public string RequestedIpAddress { get; set; }

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
    }
}