using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EHS_PORTAL.Areas.CLIP.Models
{
    public class UserCompetency
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public int CompetencyModuleId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } // 'Not Started', 'In Progress', 'Completed', 'Expired'
        
        [Column(TypeName = "Date")]
        public DateTime? CompletionDate { get; set; }
        
        [Column(TypeName = "Date")]
        public DateTime? ExpiryDate { get; set; }
        
        public string Remarks { get; set; }

        public string Building { get; set; }
        
        public string DocumentPath { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        
        [ForeignKey("CompetencyModuleId")]
        public virtual CompetencyModule CompetencyModule { get; set; }
        
        // Helper method to calculate status based on expiry date
        public void CalculateStatus()
        {
            // Skip if this is an Environment type competency (they don't expire)
            if (CompetencyModule != null && CompetencyModule.CompetencyType == "Environment")
                return;
                
            // Only update status if expiry date is set
            if (ExpiryDate.HasValue)
            {
                if (ExpiryDate < DateTime.Today)
                    Status = "Expired";
                else if (ExpiryDate < DateTime.Today.AddDays(90))
                    Status = "Expiring Soon";
                else
                    Status = "Active";
            }
        }
    }
} 