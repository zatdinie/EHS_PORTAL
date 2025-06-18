using System;
using System.Web;
using EHS_PORTAL.Areas.CLIP.Models;
using Microsoft.AspNet.Identity;

namespace EHS_PORTAL.Areas.CLIP.Services
{
    public class ActivityLogger
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpContextBase _httpContext;

        public ActivityLogger(ApplicationDbContext db, HttpContextBase httpContext)
        {
            _db = db;
            _httpContext = httpContext;
        }

        public void LogActivity(string action, string description = null, string entityName = null, 
            string entityId = null, string oldValue = null, string newValue = null)
        {
            try
            {
                var userId = _httpContext.User.Identity.GetUserId();
                var userName = _httpContext.User.Identity.Name;
                var ipAddress = GetUserIPAddress();
                var userAgent = _httpContext.Request.UserAgent;
                var pageUrl = _httpContext.Request.Url?.AbsoluteUri;
                var sessionId = _httpContext.Session?.SessionID;

                var log = new ActivityLog
                {
                    UserID = userId,
                    UserName = userName,
                    Action = action,
                    Description = description,
                    EntityName = entityName,
                    EntityID = entityId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.Now,
                    PageUrl = pageUrl,
                    SessionID = sessionId
                };

                _db.ActivityLogs.Add(log);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log the error to your error logging system
                // For now, we'll just swallow the exception to prevent it from affecting the user experience
                System.Diagnostics.Debug.WriteLine($"Error logging activity: {ex.Message}");
            }
        }

        private string GetUserIPAddress()
        {
            string ipAddress = _httpContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = _httpContext.Request.ServerVariables["REMOTE_ADDR"];
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = _httpContext.Request.UserHostAddress;
            }

            return ipAddress;
        }
    }
} 