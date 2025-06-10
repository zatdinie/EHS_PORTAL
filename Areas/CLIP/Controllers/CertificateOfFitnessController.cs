using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize]
    public class CertificateOfFitnessController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // Helper method to check if the current user has access to a specific plant
        private bool UserHasAccessToPlant(int plantId)
        {
            // Admin users have access to all plants
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            // Get current user ID
            string userId = User.Identity.GetUserId();
            
            // Check if the user is assigned to this plant
            return db.UserPlants.Any(up => up.UserId == userId && up.PlantId == plantId);
        }
        
        // Helper method to get all plant IDs the current user has access to
        private List<int> GetUserAccessiblePlantIds()
        {
            // Admin users have access to all plants
            if (User.IsInRole("Admin"))
            {
                return db.Plants.Select(p => p.Id).ToList();
            }
            
            // Get current user ID
            string userId = User.Identity.GetUserId();
            
            // Return list of plant IDs the user is assigned to
            return db.UserPlants
                .Where(up => up.UserId == userId)
                .Select(up => up.PlantId)
                .ToList();
        }

        // Method to check if registration number is unique
        [HttpGet]
        public JsonResult IsRegistrationNoAvailable(string registrationNo, int? id)
        {
            // Check if the registration number exists and doesn't belong to the current record (in case of editing)
            bool isAvailable = !db.CertificateOfFitness.Any(c => 
                c.RegistrationNo.Equals(registrationNo, StringComparison.OrdinalIgnoreCase) && 
                (id == null || c.Id != id.Value));
                
            return Json(isAvailable, JsonRequestBehavior.AllowGet);
        }

        // Helper method to calculate status based on expiry date
        private string CalculateStatus(DateTime expiryDate)
        {
            DateTime today = DateTime.Today;
            DateTime expiringSoonDate = expiryDate.AddDays(-60);

            if (today > expiryDate)
                return "Expired";
            else if (today >= expiringSoonDate)
                return "Expiring Soon";
            else
                return "Active";
        }

        // GET: CertificateOfFitness
        public ActionResult Index(string searchString, string sortOrder, string plantFilter)
        {
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.PlantSortParam = sortOrder == "plant_asc" ? "plant_desc" : "plant_asc";
            ViewBag.DateSortParam = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            ViewBag.CurrentPlantFilter = plantFilter;

            // Get user accessible plants for dropdown filter
            List<int> accessiblePlantIds = GetUserAccessiblePlantIds();
            var plants = db.Plants.OrderBy(p => p.PlantName).ToList();
            ViewBag.Plants = new SelectList(plants, "Id", "PlantName");
            
            // Store accessible plant IDs in ViewBag for the view to use
            ViewBag.UserAccessiblePlantIds = accessiblePlantIds;

            // Start with all certificates (users can see all)
            var certificates = db.CertificateOfFitness.Include(c => c.Plant);
            
            // Apply plant filter
            if (!string.IsNullOrEmpty(plantFilter))
            {
                switch (plantFilter)
                {
                    case "135":
                        certificates = certificates.Where(c => c.Plant.PlantName == "Plant 1" || 
                                                             c.Plant.PlantName == "Plant 3" || 
                                                             c.Plant.PlantName == "Plant 5");
                        break;
                    case "21":
                        certificates = certificates.Where(c => c.Plant.PlantName == "Plant 21");
                        break;
                    case "13,55":
                        certificates = certificates.Where(c => c.Plant.PlantName == "Plant 13" || 
                                                             c.Plant.PlantName == "Plant 55");
                        break;
                    case "34":
                        certificates = certificates.Where(c => c.Plant.PlantName == "Plant 34");
                        break;
                    default:
                        // Try to parse as normal plant ID if it doesn't match any of the special filters
                        int plantId;
                        if (int.TryParse(plantFilter, out plantId))
                        {
                            certificates = certificates.Where(c => c.PlantId == plantId);
                        }
                        break;
                }
            }
            
            // Apply search filter (search by registration number or machine name)
            if (!string.IsNullOrEmpty(searchString))
            {
                certificates = certificates.Where(c => c.RegistrationNo.Contains(searchString) || 
                                                     c.MachineName.Contains(searchString));
            }
            
            // Apply sorting
            switch (sortOrder)
            {
                case "plant_desc":
                    certificates = certificates.OrderByDescending(c => c.Plant.PlantName);
                    break;
                case "plant_asc":
                    certificates = certificates.OrderBy(c => c.Plant.PlantName);
                    break;
                case "date_desc":
                    certificates = certificates.OrderByDescending(c => c.ExpiryDate);
                    break;
                case "date_asc":
                    certificates = certificates.OrderBy(c => c.ExpiryDate);
                    break;
                default:
                    certificates = certificates.OrderBy(c => c.Plant.PlantName);
                    break;
            }
            
            return View(certificates.ToList());
        }

        // GET: CertificateOfFitness/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CertificateOfFitness certificateOfFitness = db.CertificateOfFitness.Include(c => c.Plant).FirstOrDefault(c => c.Id == id);
            if (certificateOfFitness == null)
            {
                return HttpNotFound();
            }
            
            // Pass information about whether the user can edit/delete this certificate
            ViewBag.UserCanEditDelete = UserHasAccessToPlant(certificateOfFitness.PlantId);
            
            return View(certificateOfFitness);
        }

        // GET: CertificateOfFitness/Create
        public ActionResult Create()
        {
            // For non-admin users, only show their assigned plants in the dropdown
            if (User.IsInRole("Admin"))
            {
                ViewBag.PlantId = new SelectList(db.Plants, "Id", "PlantName");
            }
            else
            {
                // Get user's assigned plants
                string userId = User.Identity.GetUserId();
                var userPlantIds = db.UserPlants
                    .Where(up => up.UserId == userId)
                    .Select(up => up.PlantId)
                    .ToList();
                
                // If user has no assigned plants, show error
                if (userPlantIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "You don't have any plants assigned to you. Please contact an administrator.";
                    return RedirectToAction("Index");
                }
                
                ViewBag.PlantId = new SelectList(
                    db.Plants.Where(p => userPlantIds.Contains(p.Id)), 
                    "Id", 
                    "PlantName");
            }
            
            return View();
        }

        // POST: CertificateOfFitness/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,PlantId,RegistrationNo,ExpiryDate,MachineName,Status,Remarks,Location,HostInfo,Department,ResidentInfo")] CertificateOfFitness certificateOfFitness, HttpPostedFileBase pdfDocument)
        {
            // Check if user has access to this plant
            if (!UserHasAccessToPlant(certificateOfFitness.PlantId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            
            // Check for unique registration number
            bool isDuplicate = db.CertificateOfFitness.Any(c => 
                c.RegistrationNo.Equals(certificateOfFitness.RegistrationNo, StringComparison.OrdinalIgnoreCase));
            
            if (isDuplicate)
            {
                ModelState.AddModelError("RegistrationNo", "This Registration Number is already in use.");
            }
            
            if (ModelState.IsValid)
            {
                // Set status based on expiry date
                certificateOfFitness.Status = CalculateStatus(certificateOfFitness.ExpiryDate);
                
                // Handle PDF document upload
                if (pdfDocument != null && pdfDocument.ContentLength > 0)
                {
                    // Check file size (20MB limit)
                    if (pdfDocument.ContentLength > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "The PDF document exceeds the maximum file size of 20MB.");
                        
                        // Prepare plant dropdown based on user role
                        if (User.IsInRole("Admin"))
                        {
                            ViewBag.PlantId = new SelectList(db.Plants, "Id", "PlantName", certificateOfFitness.PlantId);
                        }
                        else
                        {
                            string userId = User.Identity.GetUserId();
                            var userPlantIds = db.UserPlants
                                .Where(up => up.UserId == userId)
                                .Select(up => up.PlantId)
                                .ToList();
                            
                            ViewBag.PlantId = new SelectList(
                                db.Plants.Where(p => userPlantIds.Contains(p.Id)), 
                                "Id", 
                                "PlantName", 
                                certificateOfFitness.PlantId);
                        }
                        
                        return View(certificateOfFitness);
                    }

                    // Create folder if it doesn't exist
                    string uploadFolder = Server.MapPath("~/Areas/CLIP/Uploads/CertificateOfFitness");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // Create unique filename
                    string fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}_{Path.GetFileName(pdfDocument.FileName)}";
                    string filePath = Path.Combine(uploadFolder, fileName);
                    
                    // Save file
                    pdfDocument.SaveAs(filePath);
                    
                    // Store filename in the database
                    certificateOfFitness.DocumentPath = fileName;
                }
                
                db.CertificateOfFitness.Add(certificateOfFitness);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Prepare plant dropdown based on user role
            if (User.IsInRole("Admin"))
            {
                ViewBag.PlantId = new SelectList(db.Plants, "Id", "PlantName", certificateOfFitness.PlantId);
            }
            else
            {
                string userId = User.Identity.GetUserId();
                var userPlantIds = db.UserPlants
                    .Where(up => up.UserId == userId)
                    .Select(up => up.PlantId)
                    .ToList();
                
                ViewBag.PlantId = new SelectList(
                    db.Plants.Where(p => userPlantIds.Contains(p.Id)), 
                    "Id", 
                    "PlantName", 
                    certificateOfFitness.PlantId);
            }
            
            return View(certificateOfFitness);
        }

        // GET: CertificateOfFitness/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            CertificateOfFitness certificateOfFitness = db.CertificateOfFitness.Find(id);
            if (certificateOfFitness == null)
            {
                return HttpNotFound();
            }
            
            // Check if user has access to this plant
            if (!UserHasAccessToPlant(certificateOfFitness.PlantId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            
            // For non-admin users, only show their assigned plants in the dropdown
            if (User.IsInRole("Admin"))
            {
                ViewBag.PlantId = new SelectList(db.Plants, "Id", "PlantName", certificateOfFitness.PlantId);
            }
            else
            {
                // Get user's assigned plants
                string userId = User.Identity.GetUserId();
                var userPlantIds = db.UserPlants
                    .Where(up => up.UserId == userId)
                    .Select(up => up.PlantId)
                    .ToList();
                
                ViewBag.PlantId = new SelectList(
                    db.Plants.Where(p => userPlantIds.Contains(p.Id)), 
                    "Id", 
                    "PlantName", 
                    certificateOfFitness.PlantId);
            }
            
            return View(certificateOfFitness);
        }

        // POST: CertificateOfFitness/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PlantId,RegistrationNo,ExpiryDate,MachineName,Status,Remarks,DocumentPath,Location,HostInfo,Department,ResidentInfo")] CertificateOfFitness certificateOfFitness, HttpPostedFileBase pdfDocument)
        {
            // Check if user has access to this plant
            if (!UserHasAccessToPlant(certificateOfFitness.PlantId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            
            // Check for unique registration number (excluding current record)
            bool isDuplicate = db.CertificateOfFitness.Any(c => 
                c.RegistrationNo.Equals(certificateOfFitness.RegistrationNo, StringComparison.OrdinalIgnoreCase) && 
                c.Id != certificateOfFitness.Id);
            
            if (isDuplicate)
            {
                ModelState.AddModelError("RegistrationNo", "This Registration Number is already in use.");
            }
            
            if (ModelState.IsValid)
            {
                // Set status based on expiry date
                certificateOfFitness.Status = CalculateStatus(certificateOfFitness.ExpiryDate);
                
                // Handle PDF document upload
                if (pdfDocument != null && pdfDocument.ContentLength > 0)
                {
                    // Check file size (20MB limit)
                    if (pdfDocument.ContentLength > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "The PDF document exceeds the maximum file size of 20MB.");
                        
                        // Prepare plant dropdown based on user role
                        if (User.IsInRole("Admin"))
                        {
                            ViewBag.PlantId = new SelectList(db.Plants, "Id", "PlantName", certificateOfFitness.PlantId);
                        }
                        else
                        {
                            string userId = User.Identity.GetUserId();
                            var userPlantIds = db.UserPlants
                                .Where(up => up.UserId == userId)
                                .Select(up => up.PlantId)
                                .ToList();
                            
                            ViewBag.PlantId = new SelectList(
                                db.Plants.Where(p => userPlantIds.Contains(p.Id)), 
                                "Id", 
                                "PlantName", 
                                certificateOfFitness.PlantId);
                        }
                        
                        return View(certificateOfFitness);
                    }

                    // Create folder if it doesn't exist
                    string uploadFolder = Server.MapPath("~/Areas/CLIP/Uploads/CertificateOfFitness");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }
                    
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(certificateOfFitness.DocumentPath))
                    {
                        string oldFilePath = Path.Combine(uploadFolder, certificateOfFitness.DocumentPath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Create unique filename
                    string fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}_{Path.GetFileName(pdfDocument.FileName)}";
                    string filePath = Path.Combine(uploadFolder, fileName);
                    
                    // Save file
                    pdfDocument.SaveAs(filePath);
                    
                    // Store filename in the database
                    certificateOfFitness.DocumentPath = fileName;
                }
                
                db.Entry(certificateOfFitness).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            
            // Prepare plant dropdown based on user role
            if (User.IsInRole("Admin"))
            {
                ViewBag.PlantId = new SelectList(db.Plants, "Id", "PlantName", certificateOfFitness.PlantId);
            }
            else
            {
                string userId = User.Identity.GetUserId();
                var userPlantIds = db.UserPlants
                    .Where(up => up.UserId == userId)
                    .Select(up => up.PlantId)
                    .ToList();
                
                ViewBag.PlantId = new SelectList(
                    db.Plants.Where(p => userPlantIds.Contains(p.Id)), 
                    "Id", 
                    "PlantName", 
                    certificateOfFitness.PlantId);
            }
            
            return View(certificateOfFitness);
        }

        // GET: CertificateOfFitness/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            CertificateOfFitness certificateOfFitness = db.CertificateOfFitness.Include(c => c.Plant).FirstOrDefault(c => c.Id == id);
            if (certificateOfFitness == null)
            {
                return HttpNotFound();
            }
            
            // Check if user has access to this plant
            if (!UserHasAccessToPlant(certificateOfFitness.PlantId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            
            return View(certificateOfFitness);
        }

        // POST: CertificateOfFitness/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CertificateOfFitness certificateOfFitness = db.CertificateOfFitness.Find(id);
            
            // Check if user has access to this plant
            if (!UserHasAccessToPlant(certificateOfFitness.PlantId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
            
            // Delete the associated PDF file if it exists
            if (!string.IsNullOrEmpty(certificateOfFitness.DocumentPath))
            {
                string filePath = Path.Combine(Server.MapPath("~/Areas/CLIP/Uploads/CertificateOfFitness"), certificateOfFitness.DocumentPath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            
            db.CertificateOfFitness.Remove(certificateOfFitness);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: CertificateOfFitness/ViewDocument/5
        public ActionResult ViewDocument(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            CertificateOfFitness certificate = db.CertificateOfFitness.Find(id);
            if (certificate == null || string.IsNullOrEmpty(certificate.DocumentPath))
            {
                return HttpNotFound();
            }
            
            string filePath = Path.Combine(Server.MapPath("~/Areas/CLIP/Uploads/CertificateOfFitness"), certificate.DocumentPath);
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound();
            }
            
            return File(filePath, "application/pdf");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 