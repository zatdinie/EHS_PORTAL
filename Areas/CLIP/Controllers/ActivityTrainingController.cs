using System;
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
    public class ActivityTrainingController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        // GET: CLIP/ActivityTraining
        public ActionResult Index()
        {
            string userId = User.Identity.GetUserId();
            var activities = db.ActivityTrainings
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ActivityDate)
                .ToList();
            
            return View(activities);
        }
        
        // GET: CLIP/ActivityTraining/Create
        public ActionResult Create()
        {
            return View(new ActivityTrainingViewModel 
            { 
                ActivityDate = DateTime.Today 
            });
        }
        
        // POST: CLIP/ActivityTraining/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ActivityTrainingViewModel model)
        {
            if (ModelState.IsValid)
            {
                string userId = User.Identity.GetUserId();
                string fileName = null;
                
                // Handle document upload if provided
                if (model.DocumentFile != null && model.DocumentFile.ContentLength > 0)
                {
                    string uploadsFolder = Server.MapPath("~/Areas/CLIP/Uploads/ActivityTraining");
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Generate a unique filename
                    fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.DocumentFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    // Save the file
                    model.DocumentFile.SaveAs(filePath);
                }
                
                // Create and save the activity training record
                var activity = new ActivityTraining
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ActivityName = model.ActivityName,
                    ActivityDate = model.ActivityDate,
                    Document = fileName,
                    ATOM_CEP_Points = model.ATOM_CEP_Points ?? 0,
                    DOE_CPD_Points = model.DOE_CPD_Points ?? 0,
                    DOSH_CEP_Points = model.DOSH_CEP_Points ?? 0
                };
                
                db.ActivityTrainings.Add(activity);
                
                // Update user's points
                var user = db.Users.Find(userId);
                if (user != null)
                {
                    if (model.ATOM_CEP_Points.HasValue)
                    {
                        user.Atom_CEP = (user.Atom_CEP ?? 0) + model.ATOM_CEP_Points.Value;
                    }
                    
                    if (model.DOE_CPD_Points.HasValue)
                    {
                        user.DOE_CPD = (user.DOE_CPD ?? 0) + model.DOE_CPD_Points.Value;
                    }
                    
                    if (model.DOSH_CEP_Points.HasValue)
                    {
                        user.Dosh_CEP = (user.Dosh_CEP ?? 0) + model.DOSH_CEP_Points.Value;
                    }
                }
                
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Training activity logged successfully.";
                return RedirectToAction("Index");
            }
            
            return View(model);
        }
        
        // GET: CLIP/ActivityTraining/Details/5
        public ActionResult Details(Guid id)
        {
            var activity = db.ActivityTrainings
                .Include(a => a.User)
                .FirstOrDefault(a => a.Id == id);
                
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found.";
                return RedirectToAction("Index");
            }
            
            return View(activity);
        }
        
        // GET: CLIP/ActivityTraining/Edit/5
        public ActionResult Edit(Guid id)
        {
            var activity = db.ActivityTrainings.Find(id);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found.";
                return RedirectToAction("Index");
            }
            
            // Check if current user is the owner of this activity
            string userId = User.Identity.GetUserId();
            if (activity.UserId != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this activity.";
                return RedirectToAction("Index");
            }
            
            var viewModel = new ActivityTrainingViewModel
            {
                Id = activity.Id,
                ActivityName = activity.ActivityName,
                ActivityDate = activity.ActivityDate,
                ATOM_CEP_Points = activity.ATOM_CEP_Points,
                DOE_CPD_Points = activity.DOE_CPD_Points,
                DOSH_CEP_Points = activity.DOSH_CEP_Points
            };
            
            ViewBag.CurrentDocumentName = activity.Document;
            
            return View(viewModel);
        }
        
        // POST: CLIP/ActivityTraining/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Guid id, ActivityTrainingViewModel model)
        {
            if (ModelState.IsValid)
            {
                var activity = db.ActivityTrainings.Find(id);
                if (activity == null)
                {
                    TempData["ErrorMessage"] = "Activity not found.";
                    return RedirectToAction("Index");
                }
                
                // Check if current user is the owner of this activity
                string userId = User.Identity.GetUserId();
                if (activity.UserId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this activity.";
                    return RedirectToAction("Index");
                }
                
                // Track point changes for updating user's total points
                int? originalCEP = activity.ATOM_CEP_Points;
                int? originalCPD = activity.DOE_CPD_Points;
                int? originalDOSH = activity.DOSH_CEP_Points;

                
                // Update the activity properties
                activity.ActivityName = model.ActivityName;
                activity.ActivityDate = model.ActivityDate;
                activity.ATOM_CEP_Points = model.ATOM_CEP_Points ?? 0;
                activity.DOE_CPD_Points = model.DOE_CPD_Points ?? 0;
                activity.DOSH_CEP_Points = model.DOSH_CEP_Points ?? 0;
                
                // Handle document upload if provided
                if (model.DocumentFile != null && model.DocumentFile.ContentLength > 0)
                {
                    string uploadsFolder = Server.MapPath("~/Areas/CLIP/Uploads/ActivityTraining");
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Delete previous file if exists
                    if (!string.IsNullOrEmpty(activity.Document))
                    {
                        string previousFilePath = Path.Combine(uploadsFolder, activity.Document);
                        if (System.IO.File.Exists(previousFilePath))
                        {
                            System.IO.File.Delete(previousFilePath);
                        }
                    }
                    
                    // Generate a unique filename
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.DocumentFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    
                    // Save the file
                    model.DocumentFile.SaveAs(filePath);
                    
                    // Update document filename in the database
                    activity.Document = fileName;
                }
                
                // Update user's points based on the difference
                var user = db.Users.Find(userId);
                if (user != null)
                {
                    // Update CEP points
                    int oldPoints = originalCEP ?? 0;
                    int newPoints = model.ATOM_CEP_Points ?? 0;
                    user.Atom_CEP = (user.Atom_CEP ?? 0) - oldPoints + newPoints;
                    
                    // Update CPD points
                    int oldPointsCPD = originalCPD ?? 0;
                    int newPointsCPD = model.DOE_CPD_Points ?? 0;
                    user.DOE_CPD = (user.DOE_CPD ?? 0) - oldPointsCPD + newPointsCPD;
                    
                    // Update DOSH CEP points
                    int oldPointsDOSH = originalDOSH ?? 0;
                    int newPointsDOSH = model.DOSH_CEP_Points ?? 0;
                    user.Dosh_CEP = (user.Dosh_CEP ?? 0) - oldPointsDOSH + newPointsDOSH;
                }
                
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Training activity updated successfully.";
                return RedirectToAction("Index");
            }
            
            ViewBag.CurrentDocumentName = db.ActivityTrainings.Find(id)?.Document;
            return View(model);
        }
        
        // POST: CLIP/ActivityTraining/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid id)
        {
            var activity = db.ActivityTrainings.Find(id);
            if (activity != null)
            {
                // Check if current user is the owner of this activity
                string userId = User.Identity.GetUserId();
                if (activity.UserId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to delete this activity.";
                    return RedirectToAction("Index");
                }
                
                // Update user's points by subtracting this activity's points
                var user = db.Users.Find(userId);
                if (user != null)
                {
                    user.Atom_CEP = (user.Atom_CEP ?? 0) - activity.ATOM_CEP_Points;
                    if (user.Atom_CEP < 0) user.Atom_CEP = 0;
                    
                    user.DOE_CPD = (user.DOE_CPD ?? 0) - activity.DOE_CPD_Points;
                    if (user.DOE_CPD < 0) user.DOE_CPD = 0;
                    
                    user.Dosh_CEP = (user.Dosh_CEP ?? 0) - activity.DOSH_CEP_Points;
                    if (user.Dosh_CEP < 0) user.Dosh_CEP = 0;
                }
                
                // Delete file if exists
                if (!string.IsNullOrEmpty(activity.Document))
                {
                    string uploadsFolder = Server.MapPath("~/Areas/CLIP/Uploads/ActivityTraining");
                    string filePath = Path.Combine(uploadsFolder, activity.Document);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                
                db.ActivityTrainings.Remove(activity);
                db.SaveChanges();
                
                TempData["SuccessMessage"] = "Training activity deleted successfully.";
            }
            
            return RedirectToAction("Index");
        }
        
        // GET: CLIP/ActivityTraining/DownloadDocument/5
        public ActionResult DownloadDocument(Guid id)
        {
            var activity = db.ActivityTrainings.Find(id);
            if (activity == null || string.IsNullOrEmpty(activity.Document))
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToAction("Index");
            }
            
            string uploadsFolder = Server.MapPath("~/Areas/CLIP/Uploads/ActivityTraining");
            string filePath = Path.Combine(uploadsFolder, activity.Document);
            
            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Document file not found.";
                return RedirectToAction("Index");
            }
            
            // Determine content type based on file extension
            string contentType = MimeMapping.GetMimeMapping(filePath);
            
            return File(filePath, contentType, "Document_" + activity.ActivityName + Path.GetExtension(filePath));
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