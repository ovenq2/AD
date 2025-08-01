using System.ComponentModel.DataAnnotations;

namespace ADUserManagement.Models.ViewModels
{
    public class ApprovalViewModel
    {
        public int RequestId { get; set; }
        public string RequestType { get; set; }

        [Display(Name = "Red Gerek�esi")]
        [StringLength(500)]
        public string RejectionReason { get; set; }
    }
}