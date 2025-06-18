using System;
using System.Web;
using System.Web.Mvc;
using EHS_PORTAL.Areas.CLIP.Models;
using EHS_PORTAL.Areas.CLIP.Services;

namespace EHS_PORTAL.Areas.CLIP.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ActivityLoggerAttribute : ActionFilterAttribute
    {
        private readonly string _action;
        private readonly string _description;
        private readonly string _entityName;

        public ActivityLoggerAttribute(string action, string description = null, string entityName = null)
        {
            _action = action;
            _description = description;
            _entityName = entityName;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Exception != null)
            {
                // Don't log if there was an exception
                return;
            }

            var db = new ApplicationDbContext();
            var httpContext = filterContext.HttpContext;
            var logger = new ActivityLogger(db, httpContext);

            // Get entity ID from route data if available
            string entityId = null;
            if (filterContext.RouteData.Values.ContainsKey("id"))
            {
                entityId = filterContext.RouteData.Values["id"].ToString();
            }

            // Log the activity
            logger.LogActivity(
                _action,
                _description,
                _entityName,
                entityId
            );

            base.OnActionExecuted(filterContext);
        }
    }
} 