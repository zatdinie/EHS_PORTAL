using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EHS_PORTAL.Areas.CLIP.Models
{
    [Table("ActivityLogs", Schema = "CLIP")]
    public class ActivityLog
    {
        [Key]
        public int LogID { get; set; }
        
        public string UserID { get; set; }
        
        public string UserName { get; set; }
        
        [Required]
        public string Action { get; set; }
        
        public string Description { get; set; }
        
        public string EntityName { get; set; }
        
        public string EntityID { get; set; }
        
        public string OldValue { get; set; }
        
        public string NewValue { get; set; }
        
        public string IPAddress { get; set; }
        
        public string UserAgent { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string PageUrl { get; set; }
        
        public string SessionID { get; set; }
    }
} 