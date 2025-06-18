using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Infrastructure;
using EHS_PORTAL.Areas.CLIP.Models;
using EHS_PORTAL.Areas.CLIP.Filters;
using Newtonsoft.Json;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize]
    public class CompetencyController : BaseController
    {
        public ActionResult Index()
        {
            // Get list of all competency modules
            var competencyModules = _db.CompetencyModules.ToList();
            
            
            return View("Competency", competencyModules);
        }

        public ActionResult Add()
        {
            return View("AddCompetency");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(CompetencyModule model)
        {
            // Only validate AnnualPointDeduction when CompetencyType is Safety
            if (model.CompetencyType != "Safety")
            {
                // Remove validation errors for AnnualPointDeduction
                ModelState.Remove("AnnualPointDeduction");
                // Reset to null since it's not needed for non-Safety types
                model.AnnualPointDeduction = null;
            }
            
            if (ModelState.IsValid)
            {
                // Check for existing module with the same name
                bool nameExists = _db.CompetencyModules.Any(c => c.ModuleName == model.ModuleName);
                if (nameExists)
                {
                    ModelState.AddModelError("ModuleName", "A competency module with this name already exists.");
                    return View("AddCompetency", model);
                }

                try
                {
                    _db.CompetencyModules.Add(model);
                    _db.SaveChanges();
                    
                    // Log the creation
                    LogCreation("CompetencyModule", model.Id.ToString(), $"Created competency module: {model.ModuleName}");
                    
                    TempData["SuccessMessage"] = "Competency module added successfully.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.InnerException?.Message?.Contains("unique constraint") == true ||
                        ex.InnerException?.InnerException?.Message?.Contains("duplicate") == true)
                    {
                        ModelState.AddModelError("ModuleName", "A competency module with this name already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, contact your system administrator.");
                    }
                }
            }
            
            return View("AddCompetency", model);
        }

        public ActionResult Edit(int id)
        {
            var competency = _db.CompetencyModules.Find(id);

            if (competency == null)
            {
                TempData["ErrorMessage"] = "Competency module not found.";
                return RedirectToAction("Index");
            }

            // Log the view action
            LogView("CompetencyModule", id.ToString(), $"Viewed competency module: {competency.ModuleName}");

            return View("EditCompetency", competency);  
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CompetencyModule model)
        {
            // Only validate AnnualPointDeduction when CompetencyType is Safety
            if (model.CompetencyType != "Safety")
            {
                // Remove validation errors for AnnualPointDeduction
                ModelState.Remove("AnnualPointDeduction");
                // Reset to null since it's not needed for non-Safety types
                model.AnnualPointDeduction = null;
            }
            
            if (ModelState.IsValid)
            {
                var competency = _db.CompetencyModules.Find(model.Id);
                
                if (competency == null)
                {
                    TempData["ErrorMessage"] = "Competency module not found.";
                    return RedirectToAction("Index");
                }

                // Check for existing module with the same name (excluding current module)
                bool nameExists = _db.CompetencyModules
                    .Any(c => c.ModuleName == model.ModuleName && c.Id != model.Id);
                
                if (nameExists)
                {
                    ModelState.AddModelError("ModuleName", "A competency module with this name already exists.");
                    return View("EditCompetency", model);
                }
                
                try
                {
                    // Store old values for logging
                    string oldValues = JsonConvert.SerializeObject(new {
                        competency.ModuleName,
                        competency.Description,
                        competency.CompetencyType,
                        competency.AnnualPointDeduction
                    });
                    
                    // Update the competency properties
                    competency.ModuleName = model.ModuleName;
                    competency.Description = model.Description;
                    competency.CompetencyType = model.CompetencyType;
                    competency.AnnualPointDeduction = model.AnnualPointDeduction;
                    _db.SaveChanges();
                    
                    // Store new values for logging
                    string newValues = JsonConvert.SerializeObject(new {
                        competency.ModuleName,
                        competency.Description,
                        competency.CompetencyType,
                        competency.AnnualPointDeduction
                    });
                    
                    // Log the update
                    LogUpdate("CompetencyModule", model.Id.ToString(), oldValues, newValues, 
                        $"Updated competency module: {model.ModuleName}");
                    
                    TempData["SuccessMessage"] = "Competency module updated successfully.";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.InnerException?.Message?.Contains("unique constraint") == true ||
                        ex.InnerException?.InnerException?.Message?.Contains("duplicate") == true)
                    {
                        ModelState.AddModelError("ModuleName", "A competency module with this name already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, contact your system administrator.");
                    }
                }
            }
            
            return View("EditCompetency", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var competency = _db.CompetencyModules.Find(id);
            
            if (competency != null)
            {
                // Check if the competency is in use before deleting
                bool isInUse = _db.UserCompetencies.Any(uc => uc.CompetencyModuleId == id);
                
                if (isInUse)
                {
                    TempData["ErrorMessage"] = "This competency cannot be deleted because it is assigned to one or more users.";
                }
                else
                {
                    string moduleName = competency.ModuleName;
                    
                    _db.CompetencyModules.Remove(competency);
                    _db.SaveChanges();
                    
                    // Log the deletion
                    LogDeletion("CompetencyModule", id.ToString(), $"Deleted competency module: {moduleName}");
                    
                    TempData["SuccessMessage"] = "Competency module deleted successfully.";
                }
            }
            
            return RedirectToAction("Index");
        }
    }
} 