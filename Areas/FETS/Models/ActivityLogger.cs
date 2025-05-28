using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace FETS.Models
{
    /// <summary>
    /// Utility class for logging user activities
    /// </summary>
    public static class ActivityLogger
    {
        /// <summary>
        /// Log an activity performed by the current user
        /// </summary>
        /// <param name="action">The action performed (e.g., "Login", "Create", "Update", "Delete")</param>
        /// <param name="description">Description of the activity</param>
        /// <param name="entityType">Type of entity acted upon (e.g., "FireExtinguisher", "User")</param>
        /// <param name="entityId">ID of the entity acted upon</param>
        public static void LogActivity(string action, string description = null, string entityType = null, string entityId = null)
        {
            try
            {
                // Get current user ID
                int userId = GetCurrentUserId();
                if (userId <= 0)
                {
                    // Cannot log activity for unauthenticated user
                    return;
                }

                // Get client IP address
                string ipAddress = GetClientIPAddress();

                // Insert activity log
                using (SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = @"
                            INSERT INTO FETS.ActivityLogs (UserID, Action, Description, EntityType, EntityID, IPAddress)
                            VALUES (@UserID, @Action, @Description, @EntityType, @EntityID, @IPAddress)";

                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntityType", entityType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntityID", entityId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IPAddress", ipAddress);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - activity logging should never break the application
                System.Diagnostics.Debug.WriteLine(string.Format("Error logging activity: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Get the current user's ID
        /// </summary>
        /// <returns>User ID or 0 if not found</returns>
        private static int GetCurrentUserId()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return 0;
            }

            string username = HttpContext.Current.User.Identity.Name;
            
            try
            {
                using (SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT UserID FROM FETS.Users WHERE Username = @Username";
                        cmd.Parameters.AddWithValue("@Username", username);

                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error getting user ID: {0}", ex.Message));
                return 0;
            }
        }

        /// <summary>
        /// Get the client's IP address
        /// </summary>
        private static string GetClientIPAddress()
        {
            string ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            return ipAddress;
        }
    }
} 