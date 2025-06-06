using System;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Text;

namespace FETS
{
    /// <summary>
    /// Manages email templates for the Fire Extinguisher Tracking System
    /// </summary>
    public static class EmailTemplateManager
    {
        /// <summary>
        /// Gets the service notification email template with placeholders replaced with actual data
        /// </summary>
        public static string GetServiceEmailTemplate(
            string serialNumber,
            string plant,
            string level,
            string location,
            string type,
            string remarks = null,
            string replacement = null,
            DateTime? estimatedReturnDate = null)
        {
            // Path to the template file
            string templatePath = HttpContext.Current.Server.MapPath("~/Areas/FETS/EmailTemplates/ServiceEmailTemplate.html");
            
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Generate remarks row if remarks exist
            string remarksRow = string.IsNullOrEmpty(remarks) 
                ? string.Empty 
                : "<tr>\r\n                        <th>Remarks</th>\r\n                        <td>" + remarks + "</td>\r\n                    </tr>";
                
            // Generate replacement row if replacement exists
            string replacementRow = string.IsNullOrEmpty(replacement) 
                ? string.Empty 
                : "<tr>\r\n                        <th>Replacement</th>\r\n                        <td>" + replacement + "</td>\r\n                    </tr>";
            
            // Set default estimated return date if not provided (14 days from now)
            DateTime returnDate = estimatedReturnDate ?? DateTime.Now.AddDays(21);
            
            // Replace placeholders with actual data
            template = template.Replace("{SerialNumber}", serialNumber)
                               .Replace("{Plant}", plant)
                               .Replace("{Level}", level)
                               .Replace("{Location}", location)
                               .Replace("{Type}", type)
                               .Replace("{RemarksRow}", remarksRow + replacementRow)
                               .Replace("{ServiceDate}", DateTime.Now.ToString("MMMM dd, yyyy"))
                               .Replace("{EstimatedReturnDate}", returnDate.ToString("MMMM dd, yyyy"))
                               .Replace("{SystemUrl}", "https://yourcompany.com/FETS")
                               .Replace("{CurrentYear}", DateTime.Now.Year.ToString())
                               .Replace("{CompanyName}", "INARI AMERTRON BHD.");
            
            return template;
        }

