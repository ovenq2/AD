using System;

namespace ADUserManagement.Models.Domain
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual SystemUser User { get; set; }
    }
}