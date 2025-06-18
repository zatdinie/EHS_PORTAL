using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using System.Globalization;
using System.IO;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.AspNet.Identity.EntityFramework;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize]
    public class PlantMonitoringController : BaseController
    {
        // GET: PlantMonitoring
        public ActionResult Index(string category = null, string plantFilter = null, string status = null, string monitoringType = null, int? frequency = null)
        {
            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine($"INDEX ACTION CALLED - User: {User.Identity.Name}");
            System.Diagnostics.Debug.WriteLine($"Filters - Category: {category}, Plant: {plantFilter}, Status: {status}, Type: {monitoringType}, Frequency: {frequency}");
            System.Diagnostics.Debug.WriteLine("========================================");
            
            // Get all plant monitoring items with plant and monitoring details
            var query = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .AsQueryable();

            // Get user's assigned plants to mark which ones they can update
            var userId = User.Identity.GetUserId();
            var userPlantIds = _db.UserPlants
                .Where(up => up.UserId == userId)
                .Select(up => up.PlantId)
                .ToList();
            
            ViewBag.UserPlantIds = userPlantIds;
            
            // Apply filters if provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Monitoring.MonitoringCategory == category);
                ViewBag.SelectedCategory = category;
                System.Diagnostics.Debug.WriteLine($"Filtering by category: {category}");
            }

            // Apply plant filter
            if (!string.IsNullOrEmpty(plantFilter))
            {
                // Handle both old (concatenated) and new (comma-separated) formats
                var plantIds = plantFilter.Split(',').Select(p => int.TryParse(p, out int id) ? id : -1).Where(id => id != -1).ToList();
                
                if (plantIds.Any())
                {
                    query = query.Where(p => plantIds.Contains(p.PlantID));
                    ViewBag.SelectedPlantFilter = plantFilter;
                    System.Diagnostics.Debug.WriteLine($"Filtering by plants: {string.Join(", ", plantIds)}");
                }
                else
                {
                    // If we couldn't parse any IDs, store the original filter string
                    ViewBag.SelectedPlantFilter = plantFilter;
                    System.Diagnostics.Debug.WriteLine($"Could not parse plant IDs from: {plantFilter}");
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                // Process and expiry statuses
                if (status == "Completed" || status == "Work In Progress" || status == "ePR Raised" || 
                    status == "Quotation Requested" || status == "Not Started" || status == "In Progress" || 
                    status == "In Preparation" || status == "In Quotation")
                {
                    // Handle legacy status names
                    if (status == "In Progress") status = "Work In Progress";
                    if (status == "In Preparation") status = "ePR Raised";
                    if (status == "In Quotation") status = "Quotation Requested";
                    
                    query = query.Where(p => p.ProcStatus == status);
                    System.Diagnostics.Debug.WriteLine($"Filtering by process status: {status}");
                }
                else
                {
                    query = query.Where(p => p.ExpStatus == status);
                    System.Diagnostics.Debug.WriteLine($"Filtering by expiry status: {status}");
                }

                ViewBag.SelectedStatus = status;
            }

            // New filter for monitoring type
            if (!string.IsNullOrEmpty(monitoringType))
            {
                query = query.Where(p => p.Monitoring.MonitoringName == monitoringType);
                ViewBag.SelectedMonitoringType = monitoringType;
                System.Diagnostics.Debug.WriteLine($"Filtering by monitoring type: {monitoringType}");
            }

            // New filter for frequency
            if (frequency.HasValue)
            {
                query = query.Where(p => p.Monitoring.MonitoringFreq == frequency.Value);
                ViewBag.SelectedFrequency = frequency.Value;
                System.Diagnostics.Debug.WriteLine($"Filtering by frequency: {frequency}");
            }

            // Load plants and monitoring categories for filtering
            // If user is admin, get all plants, otherwise get only the user's plants
            if (User.IsInRole("Admin"))
            {
                ViewBag.Plants = _db.Plants.OrderBy(p => p.PlantName).ToList();
            }
            else
            {
                ViewBag.Plants = _db.UserPlants
                    .Where(up => up.UserId == userId)
                    .Select(up => up.Plant)
                    .OrderBy(p => p.PlantName)
                    .ToList();
            }
            
            ViewBag.Categories = _db.Monitorings
                .Select(m => m.MonitoringCategory)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Add monitoring types for the filter
            ViewBag.MonitoringTypes = _db.Monitorings
                .Select(m => m.MonitoringName)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            ViewBag.StatusList = new List<string>
            {
                "All",
                // Process Statuses
                "Completed",
                "Work In Progress",
                "ePR Raised",
                "Quotation Requested",
                "Not Started",
                // Expiration Statuses
                "Active",
                "Expiring Soon",
                "Expired",
                "No Expiry"
            };

            // Get monitoring notifications for the current user
            ViewBag.Notifications = GetMonitoringNotifications();
            
            // Execute the query and get results
            var result = query.ToList();
            System.Diagnostics.Debug.WriteLine($"Query returned {result.Count} results");
            
            return View(result);
        }

        // GET: PlantMonitoring/Schedule
        public ActionResult Schedule(string plantFilter = null)
        {
            // Set the selected plant filter in ViewBag
            ViewBag.SelectedPlantFilter = plantFilter;
            
            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine($"SCHEDULE ACTION CALLED - Plant Filter: {plantFilter}");
            System.Diagnostics.Debug.WriteLine("========================================");
            
            // Get all monitoring types
            var monitoringTypes = _db.Monitorings
                .OrderBy(m => m.MonitoringCategory)
                .ThenBy(m => m.MonitoringName)
                .ToList();

            // Get all plants
            var plants = _db.Plants.OrderBy(p => p.PlantName).ToList();
            
            // Get user's assigned plants to mark which ones they can update
            var userId = User.Identity.GetUserId();
            var userPlantIds = _db.UserPlants
                .Where(up => up.UserId == userId)
                .Select(up => up.PlantId)
                .ToList();
            
            ViewBag.UserPlantIds = userPlantIds;

            // Get all plant monitoring records
            var plantMonitoringsQuery = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .AsQueryable();

            // Apply plant filter if provided
            if (!string.IsNullOrEmpty(plantFilter))
            {
                var plantIds = plantFilter.Split(',').Select(p => int.TryParse(p, out int id) ? id : -1).Where(id => id != -1).ToList();
                System.Diagnostics.Debug.WriteLine($"Parsed plant IDs: {string.Join(", ", plantIds)}");
                
                // Count records before filtering
                var beforeCount = plantMonitoringsQuery.Count();
                System.Diagnostics.Debug.WriteLine($"Records before filtering: {beforeCount}");
                
                if (plantIds.Any())
                {
                    plantMonitoringsQuery = plantMonitoringsQuery.Where(p => plantIds.Contains(p.PlantID));
                    System.Diagnostics.Debug.WriteLine($"Filtered query to include only plants: {string.Join(", ", plantIds)}");
                    
                    // Count records after filtering
                    var afterCount = plantMonitoringsQuery.Count();
                    System.Diagnostics.Debug.WriteLine($"Records after filtering: {afterCount}");
                }
            }

            var plantMonitorings = plantMonitoringsQuery.ToList();

            // Create a dictionary to group by monitoring type and plant
            var currentYear = DateTime.Now.Year;
            var data = new Dictionary<int, Dictionary<int, List<PlantMonitoringViewModel>>>();

            foreach (var pm in plantMonitorings)
            {
                if (!data.ContainsKey(pm.MonitoringID))
                {
                    data[pm.MonitoringID] = new Dictionary<int, List<PlantMonitoringViewModel>>();
                }

                if (!data[pm.MonitoringID].ContainsKey(pm.PlantID))
                {
                    data[pm.MonitoringID][pm.PlantID] = new List<PlantMonitoringViewModel>();
                }

                var viewModel = new PlantMonitoringViewModel
                {
                    Id = pm.Id,
                    Area = pm.Area,
                    ProcStatus = pm.ProcStatus,
                    ExpStatus = pm.ExpStatus,
                    ExpDate = pm.ExpDate,
                    QuoteDate = pm.QuoteDate,
                    QuoteCompleteDate = pm.QuoteCompleteDate,
                    EprDate = pm.EprDate,
                    EprCompleteDate = pm.EprCompleteDate,
                    WorkDate = pm.WorkDate,
                    WorkCompleteDate = pm.WorkCompleteDate,
                    Remarks = pm.Remarks
                };

                data[pm.MonitoringID][pm.PlantID].Add(viewModel);
            }

            ViewBag.MonitoringTypes = monitoringTypes;
            ViewBag.Plants = plants;
            ViewBag.Data = data;
            ViewBag.CurrentYear = currentYear;
            ViewBag.MonthNames = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
            ViewBag.IsAdmin = User.IsInRole("Admin");

            return View();
        }

        // GET: PlantMonitoring/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PlantMonitoring plantMonitoring = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .FirstOrDefault(p => p.Id == id);

            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to update this plant monitoring record
            bool userHasAccessToUpdate = User.IsInRole("Admin");
            if (!userHasAccessToUpdate)
            {
                var userId = User.Identity.GetUserId();
                userHasAccessToUpdate = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
            }
            
            ViewBag.UserHasAccessToUpdate = userHasAccessToUpdate;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            
            // Get monitoring notifications for the current user
            ViewBag.Notifications = GetMonitoringNotifications();
            
            return View(plantMonitoring);
        }

        // GET: PlantMonitoring/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.PlantID = new SelectList(_db.Plants.OrderBy(p => p.PlantName), "Id", "PlantName");
            ViewBag.MonitoringID = new SelectList(_db.Monitorings.OrderBy(m => m.MonitoringName), "MonitoringID", "MonitoringName");
            
            // Create SelectList for user assignments
            var users = _db.Users.OrderBy(u => u.UserName).ToList();
            ViewBag.UsersList = new SelectList(users, "UserName", "UserName");
            
            return View();
        }

        // POST: PlantMonitoring/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(PlantMonitoring plantMonitoring)
        {
            if (ModelState.IsValid)
            {
                // If WorkCompleteDate is present (meaning the monitoring is completed),
                // set the renewal date (ExpDate) based on monitoring frequency
                if (plantMonitoring.WorkCompleteDate.HasValue)
                {
                    // Get the monitoring type to determine frequency
                    var monitoring = _db.Monitorings.Find(plantMonitoring.MonitoringID);
                    if (monitoring != null)
                    {
                        // Set the expiry date based on the monitoring frequency
                        int frequencyMonths = monitoring.MonitoringFreq;
                        plantMonitoring.ExpDate = plantMonitoring.WorkCompleteDate.Value.AddMonths(frequencyMonths);
                    }
                }
                
                plantMonitoring.CalculateStatuses();
                _db.PlantMonitorings.Add(plantMonitoring);
                _db.SaveChanges();
                
                // Get plant and monitoring names for logging
                var plant = _db.Plants.Find(plantMonitoring.PlantID);
                var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
                string plantName = plant != null ? plant.PlantName : "Unknown Plant";
                string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";
                
                // Log creation
                LogCreation(
                    "PlantMonitoring",
                    plantMonitoring.Id.ToString(),
                    $"Created PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}, Status: {plantMonitoring.ProcStatus}"
                );
                return RedirectToAction("Index");
            }

            ViewBag.PlantID = new SelectList(_db.Plants.OrderBy(p => p.PlantName), "Id", "PlantName", plantMonitoring.PlantID);
            ViewBag.MonitoringID = new SelectList(_db.Monitorings.OrderBy(m => m.MonitoringName), "MonitoringID", "MonitoringName", plantMonitoring.MonitoringID);
            
            // Create SelectList for user assignments
            var users = _db.Users.OrderBy(u => u.UserName).ToList();
            ViewBag.UsersList = new SelectList(users, "UserName", "UserName");
            
            return View(plantMonitoring);
        }

        // GET: PlantMonitoring/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PlantMonitoring plantMonitoring = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .FirstOrDefault(p => p.Id == id);
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }
            
            // Clear any existing model state to ensure fresh values
            ModelState.Clear();
            
            ViewBag.MonitoringID = new SelectList(_db.Monitorings.OrderBy(m => m.MonitoringName), "MonitoringID", "MonitoringName", plantMonitoring.MonitoringID);
            ViewBag.PlantID = new SelectList(_db.Plants.OrderBy(p => p.PlantName), "Id", "PlantName", plantMonitoring.PlantID);
            
            // Get users that have access to this plant
            var usersWithAccess = _db.UserPlants
                .Where(up => up.PlantId == plantMonitoring.PlantID)
                .Select(up => up.UserId)
                .ToList();
            
            var users = _db.Users
                .Where(u => usersWithAccess.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .ToList();
            
            // Create SelectList for user assignments
            ViewBag.UsersList = new SelectList(users, "UserName", "UserName");
            
            return View(plantMonitoring);
        }

        // POST: PlantMonitoring/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(PlantMonitoring plantMonitoring)
        {
            if (ModelState.IsValid)
            {
                // Fetch the original entity for logging
                var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == plantMonitoring.Id);
                // If WorkCompleteDate is present (meaning the monitoring is completed),
                // set the renewal date (ExpDate) based on monitoring frequency
                if (plantMonitoring.WorkCompleteDate.HasValue)
                {
                    // Get the monitoring type to determine frequency
                    var monitoring = _db.Monitorings.Find(plantMonitoring.MonitoringID);
                    if (monitoring != null)
                    {
                        // Set the expiry date based on the monitoring frequency
                        int frequencyMonths = monitoring.MonitoringFreq;
                        plantMonitoring.ExpDate = plantMonitoring.WorkCompleteDate.Value.AddMonths(frequencyMonths);
                    }
                }
                
                plantMonitoring.CalculateStatuses();
                _db.Entry(plantMonitoring).State = EntityState.Modified;
                _db.SaveChanges();
                
                // Get plant and monitoring names for logging
                var plant = _db.Plants.Find(plantMonitoring.PlantID);
                var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
                string plantName = plant != null ? plant.PlantName : "Unknown Plant";
                string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";
                
                // Log update
                string changes = "";
                if (original != null)
                {
                    var changedFields = new List<string>();
                    if (original.PlantID != plantMonitoring.PlantID) changedFields.Add($"PlantID: {original.PlantID} → {plantMonitoring.PlantID}");
                    if (original.MonitoringID != plantMonitoring.MonitoringID) changedFields.Add($"MonitoringID: {original.MonitoringID} → {plantMonitoring.MonitoringID}");
                    if (original.ProcStatus != plantMonitoring.ProcStatus) changedFields.Add($"ProcStatus: {original.ProcStatus} → {plantMonitoring.ProcStatus}");
                    if (original.ExpStatus != plantMonitoring.ExpStatus) changedFields.Add($"ExpStatus: {original.ExpStatus} → {plantMonitoring.ExpStatus}");
                    if (original.ExpDate != plantMonitoring.ExpDate) changedFields.Add($"ExpDate: {original.ExpDate} → {plantMonitoring.ExpDate}");
                    if (original.Remarks != plantMonitoring.Remarks) changedFields.Add($"Remarks changed");
                    changes = changedFields.Count > 0 ? string.Join("; ", changedFields) : "No fields changed";
                }
                LogUpdate(
                    "PlantMonitoring",
                    plantMonitoring.Id.ToString(),
                    null, null,
                    $"Updated PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
                );
                return RedirectToAction("Index");
            }
            ViewBag.PlantID = new SelectList(_db.Plants.OrderBy(p => p.PlantName), "Id", "PlantName", plantMonitoring.PlantID);
            ViewBag.MonitoringID = new SelectList(_db.Monitorings.OrderBy(m => m.MonitoringName), "MonitoringID", "MonitoringName", plantMonitoring.MonitoringID);
            return View(plantMonitoring);
        }

        // GET: PlantMonitoring/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PlantMonitoring plantMonitoring = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .FirstOrDefault(p => p.Id == id);
                
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }
            return View(plantMonitoring);
        }

        // POST: PlantMonitoring/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            PlantMonitoring plantMonitoring = _db.PlantMonitorings.Find(id);
            // Store details for logging before deletion
            int plantId = plantMonitoring.PlantID;
            int monitoringId = plantMonitoring.MonitoringID;
            string procStatus = plantMonitoring.ProcStatus;
            
            // Get plant and monitoring names for logging
            var plant = _db.Plants.Find(plantId);
            var monitoring = _db.Monitorings.Find(monitoringId);
            string plantName = plant != null ? plant.PlantName : "Unknown Plant";
            string monitoringName = monitoring != null ? monitoring.MonitoringName : "Unknown Monitoring";
            
            _db.PlantMonitorings.Remove(plantMonitoring);
            _db.SaveChanges();
            // Log deletion
            LogDeletion(
                "PlantMonitoring",
                id.ToString(),
                $"Deleted PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}, Status: {procStatus}"
            );
            return RedirectToAction("Index");
        }

        // GET: PlantMonitoring/UpdateStatus/5
        public ActionResult UpdateStatus(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PlantMonitoring plantMonitoring = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .FirstOrDefault(p => p.Id == id);
                
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }

            // Get users for dropdown lists
            List<ApplicationUser> users;
            
            if (User.IsInRole("Admin"))
            {
                // If admin, show only users that have access to this plant
                var usersWithAccess = _db.UserPlants
                    .Where(up => up.PlantId == plantMonitoring.PlantID)
                    .Select(up => up.UserId)
                    .ToList();
                
                users = _db.Users
                    .Where(u => usersWithAccess.Contains(u.Id))
                    .OrderBy(u => u.UserName)
                    .ToList();
            }
            else
            {
                // For non-admin users, they can only self-assign
                users = new List<ApplicationUser> { _db.Users.FirstOrDefault(u => u.UserName == User.Identity.Name) };
            }
            
            // Create SelectList for user assignments
            ViewBag.UsersList = new SelectList(users, "UserName", "UserName");

            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View(plantMonitoring);
        }

        // POST: PlantMonitoring/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, PlantMonitoring model, HttpPostedFileBase quoteDocument, HttpPostedFileBase eprDocument, HttpPostedFileBase workDocument)
        {
            if (ModelState.IsValid)
            {
                // Fetch the original entity for logging
                var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == id);
                var plantMonitoring = _db.PlantMonitorings.Find(id);
                if (plantMonitoring == null)
                {
                    return HttpNotFound();
                }

                // Check if user has access to this plant monitoring record
                if (!User.IsInRole("Admin"))
                {
                    var userId = User.Identity.GetUserId();
                    var userHasAccessToPlant = _db.UserPlants
                        .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                    
                    if (!userHasAccessToPlant)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                    }
                    
                    // For non-admin users, enforce self-assignment to all phases
                    model.QuoteUserAssign = User.Identity.Name;
                    model.EprUserAssign = User.Identity.Name;
                    model.WorkUserAssign = User.Identity.Name;
                }
                else
                {
                    // For admin users, validate that assigned users have access to this plant
                    var usersWithAccess = _db.UserPlants
                        .Where(up => up.PlantId == plantMonitoring.PlantID)
                        .Include(up => up.User)
                        .Select(up => up.User.UserName)
                        .ToList();
                        
                    // Check if assigned users have access to the plant
                    if (!string.IsNullOrEmpty(model.QuoteUserAssign) && !usersWithAccess.Contains(model.QuoteUserAssign))
                    {
                        ModelState.AddModelError("QuoteUserAssign", "The assigned user does not have access to this plant.");
                        var validationEntity = _db.PlantMonitorings
                            .Include(p => p.Plant)
                            .Include(p => p.Monitoring)
                            .FirstOrDefault(p => p.Id == id);
                        
                        if (validationEntity != null)
                        {
                            model.Plant = validationEntity.Plant;
                            model.Monitoring = validationEntity.Monitoring;
                        }
                        
                        // Recreate user list
                        var filteredUsers = _db.Users
                            .Where(u => usersWithAccess.Contains(u.UserName))
                            .OrderBy(u => u.UserName)
                            .ToList();
                            
                        ViewBag.UsersList = new SelectList(filteredUsers, "UserName", "UserName");
                        ViewBag.IsAdmin = User.IsInRole("Admin");
                        return View(model);
                    }
                    
                    if (!string.IsNullOrEmpty(model.EprUserAssign) && !usersWithAccess.Contains(model.EprUserAssign))
                    {
                        ModelState.AddModelError("EprUserAssign", "The assigned user does not have access to this plant.");
                        var validationEntity = _db.PlantMonitorings
                            .Include(p => p.Plant)
                            .Include(p => p.Monitoring)
                            .FirstOrDefault(p => p.Id == id);
                        
                        if (validationEntity != null)
                        {
                            model.Plant = validationEntity.Plant;
                            model.Monitoring = validationEntity.Monitoring;
                        }
                        
                        // Recreate user list
                        var filteredUsers = _db.Users
                            .Where(u => usersWithAccess.Contains(u.UserName))
                            .OrderBy(u => u.UserName)
                            .ToList();
                            
                        ViewBag.UsersList = new SelectList(filteredUsers, "UserName", "UserName");
                        ViewBag.IsAdmin = User.IsInRole("Admin");
                        return View(model);
                    }
                    
                    if (!string.IsNullOrEmpty(model.WorkUserAssign) && !usersWithAccess.Contains(model.WorkUserAssign))
                    {
                        ModelState.AddModelError("WorkUserAssign", "The assigned user does not have access to this plant.");
                        var validationEntity = _db.PlantMonitorings
                            .Include(p => p.Plant)
                            .Include(p => p.Monitoring)
                            .FirstOrDefault(p => p.Id == id);
                        
                        if (validationEntity != null)
                        {
                            model.Plant = validationEntity.Plant;
                            model.Monitoring = validationEntity.Monitoring;
                        }
                        
                        // Recreate user list
                        var filteredUsers = _db.Users
                            .Where(u => usersWithAccess.Contains(u.UserName))
                            .OrderBy(u => u.UserName)
                            .ToList();
                            
                        ViewBag.UsersList = new SelectList(filteredUsers, "UserName", "UserName");
                        ViewBag.IsAdmin = User.IsInRole("Admin");
                        return View(model);
                    }
                }

                // Update properties
                plantMonitoring.ExpDate = model.ExpDate;
                plantMonitoring.Remarks = model.Remarks;
                
                // Quotation Phase
                plantMonitoring.QuoteDate = model.QuoteDate;
                plantMonitoring.QuoteCompleteDate = model.QuoteCompleteDate;
                plantMonitoring.QuoteUserAssign = model.QuoteUserAssign;
                
                // Preparation Phase
                plantMonitoring.EprDate = model.EprDate;
                plantMonitoring.EprCompleteDate = model.EprCompleteDate;
                plantMonitoring.EprUserAssign = model.EprUserAssign;
                
                // Work Execution Phase
                plantMonitoring.WorkDate = model.WorkDate;
                plantMonitoring.WorkSubmitDate = model.WorkSubmitDate;
                plantMonitoring.WorkCompleteDate = model.WorkCompleteDate;
                plantMonitoring.WorkUserAssign = model.WorkUserAssign;
                
                // Auto-fill dates based on phase completion
                if (plantMonitoring.QuoteCompleteDate.HasValue && !plantMonitoring.EprDate.HasValue)
                {
                    plantMonitoring.EprDate = plantMonitoring.QuoteCompleteDate;
                }
                
                if (plantMonitoring.EprCompleteDate.HasValue && !plantMonitoring.WorkDate.HasValue)
                {
                    plantMonitoring.WorkDate = plantMonitoring.EprCompleteDate;
                }

                // If WorkCompleteDate is present (meaning the monitoring is completed),
                // set the renewal date (ExpDate) based on monitoring frequency
                if (model.WorkCompleteDate.HasValue)
                {
                    // Get the monitoring frequency in months
                    int frequencyMonths = plantMonitoring.Monitoring.MonitoringFreq;
                    // Set the expiry date based on the frequency
                    plantMonitoring.ExpDate = model.WorkCompleteDate.Value.AddMonths(frequencyMonths);
                }

                // Calculate and set statuses
                plantMonitoring.CalculateStatuses();

                // Handle file uploads
                if (quoteDocument != null && quoteDocument.ContentLength > 0)
                {
                    // Check file size (20MB limit)
                    if (quoteDocument.ContentLength > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "The quote document exceeds the maximum file size of 20MB.");
                        var quoteDocumentValidationEntity = _db.PlantMonitorings
                            .Include(p => p.Plant)
                            .Include(p => p.Monitoring)
                            .FirstOrDefault(p => p.Id == id);
                        
                        if (quoteDocumentValidationEntity != null)
                        {
                            model.Plant = quoteDocumentValidationEntity.Plant;
                            model.Monitoring = quoteDocumentValidationEntity.Monitoring;
                        }
                        ViewBag.IsAdmin = User.IsInRole("Admin");
                        return View(model);
                    }

                    string fileName = Path.GetFileName(quoteDocument.FileName);
                    string uniqueFileName = $"Quote_{id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}";
                    string path = Path.Combine(Server.MapPath("~/Uploads/Monitoring"), uniqueFileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    quoteDocument.SaveAs(path);
                    plantMonitoring.QuoteDoc = "~/Uploads/Monitoring/" + uniqueFileName;
                }

                if (eprDocument != null && eprDocument.ContentLength > 0)
                {
                    // Check file size (20MB limit)
                    if (eprDocument.ContentLength > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "The EPR document exceeds the maximum file size of 20MB.");
                        var eprDocumentValidationEntity = _db.PlantMonitorings
                            .Include(p => p.Plant)
                            .Include(p => p.Monitoring)
                            .FirstOrDefault(p => p.Id == id);
                        
                        if (eprDocumentValidationEntity != null)
                        {
                            model.Plant = eprDocumentValidationEntity.Plant;
                            model.Monitoring = eprDocumentValidationEntity.Monitoring;
                        }
                        ViewBag.IsAdmin = User.IsInRole("Admin");
                        return View(model);
                    }

                    string fileName = Path.GetFileName(eprDocument.FileName);
                    string uniqueFileName = $"EPR_{id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}";
                    string path = Path.Combine(Server.MapPath("~/Uploads/Monitoring"), uniqueFileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    eprDocument.SaveAs(path);
                    plantMonitoring.EprDoc = "~/Uploads/Monitoring/" + uniqueFileName;
                }

                if (workDocument != null && workDocument.ContentLength > 0)
                {
                    // Check file size (20MB limit)
                    if (workDocument.ContentLength > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "The work document exceeds the maximum file size of 20MB.");
                        var workDocumentValidationEntity = _db.PlantMonitorings
                            .Include(p => p.Plant)
                            .Include(p => p.Monitoring)
                            .FirstOrDefault(p => p.Id == id);
                        
                        if (workDocumentValidationEntity != null)
                        {
                            model.Plant = workDocumentValidationEntity.Plant;
                            model.Monitoring = workDocumentValidationEntity.Monitoring;
                        }
                        ViewBag.IsAdmin = User.IsInRole("Admin");
                        return View(model);
                    }

                    string fileName = Path.GetFileName(workDocument.FileName);
                    string uniqueFileName = $"Work_{id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}";
                    string path = Path.Combine(Server.MapPath("~/Uploads/Monitoring"), uniqueFileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    workDocument.SaveAs(path);
                    plantMonitoring.WorkDoc = "~/Uploads/Monitoring/" + uniqueFileName;
                }

                _db.Entry(plantMonitoring).State = EntityState.Modified;
                _db.SaveChanges();

                // Get plant and monitoring names for logging
                var plant = _db.Plants.Find(plantMonitoring.PlantID);
                var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
                string plantName = plant != null ? plant.PlantName : "Unknown Plant";
                string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";

                // Log update
                string changes = "";
                if (original != null)
                {
                    var changedFields = new List<string>();
                    if (original.ProcStatus != plantMonitoring.ProcStatus) changedFields.Add($"ProcStatus: {original.ProcStatus} → {plantMonitoring.ProcStatus}");
                    if (original.ExpStatus != plantMonitoring.ExpStatus) changedFields.Add($"ExpStatus: {original.ExpStatus} → {plantMonitoring.ExpStatus}");
                    if (original.ExpDate != plantMonitoring.ExpDate) changedFields.Add($"ExpDate: {original.ExpDate} → {plantMonitoring.ExpDate}");
                    if (original.Remarks != plantMonitoring.Remarks) changedFields.Add($"Remarks changed");
                    if (original.QuoteUserAssign != plantMonitoring.QuoteUserAssign) changedFields.Add($"QuoteUserAssign: {original.QuoteUserAssign} → {plantMonitoring.QuoteUserAssign}");
                    if (original.EprUserAssign != plantMonitoring.EprUserAssign) changedFields.Add($"EprUserAssign: {original.EprUserAssign} → {plantMonitoring.EprUserAssign}");
                    if (original.WorkUserAssign != plantMonitoring.WorkUserAssign) changedFields.Add($"WorkUserAssign: {original.WorkUserAssign} → {plantMonitoring.WorkUserAssign}");
                    changes = changedFields.Count > 0 ? string.Join("; ", changedFields) : "No fields changed";
                }
                LogUpdate(
                    "PlantMonitoring",
                    plantMonitoring.Id.ToString(),
                    null, null,
                    $"UpdatedStatus PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
                );
                return RedirectToAction("Details", new { id = plantMonitoring.Id });
            }

            // If we got this far, something failed, redisplay form
            var plantMonitoringFromDb = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .FirstOrDefault(p => p.Id == id);

            if (plantMonitoringFromDb == null)
            {
                return HttpNotFound();
            }

            // Copy document paths back from DB entity to model to preserve them
            model.QuoteDoc = plantMonitoringFromDb.QuoteDoc;
            model.EprDoc = plantMonitoringFromDb.EprDoc;
            model.WorkDoc = plantMonitoringFromDb.WorkDoc;
            
            // Set navigation properties
            model.Plant = plantMonitoringFromDb.Plant;
            model.Monitoring = plantMonitoringFromDb.Monitoring;

            // Recreate user list for dropdown
            var users = _db.Users.OrderBy(u => u.UserName).ToList();
            
            // Filter out admin users if the current user is not an admin
            if (!User.IsInRole("Admin"))
            {
                try
                {
                    // Get the admin role
                    var adminRole = _db.Roles.FirstOrDefault(r => r.Name == "Admin");
                    
                    if (adminRole != null)
                    {
                        // Use SQL to get usernames directly from database
                        var adminUsernames = _db.Database.SqlQuery<string>(@"
                            SELECT u.UserName 
                            FROM AspNetUsers u
                            INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                            WHERE ur.RoleId = @roleId",
                            new System.Data.SqlClient.SqlParameter("@roleId", adminRole.Id)
                        ).ToList();
                        
                        // Filter out admin users
                        users = users.Where(u => !adminUsernames.Contains(u.UserName)).ToList();
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't break the page
                    System.Diagnostics.Debug.WriteLine("Failed to filter admin users: " + ex.Message);
                }
            }
            
            // Create SelectList for user assignments
            ViewBag.UsersList = new SelectList(users, "UserName", "UserName");

            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View(model);
        }

        // POST: PlantMonitoring/UpdateExpiry/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateExpiry(int id, PlantMonitoring model)
        {
            var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == id);
            var plantMonitoring = _db.PlantMonitorings.Find(id);
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }

            // Update only expiry and remarks
            plantMonitoring.ExpDate = model.ExpDate;
            plantMonitoring.Remarks = model.Remarks;
            
            // Calculate statuses after update
            plantMonitoring.CalculateStatuses();
            
            _db.Entry(plantMonitoring).State = EntityState.Modified;
            _db.SaveChanges();

            // Get plant and monitoring names for logging
            var plant = _db.Plants.Find(plantMonitoring.PlantID);
            var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
            string plantName = plant != null ? plant.PlantName : "Unknown Plant";
            string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";

            // Log update
            string changes = "";
            if (original != null)
            {
                var changedFields = new List<string>();
                if (original.ExpDate != plantMonitoring.ExpDate) changedFields.Add($"ExpDate: {original.ExpDate} → {plantMonitoring.ExpDate}");
                if (original.Remarks != plantMonitoring.Remarks) changedFields.Add($"Remarks changed");
                changes = changedFields.Count > 0 ? string.Join("; ", changedFields) : "No fields changed";
            }
            LogUpdate(
                "PlantMonitoring",
                plantMonitoring.Id.ToString(),
                null, null,
                $"UpdatedExpiry PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
            );

            // Success message
            TempData["SuccessMessage"] = "Expiry date and remarks successfully updated.";

            // Redirect back to the update page
            return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
        }

        // POST: PlantMonitoring/UpdateQuotation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuotation(int id, PlantMonitoring model, HttpPostedFileBase quoteDocument)
        {
            var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == id);
            var plantMonitoring = _db.PlantMonitorings.Find(id);
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
                
                // For non-admin users, always set the current user as the assignee
                model.QuoteUserAssign = User.Identity.Name;
            }
            else
            {
                // For admin users, validate that the assigned user has access to this plant
                if (!string.IsNullOrEmpty(model.QuoteUserAssign))
                {
                    var userHasAccess = _db.UserPlants
                        .Include(up => up.User)
                        .Any(up => up.PlantId == plantMonitoring.PlantID && up.User.UserName == model.QuoteUserAssign);
                        
                    if (!userHasAccess)
                    {
                        TempData["ErrorMessage"] = "The assigned user does not have access to this plant.";
                        return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
                    }
                }
            }

            // Update only quotation phase
            plantMonitoring.QuoteDate = model.QuoteDate;
            plantMonitoring.QuoteCompleteDate = model.QuoteCompleteDate;
            plantMonitoring.QuoteUserAssign = model.QuoteUserAssign;

            // If Quotation phase is completed, set the EPR phase date if not already set
            if (model.QuoteCompleteDate.HasValue && !plantMonitoring.EprDate.HasValue)
            {
                plantMonitoring.EprDate = model.QuoteCompleteDate;
            }

            // Handle document upload
            if (quoteDocument != null && quoteDocument.ContentLength > 0)
            {
                // Check file size (20MB limit)
                if (quoteDocument.ContentLength > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "The quote document exceeds the maximum file size of 20MB.";
                    return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
                }

                string fileName = Path.GetFileName(quoteDocument.FileName);
                string uniqueFileName = $"Quote_{id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}";
                string path = Path.Combine(Server.MapPath("~/Uploads/Monitoring"), uniqueFileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                quoteDocument.SaveAs(path);
                plantMonitoring.QuoteDoc = "~/Uploads/Monitoring/" + uniqueFileName;
            }
            
            // Calculate statuses after update
            plantMonitoring.CalculateStatuses();
            
            _db.Entry(plantMonitoring).State = EntityState.Modified;
            _db.SaveChanges();

            // Get plant and monitoring names for logging
            var plant = _db.Plants.Find(plantMonitoring.PlantID);
            var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
            string plantName = plant != null ? plant.PlantName : "Unknown Plant";
            string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";

            // Log update
            string changes = "";
            if (original != null)
            {
                var changedFields = new List<string>();
                if (original.QuoteDate != plantMonitoring.QuoteDate) changedFields.Add($"QuoteDate: {original.QuoteDate} → {plantMonitoring.QuoteDate}");
                if (original.QuoteCompleteDate != plantMonitoring.QuoteCompleteDate) changedFields.Add($"QuoteCompleteDate: {original.QuoteCompleteDate} → {plantMonitoring.QuoteCompleteDate}");
                if (original.QuoteUserAssign != plantMonitoring.QuoteUserAssign) changedFields.Add($"QuoteUserAssign: {original.QuoteUserAssign} → {plantMonitoring.QuoteUserAssign}");
                if (original.EprDate != plantMonitoring.EprDate) changedFields.Add($"EprDate: {original.EprDate} → {plantMonitoring.EprDate}");
                changes = changedFields.Count > 0 ? string.Join("; ", changedFields) : "No fields changed";
            }
            LogUpdate(
                "PlantMonitoring",
                plantMonitoring.Id.ToString(),
                null, null,
                $"UpdatedQuotation PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
            );

            // Success message
            TempData["SuccessMessage"] = "Quotation phase successfully updated.";

            // Redirect back to the update page
            return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
        }

        // POST: PlantMonitoring/UpdateEpr/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateEpr(int id, PlantMonitoring model, HttpPostedFileBase eprDocument)
        {
            var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == id);
            var plantMonitoring = _db.PlantMonitorings.Find(id);
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
                
                // For non-admin users, always set the current user as the assignee
                model.EprUserAssign = User.Identity.Name;
            }
            else
            {
                // For admin users, validate that the assigned user has access to this plant
                if (!string.IsNullOrEmpty(model.EprUserAssign))
                {
                    var userHasAccess = _db.UserPlants
                        .Include(up => up.User)
                        .Any(up => up.PlantId == plantMonitoring.PlantID && up.User.UserName == model.EprUserAssign);
                        
                    if (!userHasAccess)
                    {
                        TempData["ErrorMessage"] = "The assigned user does not have access to this plant.";
                        return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
                    }
                }
            }

            // Update only ePR phase
            plantMonitoring.EprDate = model.EprDate;
            plantMonitoring.EprCompleteDate = model.EprCompleteDate;
            plantMonitoring.EprUserAssign = model.EprUserAssign;

            // If EPR phase is completed, set the Work phase date if not already set
            if (model.EprCompleteDate.HasValue && !plantMonitoring.WorkDate.HasValue)
            {
                plantMonitoring.WorkDate = model.EprCompleteDate;
            }

            // Handle document upload
            if (eprDocument != null && eprDocument.ContentLength > 0)
            {
                // Check file size (20MB limit)
                if (eprDocument.ContentLength > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "The EPR document exceeds the maximum file size of 20MB.";
                    return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
                }

                string fileName = Path.GetFileName(eprDocument.FileName);
                string uniqueFileName = $"EPR_{id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}";
                string path = Path.Combine(Server.MapPath("~/Uploads/Monitoring"), uniqueFileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                eprDocument.SaveAs(path);
                plantMonitoring.EprDoc = "~/Uploads/Monitoring/" + uniqueFileName;
            }
            
            // Calculate statuses after update
            plantMonitoring.CalculateStatuses();
            
            _db.Entry(plantMonitoring).State = EntityState.Modified;
            _db.SaveChanges();

            // Get plant and monitoring names for logging
            var plant = _db.Plants.Find(plantMonitoring.PlantID);
            var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
            string plantName = plant != null ? plant.PlantName : "Unknown Plant";
            string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";

            // Log update
            string changes = "";
            if (original != null)
            {
                var changedFields = new List<string>();
                if (original.EprDate != plantMonitoring.EprDate) changedFields.Add($"EprDate: {original.EprDate} → {plantMonitoring.EprDate}");
                if (original.EprCompleteDate != plantMonitoring.EprCompleteDate) changedFields.Add($"EprCompleteDate: {original.EprCompleteDate} → {plantMonitoring.EprCompleteDate}");
                if (original.EprUserAssign != plantMonitoring.EprUserAssign) changedFields.Add($"EprUserAssign: {original.EprUserAssign} → {plantMonitoring.EprUserAssign}");
                if (original.WorkDate != plantMonitoring.WorkDate) changedFields.Add($"WorkDate: {original.WorkDate} → {plantMonitoring.WorkDate}");
                changes = changedFields.Count > 0 ? string.Join("; ", changedFields) : "No fields changed";
            }
            LogUpdate(
                "PlantMonitoring",
                plantMonitoring.Id.ToString(),
                null, null,
                $"UpdatedEpr PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
            );

            // Success message
            TempData["SuccessMessage"] = "EPR phase successfully updated.";

            // Redirect back to the update page
            return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
        }

        // POST: PlantMonitoring/UpdateWork/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateWork(int id, PlantMonitoring model, HttpPostedFileBase workDocument)
        {
            var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == id);
            var plantMonitoring = _db.PlantMonitorings.Find(id);
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
                
                // For non-admin users, always set the current user as the assignee
                model.WorkUserAssign = User.Identity.Name;
            }
            else
            {
                // For admin users, validate that the assigned user has access to this plant
                if (!string.IsNullOrEmpty(model.WorkUserAssign))
                {
                    var userHasAccess = _db.UserPlants
                        .Include(up => up.User)
                        .Any(up => up.PlantId == plantMonitoring.PlantID && up.User.UserName == model.WorkUserAssign);
                        
                    if (!userHasAccess)
                    {
                        TempData["ErrorMessage"] = "The assigned user does not have access to this plant.";
                        return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
                    }
                }
            }

            // Update only work execution phase
            plantMonitoring.WorkDate = model.WorkDate;
            plantMonitoring.WorkSubmitDate = model.WorkSubmitDate;
            plantMonitoring.WorkCompleteDate = model.WorkCompleteDate;
            plantMonitoring.WorkUserAssign = model.WorkUserAssign;

            // If WorkCompleteDate is present (meaning the monitoring is completed),
            // set the renewal date (ExpDate) based on monitoring frequency
            if (model.WorkCompleteDate.HasValue)
            {
                // Get the monitoring frequency in months
                int frequencyMonths = plantMonitoring.Monitoring.MonitoringFreq;
                // Set the expiry date based on the frequency
                plantMonitoring.ExpDate = model.WorkCompleteDate.Value.AddMonths(frequencyMonths);
            }

            // Handle document upload
            if (workDocument != null && workDocument.ContentLength > 0)
            {
                // Check file size (20MB limit)
                if (workDocument.ContentLength > 20 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "The work document exceeds the maximum file size of 20MB.";
                    return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
                }

                string fileName = Path.GetFileName(workDocument.FileName);
                string uniqueFileName = $"Work_{id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fileName}";
                string path = Path.Combine(Server.MapPath("~/Uploads/Monitoring"), uniqueFileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                workDocument.SaveAs(path);
                plantMonitoring.WorkDoc = "~/Uploads/Monitoring/" + uniqueFileName;
            }
            
            // Calculate statuses after update
            plantMonitoring.CalculateStatuses();
            
            _db.Entry(plantMonitoring).State = EntityState.Modified;
            _db.SaveChanges();

            // Get plant and monitoring names for logging
            var plant = _db.Plants.Find(plantMonitoring.PlantID);
            var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
            string plantName = plant != null ? plant.PlantName : "Unknown Plant";
            string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";

            // Log update
            string changes = "";
            if (original != null)
            {
                var changedFields = new List<string>();
                if (original.WorkDate != plantMonitoring.WorkDate) changedFields.Add($"WorkDate: {original.WorkDate} → {plantMonitoring.WorkDate}");
                if (original.WorkSubmitDate != plantMonitoring.WorkSubmitDate) changedFields.Add($"WorkSubmitDate: {original.WorkSubmitDate} → {plantMonitoring.WorkSubmitDate}");
                if (original.WorkCompleteDate != plantMonitoring.WorkCompleteDate) changedFields.Add($"WorkCompleteDate: {original.WorkCompleteDate} → {plantMonitoring.WorkCompleteDate}");
                if (original.WorkUserAssign != plantMonitoring.WorkUserAssign) changedFields.Add($"WorkUserAssign: {original.WorkUserAssign} → {plantMonitoring.WorkUserAssign}");
                if (original.ExpDate != plantMonitoring.ExpDate) changedFields.Add($"ExpDate: {original.ExpDate} → {plantMonitoring.ExpDate}");
                changes = changedFields.Count > 0 ? string.Join("; ", changedFields) : "No fields changed";
            }
            LogUpdate(
                "PlantMonitoring",
                plantMonitoring.Id.ToString(),
                null, null,
                $"UpdatedWork PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
            );

            // Success message
            TempData["SuccessMessage"] = "Work execution phase successfully updated.";

            // Redirect back to the update page
            return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
        }

        // POST: PlantMonitoring/MarkNotificationAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkNotificationAsRead(int id)
        {
            try
            {
                // We're not storing notifications in the database, so we'll use a session variable
                // to track read notifications per user
                var readNotifications = Session["ReadNotifications"] as List<int> ?? new List<int>();
                
                if (!readNotifications.Contains(id))
                {
                    readNotifications.Add(id);
                    Session["ReadNotifications"] = readNotifications;
                }
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error marking notification as read: " + ex.Message);
                return Json(new { success = false, message = "Error marking notification as read" });
            }
        }

        // POST: PlantMonitoring/ClearAllNotifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearAllNotifications()
        {
            try
            {
                // Get all notification IDs
                var notifications = GetMonitoringNotifications();
                var notificationIds = notifications.Select(n => n.ItemId).ToList();
                
                // Store them in session as read
                Session["ReadNotifications"] = notificationIds;
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error clearing notifications: " + ex.Message);
                return Json(new { success = false, message = "Error clearing notifications" });
            }
        }

        // POST: PlantMonitoring/RenewMonitoring/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RenewMonitoring(int id)
        {
            var plantMonitoring = _db.PlantMonitorings
                .Include(p => p.Plant)
                .Include(p => p.Monitoring)
                .FirstOrDefault(p => p.Id == id);
            
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }
            
            try
            {
                // Create a new monitoring cycle from the existing one
                var newMonitoring = new PlantMonitoring
                {
                    PlantID = plantMonitoring.PlantID,
                    MonitoringID = plantMonitoring.MonitoringID,
                    Area = plantMonitoring.Area,
                    ProcStatus = "Not Started",
                    
                    // Assign the same users if they exist
                    QuoteUserAssign = plantMonitoring.QuoteUserAssign,
                    EprUserAssign = plantMonitoring.EprUserAssign,
                    WorkUserAssign = plantMonitoring.WorkUserAssign,
                    
                    // Set current date as the start date for the new cycle
                    QuoteDate = DateTime.Now,
                    
                    // Copy remarks if needed
                    Remarks = $"Renewed from previous monitoring cycle (ID: {plantMonitoring.Id}) that expired on {plantMonitoring.ExpDate?.ToString("dd/MM/yyyy") ?? "N/A"}"
                };
                
                // Calculate statuses
                newMonitoring.CalculateStatuses();
                
                // Add to database
                _db.PlantMonitorings.Add(newMonitoring);
                _db.SaveChanges();
                
                // Get plant and monitoring names for logging
                var plant = _db.Plants.Find(newMonitoring.PlantID);
                var monitoringType = _db.Monitorings.Find(newMonitoring.MonitoringID);
                string plantName = plant != null ? plant.PlantName : "Unknown Plant";
                string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";
                
                // Log creation
                LogCreation(
                    "PlantMonitoring",
                    newMonitoring.Id.ToString(),
                    $"Created PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}, Status: {newMonitoring.ProcStatus}"
                );
                return RedirectToAction("Details", new { id = newMonitoring.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error renewing monitoring: " + ex.Message;
                return RedirectToAction("Details", new { id = id });
            }
        }

        // POST: PlantMonitoring/UpdateRemarks/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRemarks(int id, string Remarks)
        {
            var original = _db.PlantMonitorings.AsNoTracking().FirstOrDefault(p => p.Id == id);
            var plantMonitoring = _db.PlantMonitorings.Find(id);
            if (plantMonitoring == null)
            {
                return HttpNotFound();
            }

            // Check if user has access to this plant monitoring record
            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity.GetUserId();
                var userHasAccessToPlant = _db.UserPlants
                    .Any(up => up.UserId == userId && up.PlantId == plantMonitoring.PlantID);
                
                if (!userHasAccessToPlant)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
            }

            // Update only remarks
            plantMonitoring.Remarks = Remarks;
            
            // Save changes
            _db.Entry(plantMonitoring).State = EntityState.Modified;
            _db.SaveChanges();

            // Get plant and monitoring names for logging
            var plant = _db.Plants.Find(plantMonitoring.PlantID);
            var monitoringType = _db.Monitorings.Find(plantMonitoring.MonitoringID);
            string plantName = plant != null ? plant.PlantName : "Unknown Plant";
            string monitoringName = monitoringType != null ? monitoringType.MonitoringName : "Unknown Monitoring";

            // Log update
            string changes = "";
            if (original != null && original.Remarks != plantMonitoring.Remarks)
            {
                changes = "Remarks changed";
            }
            else
            {
                changes = "No fields changed";
            }
            LogUpdate(
                "PlantMonitoring",
                plantMonitoring.Id.ToString(),
                null, null,
                $"UpdatedRemarks PlantMonitoring for Plant: {plantName}, Monitoring: {monitoringName}. Changes: {changes}"
            );

            // Success message
            TempData["SuccessMessage"] = "Remarks successfully updated.";

            // Redirect back to the update page
            return RedirectToAction("UpdateStatus", new { id = plantMonitoring.Id });
        }

        // Generates monitoring notifications for the current user
        private List<MonitoringNotification> GetMonitoringNotifications()
        {
            var notifications = new List<MonitoringNotification>();
            var currentUser = User.Identity.Name;
            var today = DateTime.Now.Date;
            var ninetyDaysFromNow = today.AddDays(90); // Calculate date before using in LINQ
            
            // Get the list of read notification IDs from session
            var readNotificationIds = Session["ReadNotifications"] as List<int> ?? new List<int>();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Generating notifications for user: {currentUser}");
                
                // Get plants the user has access to
                var userId = User.Identity.GetUserId();
                var userPlantIds = new List<int>();
                
                // If user is admin, they can see all plants, otherwise only assigned plants
                if (User.IsInRole("Admin"))
                {
                    userPlantIds = _db.Plants.Select(p => p.Id).ToList();
                }
                else
                {
                    userPlantIds = _db.UserPlants
                        .Where(up => up.UserId == userId)
                        .Select(up => up.PlantId)
                        .ToList();
                }
                
                System.Diagnostics.Debug.WriteLine($"User has access to {userPlantIds.Count} plants");
                
                // 1. Items expiring soon (90 days)
                var expiringItems = _db.PlantMonitorings
                    .Include(p => p.Plant)
                    .Include(p => p.Monitoring)
                    .Where(p => p.ExpDate.HasValue && 
                           p.ExpDate.Value >= today &&
                           p.ExpDate.Value <= ninetyDaysFromNow &&
                           p.ProcStatus == "Not Started" &&
                           userPlantIds.Contains(p.PlantID))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Found {expiringItems.Count} expiring items");
                foreach (var item in expiringItems)
                {
                    var daysRemaining = (item.ExpDate.Value - today).Days;
                    
                    notifications.Add(new MonitoringNotification
                    {
                        Type = NotificationType.Expiring,
                        Title = "Monitoring Expiring",
                        Message = $"{item.Monitoring.MonitoringName} for {item.Plant.PlantName} {(string.IsNullOrEmpty(item.Area) ? "" : "(" + item.Area + ")")} is expiring in {daysRemaining} day{(daysRemaining != 1 ? "s" : "")}",
                        Link = Url.Action("Details", new { id = item.Id }),
                        ItemId = item.Id,
                        IsRead = readNotificationIds.Contains(item.Id)
                    });
                }
                
                // 2. Items assigned to the current user
                var assignedItems = _db.PlantMonitorings
                    .Include(p => p.Plant)
                    .Include(p => p.Monitoring)
                    .Where(p => ((p.QuoteUserAssign == currentUser && p.QuoteCompleteDate == null) || 
                           (p.EprUserAssign == currentUser && p.EprCompleteDate == null) ||
                           (p.WorkUserAssign == currentUser && p.WorkCompleteDate == null)) &&
                           userPlantIds.Contains(p.PlantID))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Found {assignedItems.Count} assigned items");
                foreach (var item in assignedItems)
                {
                    string phase = "";
                    if (item.QuoteUserAssign == currentUser && item.QuoteCompleteDate == null)
                        phase = "Quotation";
                    else if (item.EprUserAssign == currentUser && item.EprCompleteDate == null)
                        phase = "ePR";
                    else if (item.WorkUserAssign == currentUser && item.WorkCompleteDate == null)
                        phase = "Work Execution";
                    
                    notifications.Add(new MonitoringNotification
                    {
                        Type = NotificationType.Assignment,
                        Title = "Task Assigned",
                        Message = $"You are assigned to the {phase} phase for {item.Monitoring.MonitoringName} at {item.Plant.PlantName}",
                        Link = Url.Action("UpdateStatus", new { id = item.Id }),
                        ItemId = item.Id,
                        IsRead = readNotificationIds.Contains(item.Id)
                    });
                }
                
                // 3. Items with completed phase needing next phase initiation
                var readyForNextPhase = _db.PlantMonitorings
                    .Include(p => p.Plant)
                    .Include(p => p.Monitoring)
                    .Where(p => ((p.QuoteCompleteDate.HasValue && !p.EprCompleteDate.HasValue) ||
                           (p.EprCompleteDate.HasValue && !p.WorkCompleteDate.HasValue)) &&
                           userPlantIds.Contains(p.PlantID))
                    .ToList()
                    .Where(p => (p.QuoteCompleteDate.HasValue && !p.EprCompleteDate.HasValue && p.EprUserAssign == currentUser) ||
                           (p.EprCompleteDate.HasValue && !p.WorkCompleteDate.HasValue && p.WorkUserAssign == currentUser))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Found {readyForNextPhase.Count} items ready for next phase");
                foreach (var item in readyForNextPhase)
                {
                    string phase = "";
                    if (item.QuoteCompleteDate.HasValue && !item.EprCompleteDate.HasValue)
                        phase = "ePR";
                    else
                        phase = "Work Execution";
                    
                    notifications.Add(new MonitoringNotification
                    {
                        Type = NotificationType.NextPhaseReady,
                        Title = "Next Phase Ready",
                        Message = $"The {phase} phase is ready to begin for {item.Monitoring.MonitoringName} at {item.Plant.PlantName}",
                        Link = Url.Action("UpdateStatus", new { id = item.Id }),
                        ItemId = item.Id,
                        IsRead = readNotificationIds.Contains(item.Id)
                    });
                }
                
                // 4. Recently completed monitoring items
                var lastWeek = today.AddDays(-7); // Calculate date before using in LINQ
                var completedItems = _db.PlantMonitorings
                    .Include(p => p.Plant)
                    .Include(p => p.Monitoring)
                    .Where(p => p.ProcStatus == "Completed" && 
                          p.WorkCompleteDate.HasValue &&
                          p.WorkCompleteDate.Value >= lastWeek &&
                          userPlantIds.Contains(p.PlantID))
                    .ToList()
                    .Where(p => p.QuoteUserAssign == currentUser || p.EprUserAssign == currentUser || p.WorkUserAssign == currentUser)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Found {completedItems.Count} completed items");
                foreach (var item in completedItems)
                {
                    notifications.Add(new MonitoringNotification
                    {
                        Type = NotificationType.PhaseComplete,
                        Title = "Monitoring Completed",
                        Message = $"{item.Monitoring.MonitoringName} for {item.Plant.PlantName} has been completed successfully",
                        Link = Url.Action("Details", new { id = item.Id }),
                        ItemId = item.Id,
                        IsRead = readNotificationIds.Contains(item.Id)
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break the page
                System.Diagnostics.Debug.WriteLine("Error generating monitoring notifications: " + ex.Message);
            }
            
            var filteredNotifications = notifications
                .OrderByDescending(n => n.Type == NotificationType.Overdue)  // Overdue first
                .ThenByDescending(n => n.Type == NotificationType.Expiring)  // Then expiring
                .ThenBy(n => n.CreatedDate)  // Then by date
                .Take(10)  // Limit to 10 items
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"Returning {filteredNotifications.Count} total notifications");
            return filteredNotifications;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // View Model for Progress Tracker
    public class PlantMonitoringViewModel
    {
        public int Id { get; set; }
        public string Area { get; set; }
        public string ProcStatus { get; set; }
        public string ExpStatus { get; set; }
        public DateTime? ExpDate { get; set; }
        public DateTime? QuoteDate { get; set; }
        public DateTime? QuoteCompleteDate { get; set; }
        public DateTime? EprDate { get; set; }
        public DateTime? EprCompleteDate { get; set; }
        public DateTime? WorkDate { get; set; }
        public DateTime? WorkCompleteDate { get; set; }
        public string Remarks { get; set; }

        public string ProcStatusCssClass
        {
            get
            {
                switch (ProcStatus)
                {
                    case "Completed":
                        return "bg-success";
                    case "Work In Progress":
                    case "In Progress": // For backward compatibility
                        return "bg-warning";
                    case "ePR Raised":
                    case "In Preparation": // For backward compatibility
                        return "bg-info";
                    case "Quotation Requested":
                    case "In Quotation": // For backward compatibility
                        return "bg-primary";
                    case "Not Started":
                        return "bg-notstarted";
                    default:
                        return "";
                }
            }
        }

        public string ExpStatusCssClass
        {
            get
            {
                switch (ExpStatus)
                {
                    case "Expired":
                        return "bg-danger";
                    case "Expiring Soon":
                        return "bg-warning";
                    case "Active":
                        return "bg-success";
                    case "No Expiry":
                        return "bg-secondary";
                    default:
                        return "";
                }
            }
        }
    }
} 