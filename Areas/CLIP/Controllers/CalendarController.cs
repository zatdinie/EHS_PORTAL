using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using Microsoft.AspNet.Identity;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: CLIP/Calendar
        public ActionResult Index()
        {
            var summary = GetSummaryStatistics();
            return View(summary);
        }

        // GET: CLIP/Calendar/GetEvents
        [HttpGet]
        public JsonResult GetEvents()
        {
            var events = new List<object>();
            
            // Get Plant Monitoring expiry dates
            var plantMonitorings = db.PlantMonitorings
                .Include(pm => pm.Plant)
                .Include(pm => pm.Monitoring)
                .Where(pm => pm.ExpDate.HasValue)
                .ToList();
                
            foreach (var pm in plantMonitorings)
            {
                events.Add(new
                {
                    id = "pm_" + pm.Id,
                    title = $"[PM] {pm.Plant?.PlantName} - {pm.Monitoring?.MonitoringName}",
                    start = pm.ExpDate.Value.ToString("yyyy-MM-dd"),
                    end = pm.ExpDate.Value.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                    allDay = true,
                    color = pm.ExpStatus == "Expired" ? "#dc3545" : 
                           pm.ExpStatus == "Expiring Soon" ? "#ffc107" : "#198754",
                    description = $"Plant Monitoring - {pm.Plant?.PlantName} - {pm.Monitoring?.MonitoringName}",
                    type = "Plant Monitoring"
                });
            }
            
            // Get Competency expiry dates
            var competencies = db.UserCompetencies
                .Include(uc => uc.User)
                .Include(uc => uc.CompetencyModule)
                .Where(uc => uc.ExpiryDate.HasValue)
                .ToList();
                
            foreach (var comp in competencies)
            {
                events.Add(new
                {
                    id = "comp_" + comp.Id,
                    title = $"[COMP] {comp.User?.UserName} - {comp.CompetencyModule?.ModuleName}",
                    start = comp.ExpiryDate.Value.ToString("yyyy-MM-dd"),
                    end = comp.ExpiryDate.Value.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                    allDay = true,
                    color = comp.Status == "Expired" ? "#dc3545" : 
                           DateTime.Now.AddDays(90) >= comp.ExpiryDate ? "#ffc107" : "#198754",
                    description = $"Competency - {comp.User?.UserName} - {comp.CompetencyModule?.ModuleName}",
                    type = "Competency"
                });
            }
            
            // Get Certificate of Fitness expiry dates
            var certificates = db.CertificateOfFitness
                .Include(cf => cf.Plant)
                .ToList();
                
            foreach (var cert in certificates)
            {
                events.Add(new
                {
                    id = "cof_" + cert.Id,
                    title = $"[COF] {cert.Plant?.PlantName} - {cert.MachineName} ({cert.RegistrationNo})",
                    start = cert.ExpiryDate.ToString("yyyy-MM-dd"),
                    end = cert.ExpiryDate.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss"),
                    allDay = true,
                    color = cert.Status == "Expired" ? "#dc3545" : 
                           cert.Status == "Expiring Soon" ? "#ffc107" : "#198754",
                    description = $"Certificate of Fitness - {cert.Plant?.PlantName} - {cert.MachineName} ({cert.RegistrationNo})",
                    type = "Certificate of Fitness"
                });
            }
            
            return Json(events, JsonRequestBehavior.AllowGet);
        }

        private CalendarSummaryViewModel GetSummaryStatistics()
        {
            var today = DateTime.Today;
            var next90Days = today.AddDays(90);
            
            var summary = new CalendarSummaryViewModel();
            
            // Plant Monitoring statistics
            var plantMonitorings = db.PlantMonitorings
                .Where(pm => pm.ExpDate.HasValue)
                .ToList();
                
            summary.PlantMonitoring.Total = plantMonitorings.Count;
            summary.PlantMonitoring.Expired = plantMonitorings.Count(pm => pm.ExpStatus == "Expired");
            summary.PlantMonitoring.ExpiringSoon = plantMonitorings.Count(pm => pm.ExpStatus == "Expiring Soon");
            summary.PlantMonitoring.ExpiringThisMonth = plantMonitorings.Count(pm => 
                pm.ExpDate.HasValue && 
                pm.ExpDate.Value >= today && 
                pm.ExpDate.Value <= next90Days);
            
            // Competency statistics
            var competencies = db.UserCompetencies
                .Where(uc => uc.ExpiryDate.HasValue)
                .ToList();
                
            summary.Competency.Total = competencies.Count;
            summary.Competency.Expired = competencies.Count(uc => uc.Status == "Expired");
            summary.Competency.ExpiringSoon = competencies.Count(uc => 
                uc.Status != "Expired" && 
                uc.ExpiryDate.HasValue && 
                uc.ExpiryDate.Value <= today.AddDays(90));
            summary.Competency.ExpiringThisMonth = competencies.Count(uc => 
                uc.ExpiryDate.HasValue && 
                uc.ExpiryDate.Value >= today && 
                uc.ExpiryDate.Value <= next90Days);
            
            // Certificate of Fitness statistics
            var certificates = db.CertificateOfFitness.ToList();
            
            summary.CertificateOfFitness.Total = certificates.Count;
            summary.CertificateOfFitness.Expired = certificates.Count(cf => cf.Status == "Expired");
            summary.CertificateOfFitness.ExpiringSoon = certificates.Count(cf => cf.Status == "Expiring Soon");
            summary.CertificateOfFitness.ExpiringThisMonth = certificates.Count(cf => 
                cf.ExpiryDate >= today && 
                cf.ExpiryDate <= next90Days);
            
            return summary;
        }
    }

    public class CalendarSummaryViewModel
    {
        public ExpiryStatistics PlantMonitoring { get; set; } = new ExpiryStatistics();
        public ExpiryStatistics Competency { get; set; } = new ExpiryStatistics();
        public ExpiryStatistics CertificateOfFitness { get; set; } = new ExpiryStatistics();
    }

    public class ExpiryStatistics
    {
        public int Total { get; set; }
        public int Expired { get; set; }
        public int ExpiringSoon { get; set; }
        public int ExpiringThisMonth { get; set; }
    }
} 