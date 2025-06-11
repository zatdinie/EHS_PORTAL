using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;

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
            // Date 30 days from now for "expiring soon" calculation
            var expiringDate = currentDate.AddDays(30);
            
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
    }
}