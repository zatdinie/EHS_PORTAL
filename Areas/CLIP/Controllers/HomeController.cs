using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using System.IO;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        [Authorize]
        public ActionResult Index()
        {
            var plantCounts = GetPlantMachineCounts();
            ViewBag.CompetencySummary = GetCompetencySummary();
            ViewBag.PlantMonitoringSummary = GetPlantMonitoringSummary();
            return View(plantCounts);
        }

        // Class to hold plant/machine data for the dashboard
        public class PlantMachineCount
        {
            public string PlantName { get; set; }
            public int MachineCount { get; set; }
            public int ActiveCount { get; set; }
            public int ExpiringSoonCount { get; set; }
            public int ExpiredCount { get; set; }
        }

        // Class to hold competency summary data
        public class CompetencySummary
        {
            public int TotalModules { get; set; }
            public int TotalUsers { get; set; }
            public int ActiveCompetencies { get; set; }
            public int PendingCompetencies { get; set; }
            public int ExpiredCompetencies { get; set; }
            public Dictionary<string, int> CompetencyTypeCount { get; set; }
        }

        // Helper method to get competency summary for dashboard
        private CompetencySummary GetCompetencySummary()
        {
            var currentDate = DateTime.Now;
            
            // Get all modules and user competencies
            var modules = db.CompetencyModules.ToList();
            var userCompetencies = db.UserCompetencies.ToList();
            
            // Count by status
            var activeCount = userCompetencies.Count(uc => uc.Status == "Active" && 
                                                   (!uc.ExpiryDate.HasValue || uc.ExpiryDate.Value > currentDate));
            var pendingCount = userCompetencies.Count(uc => uc.Status == "Pending");
            var expiredCount = userCompetencies.Count(uc => uc.ExpiryDate.HasValue && uc.ExpiryDate.Value < currentDate);
            
            // Count by competency type
            var typeCount = new Dictionary<string, int>();
            foreach (var module in modules)
            {
                string type = !string.IsNullOrEmpty(module.CompetencyType) ? module.CompetencyType : "Other";
                if (!typeCount.ContainsKey(type))
                {
                    typeCount[type] = 0;
                }
                typeCount[type] += module.UserCompetencies.Count;
            }
            
            return new CompetencySummary
            {
                TotalModules = modules.Count,
                TotalUsers = userCompetencies.Select(uc => uc.UserId).Distinct().Count(),
                ActiveCompetencies = activeCount,
                PendingCompetencies = pendingCount,
                ExpiredCompetencies = expiredCount,
                CompetencyTypeCount = typeCount
            };
        }

        // Helper method to get plant machine counts for dashboards
        private List<PlantMachineCount> GetPlantMachineCounts()
        {
            // Get current date
            var currentDate = DateTime.Now;
            // Date 90 days from now for "expiring soon" calculation (changed from 30 to 90 days)
            var expiringDate = currentDate.AddDays(90);
            
            // Get all plants
            var plants = db.Plants.ToList();
            
            // Get counts for each plant
            var plantCounts = new List<PlantMachineCount>();
            
            foreach (var plant in plants)
            {
                // Get certificates for this plant
                var certificates = db.CertificateOfFitness.Where(c => c.PlantId == plant.Id).ToList();
                
                // Count machines by status
                var activeCount = certificates.Count(c => c.ExpiryDate > expiringDate);
                var expiringSoonCount = certificates.Count(c => c.ExpiryDate <= expiringDate && c.ExpiryDate >= currentDate);
                var expiredCount = certificates.Count(c => c.ExpiryDate < currentDate);
                
                // Add to results
                plantCounts.Add(new PlantMachineCount
                {
                    PlantName = plant.PlantName,
                    MachineCount = certificates.Count,
                    ActiveCount = activeCount,
                    ExpiringSoonCount = expiringSoonCount,
                    ExpiredCount = expiredCount
                });
            }
            
            return plantCounts;
        }

        [Authorize]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [Authorize]
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        
        [AllowAnonymous]
        public ActionResult Welcome()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { area = "CLIP" });
            }
            
            // Get plant machine counts for the public dashboard
            var plantCounts = GetPlantMachineCounts();
            
            // Get competency summary for the public dashboard
            ViewBag.CompetencySummary = GetCompetencySummary();
            
            // Get plant monitoring summary for the public dashboard
            ViewBag.PlantMonitoringSummary = GetPlantMonitoringSummary();
            
            return View(plantCounts);
        }

        // Redirect to the new Competency controller for backward compatibility
        [Authorize]
        public ActionResult Competency()
        {
            return RedirectToAction("Index", "Competency", new { area = "CLIP" });
        }

        [Authorize]
        public ActionResult AddCompetency()
        {
            return View();
        }

        [Authorize]
        public ActionResult Monitoring()
        {
            return View();
        }
        
        [Authorize]
        public ActionResult EnvironmentMonitoring()
        {
            return View();
        }
        
        [Authorize]
        public ActionResult SafetyHealthMonitoring()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddCompetency(CompetencyModule model)
        {
            if (ModelState.IsValid)
            {
                var db = new ApplicationDbContext();
                db.CompetencyModules.Add(model);
                db.SaveChanges();
                
                return RedirectToAction("Competency", new { area = "CLIP" });
            }
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCompetency(int id)
        {
            var db = new ApplicationDbContext();
            var competency = db.CompetencyModules.Find(id);
            
            if (competency != null)
            {
                // Check if the competency is in use before deleting
                bool isInUse = db.UserCompetencies.Any(uc => uc.CompetencyModuleId == id);
                
                if (isInUse)
                {
                    TempData["ErrorMessage"] = "This competency cannot be deleted because it is assigned to one or more users.";
                }
                else
                {
                    db.CompetencyModules.Remove(competency);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Competency module deleted successfully.";
                }
            }
            
            return RedirectToAction("Competency", new { area = "CLIP" });
        }

        [Authorize]
        public ActionResult EditCompetency(int id)
        {
            var db = new ApplicationDbContext();
            var competency = db.CompetencyModules.Find(id);
            
            if (competency == null)
            {
                TempData["ErrorMessage"] = "Competency module not found.";
                return RedirectToAction("Competency", new { area = "CLIP" });
            }
            
            return View(competency);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult EditCompetency(CompetencyModule model)
        {
            if (ModelState.IsValid)
            {
                var db = new ApplicationDbContext();
                var competency = db.CompetencyModules.Find(model.Id);
                
                if (competency == null)
                {
                    TempData["ErrorMessage"] = "Competency module not found.";
                    return RedirectToAction("Competency", new { area = "CLIP" });
                }
                
                // Update the competency properties
                competency.ModuleName = model.ModuleName;
                competency.Description = model.Description;
                competency.AnnualPointDeduction = model.AnnualPointDeduction;
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Competency module updated successfully.";
                return RedirectToAction("Competency", new { area = "CLIP" });
            }
            
            
            return View(model);
        }

        [Authorize]
        public ActionResult OpenFile()
        {
            // Path to the file you want to open
            string filePath = Server.MapPath("~/Areas/CLIP/uploads/CLIP USER GUIDE.pdf");
            
            // Check if file exists
            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "The requested file does not exist.";
                return RedirectToAction("Index");
            }
            
            // Set response headers to display in browser
            Response.AppendHeader("Content-Disposition", "inline; filename=\"CLIP USER GUIDE.pdf\"");
            
            // Return the file to be displayed in the browser
            return File(filePath, "application/pdf");
        }

        // Class to hold plant monitoring summary data
        public class PlantMonitoringSummary
        {
            public int TotalRecords { get; set; }
            public int CompletedCount { get; set; }
            public int InProgressCount { get; set; }
            public int ExpiredCount { get; set; }
            public int NotStartedCount { get; set; }
            public List<ExpiryTimelineItem> ExpiryTimeline { get; set; }
            public List<WorkAssignmentItem> WorkAssignments { get; set; }
            public Dictionary<string, int> PhaseDistribution { get; set; }
        }

        public class ExpiryTimelineItem
        {
            public string Month { get; set; }
            public int Count { get; set; }
            public bool IsCurrentMonth { get; set; }
            public bool IsNextMonth { get; set; }
        }

        public class WorkAssignmentItem
        {
            public string UserName { get; set; }
            public int Count { get; set; }
        }

        private PlantMonitoringSummary GetPlantMonitoringSummary()
        {
            var summary = new PlantMonitoringSummary
            {
                TotalRecords = 0,
                CompletedCount = 0,
                InProgressCount = 0,
                ExpiredCount = 0,
                NotStartedCount = 0,
                ExpiryTimeline = new List<ExpiryTimelineItem>(),
                WorkAssignments = new List<WorkAssignmentItem>(),
                PhaseDistribution = new Dictionary<string, int>()
            };

            try
            {
                // Get all plant monitoring records
                var plantMonitorings = db.PlantMonitorings.ToList();
                
                // Calculate total records and status counts
                summary.TotalRecords = plantMonitorings.Count;
                summary.CompletedCount = plantMonitorings.Count(p => p.ProcStatus == "Completed");
                summary.InProgressCount = plantMonitorings.Count(p => 
                    p.ProcStatus == "Work In Progress" || 
                    p.ProcStatus == "ePR Raised" || 
                    p.ProcStatus == "Quotation Requested");
                summary.ExpiredCount = plantMonitorings.Count(p => p.ExpStatus == "Expired");
                summary.NotStartedCount = plantMonitorings.Count(p => p.ProcStatus == "Not Started");

                // Calculate phase distribution
                summary.PhaseDistribution["Quotation Phase"] = plantMonitorings.Count(p => 
                    p.QuoteDate.HasValue && (!p.QuoteCompleteDate.HasValue || !p.EprDate.HasValue));
                
                summary.PhaseDistribution["ePR Phase"] = plantMonitorings.Count(p => 
                    p.EprDate.HasValue && (!p.EprCompleteDate.HasValue || !p.WorkDate.HasValue));
                
                summary.PhaseDistribution["Work Execution"] = plantMonitorings.Count(p => 
                    p.WorkDate.HasValue && !p.WorkCompleteDate.HasValue);
                
                summary.PhaseDistribution["Completed"] = plantMonitorings.Count(p => 
                    p.WorkCompleteDate.HasValue);
                
                summary.PhaseDistribution["Not Started"] = plantMonitorings.Count(p => 
                    !p.QuoteDate.HasValue && !p.EprDate.HasValue && !p.WorkDate.HasValue);

                // Generate expiry timeline data for the next 6 months
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                
                for (int i = 0; i < 6; i++)
                {
                    var targetMonth = currentMonth + i;
                    var targetYear = currentYear;
                    
                    if (targetMonth > 12)
                    {
                        targetMonth -= 12;
                        targetYear++;
                    }
                    
                    var monthStart = new DateTime(targetYear, targetMonth, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    
                    var expiringCount = plantMonitorings.Count(p => 
                        p.ExpDate.HasValue && 
                        p.ExpDate.Value >= monthStart && 
                        p.ExpDate.Value <= monthEnd);
                    
                    summary.ExpiryTimeline.Add(new ExpiryTimelineItem
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        Count = expiringCount,
                        IsCurrentMonth = i == 0,
                        IsNextMonth = i == 1
                    });
                }

                // Generate work assignment data
                var workAssignments = plantMonitorings
                    .Where(p => !string.IsNullOrEmpty(p.WorkUserAssign) || 
                               !string.IsNullOrEmpty(p.QuoteUserAssign) || 
                               !string.IsNullOrEmpty(p.EprUserAssign))
                    .GroupBy(p => 
                        !string.IsNullOrEmpty(p.WorkUserAssign) ? p.WorkUserAssign :
                        !string.IsNullOrEmpty(p.EprUserAssign) ? p.EprUserAssign :
                        p.QuoteUserAssign)
                    .Select(g => new WorkAssignmentItem
                    {
                        UserName = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(w => w.Count)
                    .Take(7) // Limit to top 7 users
                    .ToList();
                
                summary.WorkAssignments = workAssignments;
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error getting plant monitoring summary: {ex.Message}");
                
                // Ensure we have at least some default data for the charts
                if (summary.ExpiryTimeline.Count == 0)
                {
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    
                    for (int i = 0; i < 6; i++)
                    {
                        var targetMonth = currentMonth + i;
                        var targetYear = currentYear;
                        
                        if (targetMonth > 12)
                        {
                            targetMonth -= 12;
                            targetYear++;
                        }
                        
                        var monthStart = new DateTime(targetYear, targetMonth, 1);
                        
                        summary.ExpiryTimeline.Add(new ExpiryTimelineItem
                        {
                            Month = monthStart.ToString("MMM yyyy"),
                            Count = 0,
                            IsCurrentMonth = i == 0,
                            IsNextMonth = i == 1
                        });
                    }
                }
                
                // Add a default user if no assignments
                if (summary.WorkAssignments.Count == 0)
                {
                    summary.WorkAssignments.Add(new WorkAssignmentItem
                    {
                        UserName = "No Assignments",
                        Count = 0
                    });
                }
                
                // Initialize phase distribution with default values
                if (summary.PhaseDistribution.Count == 0)
                {
                    summary.PhaseDistribution["Quotation Phase"] = 0;
                    summary.PhaseDistribution["ePR Phase"] = 0;
                    summary.PhaseDistribution["Work Execution"] = 0;
                    summary.PhaseDistribution["Completed"] = 0;
                    summary.PhaseDistribution["Not Started"] = 0;
                }
            }

            return summary;
        }
    }
}