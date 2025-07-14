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
    public class UserCompetencyController : BaseController
    {
        // GET: UserCompetency
        public ActionResult Index()
        {
            // Get all competency modules with associated user competencies
            var competencyModules = _db.CompetencyModules
                .Include(cm => cm.UserCompetencies.Select(uc => uc.User))
                .ToList();
            
            
            return View(competencyModules);
        }

        // GET: UserCompetency/Assign
        public ActionResult Assign()
        {
            // Get all users and competency modules for dropdowns
            ViewBag.Users = _db.Users.ToList();
            ViewBag.CompetencyModules = _db.CompetencyModules.ToList();
            
            // Log the view action
            LogView("UserCompetency", null, "Accessed user competency assignment form");
            
            return View();
        }

        // POST: UserCompetency/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Assign(UserCompetency model, string[] Building, HttpPostedFileBase documentFile)
        {
            if (ModelState.IsValid)
            {
                // Check if this competency is already assigned to the user
                bool exists = _db.UserCompetencies.Any(uc => 
                    uc.UserId == model.UserId && 
                    uc.CompetencyModuleId == model.CompetencyModuleId);
                
                if (exists)
                {
                    TempData["ErrorMessage"] = "This competency is already assigned to the selected user.";
                    
                    // Reload ViewBag data for dropdowns
                    ViewBag.Users = _db.Users.ToList();
                    ViewBag.CompetencyModules = _db.CompetencyModules.ToList();
                    
                    return View(model);
                }
                
                // Get the competency module type
                var competencyModule = _db.CompetencyModules.Find(model.CompetencyModuleId);
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
                
                _db.UserCompetencies.Add(model);
                _db.SaveChanges();
                
                // Get user and competency info for logging
                var user = _db.Users.Find(model.UserId);
                var comp = _db.CompetencyModules.Find(model.CompetencyModuleId);
                
                // Log the creation
                LogCreation("UserCompetency", model.Id.ToString(), 
                    $"Assigned competency '{comp?.ModuleName}' to user '{user?.UserName}'");
                
                TempData["SuccessMessage"] = "Competency assigned to user successfully.";
                return RedirectToAction("Index");
            }
            
            // If we got this far, something failed; reload form
            ViewBag.Users = _db.Users.ToList();
            ViewBag.CompetencyModules = _db.CompetencyModules.ToList();
            
            return View(model);
        }

        // GET: UserCompetency/Edit/5
        public ActionResult Edit(int id)
        {
            var userCompetency = _db.UserCompetencies
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
                "Expiring Soon",
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
            
            // Log the view action
            LogView("UserCompetency", id.ToString(), 
                $"Accessed edit form for competency '{userCompetency.CompetencyModule?.ModuleName}' assigned to user '{userCompetency.User?.UserName}'");
            
            return View(userCompetency);
        }

        // POST: UserCompetency/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserCompetency model, string[] Building, HttpPostedFileBase documentFile)
        {
            if (ModelState.IsValid)
            {
                var userCompetency = _db.UserCompetencies
                    .Include(uc => uc.User)
                    .Include(uc => uc.CompetencyModule)
                    .FirstOrDefault(uc => uc.Id == model.Id);
                
                if (userCompetency == null)
                {
                    TempData["ErrorMessage"] = "User competency not found.";
                    return RedirectToAction("Index");
                }
                
                // Store old values for logging
                var oldStatus = userCompetency.Status;
                var oldCompletionDate = userCompetency.CompletionDate;
                var oldExpiryDate = userCompetency.ExpiryDate;
                var oldRemarks = userCompetency.Remarks;
                var oldBuilding = userCompetency.Building;
                
                // Update properties
                userCompetency.CompletionDate = model.CompletionDate;
                
                // Get the competency module type
                var competencyModule = _db.CompetencyModules.Find(userCompetency.CompetencyModuleId);
                if (competencyModule != null && competencyModule.CompetencyType == "Environment")
                {
                    // For Environment type, no expiry date is needed
                    userCompetency.ExpiryDate = null;
                    userCompetency.Status = model.Status; // Use status from form for Environment type
                }
                else
                {
                    // For other types (Safety), use the expiry date from the form
                    userCompetency.ExpiryDate = model.ExpiryDate;
                    
                    // If expiry date changed, automatically update the status
                    if (oldExpiryDate != model.ExpiryDate)
                    {
                        userCompetency.CalculateStatus();
                    }
                    else
                    {
                        userCompetency.Status = model.Status; // Use status from form if expiry date didn't change
                    }
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
                bool documentChanged = false;
                if (documentFile != null && documentFile.ContentLength > 0)
                {
                    documentChanged = true;
                    
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
                
                _db.SaveChanges();
                
                // Prepare changes log
                var changes = new List<string>();
                
                if (oldStatus != userCompetency.Status)
                    changes.Add($"Status: {oldStatus} → {userCompetency.Status}");
                    
                if (oldCompletionDate != userCompetency.CompletionDate)
                    changes.Add($"Completion Date: {oldCompletionDate?.ToShortDateString() ?? "None"} → {userCompetency.CompletionDate?.ToShortDateString() ?? "None"}");
                    
                if (oldExpiryDate != userCompetency.ExpiryDate)
                    changes.Add($"Expiry Date: {oldExpiryDate?.ToShortDateString() ?? "None"} → {userCompetency.ExpiryDate?.ToShortDateString() ?? "None"}");
                    
                if (oldRemarks != userCompetency.Remarks)
                    changes.Add($"Remarks: {oldRemarks ?? "None"} → {userCompetency.Remarks ?? "None"}");
                    
                if (oldBuilding != userCompetency.Building)
                    changes.Add($"Building: {oldBuilding ?? "None"} → {userCompetency.Building ?? "None"}");
                    
                if (documentChanged)
                    changes.Add("Document: Updated");
                
                // Log the update
                string changeDetails = changes.Count > 0 ? string.Join("; ", changes) : "No fields changed";
                LogUpdate("UserCompetency", model.Id.ToString(), 
                    null, null, 
                    $"Updated competency '{userCompetency.CompetencyModule?.ModuleName}' for user '{userCompetency.User?.UserName}'. Changes: {changeDetails}");
                
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

        // GET: UserCompetency/DeleteConfirm/5
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirm(int id)
        {
            var userCompetency = _db.UserCompetencies
                .Include(uc => uc.User)
                .Include(uc => uc.CompetencyModule)
                .FirstOrDefault(uc => uc.Id == id);
                
            if (userCompetency == null)
            {
                TempData["ErrorMessage"] = "User competency not found.";
                return RedirectToAction("Index");
            }
            
            // Log the view action
            LogView("UserCompetency", id.ToString(), 
                $"Viewed delete confirmation for competency '{userCompetency.CompetencyModule?.ModuleName}' assigned to user '{userCompetency.User?.UserName}'");
            
            return View(userCompetency);
        }

        // POST: UserCompetency/Delete/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                var userCompetency = _db.UserCompetencies
                    .Include(uc => uc.User)
                    .Include(uc => uc.CompetencyModule)
                    .FirstOrDefault(uc => uc.Id == id);
                
                if (userCompetency != null)
                {
                    // Store info for logging
                    string userName = userCompetency.User?.UserName;
                    string moduleName = userCompetency.CompetencyModule?.ModuleName;
                    
                    // Delete associated document if exists
                    if (!string.IsNullOrEmpty(userCompetency.DocumentPath))
                    {
                        string filePath = Server.MapPath(userCompetency.DocumentPath);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    _db.UserCompetencies.Remove(userCompetency);
                    _db.SaveChanges();
                    
                    // Log the deletion
                    LogDeletion("UserCompetency", id.ToString(), 
                        $"Removed competency '{moduleName}' from user '{userName}'");
                    
                    TempData["SuccessMessage"] = "User competency removed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "User competency not found.";
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine("Error deleting user competency: " + ex.Message);
                TempData["ErrorMessage"] = "An error occurred while deleting the competency: " + ex.Message;
            }
            
            return RedirectToAction("Index");
        }

        // GET: UserCompetency/DownloadDocument/5
        public ActionResult DownloadDocument(int id)
        {
            var userCompetency = _db.UserCompetencies
                .Include(uc => uc.User)
                .Include(uc => uc.CompetencyModule)
                .FirstOrDefault(uc => uc.Id == id);
            
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
            
            // Log the view action
            LogView("UserCompetencyDocument", id.ToString(), 
                $"Downloaded document for competency '{userCompetency.CompetencyModule?.ModuleName}' assigned to user '{userCompetency.User?.UserName}'");
            
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
            
            var user = _db.Users.Find(userId);
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }
            
            var userCompetencies = _db.UserCompetencies
                .Include(uc => uc.CompetencyModule)
                .Where(uc => uc.UserId == userId)
                .ToList();
                
            ViewBag.User = user;
            ViewBag.ATOMCEPPoints = user.Atom_CEP ?? 0;
            ViewBag.DOECPDPoints = user.DOE_CPD ?? 0;
            ViewBag.DOSHCEPPoints = user.Dosh_CEP ?? 0;
            
            // Log the view action
            LogView("UserCompetency", userId, $"Viewed competencies for user '{user.UserName}'");
            
            return View(userCompetencies);
        }

        // GET: UserCompetency/MyCompetencies
        public ActionResult MyCompetencies()
        {
            string userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);
            
            var userCompetencies = _db.UserCompetencies
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