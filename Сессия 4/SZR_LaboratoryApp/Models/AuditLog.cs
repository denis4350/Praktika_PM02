using System;

namespace SZR_LaboratoryApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public string IpAddress { get; set; }
    }
}