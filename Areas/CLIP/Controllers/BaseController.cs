using System;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using EHS_PORTAL.Areas.CLIP.Services;
using Microsoft.AspNet.Identity;

namespace EHS_PORTAL.Areas.CLIP.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ApplicationDbContext _db;
        
        public BaseController()
        {
            _db = new ApplicationDbContext();
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
        
        /// <summary>
        /// Logs a user activity
        /// </summary>
        protected void LogActivity(string action, string description = null, string entityName = null, 
            string entityId = null, string oldValue = null, string newValue = null)
        {
            var logger = new ActivityLogger(_db, HttpContext);
            logger.LogActivity(action, description, entityName, entityId, oldValue, newValue);
        }
        
        /// <summary>
        /// Helper method to log creation activities
        /// </summary>
        protected void LogCreation(string entityName, string entityId, string description = null)
        {
            LogActivity("CREATE", description ?? $"Created {entityName}", entityName, entityId);
        }
        
        /// <summary>
        /// Helper method to log update activities
        /// </summary>
        protected void LogUpdate(string entityName, string entityId, string oldValue = null, string newValue = null, string description = null)
        {
            LogActivity("UPDATE", description ?? $"Updated {entityName}", entityName, entityId, oldValue, newValue);
        }
        
        /// <summary>
        /// Helper method to log deletion activities
        /// </summary>
        protected void LogDeletion(string entityName, string entityId, string description = null)
        {
            LogActivity("DELETE", description ?? $"Deleted {entityName}", entityName, entityId);
        }
        
        /// <summary>
        /// Helper method to log view activities
        /// </summary>
        protected void LogView(string entityName, string entityId, string description = null)
        {
            LogActivity("VIEW", description ?? $"Viewed {entityName}", entityName, entityId);
        }
    }
} 