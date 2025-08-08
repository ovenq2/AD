namespace ADUserManagement.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int PendingCreationRequests { get; set; }
        public int PendingDeletionRequests { get; set; }
        public int ApprovedCreationRequests { get; set; }
        public int ApprovedDeletionRequests { get; set; }
        public int RejectedCreationRequests { get; set; }
        public int RejectedDeletionRequests { get; set; }
        public int TotalRequests { get; set; }
    }
}
