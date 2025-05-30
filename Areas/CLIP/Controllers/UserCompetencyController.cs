using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        public ActionResult Assign(UserCompetency model, string[] Building)
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
        public ActionResult Edit(UserCompetency model, string[] Building)
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
                db.UserCompetencies.Remove(userCompetency);
                db.SaveChanges();
                TempData["SuccessMessage"] = "User competency removed successfully.";
            }
            
            return RedirectToAction("Index");
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