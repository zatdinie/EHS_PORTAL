using System;
using System.Web;
using System.Web.Mvc;

namespace EHS_PORTAL.Areas.CLIP.Core
{
    public static class DateFormatHelper
    {
        /// <summary>
        /// Format a nullable DateTime in the standard dd/MM/yyyy format
        /// </summary>
        public static string FormatDate(this DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("dd/MM/yyyy") : "-";
        }
        
        /// <summary>
        /// Format a DateTime in the standard dd/MM/yyyy format
        /// </summary>
        public static string FormatDate(this DateTime date)
        {
            return date.ToString("dd/MM/yyyy");
        }
        
        /// <summary>
        /// Format a DateTime for HTML input elements (yyyy-MM-dd)
        /// </summary>
        public static string FormatForHtml(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
        
        /// <summary>
        /// Format a nullable DateTime for HTML input elements (yyyy-MM-dd)
        /// </summary>
        public static string FormatForHtml(this DateTime? date)
        {
            return date.HasValue ? date.Value.ToString("yyyy-MM-dd") : string.Empty;
        }
    }
} 