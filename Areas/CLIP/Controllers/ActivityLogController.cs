using System;
using System.Linq;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using PagedList;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : BaseController
    {
        // GET: CLIP/ActivityLog
        public ActionResult Index(string searchTerm = "", string filterAction = "", string filterEntity = "", 
            DateTime? startDate = null, DateTime? endDate = null, int? page = 1)
        {
            int pageSize = 50;
            int pageNumber = page ?? 1;

            // Start with all logs
            var query = _db.ActivityLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(log => 
                    log.UserName.Contains(searchTerm) || 
                    log.Description.Contains(searchTerm) ||
                    log.EntityName.Contains(searchTerm) ||
                    log.EntityID.Contains(searchTerm)
                );
            }

            if (!string.IsNullOrEmpty(filterAction))
            {
                query = query.Where(log => log.Action == filterAction);
            }

            if (!string.IsNullOrEmpty(filterEntity))
            {
                query = query.Where(log => log.EntityName == filterEntity);
            }

            if (startDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Include the entire end date
                DateTime endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(log => log.CreatedAt <= endOfDay);
            }

            // Order by most recent first
            query = query.OrderByDescending(log => log.CreatedAt);

            // Get distinct actions and entity names for filters
            ViewBag.Actions = _db.ActivityLogs.Select(log => log.Action).Distinct().OrderBy(a => a).ToList();
            ViewBag.EntityNames = _db.ActivityLogs.Select(log => log.EntityName).Where(e => e != null).Distinct().OrderBy(e => e).ToList();
            
            // Set filter values for the view
            ViewBag.CurrentSearchTerm = searchTerm;
            ViewBag.CurrentFilterAction = filterAction;
            ViewBag.CurrentFilterEntity = filterEntity;
            ViewBag.CurrentStartDate = startDate;
            ViewBag.CurrentEndDate = endDate;

            // Execute query with paging
            var logs = query.ToPagedList(pageNumber, pageSize);

            return View(logs);
        }

        // GET: CLIP/ActivityLog/Details/5
        public ActionResult Details(int id)
        {
            var log = _db.ActivityLogs.Find(id);
            if (log == null)
            {
                return HttpNotFound();
            }

            return View(log);
        }

        // GET: CLIP/ActivityLog/UserActivity/userId
        public ActionResult UserActivity(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }

            var logs = _db.ActivityLogs
                .Where(log => log.UserID == userId)
                .OrderByDescending(log => log.CreatedAt)
                .ToList();

            var user = _db.Users.Find(userId);
            ViewBag.UserName = user?.UserName ?? "Unknown User";

            return View(logs);
        }
    }
} 