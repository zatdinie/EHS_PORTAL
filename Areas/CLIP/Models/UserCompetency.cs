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
    }
} 