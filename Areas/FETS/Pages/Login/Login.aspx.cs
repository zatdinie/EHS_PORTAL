using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Security;
using FETS.Models;
using System.Web;

namespace FETS.Pages.Login
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Check if a user is already authenticated and redirect them to the dashboard
            // This prevents authenticated users from accessing the login page again
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/FETS/Dashboard");
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            // Extract and sanitize user credentials from input fields
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            // Attempt to validate user credentials against the database
            if (ValidateUser(username, password))
            {
                // Get user ID and role for the authenticated user
                int userId = GetUserId(username);
                string userRole = GetUserRole(username);
                
                // Create an authentication ticket with the user's role
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,                              // ticket version
                    username,                       // username
                    DateTime.Now,                   // issue time
                    DateTime.Now.AddMinutes(30),    // expiration time
                    false,                          // persistent cookie
                    userRole                        // user data (role)
                );

                // Encrypt the ticket
                string encTicket = FormsAuthentication.Encrypt(ticket);
                
                // Create a cookie with the encrypted ticket using the hardcoded cookie name
                HttpCookie authCookie = new HttpCookie(".FETS_AUTH_COOKIE", encTicket);
                authCookie.Path = "/FETS";
                Response.Cookies.Add(authCookie);

                // Also store role in session for quicker access
                Session["UserRole"] = userRole;
                
                // Log successful login directly with user ID
                try
                {
                    // Get the client's IP address
                    string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        ipAddress = Request.ServerVariables["REMOTE_ADDR"];
                    }

                    // Log directly to the database
                    string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        string query = @"
                            INSERT INTO ActivityLogs (UserID, Action, Description, EntityType, EntityID, IPAddress)
                            VALUES (@UserID, @Action, @Description, @EntityType, @EntityID, @IPAddress)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@Action", "Login");
                            cmd.Parameters.AddWithValue("@Description", "User logged in successfully");
                            cmd.Parameters.AddWithValue("@EntityType", "User");
                            cmd.Parameters.AddWithValue("@EntityID", userId.ToString());
                            cmd.Parameters.AddWithValue("@IPAddress", ipAddress);

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Don't let logging failure prevent login
                    System.Diagnostics.Debug.WriteLine(string.Format("Error logging login activity: {0}", ex.Message));
                }

                Response.Redirect("~/FETS/Dashboard");
            }
            else
            {
                // For invalid credentials:
                // Display appropriate error message to the user
                // without revealing which field (username or password) was incorrect for security
                lblMessage.Text = "Invalid username or password!";
                lblMessage.CssClass = "message error";
                lblMessage.Visible = true;

                // Log failed login attempt (we don't have a valid user ID here so we can't use ActivityLogger)
                try
                {
                    LogFailedLoginAttempt(username);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("Error logging failed login: {0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// Validates user credentials against the database
        /// </summary>
        /// <param name="username">The username to validate</param>
        /// <param name="password">The password to validate (plain text)</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        private bool ValidateUser(string username, string password)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM FETS.Users WHERE Username = @Username AND PasswordHash = HASHBYTES('SHA2_256', @Password)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);

                    try
                    {
                        conn.Open();
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        // Log error in production
                        System.Diagnostics.Debug.WriteLine(string.Format("Error validating user: {0}", ex.Message));
                        return false;
                    }
                }
            }
        }

        protected void lnkForgotPassword_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            
            if (string.IsNullOrEmpty(username))
            {
                // Prompt user to enter username
                lblMessage.Text = "Please enter your username first.";
                lblMessage.CssClass = "message error";
                lblMessage.Visible = true;
                return;
            }

            if (username.ToLower() == "admin")
            {
                // Reset admin password to default
                ResetAdminPassword();
                lblMessage.Text = "Admin password has been reset to the default. Please try logging in with the default password.";
                lblMessage.CssClass = "message success";
                lblMessage.Visible = true;
                return;
            }

            // Prompt other users to contact admin for password reset
            lblMessage.Text = "Please contact your system administrator to reset your password.";
            lblMessage.CssClass = "message success";
            lblMessage.Visible = true;
        }

        private void ResetAdminPassword()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE FETS.Users SET PasswordHash = HASHBYTES('SHA2_256', N'admin123') WHERE Username = 'admin'", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // Log error and display message
                    System.Diagnostics.Debug.WriteLine(string.Format("Error resetting admin password: {0}", ex.Message));
                    lblMessage.Text = "An error occurred while resetting the password. Please try again later.";
                    lblMessage.CssClass = "message error";
                    lblMessage.Visible = true;
                }
            }
        }

        /// <summary>
        /// Logs a failed login attempt
        /// </summary>
        private void LogFailedLoginAttempt(string username)
        {
            // Get the client's IP address
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }

            // Since we don't have a valid session/user yet, we'll log directly to the database
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    INSERT INTO ActivityLogs (UserID, Action, Description, EntityType, EntityID, IPAddress)
                    SELECT UserID, 'Failed Login', 'Failed login attempt', 'User', CAST(UserID AS NVARCHAR(50)), @IPAddress
                    FROM FETS.Users 
                    WHERE Username = @Username";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@IPAddress", ipAddress);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        // Just log the error but don't throw
                        System.Diagnostics.Debug.WriteLine(string.Format("Error logging failed login: {0}", ex.Message));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the role of a user from the database
        /// </summary>
        /// <param name="username">The username to get the role for</param>
        /// <returns>The user's role or empty string if not found</returns>
        private string GetUserRole(string username)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT Role FROM FETS.Users WHERE Username = @Username";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);

                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        return result != null ? result.ToString() : string.Empty;
                    }
                    catch (Exception ex)
                    {
                        // Log error in production
                        System.Diagnostics.Debug.WriteLine(string.Format("Error getting user role: {0}", ex.Message));
                        return string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the user ID from the database
        /// </summary>
        /// <param name="username">The username to get the ID for</param>
        /// <returns>The user's ID or 0 if not found</returns>
        private int GetUserId(string username)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT UserID FROM FETS.Users WHERE Username = @Username";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);

                    try
                    {
                        conn.Open();
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                    catch (Exception ex)
                    {
                        // Log error in production
                        System.Diagnostics.Debug.WriteLine(string.Format("Error getting user ID: {0}", ex.Message));
                        return 0;
                    }
                }
            }
        }
    }
}