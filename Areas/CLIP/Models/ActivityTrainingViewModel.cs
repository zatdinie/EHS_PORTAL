using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace EHS_PORTAL.Areas.CLIP.Models
{
    public class ActivityTrainingViewModel
    {
        [Required]
        [Display(Name = "Activity Name")]
        public string ActivityName { get; set; }

        [Required]
        [Display(Name = "Activity Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ActivityDate { get; set; }

        [Display(Name = "Supporting Document")]
        public HttpPostedFileBase DocumentFile { get; set; }

        [Display(Name = "ATOM CEP Points")]
        [Range(0, 100, ErrorMessage = "ATOM CEP Points must be between 0 and 100")]
        public int? ATOM_CEP_Points { get; set; }

        [Display(Name = "DOE CPD Points")]
        [Range(0, 100, ErrorMessage = "DOE CPD Points must be between 0 and 100")]
        public int? DOE_CPD_Points { get; set; }
        
        [Display(Name = "DOSH CEP Points")]
        [Range(0, 100, ErrorMessage = "DOSH CEP Points must be between 0 and 100")]
        public int? DOSH_CEP_Points { get; set; }
    }
} 