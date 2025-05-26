using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Security;
using System.Web.UI;
using System.Collections.Generic;

namespace FETS.Pages.Profile
{
    public partial class Profile : System.Web.UI.Page
    {
        // Add these control declarations to match what's in your ASPX file
        protected global::System.Web.UI.WebControls.DropDownList ddlPlant;
        protected global::System.Web.UI.WebControls.Button btnUpdateUser;
        protected global::System.Web.UI.WebControls.Button btnCancelUserEdit;
        protected global::System.Web.UI.WebControls.HiddenField hdnUserID;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                // Redirect to login page
                Response.Redirect("~/Areas/FETS/Pages/Login/Login.aspx");
            }

            if (!IsPostBack)
            {
                CheckAdminAccess();
                if (pnlUserManagement.Visible)
                {
                    LoadUsers();
                    LoadEmailRecipients();
                    LoadPlants();
                }
            }
        }

        /// <summary>
        /// Checks if the current user has administrator privileges and shows/hides admin panels accordingly
        /// </summary>
        private void CheckAdminAccess()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT Role FROM FETS.Users WHERE Username = @Username", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", User.Identity.Name);
                    string role = (string)cmd.ExecuteScalar();
                    pnlUserManagement.Visible = (role == "Administrator");
                    pnlEmailRecipients.Visible = (role == "Administrator");
                }
            }
        }

        /// <summary>
        /// Loads all users from the database and binds them to the users grid view
        /// </summary>
        private void LoadUsers()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT u.UserID, u.Username, u.Role, u.PlantID, ISNULL(p.PlantName, 'None') AS PlantName " +
                    "FROM FETS.Users u " +
                    "LEFT JOIN FETS.Plants p ON u.PlantID = p.PlantID " +
                    "ORDER BY Username", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvUsers.DataSource = dt;
                        gvUsers.DataBind();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the add user button click event to create a new user in the system
        /// </summary>
        protected void btnAddUser_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            string username = txtNewUsername.Text;
            string password = txtUserPassword.Text;
            string role = ddlRole.SelectedValue;
            
            // Get the selected plant ID (or DBNull.Value if "None" is selected)
            object plantId = ddlPlant.SelectedValue == string.Empty ? 
                (object)DBNull.Value : 
                Convert.ToInt32(ddlPlant.SelectedValue);
            
            // Get plant name for logging
            string plantName = ddlPlant.SelectedItem.Text;
            int newUserId = 0;

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Check if username already exists
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM FETS.Users WHERE Username = @Username", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        ShowMessage("Username already exists.", false);
                        
                        // Log failed user creation attempt
                        string failedDescription = $"Failed to create user '{username}' - Username already exists";
                        FETS.Models.ActivityLogger.LogActivity(
                            action: "UserCreationFailed", 
                            description: failedDescription, 
                            entityType: "User", 
                            entityId: username);
                            
                        return;
                    }
                }

                // Add new user with plant assignment
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO FETS.Users (Username, PasswordHash, Role, PlantID) 
                    VALUES (@Username, HASHBYTES('SHA2_256', @Password), @Role, @PlantID);
                    SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@Role", role);
                    cmd.Parameters.AddWithValue("@PlantID", plantId);
                    newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                }
                
                // Log user creation activity
                string description = $"Created new user '{username}'";
                description += $", Role: {role}";
                description += $", Plant: {plantName}";
                
                FETS.Models.ActivityLogger.LogActivity(
                    action: "UserCreated", 
                    description: description, 
                    entityType: "User", 
                    entityId: newUserId.ToString());
            }

            ShowMessage("User added successfully!", true);
            ClearNewUserForm();
            LoadUsers();
        }

        /// <summary>
        /// Handles row commands in the users grid view
        /// </summary>
        protected void gvUsers_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeleteUser")
            {
                int userId = Convert.ToInt32(e.CommandArgument);
                DeleteUser(userId);
            }
            else if (e.CommandName == "EditUser")
            {
                int userId = Convert.ToInt32(e.CommandArgument);
                LoadUserForEdit(userId);
            }
        }

        /// <summary>
        /// Deletes a user from the database by user ID
        /// </summary>
        private void DeleteUser(int userId)
        {
            string username = "";
            string role = "";
            string plantName = "";
            
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // Get user details for logging before deletion
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT u.Username, u.Role, ISNULL(p.PlantName, '-- None --') AS PlantName 
                    FROM FETS.Users u
                    LEFT JOIN FETS.Plants p ON u.PlantID = p.PlantID
                    WHERE u.UserID = @UserID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            username = reader["Username"].ToString();
                            role = reader["Role"].ToString();
                            plantName = reader["PlantName"].ToString();
                        }
                    }
                }
                
                // Delete the user
                using (SqlCommand cmd = new SqlCommand("DELETE FROM FETS.Users WHERE UserID = @UserID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.ExecuteNonQuery();
                }
                
                // Log user deletion activity
                string description = $"Deleted user '{username}'";
                description += $", Role: {role}";
                description += $", Plant: {plantName}";
                
                FETS.Models.ActivityLogger.LogActivity(
                    action: "UserDeleted", 
                    description: description, 
                    entityType: "User", 
                    entityId: userId.ToString());
            }

            LoadUsers();
        }

        /// <summary>
        /// Clears the new user form fields
        /// </summary>
        private void ClearNewUserForm()
        {
            txtNewUsername.Text = string.Empty;
            txtUserPassword.Text = string.Empty;
            ddlRole.SelectedIndex = 0;
            ddlPlant.SelectedIndex = 0;
        }
        
        /// <summary>
        /// Loads all email recipients from the database and creates the table if it doesn't exist
        /// </summary>
        private void LoadEmailRecipients()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // First check if the table exists
                bool tableExists = false;
                string checkTableQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FETS.EmailRecipients'";
                using (SqlCommand checkCmd = new SqlCommand(checkTableQuery, conn))
                {
                    int tableCount = (int)checkCmd.ExecuteScalar();
                    tableExists = (tableCount > 0);
                }
                
                // Create the table if it doesn't exist
                if (!tableExists)
                {
                    string createTableQuery = @"
                        CREATE TABLE EmailRecipients (
                            RecipientID INT IDENTITY(1,1) PRIMARY KEY,
                            EmailAddress NVARCHAR(255) NOT NULL,
                            RecipientName NVARCHAR(100),
                            NotificationType NVARCHAR(50) NOT NULL DEFAULT 'All',
                            IsActive BIT NOT NULL DEFAULT 1,
                            DateAdded DATETIME NOT NULL DEFAULT GETDATE()
                        )";
                   
                }
                
                // Query to get all recipients
                using (SqlCommand cmd = new SqlCommand("SELECT RecipientID, EmailAddress, RecipientName, NotificationType, IsActive FROM FETS.EmailRecipients ORDER BY RecipientName", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvEmailRecipients.DataSource = dt;
                        gvEmailRecipients.DataBind();
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds a new email recipient to the database
        /// </summary>
        protected void btnAddRecipient_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;
                
            string emailAddress = txtEmailAddress.Text.Trim();
            string recipientName = txtRecipientName.Text.Trim();
            string notificationType = ddlNotificationType.SelectedValue;
            
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // Check if email already exists
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM FETS.EmailRecipients WHERE EmailAddress = @EmailAddress", conn))
                {
                    cmd.Parameters.AddWithValue("@EmailAddress", emailAddress);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        ShowMessage("Email address already exists.", false);
                        return;
                    }
                }
                
                // Add new recipient
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO FETS.EmailRecipients (EmailAddress, RecipientName, NotificationType) VALUES (@EmailAddress, @RecipientName, @NotificationType)", conn))
                {
                    cmd.Parameters.AddWithValue("@EmailAddress", emailAddress);
                    cmd.Parameters.AddWithValue("@RecipientName", recipientName);
                    cmd.Parameters.AddWithValue("@NotificationType", notificationType);
                    cmd.ExecuteNonQuery();
                }
            }
            
            ShowMessage("Email recipient added successfully!", true);
            ClearRecipientForm();
            LoadEmailRecipients();
        }
        
        /// <summary>
        /// Clears the recipient form fields
        /// </summary>
        private void ClearRecipientForm()
        {
            txtEmailAddress.Text = string.Empty;
            txtRecipientName.Text = string.Empty;
            ddlNotificationType.SelectedIndex = 0;
        }
        
        /// <summary>
        /// Handles row commands in the email recipients grid view
        /// </summary>
        protected void gvEmailRecipients_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeleteRecipient")
            {
                int recipientId = Convert.ToInt32(e.CommandArgument);
                DeleteRecipient(recipientId);
            }
            else if (e.CommandName == "EditRecipient")
            {
                int recipientId = Convert.ToInt32(e.CommandArgument);
                LoadRecipientForEdit(recipientId);
            }
            else if (e.CommandName == "ToggleStatus")
            {
                int recipientId = Convert.ToInt32(e.CommandArgument);
                ToggleRecipientStatus(recipientId);
            }
        }
        
        /// <summary>
        /// Deletes an email recipient from the database
        /// </summary>
        private void DeleteRecipient(int recipientId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM FETS.EmailRecipients WHERE RecipientID = @RecipientID", conn))
                {
                    cmd.Parameters.AddWithValue("@RecipientID", recipientId);
                    cmd.ExecuteNonQuery();
                }
            }
            
            ShowMessage("Email recipient deleted successfully!", true);
            LoadEmailRecipients();
        }
        
        /// <summary>
        /// Loads a recipient's data for editing
        /// </summary>
        private void LoadRecipientForEdit(int recipientId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT RecipientID, EmailAddress, RecipientName, NotificationType FROM FETS.EmailRecipients WHERE RecipientID = @RecipientID", conn))
                {
                    cmd.Parameters.AddWithValue("@RecipientID", recipientId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            hdnRecipientID.Value = recipientId.ToString();
                            txtEmailAddress.Text = reader["EmailAddress"].ToString();
                            txtRecipientName.Text = reader["RecipientName"].ToString();
                            ddlNotificationType.SelectedValue = reader["NotificationType"].ToString();
                            
                            btnAddRecipient.Visible = false;
                            btnUpdateRecipient.Visible = true;
                            btnCancelEdit.Visible = true;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates an existing email recipient
        /// </summary>
        protected void btnUpdateRecipient_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;
                
            int recipientId = Convert.ToInt32(hdnRecipientID.Value);
            string emailAddress = txtEmailAddress.Text.Trim();
            string recipientName = txtRecipientName.Text.Trim();
            string notificationType = ddlNotificationType.SelectedValue;
            
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // Check if email already exists but not for this recipient
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM FETS.EmailRecipients WHERE EmailAddress = @EmailAddress AND RecipientID != @RecipientID", conn))
                {
                    cmd.Parameters.AddWithValue("@EmailAddress", emailAddress);
                    cmd.Parameters.AddWithValue("@RecipientID", recipientId);
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        ShowMessage("Email address already exists for another recipient.", false);
                        return;
                    }
                }
                
                // Update recipient
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FETS.EmailRecipients SET EmailAddress = @EmailAddress, RecipientName = @RecipientName, NotificationType = @NotificationType WHERE RecipientID = @RecipientID", conn))
                {
                    cmd.Parameters.AddWithValue("@RecipientID", recipientId);
                    cmd.Parameters.AddWithValue("@EmailAddress", emailAddress);
                    cmd.Parameters.AddWithValue("@RecipientName", recipientName);
                    cmd.Parameters.AddWithValue("@NotificationType", notificationType);
                    cmd.ExecuteNonQuery();
                }
            }
            
            ShowMessage("Email recipient updated successfully!", true);
            ClearRecipientForm();
            btnAddRecipient.Visible = true;
            btnUpdateRecipient.Visible = false;
            btnCancelEdit.Visible = false;
            LoadEmailRecipients();
        }
        
        /// <summary>
        /// Cancels the current recipient edit operation
        /// </summary>
        protected void btnCancelEdit_Click(object sender, EventArgs e)
        {
            ClearRecipientForm();
            btnAddRecipient.Visible = true;
            btnUpdateRecipient.Visible = false;
            btnCancelEdit.Visible = false;
        }
        
        /// <summary>
        /// Toggles the active status of an email recipient (enabled/disabled)
        /// </summary>
        private void ToggleRecipientStatus(int recipientId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FETS.EmailRecipients SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE RecipientID = @RecipientID", conn))
                {
                    cmd.Parameters.AddWithValue("@RecipientID", recipientId);
                    cmd.ExecuteNonQuery();
                }
            }
            
            ShowMessage("Recipient status updated successfully!", true);
            LoadEmailRecipients();
        }

        /// <summary>
        /// Handles password change for the currently logged in user
        /// </summary>
        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            string username = User.Identity.Name;
            string currentPassword = txtCurrentPassword.Text;
            string newPassword = txtNewPassword.Text;

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // First verify current password
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM FETS.Users WHERE Username = @Username AND PasswordHash = HASHBYTES('SHA2_256', @Password)", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", currentPassword);
                    int count = (int)cmd.ExecuteScalar();

                    if (count == 0)
                    {
                        ShowMessage("Current password is incorrect.", false);
                        
                        // Log failed password change attempt
                        string failedDescription = "Failed password change attempt - Current password incorrect";
                        FETS.Models.ActivityLogger.LogActivity(
                            action: "PasswordChangeFailed", 
                            description: failedDescription, 
                            entityType: "User", 
                            entityId: username);
                            
                        return;
                    }
                }

                // Get user ID for activity logging
                int userId = 0;
                using (SqlCommand cmd = new SqlCommand("SELECT UserID FROM FETS.Users WHERE Username = @Username", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    userId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Update to new password
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FETS.Users SET PasswordHash = HASHBYTES('SHA2_256', @NewPassword) WHERE Username = @Username", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@NewPassword", newPassword);
                    cmd.ExecuteNonQuery();
                }
                
                // Log successful password change
                string description = "Password changed successfully";
                FETS.Models.ActivityLogger.LogActivity(
                    action: "PasswordChanged", 
                    description: description, 
                    entityType: "User", 
                    entityId: username);
            }

            ShowMessage("Password changed successfully!", true);
            ClearPasswordForm();
        }

        /// <summary>
        /// Displays a message to the user with appropriate styling
        /// </summary>
        private void ShowMessage(string message, bool isSuccess)
        {
            lblMessage.Text = message;
            lblMessage.CssClass = isSuccess ? "message success" : "message error";
            lblMessage.Visible = true;
        }

        /// <summary>
        /// Clears the password change form fields
        /// </summary>
        private void ClearPasswordForm()
        {
            txtCurrentPassword.Text = string.Empty;
            txtNewPassword.Text = string.Empty;
            txtConfirmPassword.Text = string.Empty;
        }

        /// <summary>
        /// Loads plants from the database and populates the plant dropdown
        /// </summary>
        private void LoadPlants()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT PlantID, PlantName FROM FETS.Plants ORDER BY PlantName", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        
                        // Add a "None" option
                        DataRow noneRow = dt.NewRow();
                        noneRow["PlantID"] = DBNull.Value;
                        noneRow["PlantName"] = "-- None --";
                        dt.Rows.InsertAt(noneRow, 0);
                        
                        ddlPlant.DataSource = dt;
                        ddlPlant.DataTextField = "PlantName";
                        ddlPlant.DataValueField = "PlantID";
                        ddlPlant.DataBind();
                    }
                }
            }
        }

        /// <summary>
        /// Loads a user for editing
        /// </summary>
        private void LoadUserForEdit(int userId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT UserID, Username, Role, PlantID FROM FETS.Users WHERE UserID = @UserID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            hdnUserID.Value = userId.ToString();
                            txtNewUsername.Text = reader["Username"].ToString();
                            txtNewUsername.Enabled = false; // Don't allow changing username
                            txtUserPassword.Text = string.Empty; // Clear password field
                            txtUserPassword.Enabled = false; // Don't allow changing password here
                            
                            ddlRole.SelectedValue = reader["Role"].ToString();
                            
                            // Set plant dropdown value
                            if (reader["PlantID"] != DBNull.Value)
                            {
                                ddlPlant.SelectedValue = reader["PlantID"].ToString();
                            }
                            else
                            {
                                ddlPlant.SelectedIndex = 0; // Select "None"
                            }
                            
                            btnAddUser.Visible = false;
                            btnUpdateUser.Visible = true;
                            btnCancelUserEdit.Visible = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates an existing user
        /// </summary>
        protected void btnUpdateUser_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;
            
            int userId = Convert.ToInt32(hdnUserID.Value);
            string username = txtNewUsername.Text;
            string role = ddlRole.SelectedValue;
            string plantName = ddlPlant.SelectedItem.Text;
            
            // Get the selected plant ID (or DBNull.Value if "None" is selected)
            object plantId = ddlPlant.SelectedValue == string.Empty ? 
                (object)DBNull.Value : 
                Convert.ToInt32(ddlPlant.SelectedValue);
            
            // Get original user data for comparison and logging
            string oldRole = string.Empty;
            string oldPlantName = string.Empty;
            
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // Get current user info for logging changes
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT u.Role, ISNULL(p.PlantName, '-- None --') AS PlantName 
                    FROM FETS.Users u
                    LEFT JOIN FETS.Plants p ON u.PlantID = p.PlantID
                    WHERE u.UserID = @UserID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            oldRole = reader["Role"].ToString();
                            oldPlantName = reader["PlantName"].ToString();
                        }
                    }
                }
                
                // Update user
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FETS.Users SET Role = @Role, PlantID = @PlantID WHERE UserID = @UserID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Role", role);
                    cmd.Parameters.AddWithValue("@PlantID", plantId);
                    cmd.ExecuteNonQuery();
                }
                
                // Log user update activity
                string description = $"Updated user '{username}'";
                
                // Add role change to description if it changed
                if (oldRole != role)
                {
                    description += $", Role: {oldRole} → {role}";
                }
                
                // Add plant assignment change to description if it changed
                if (oldPlantName != plantName)
                {
                    description += $", Plant: {oldPlantName} → {plantName}";
                }
                
                // Log the activity
                FETS.Models.ActivityLogger.LogActivity(
                    action: "UserUpdated", 
                    description: description, 
                    entityType: "User", 
                    entityId: userId.ToString());
            }
            
            ShowMessage("User updated successfully!", true);
            ClearNewUserForm();
            txtNewUsername.Enabled = true;
            txtUserPassword.Enabled = true;
            btnAddUser.Visible = true;
            btnUpdateUser.Visible = false;
            btnCancelUserEdit.Visible = false;
            LoadUsers();
        }

        /// <summary>
        /// Cancels the current user edit operation
        /// </summary>
        protected void btnCancelUserEdit_Click(object sender, EventArgs e)
        {
            ClearNewUserForm();
            txtNewUsername.Enabled = true;
            txtUserPassword.Enabled = true;
            btnAddUser.Visible = true;
            btnUpdateUser.Visible = false;
            btnCancelUserEdit.Visible = false;
        }
    }
}