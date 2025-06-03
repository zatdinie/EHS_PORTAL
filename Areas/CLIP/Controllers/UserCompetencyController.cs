using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using EHS_PORTAL.Areas.CLIP.Models;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize]
    public class UserCompetencyController : Controller
    {
        // GET: UserCompetency
        public ActionResult Index()
        {
            var db = new ApplicationDbContext();
            // Get all competency modules with associated user competencies
            var competencyModules = db.CompetencyModules
                .Include(cm => cm.UserCompetencies.Select(uc => uc.User))
                .ToList();
            
            return View(competencyModules);
        }

        // GET: UserCompetency/Assign
        public ActionResult Assign()
        {
            var db = new ApplicationDbContext();
            
            // Get all users and competency modules for dropdowns
            ViewBag.Users = db.Users.ToList();
            ViewBag.CompetencyModules = db.CompetencyModules.ToList();
            
            return View();
        }

        // POST: UserCompetency/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(UserCompetency model, string[] Building, HttpPostedFileBase documentFile)
        {
            if (ModelState.IsValid)
            {
                var db = new ApplicationDbContext();
                
                // Check if this competency is already assigned to the user
                bool exists = db.UserCompetencies.Any(uc => 
                    uc.UserId == model.UserId && 
                    uc.CompetencyModuleId == model.CompetencyModuleId);
                
                if (exists)
                {
                    TempData["ErrorMessage"] = "This competency is already assigned to the selected user.";
                    
                    // Reload ViewBag data for dropdowns
                    ViewBag.Users = db.Users.ToList();
                    ViewBag.CompetencyModules = db.CompetencyModules.ToList();
                    
                    return View(model);
                }
                
                // Get the competency module type
                var competencyModule = db.CompetencyModules.Find(model.CompetencyModuleId);
                if (competencyModule != null && competencyModule.CompetencyType == "Environment")
                {
                    // For Environment type, no expiry date is needed
                    model.ExpiryDate = null;
                }
                
                // Process selected buildings from form collection
                var selectedBuildings = Request.Form.GetValues("Building");
                if (selectedBuildings != null && selectedBuildings.Length > 0)
                {
                    model.Building = string.Join(",", selectedBuildings);
                }

                // Process document upload if provided
                if (documentFile != null && documentFile.ContentLength > 0)
                {
                    // Create directory if it doesn't exist
                    string uploadsFolder = Server.MapPath("~/Areas/CLIP/Uploads/UserCompetency");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate a unique filename
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(documentFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Save the file
                    documentFile.SaveAs(filePath);
                    
                    // Store the relative path in the database
                    model.DocumentPath = "~/Areas/CLIP/Uploads/UserCompetency/" + uniqueFileName;
                }
                
                db.UserCompetencies.Add(model);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Competency assigned to user successfully.";
                return RedirectToAction("Index");
            }
            
            // If we got this far, something failed; reload form
            var context = new ApplicationDbContext();
            ViewBag.Users = context.Users.ToList();
            ViewBag.CompetencyModules = context.CompetencyModules.ToList();
            
            return View(model);
        }

        // GET: UserCompetency/Edit/5
        public ActionResult Edit(int id)
        {
            var db = new ApplicationDbContext();
            var userCompetency = db.UserCompetencies
                .Include(uc => uc.User)
                .Include(uc => uc.CompetencyModule)
                .FirstOrDefault(uc => uc.Id == id);
                
            if (userCompetency == null)
            {
                TempData["ErrorMessage"] = "User competency not found.";
                return RedirectToAction("Index");
            }
            
            // Prepare status dropdown items with new options
            ViewBag.Statuses = new List<string> 
            {
                "Active",
                "Pending",
                "Expired"
            };
            
            // Set up the selected buildings if any
            if (!string.IsNullOrEmpty(userCompetency.Building))
            {
                ViewBag.SelectedBuildings = userCompetency.Building.Split(',');
            }
            else
            {
                ViewBag.SelectedBuildings = new string[] { };
            }
            
            return View(userCompetency);
        }

        // POST: UserCompetency/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserCompetency model, string[] Building, HttpPostedFileBase documentFile)
        {
            if (ModelState.IsValid)
            {
                var db = new ApplicationDbContext();
                var userCompetency = db.UserCompetencies.Find(model.Id);
                
                if (userCompetency == null)
                {
                    TempData["ErrorMessage"] = "User competency not found.";
                    return RedirectToAction("Index");
                }
                
                // Update properties
                userCompetency.Status = model.Status;
                userCompetency.CompletionDate = model.CompletionDate;
                
                // Get the competency module type
                var competencyModule = db.CompetencyModules.Find(userCompetency.CompetencyModuleId);
                if (competencyModule != null && competencyModule.CompetencyType == "Environment")
                {
                    // For Environment type, no expiry date is needed
                    userCompetency.ExpiryDate = null;
                }
                else
                {
                    // For other types (Safety), use the expiry date from the form
                    userCompetency.ExpiryDate = model.ExpiryDate;
                }
                
                userCompetency.Remarks = model.Remarks;
                
                // Process selected buildings from form collection
                var selectedBuildings = Request.Form.GetValues("Building");
                if (selectedBuildings != null && selectedBuildings.Length > 0)
                {
                    userCompetency.Building = string.Join(",", selectedBuildings);
                }
                else
                {
                    userCompetency.Building = null;
                }
                
                // If status is changed to "Active" and no completion date is set, set it to today
                if (model.Status == "Active" && !userCompetency.CompletionDate.HasValue)
                {
                    userCompetency.CompletionDate = DateTime.Today;
                }

                // Process document upload if provided
                if (documentFile != null && documentFile.ContentLength > 0)
                {
                    // Create directory if it doesn't exist
                    string uploadsFolder = Server.MapPath("~/Areas/CLIP/Uploads/UserCompetency");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(userCompetency.DocumentPath))
                    {
                        string oldFilePath = Server.MapPath(userCompetency.DocumentPath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Generate a unique filename
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(documentFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Save the file
                    documentFile.SaveAs(filePath);
                    
                    // Store the relative path in the database
                    userCompetency.DocumentPath = "~/Areas/CLIP/Uploads/UserCompetency/" + uniqueFileName;
                }
                
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "User competency updated successfully.";
                return RedirectToAction("Index");
            }
            
            // If we got this far, something failed; reload form
            ViewBag.Statuses = new List<string> 
            {
                "Active",
                "Pending",
                "Expired"
            };
            
            // Set up the selected buildings if any
            if (Building != null && Building.Length > 0)
            {
                ViewBag.SelectedBuildings = Building;
            }
            else
            {
                ViewBag.SelectedBuildings = new string[] { };
            }
            
            return View(model);
        }

        // POST: UserCompetency/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var db = new ApplicationDbContext();
            var userCompetency = db.UserCompetencies.Find(id);
            
            if (userCompetency != null)
            {
                // Delete associated document if exists
                if (!string.IsNullOrEmpty(userCompetency.DocumentPath))
                {
                    string filePath = Server.MapPath(userCompetency.DocumentPath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                db.UserCompetencies.Remove(userCompetency);
                db.SaveChanges();
                TempData["SuccessMessage"] = "User competency removed successfully.";
            }
            
            return RedirectToAction("Index");
        }

        // GET: UserCompetency/DownloadDocument/5
        public ActionResult DownloadDocument(int id)
        {
            var db = new ApplicationDbContext();
            var userCompetency = db.UserCompetencies.Find(id);
            
            if (userCompetency == null || string.IsNullOrEmpty(userCompetency.DocumentPath))
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index");
            }
            
            // Get the file path
            string filePath = Server.MapPath(userCompetency.DocumentPath);
            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Document file not found.";
                return RedirectToAction("Index");
            }
            
            // Get file info
            var fileInfo = new FileInfo(filePath);
            
            // Return the file
            return File(filePath, GetContentType(fileInfo.Extension), Path.GetFileName(filePath));
        }

        // Helper method to determine content type
        private string GetContentType(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }

        // GET: UserCompetency/UserCompetencies/userId
        public ActionResult UserCompetencies(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }
            
            var db = new ApplicationDbContext();
            var user = db.Users.Find(userId);
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }
            
            var userCompetencies = db.UserCompetencies
                .Include(uc => uc.CompetencyModule)
                .Where(uc => uc.UserId == userId)
                .ToList();
                
            ViewBag.User = user;
            ViewBag.ATOMCEPPoints = user.Atom_CEP ?? 0;
            ViewBag.DOECPDPoints = user.DOE_CPD ?? 0;
            ViewBag.DOSHCEPPoints = user.Dosh_CEP ?? 0;
            
            return View(userCompetencies);
        }

        // GET: UserCompetency/MyCompetencies
        public ActionResult MyCompetencies()
        {
            string userId = User.Identity.GetUserId();
            var db = new ApplicationDbContext();
            
            var user = db.Users.Find(userId);
            
            var userCompetencies = db.UserCompetencies
                .Include(uc => uc.CompetencyModule)
                .Where(uc => uc.UserId == userId)
                .ToList();
                
            ViewBag.User = user;
            ViewBag.ATOMCEPPoints = user.Atom_CEP ?? 0;
            ViewBag.DOECPDPoints = user.DOE_CPD ?? 0;
            ViewBag.DOSHCEPPoints = user.Dosh_CEP ?? 0;
            
            return View(userCompetencies);
        }
    }
} 