        /// <summary>
        /// Gets the service notification email template for multiple fire extinguishers
        /// </summary>
        public static string GetMultipleServiceEmailTemplate(List<FireExtinguisherServiceInfo> extinguishers)
        {
            // Path to the template file
            string templatePath = HttpContext.Current.Server.MapPath("~/Areas/FETS/EmailTemplates/ServiceEmailTemplate.html");
            
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Create the table rows for multiple extinguishers
            StringBuilder tableContent = new StringBuilder();
            tableContent.Append(@"
                <table>
                    <thead>
                        <tr>
                            <th>Serial Number</th>
                            <th>Plant</th>
                            <th>Level</th>
                            <th>Location</th>
                            <th>Type</th>
                            <th>Replacement</th>
                        </tr>
                    </thead>
                    <tbody>");
            
            foreach (var extinguisher in extinguishers)
            {
                string replacement = !string.IsNullOrEmpty(extinguisher.Replacement) ? extinguisher.Replacement : "-";
                tableContent.Append("<tr>\r\n                            <td>" + extinguisher.SerialNumber + 
                                   "</td>\r\n                            <td>" + extinguisher.Plant + 
                                   "</td>\r\n                            <td>" + extinguisher.Level + 
                                   "</td>\r\n                            <td>" + extinguisher.Location + 
                                   "</td>\r\n                            <td>" + extinguisher.Type + 
                                   "</td>\r\n                            <td>" + replacement + 
                                   "</td>\r\n                        </tr>");
            }
            
            tableContent.Append(@"
                    </tbody>
                </table>");
            
            // Replace the single extinguisher table with the multiple extinguisher table
            string modifiedTemplate = template
                .Replace(@"<h2>Fire Extinguisher Details:</h2>
            
            <table>
                <tr>
                    <th>Serial Number</th>
                    <td>{SerialNumber}</td>
                </tr>
                <tr>
                    <th>Plant</th>
                    <td>{Plant}</td>
                </tr>
                <tr>
                    <th>Level</th>
                    <td>{Level}</td>
                </tr>
                <tr>
                    <th>Location</th>
                    <td>{Location}</td>
                </tr>
                <tr>
                    <th>Type</th>
                    <td>{Type}</td>
                </tr>
                {RemarksRow}
            </table>", "<h2>Fire Extinguishers Details:</h2>\n" + tableContent)
                .Replace("<p>This is to inform you that the following fire extinguisher has been sent for service on <strong>{ServiceDate}</strong>.</p>", 
                         "<p>This is to inform you that the following " + extinguishers.Count + " fire extinguishers have been sent for service on <strong>" + DateTime.Now.ToString("MMMM dd, yyyy") + "</strong>.</p>")
                .Replace("{ServiceDate}", DateTime.Now.ToString("MMMM dd, yyyy"))
                .Replace("{EstimatedReturnDate}", DateTime.Now.AddDays(14).ToString("MMMM dd, yyyy"))
                .Replace("{SystemUrl}", "https://yourcompany.com/FETS")
                .Replace("{CurrentYear}", DateTime.Now.Year.ToString())
                .Replace("{CompanyName}", "INARI AMERTRON BHD.");
            
            return modifiedTemplate;
        }

      

        /// <summary>
        /// Gets the service completion email template with placeholders replaced with actual data
        /// </summary>
        public static string GetServiceCompletionEmailTemplate(
            string serialNumber,
            string plant,
            string level,
            string location,
            string type,
            DateTime serviceCompletionDate,
            DateTime newExpiryDate,
            string remarks = null)
        {
            // Path to the template file
            string templatePath = HttpContext.Current.Server.MapPath("~/Areas/FETS/EmailTemplates/ServiceEmailTemplate.html");
            
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Generate remarks row if remarks exist
            string remarksRow = string.IsNullOrEmpty(remarks) 
                ? string.Empty 
                : "<tr>\r\n                        <th>Remarks</th>\r\n                        <td>" + remarks + "</td>\r\n                    </tr>";
            
            // Add new rows for service completion and new expiry date
            string serviceCompletionRow = "<tr>\r\n                        <th>Service Completion Date</th>\r\n                        <td>" + serviceCompletionDate.ToString("MMMM dd, yyyy") + "</td>\r\n                    </tr>";
            string newExpiryDateRow = "<tr>\r\n                        <th>New Expiry Date</th>\r\n                        <td>" + newExpiryDate.ToString("MMMM dd, yyyy") + "</td>\r\n                    </tr>";
            
            // Replace placeholders with actual data
            template = template.Replace("{SerialNumber}", serialNumber)
                               .Replace("{Plant}", plant)
                               .Replace("{Level}", level)
                               .Replace("{Location}", location)
                               .Replace("{Type}", type)
                               .Replace("{RemarksRow}", remarksRow + serviceCompletionRow + newExpiryDateRow)
                               .Replace("<h1>Fire Extinguisher Service Notification</h1>", "<h1>Fire Extinguisher Service Completion</h1>")
                               .Replace("<p>This is to inform you that the following fire extinguisher has been sent for service on <strong>{ServiceDate}</strong>.</p>", 
                                        "<p>This is to inform you that the following fire extinguisher has completed service on <strong>" + serviceCompletionDate.ToString("MMMM dd, yyyy") + "</strong> and is now active.</p>")
                               .Replace("<div class=\"remarks\">\r\n                <p><strong>Service Information:</strong></p>\r\n                <p>The fire extinguisher has been marked as \"Under Service\" in the tracking system. Please ensure it is returned to service as soon as maintenance is completed.</p>\r\n            </div>\r\n            \r\n            <p>Estimated return to service: <strong>{EstimatedReturnDate}</strong></p>", 
                                        "<div class=\"remarks\">\r\n                <p><strong>Service Information:</strong></p>\r\n                <p>The fire extinguisher has been marked as \"Active\" in the tracking system. A reminder will be sent in one week to follow up with the vendor regarding service quality.</p>\r\n            </div>\r\n            \r\n            <p>Estimated return to service: <strong>" + serviceCompletionDate.AddDays(7).ToString("MMMM dd, yyyy") + "</strong></p>")
                               .Replace("{ServiceDate}", serviceCompletionDate.ToString("MMMM dd, yyyy"))
                               .Replace("{SystemUrl}", "https://yourcompany.com/FETS")
                               .Replace("{CurrentYear}", DateTime.Now.Year.ToString())
                               .Replace("{CompanyName}", "INARI AMERTRON BHD.");
            
            return template;
        }

        /// <summary>
        /// Gets the service completion email template for multiple fire extinguishers
        /// </summary>
        public static string GetMultipleServiceCompletionEmailTemplate(List<FireExtinguisherServiceInfo> extinguishers, DateTime serviceCompletionDate, DateTime newExpiryDate)
        {
            // Path to the template file
            string templatePath = HttpContext.Current.Server.MapPath("~/Areas/FETS/EmailTemplates/ServiceEmailTemplate.html");
            
            // Read the template
            string template = File.ReadAllText(templatePath);
            
            // Create the table rows for multiple extinguishers
            StringBuilder tableContent = new StringBuilder();
            tableContent.Append(@"
                <table>
                    <thead>
                        <tr>
                            <th>Serial Number</th>
                            <th>Plant</th>
                            <th>Level</th>
                            <th>Location</th>
                            <th>Type</th>
                            <th>New Expiry Date</th>
                        </tr>
                    </thead>
                    <tbody>");
            
            foreach (var extinguisher in extinguishers)
            {
                tableContent.Append("<tr>\r\n                            <td>" + extinguisher.SerialNumber + "</td>\r\n                            <td>" + extinguisher.Plant + "</td>\r\n                            <td>" + extinguisher.Level + "</td>\r\n                            <td>" + extinguisher.Location + "</td>\r\n                            <td>" + extinguisher.Type + "</td>\r\n                            <td>" + newExpiryDate.ToString("MMM dd, yyyy") + "</td>\r\n                        </tr>");
            }
            
            tableContent.Append(@"
                    </tbody>
                </table>");
            
            // Replace the single extinguisher table with the multiple extinguisher table
            string modifiedTemplate = template
                .Replace(@"<h2>Fire Extinguisher Details:</h2>
            
            <table>
                <tr>
                    <th>Serial Number</th>
                    <td>{SerialNumber}</td>
                </tr>
                <tr>
                    <th>Plant</th>
                    <td>{Plant}</td>
                </tr>
                <tr>
                    <th>Level</th>
                    <td>{Level}</td>
                </tr>
                <tr>
                    <th>Location</th>
                    <td>{Location}</td>
                </tr>
                <tr>
                    <th>Type</th>
                    <td>{Type}</td>
                </tr>
                {RemarksRow}
            </table>", "<h2>Fire Extinguishers Details:</h2>\n" + tableContent)
                .Replace("<h1>Fire Extinguisher Service Notification</h1>", "<h1>Fire Extinguisher Service Completion</h1>")
                .Replace("<p>This is to inform you that the following fire extinguisher has been sent for service on <strong>{ServiceDate}</strong>.</p>", 
                         "<p>This is to inform you that the following " + extinguishers.Count + " fire extinguishers have completed service on <strong>" + serviceCompletionDate.ToString("MMMM dd, yyyy") + "</strong> and are now active.</p>")
                .Replace("<div class=\"remarks\">\r\n                <p><strong>Service Information:</strong></p>\r\n                <p>The fire extinguisher has been marked as \"Under Service\" in the tracking system. Please ensure it is returned to service as soon as maintenance is completed.</p>\r\n            </div>\r\n            \r\n            <p>Estimated return to service: <strong>{EstimatedReturnDate}</strong></p>", "")
                .Replace("{ServiceDate}", serviceCompletionDate.ToString("MMMM dd, yyyy"))
                .Replace("{SystemUrl}", "https://yourcompany.com/FETS")
                .Replace("{CurrentYear}", DateTime.Now.Year.ToString())
                .Replace("{CompanyName}", "INARI AMERTRON BHD.");
            
            return modifiedTemplate;
        }
    }

    /// <summary>
    /// Data class to hold fire extinguisher information for service emails
    /// </summary>
    public class FireExtinguisherServiceInfo
    {
        public string SerialNumber { get; set; }
        public string AreaCode { get; set; }
        public string Plant { get; set; }
        public string Level { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public string Replacement { get; set; }
        public string Remarks { get; set; }
    }

    /// <summary>
    /// Data class to hold fire extinguisher information for expiry emails
    /// </summary>
    public class FireExtinguisherExpiryInfo
    {
        public string SerialNumber { get; set; }
        public string Plant { get; set; }
        public string Level { get; set; }
        public string Location { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Remarks { get; set; }
    }
}
