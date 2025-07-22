<%@ WebHandler Language="C#" Class="FETS.Pages.PublicDashboard.UserGuideHandler" %>

using System;
using System.Web;
using System.IO;

namespace FETS.Pages.PublicDashboard
{
    public class UserGuideHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            // Path to the file you want to open
            string filePath = context.Server.MapPath("~/Areas/FETS/Uploads/FETS_USER GUIDE.pdf");
            
            // Check if file exists
            if (!File.Exists(filePath))
            {
                context.Response.ContentType = "text/plain";
                context.Response.Write("The requested file does not exist.");
                context.Response.End();
                return;
            }
            
            // Set response headers to display in browser
            context.Response.Clear();
            context.Response.ContentType = "application/pdf";
            context.Response.AppendHeader("Content-Disposition", "inline; filename=\"FETS_USER GUIDE.pdf\"");
            context.Response.TransmitFile(filePath);
            context.Response.End();
        }
        
        public bool IsReusable
        {
            get { return false; }
        }
    }
} 