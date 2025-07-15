using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Collections.Generic;
using FETS.Models;

namespace FETS.Pages.ViewSection
{
    public partial class ViewSection : System.Web.UI.Page, IPostBackEventHandler
    {
        // Class to store fire extinguisher details for emails and notifications
        public class FireExtinguisherDetails
        {
            public string SerialNumber { get; set; }
            public string Plant { get; set; }
            public string Level { get; set; }
            public string Location { get; set; }
            public string Type { get; set; }
            public string Remarks { get; set; }
        }

        protected UpdatePanel upMonitoring;
        protected UpdatePanel upMainGrid;
        protected UpdatePanel upServiceConfirmation;
        protected GridView gvServiceConfirmation;
        protected GridView gvServiceSelection;
        protected Panel pnlServiceSelection;
        protected UpdatePanel upServiceSelection;
        protected System.Web.UI.HtmlControls.HtmlGenericControl divResultCount;
        protected Label lblResultCount;
        protected Panel pnlMapLayout;
        protected Repeater rptMaps;
        protected Panel pnlNoMaps;

        private string SortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "SerialNumber"; }
            set { ViewState["SortExpression"] = value; }    
        }

        private string SortDirection
        {
            get { return ViewState["SortDirection"] as string ?? "ASC"; }
            set { ViewState["SortDirection"] = value; }
        }

        private string activeTab = "expired";

        // Properties for counts
        protected int ExpiredCount { get; private set; }
        protected int ExpiringSoonCount { get; private set; }
        protected int UnderServiceCount { get; private set; }

        // Add these properties at the class level, right after the class declaration
        private int? UserPlantID { get; set; }
        private bool IsAdministrator { get; set; }

        // Method for tab button class
        protected string GetTabButtonClass(string tabName)
        {
            return "tab-button" + (activeTab == tabName ? " active" : "");
        }

        // Method to check if current user is an administrator
        public bool IsAdmin()
        {
            return IsAdministrator;
        }

        /// <summary>
        /// Loads the monitoring panels with fire extinguisher data:
        /// - Expired fire extinguishers
        /// - Fire extinguishers expiring soon (within 60 days)
        /// - Fire extinguishers under service
        /// Each panel shows relevant information and allows appropriate actions
        /// </summary>
        protected void Page_Load(object sender, EventArgs e)
        {
             // Check if user is authenticated            if (!User.Identity.IsAuthenticated)            {                // Redirect to login page                Response.Redirect("~/Areas/FETS/Pages/Login/Login.aspx");            }

            // Add this line to get user's plant and role
            GetUserPlantAndRole();

            if (!IsPostBack)
            {
                if (Session["NotificationMessage"] != null)
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "emailSentPopup",
                        $"showNotification('{Session["NotificationMessage"]}');", true);
                    Session["NotificationMessage"] = null; // Clear message after showing
                }

                LoadDropDownLists();
                LoadMonitoringPanels();
                LoadFireExtinguishers();
                
                // Load plant maps only for non-admin users
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    LoadPlantMaps();
                }
            }
            
        }

        /// <summary>
        /// Gets the current user's assigned plant and role from the database
        /// </summary>
        private void GetUserPlantAndRole()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT PlantID, Role FROM FETS.Users WHERE Username = @Username", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", User.Identity.Name);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Get the user's plant ID
                            if (!reader.IsDBNull(reader.GetOrdinal("PlantID")))
                            {
                                UserPlantID = reader.GetInt32(reader.GetOrdinal("PlantID"));
                            }
                            else
                            {
                                UserPlantID = null;
                            }

                            // Check if user is an administrator
                            IsAdministrator = reader["Role"].ToString() == "Administrator";
                        }
                    }
                }
            }
        }

        
        /// <summary>
        /// Loads all dropdown lists with data:
        /// - FETS.Plants dropdown
        /// - Status dropdown
        /// Used for filtering fire extinguishers in the view
        /// </summary>
        private void LoadDropDownLists()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Modify the Plant dropdown loading to respect user's assigned plant
                string plantQuery = "SELECT PlantID, PlantName FROM FETS.Plants";
                
                // If not administrator and has assigned plant, only show that plant
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    plantQuery += " WHERE PlantID = @UserPlantID";
                }
                
                plantQuery += " ORDER BY PlantName";

                using (SqlCommand cmd = new SqlCommand(plantQuery, conn))
                {
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                    }

                    ddlFilterPlant.Items.Clear();
                    
                    // For regular users with assigned plant, don't add the "All Plants" option
                    // and disable the dropdown
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        // Don't add "All Plants" option
                        ddlFilterPlant.Enabled = false; // Lock the dropdown
                    }
                    else
                    {
                        // For admins or users without assigned plant, add "All Plants" option
                        ddlFilterPlant.Items.Add(new ListItem("-- All Plants --", ""));
                        ddlFilterPlant.Enabled = true;
                    }
                    
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlFilterPlant.Items.Add(new ListItem(
                                reader["PlantName"].ToString(),
                                reader["PlantID"].ToString()
                            ));
                        }
                    }
                    
                    // For regular users with assigned plant, auto-select their plant
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        // Set the selected value to user's plant
                        ddlFilterPlant.SelectedValue = UserPlantID.ToString();
                        
                        // Trigger the selection change to load appropriate levels
                        ddlFilterPlant_SelectedIndexChanged(ddlFilterPlant, EventArgs.Empty);
                    }
                }

                // Load Status
                using (SqlCommand cmd = new SqlCommand("SELECT StatusID, StatusName FROM FETS.Status ORDER BY StatusName", conn))
                {
                    ddlFilterStatus.Items.Clear();
                    ddlFilterStatus.Items.Add(new ListItem("-- All Status --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlFilterStatus.Items.Add(new ListItem(
                                reader["StatusName"].ToString(),
                                reader["StatusID"].ToString()
                            ));
                        }
                    }
                }
                
                // Load Fire Extinguisher Types
                using (SqlCommand cmd = new SqlCommand("SELECT TypeID, TypeName FROM FETS.FireExtinguisherTypes ORDER BY TypeName", conn))
                {
                    ddlFilterType.Items.Clear();
                    ddlFilterType.Items.Add(new ListItem("-- All Types --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlFilterType.Items.Add(new ListItem(
                                reader["TypeName"].ToString(),
                                reader["TypeID"].ToString()
                            ));
                        }
                    }
                }
                
                // Load Month Filter
                ddlFilterMonth.Items.Clear();
                ddlFilterMonth.Items.Add(new ListItem("-- All Months --", ""));
                for (int i = 1; i <= 12; i++)
                {
                    string monthName = new DateTime(2000, i, 1).ToString("MMMM");
                    ddlFilterMonth.Items.Add(new ListItem(monthName, i.ToString()));
                }
                
                // Load Year Filter
                ddlFilterYear.Items.Clear();
                ddlFilterYear.Items.Add(new ListItem("-- All Years --", ""));
                int currentYear = DateTime.Now.Year;
                for (int i = currentYear; i <= currentYear + 5; i++)
                {
                    ddlFilterYear.Items.Add(new ListItem(i.ToString(), i.ToString()));
                }
            }
        }

        /// <summary>
        /// Event handler for plant dropdown selection change.
        /// Loads levels based on the selected plant.
        /// </summary>
        protected void ddlFilterPlant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlFilterPlant.SelectedValue))
            {
                ddlFilterLevel.Items.Clear();
                ddlFilterLevel.Items.Add(new ListItem("-- All FETS.Levels --", ""));
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT LevelID, LevelName FROM FETS.Levels WHERE PlantID = @PlantID ORDER BY LevelName", conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", ddlFilterPlant.SelectedValue);
                    ddlFilterLevel.Items.Clear();
                    ddlFilterLevel.Items.Add(new ListItem("-- All FETS.Levels --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlFilterLevel.Items.Add(new ListItem(
                                reader["LevelName"].ToString(),
                                reader["LevelID"].ToString()
                            ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the main fire extinguishers grid with optional filtering.
        /// Supports filtering by plant, level, status, and search text.
        /// </summary>
        private void LoadFireExtinguishers()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string baseQuery = @"
                    SELECT fe.FEID, fe.SerialNumber, fe.AreaCode, p.PlantName, l.LevelName, 
                           fe.Location, t.TypeName, fe.DateExpired, s.StatusName,
                           s.ColorCode, fe.Remarks, fe.Replacement
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    WHERE 1=1";

                // Add restriction based on user's assigned plant (if not administrator)
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    baseQuery += " AND fe.PlantID = @UserPlantID";
                }

                // Add other filters
                if (!string.IsNullOrEmpty(ddlFilterPlant.SelectedValue))
                    baseQuery += " AND fe.PlantID = @PlantID";
                if (!string.IsNullOrEmpty(ddlFilterLevel.SelectedValue))
                    baseQuery += " AND fe.LevelID = @LevelID";
                if (!string.IsNullOrEmpty(ddlFilterStatus.SelectedValue))
                    baseQuery += " AND fe.StatusID = @StatusID";
                if (!string.IsNullOrEmpty(ddlFilterType.SelectedValue))
                    baseQuery += " AND fe.TypeID = @TypeID";
                if (!string.IsNullOrEmpty(ddlFilterMonth.SelectedValue))
                    baseQuery += " AND MONTH(fe.DateExpired) = @Month";
                if (!string.IsNullOrEmpty(ddlFilterYear.SelectedValue))
                    baseQuery += " AND YEAR(fe.DateExpired) = @Year";
                if (!string.IsNullOrEmpty(txtSearch.Text))
                    baseQuery += @" AND (
                        fe.SerialNumber LIKE @Search OR 
                        p.PlantName LIKE @Search OR 
                        l.LevelName LIKE @Search OR
                        fe.Location LIKE @Search OR
                        t.TypeName LIKE @Search OR
                        s.StatusName LIKE @Search OR
                        fe.Remarks LIKE @Search
                    )";

                // First get the count of filtered records
                string countQuery = "SELECT COUNT(*) FROM (" + baseQuery + ") AS FilteredResults";
                int filteredCount = 0;

                using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                {
                    // Add parameters to count query
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        countCmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                    }

                    if (!string.IsNullOrEmpty(ddlFilterPlant.SelectedValue))
                        countCmd.Parameters.AddWithValue("@PlantID", ddlFilterPlant.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterLevel.SelectedValue))
                        countCmd.Parameters.AddWithValue("@LevelID", ddlFilterLevel.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterStatus.SelectedValue))
                        countCmd.Parameters.AddWithValue("@StatusID", ddlFilterStatus.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterType.SelectedValue))
                        countCmd.Parameters.AddWithValue("@TypeID", ddlFilterType.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterMonth.SelectedValue))
                        countCmd.Parameters.AddWithValue("@Month", ddlFilterMonth.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterYear.SelectedValue))
                        countCmd.Parameters.AddWithValue("@Year", ddlFilterYear.SelectedValue);
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                        countCmd.Parameters.AddWithValue("@Search", "%" + txtSearch.Text + "%");

                    conn.Open();
                    filteredCount = (int)countCmd.ExecuteScalar();
                }

                // Add the order by for the data query
                baseQuery += " ORDER BY fe.DateExpired ASC";

                using (SqlCommand cmd = new SqlCommand(baseQuery, conn))
                {
                    // Add user plant parameter if needed
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                    }

                    // Add other parameters
                    if (!string.IsNullOrEmpty(ddlFilterPlant.SelectedValue))
                        cmd.Parameters.AddWithValue("@PlantID", ddlFilterPlant.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterLevel.SelectedValue))
                        cmd.Parameters.AddWithValue("@LevelID", ddlFilterLevel.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterStatus.SelectedValue))
                        cmd.Parameters.AddWithValue("@StatusID", ddlFilterStatus.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterType.SelectedValue))
                        cmd.Parameters.AddWithValue("@TypeID", ddlFilterType.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterMonth.SelectedValue))
                        cmd.Parameters.AddWithValue("@Month", ddlFilterMonth.SelectedValue);
                    if (!string.IsNullOrEmpty(ddlFilterYear.SelectedValue))
                        cmd.Parameters.AddWithValue("@Year", ddlFilterYear.SelectedValue);
                    if (!string.IsNullOrEmpty(txtSearch.Text))
                        cmd.Parameters.AddWithValue("@Search", "%" + txtSearch.Text + "%");

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvFireExtinguishers.DataSource = dt;
                        gvFireExtinguishers.DataBind();
                    }
                }

                // Display the count of filtered results
                lblResultCount.Text = filteredCount.ToString();
                divResultCount.Visible = true;
            }
        }

        /// <summary>
        /// Event handler for the main grid's row data binding.
        /// Sets the status badge color and text based on the fire extinguisher's status.
        /// </summary>
        protected void gvFireExtinguishers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView row = (DataRowView)e.Row.DataItem;
                Label lblStatus = (Label)e.Row.FindControl("lblStatus");
                if (lblStatus != null)
                {
                    lblStatus.Text = row["StatusName"].ToString();
                    lblStatus.Style["background-color"] = row["ColorCode"].ToString();
                }

                // Hide action buttons for non-admin users
                if (!IsAdministrator)
                {
                    e.Row.Cells[e.Row.Cells.Count - 1].Visible = false;
                }
            }
            else if (e.Row.RowType == DataControlRowType.Header)
            {
                // Hide the Actions column header for non-admin users
                if (!IsAdministrator)
                {
                    e.Row.Cells[e.Row.Cells.Count - 1].Visible = false;
                }
            }
        }

        /// <summary>
        /// Event handler for applying filters to the fire extinguishers grid.
        /// Called when filter dropdowns change or search button is clicked.
        /// </summary>
        protected void ApplyFilters(object sender, EventArgs e)
        {
            LoadFireExtinguishers();
        }

        /// <summary>
        /// Event handler for clearing all filters.
        /// Resets all filter dropdowns and search text.
        /// </summary>
        protected void btnClearFilters_Click(object sender, EventArgs e)
        {
            ddlFilterPlant.SelectedIndex = 0;
            LoadDropDownLists();
            
            // Initialize Level dropdown with default "All FETS.Levels" option
            ddlFilterLevel.Items.Clear();
            ddlFilterLevel.Items.Add(new ListItem("-- All FETS.Levels --", ""));
            
            // Now we can safely set the selected index
            ddlFilterLevel.SelectedIndex = 0;
            ddlFilterStatus.SelectedIndex = 0;
            ddlFilterType.SelectedIndex = 0;
            ddlFilterMonth.SelectedIndex = 0;
            ddlFilterYear.SelectedIndex = 0;
            txtSearch.Text = string.Empty;
            ApplyFilters(sender, e);
        }

        /// <summary>
        /// Event handler for sending a fire extinguisher to service.
        /// Updates the fire extinguisher's status to 'Under Service'.
        /// </summary>
        protected void btnConfirmSendToService_Click(object sender, EventArgs e)
        {
            int feId;
            if (int.TryParse(hdnSelectedFEIDForService.Value, out feId))
            {
                // Get the remarks text
                string remarks = txtServiceRemarks.Text.Trim();
                
                // Get FE details for email
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"   
                        SELECT 
                            fe.SerialNumber,
                            p.PlantName,
                            l.LevelName,
                            fe.Location,
                            t.TypeName,
                            fe.Remarks
                        FROM FETS.FireExtinguishers fe
                        INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                        INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                        INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                        WHERE fe.FEID = @FEID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FEID", feId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string serialNumber = reader["SerialNumber"].ToString();
                                string plant = reader["PlantName"].ToString();
                                string level = reader["LevelName"].ToString();
                                string location = reader["Location"].ToString();
                                string type = reader["TypeName"].ToString();
                                
                                // Use the new remarks if provided, otherwise use existing remarks
                                string emailRemarks = !string.IsNullOrEmpty(remarks) 
                                    ? remarks 
                                    : (reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : null);
                                    
                                // Get the replacement value
                                string replacement = ddlServiceReplacement.SelectedValue;

                                // Send email using the template
                                string subject = "Fire Extinguisher Sent for Service";
                                string body = EmailTemplateManager.GetServiceEmailTemplate(
                                    serialNumber,
                                    plant,
                                    level,
                                    location,
                                    type,
                                    emailRemarks,
                                    replacement
                                );

                                var (success, message) = EmailService.SendEmail("", subject, body, "Service");

                                if (success)
                                {
                                    lblExpiryStats.Text = $"Fire extinguisher {serialNumber} sent for service. Email notification sent.";
                                }
                                else
                                {
                                    lblExpiryStats.Text = $"Fire extinguisher {serialNumber} sent for service. Failed to send email: {message}";
                                }
                            }
                        }
                    }
                }

                // Send FE to service
                SendSingleToService(feId);
                LoadMonitoringPanels();
                LoadFireExtinguishers();
                hideSendToServicePanel();
                upMonitoring.Update();
                upMainGrid.Update();
            }
        }

        private void SendSingleToService(int feId)
        {
            string remarks = txtServiceRemarks.Text.Trim();
            string replacement = ddlServiceReplacement.SelectedValue;
            string serialNumber = "";
            string plantName = "";
            string levelName = "";
            string location = "";
            
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Get fire extinguisher details for logging
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT fe.SerialNumber, p.PlantName, l.LevelName, fe.Location
                    FROM FETS.FireExtinguishers fe
                    JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    WHERE fe.FEID = @FEID", conn))
                {
                    cmd.Parameters.AddWithValue("@FEID", feId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            serialNumber = reader["SerialNumber"].ToString();
                            plantName = reader["PlantName"].ToString();
                            levelName = reader["LevelName"].ToString();
                            location = reader["Location"].ToString();
                        }
                    }
                }

                // Get the 'Under Service' status ID
                int underServiceStatusId;
                using (SqlCommand cmd = new SqlCommand("SELECT StatusID FROM FETS.Status WHERE StatusName = 'Under Service'", conn))
                {
                    underServiceStatusId = (int)cmd.ExecuteScalar();
                }

                // Update fire extinguisher status, remarks and replacement
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FETS.FireExtinguishers SET StatusID = @StatusID, Remarks = @Remarks, Replacement = @Replacement, DateSentService = @DateSentService WHERE FEID = @FEID", conn))
                {
                    cmd.Parameters.AddWithValue("@StatusID", underServiceStatusId);
                    cmd.Parameters.AddWithValue("@FEID", feId);
                    cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
                    cmd.Parameters.AddWithValue("@Replacement", string.IsNullOrEmpty(replacement) ? (object)DBNull.Value : replacement);
                    cmd.Parameters.AddWithValue("@DateSentService", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
                
                // Log the send to service activity
                string description = $"Sent fire extinguisher to service - SN: {serialNumber}, Plant: {plantName}, Level: {levelName}, Location: {location}";
                if (!string.IsNullOrEmpty(remarks))
                {
                    description += $", Remarks: {remarks}";
                }
                if (!string.IsNullOrEmpty(replacement) && replacement != "No")
                {
                    description += $", Replacement: {replacement}";
                }
                
                FETS.Models.ActivityLogger.LogActivity(
                    action: "SendToService", 
                    description: description, 
                    entityType: "FireExtinguisher", 
                    entityId: feId.ToString());
            }
            
            // Clear the inputs
            txtServiceRemarks.Text = string.Empty;
            ddlServiceReplacement.SelectedIndex = 0;
        }

        /// <summary>
        /// Event handler for the expired tab button click.
        /// Shows the expired fire extinguishers panel and updates statistics.
        /// </summary>
        protected void btnExpiredTab_Click(object sender, EventArgs e)
        {
            activeTab = "expired";
            mvMonitoring.SetActiveView(vwExpired);
            LoadTabData();
        }

        /// <summary>
        /// Event handler for the expiring soon tab button click.
        /// Shows the fire extinguishers that will expire within 60 days.
        /// </summary>
        protected void btnExpiringSoonTab_Click(object sender, EventArgs e)
        {
            activeTab = "expiringSoon";
            mvMonitoring.SetActiveView(vwExpiringSoon);
            LoadTabData();
        }

        /// <summary>
        /// Event handler for the under service tab button click.
        /// Shows fire extinguishers currently under maintenance.
        /// </summary>
        protected void btnUnderServiceTab_Click(object sender, EventArgs e)
        {
            activeTab = "underService";
            mvMonitoring.SetActiveView(vwUnderService);
            LoadTabData();
        }

        /// <summary>
        /// Event handler for expired grid page index changing.
        /// Handles pagination for the expired fire extinguishers grid.
        /// </summary>
        protected void gvExpired_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                gvExpired.PageIndex = e.NewPageIndex;
                
                // Set the active tab to ensure correct data is loaded
                activeTab = "expired";
                mvMonitoring.SetActiveView(vwExpired);
                
                // Update all monitoring data
                LoadTabData();
                
                // Ensure the UpdatePanel is refreshed
                upMonitoring.Update();
                
                // Add debug information
                System.Diagnostics.Debug.WriteLine($"Changed Expired grid to page {e.NewPageIndex}");
            }
            catch (Exception ex)
            {
                // Log error and show user notification
                System.Diagnostics.Debug.WriteLine($"Error in gvExpired_PageIndexChanging: {ex.Message}");
                ScriptManager.RegisterStartupScript(this, GetType(), "paginationError", 
                    $"showNotification('❌ Error changing page: {ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        /// <summary>
        /// Event handler for expired grid row commands.
        /// Handles actions like sending expired fire extinguishers for service.
        /// </summary>
        protected void gvExpired_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SendForService")
            {
                int feId = Convert.ToInt32(e.CommandArgument);
                hdnSelectedFEIDForService.Value = feId.ToString();
                LoadServiceConfirmationGrid("single");
                upServiceConfirmation.Update();
            }
        }

        /// <summary>
        /// Event handler for expiring soon grid page index changing.
        /// Handles pagination for the expiring soon fire extinguishers grid.
        /// </summary>
        protected void gvExpiringSoon_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                gvExpiringSoon.PageIndex = e.NewPageIndex;
                
                // Set the active tab to ensure correct data is loaded
                activeTab = "expiringSoon";
                mvMonitoring.SetActiveView(vwExpiringSoon);
                
                // Update all monitoring data
                LoadTabData();
                
                // Ensure the UpdatePanel is refreshed
                upMonitoring.Update();
                
                // Add debug information
                System.Diagnostics.Debug.WriteLine($"Changed ExpiringSoon grid to page {e.NewPageIndex}");
            }
            catch (Exception ex)
            {
                // Log error and show user notification
                System.Diagnostics.Debug.WriteLine($"Error in gvExpiringSoon_PageIndexChanging: {ex.Message}");
                ScriptManager.RegisterStartupScript(this, GetType(), "paginationError", 
                    $"showNotification('❌ Error changing page: {ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        /// <summary>
        /// Event handler for expiring soon grid row commands.
        /// Handles actions like sending expiring soon fire extinguishers for service.
        /// </summary>
        protected void gvExpiringSoon_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SendForService")
            {
                int feId = Convert.ToInt32(e.CommandArgument);
                hdnSelectedFEIDForService.Value = feId.ToString();
                LoadServiceConfirmationGrid("single");
                upServiceConfirmation.Update();
            }
        }
    
        /// <summary>
        /// Event handler for under service grid page index changing.
        /// Handles pagination for the under service fire extinguishers grid.
        /// </summary>
        protected void gvUnderService_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            try
            {
                gvUnderService.PageIndex = e.NewPageIndex;
                
                // Set the active tab to ensure correct data is loaded
                activeTab = "underService";
                mvMonitoring.SetActiveView(vwUnderService);
                
                // Update all monitoring data
                LoadTabData();
                
                // Ensure the UpdatePanel is refreshed
                upMonitoring.Update();
                
                // Add debug information
                System.Diagnostics.Debug.WriteLine($"Changed UnderService grid to page {e.NewPageIndex}");
            }
            catch (Exception ex)
            {
                // Log error and show user notification
                System.Diagnostics.Debug.WriteLine($"Error in gvUnderService_PageIndexChanging: {ex.Message}");
                ScriptManager.RegisterStartupScript(this, GetType(), "paginationError", 
                    $"showNotification('❌ Error changing page: {ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        /// <summary>
        /// Event handler for under service grid row commands.
        /// Handles actions like completing service for fire extinguishers.
        /// </summary>
        protected void gvUnderService_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // The CompleteService command has been replaced by the bulk completion functionality
        }

        /// <summary>
        /// Loads the under service grid with fire extinguishers currently under maintenance.
        /// Shows serial number, location, expiry date, and status.
        /// </summary>
        private void LoadUnderServiceGrid()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        fe.FEID,
                        fe.SerialNumber,
                        p.PlantName,
                        l.LevelName,
                        fe.Location,
                        t.TypeName,
                        fe.DateExpired,
                        fe.DateSentService,
                        s.StatusName
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    WHERE s.StatusName = 'Under Service'
                    ORDER BY fe.DateExpired ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvUnderService.DataSource = dt;
                        gvUnderService.DataBind();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the connection string with extended timeout for long-running operations.
        /// </summary>
        private string GetConnectionString()
        {
            var builder = new SqlConnectionStringBuilder(
                ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
            builder.ConnectTimeout = 120; // 2 minutes
            return builder.ConnectionString;
        }

        /// <summary>
        /// Loads the service confirmation grid with fire extinguishers to be sent for service.
        /// Can handle single fire extinguisher or all expired/expiring soon ones.
        /// </summary>
        private void LoadServiceConfirmationGrid(string mode)
        {
            string connectionString = GetConnectionString();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        fe.FEID,
                        fe.SerialNumber,
                        fe.AreaCode,
                        p.PlantName,
                        l.LevelName,
                        fe.Location,
                        fe.DateExpired,
                        s.StatusName,
                        fe.Remarks,
                        fe.Replacement,
                        CASE 
                            WHEN fe.DateExpired < GETDATE() THEN 'Expired'
                            ELSE 'Expiring Soon'
                        END as FETS.Status
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    WHERE ";

                if (mode == "single")
                {
                    query += "fe.FEID = @FEID";
                }
                else
                {
                    query += @"(
                        (fe.DateExpired < GETDATE() OR 
                        (fe.DateExpired >= GETDATE() AND fe.DateExpired <= DATEADD(day, 60, GETDATE())))
                        AND s.StatusName != 'Under Service'
                    )
                    ORDER BY 
                        CASE 
                            WHEN fe.DateExpired < GETDATE() THEN 1
                            ELSE 2
                        END,
                        fe.DateExpired ASC";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (mode == "single")
                    {
                        int feId;
                        if (int.TryParse(hdnSelectedFEIDForService.Value, out feId))
                        {
                            cmd.Parameters.AddWithValue("@FEID", feId);
                        }
                        else
                        {
                            return;
                        }
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvServiceConfirmation.DataSource = dt;
                        gvServiceConfirmation.DataBind();
                    }
                }
            }
        }

        /// <summary>
        /// Loads all monitoring panels with their respective data and updates counts.
        /// </summary>
        private void LoadMonitoringPanels()
        {
            LoadTabData();
        }

        private void LoadTabData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    
                    // First, get counts for all categories regardless of which tab is active
                    string countQuery = @"
                        SELECT 
                            SUM(CASE WHEN fe.DateExpired < GETDATE() AND s.StatusName != 'Under Service' THEN 1 ELSE 0 END) as ExpiredCount,
                            SUM(CASE WHEN fe.DateExpired >= GETDATE() AND fe.DateExpired <= DATEADD(day, 60, GETDATE()) AND s.StatusName != 'Under Service' THEN 1 ELSE 0 END) as ExpiringSoonCount,
                            SUM(CASE WHEN s.StatusName = 'Under Service' THEN 1 ELSE 0 END) as UnderServiceCount
                        FROM FETS.FireExtinguishers fe
                        INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                        WHERE 1=1";
                        
                    // Add plant restriction for non-admin users
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        countQuery += " AND fe.PlantID = @UserPlantID";
                    }
                        
                    using (SqlCommand countCmd = new SqlCommand(countQuery, conn))
                    {
                        // Add user plant parameter if needed
                        if (!IsAdministrator && UserPlantID.HasValue)
                        {
                            countCmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                        }

                        using (SqlDataReader countReader = countCmd.ExecuteReader())
                        {
                            if (countReader.Read())
                            {
                                // Set the count properties for the badge displays
                                ExpiredCount = countReader.IsDBNull(0) ? 0 : countReader.GetInt32(0);
                                ExpiringSoonCount = countReader.IsDBNull(1) ? 0 : countReader.GetInt32(1);
                                UnderServiceCount = countReader.IsDBNull(2) ? 0 : countReader.GetInt32(2);
                            }
                            else
                            {
                                // Default to 0 if no data found
                                ExpiredCount = 0;
                                ExpiringSoonCount = 0;
                                UnderServiceCount = 0;
                            }
                        }
                    }
                    
                    // Now get the specific data for the active tab
                    string dataQuery = "";
                    
                    switch (activeTab)
                    {
                        case "expired":
                            dataQuery = @"
                                SELECT 
                                    fe.FEID,
                                    fe.SerialNumber,
                                    fe.AreaCode,
                                    p.PlantName,
                                    l.LevelName,
                                    fe.Location,
                                    t.TypeName,
                                    fe.DateExpired,
                                    s.StatusName,
                                    DATEDIFF(day, fe.DateExpired, GETDATE()) as DaysExpired
                                FROM FETS.FireExtinguishers fe
                                INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                                INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                                INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                                INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                                WHERE fe.DateExpired < GETDATE() AND s.StatusName != 'Under Service'";
                                
                            // Add plant restriction for non-admin users
                            if (!IsAdministrator && UserPlantID.HasValue)
                            {
                                dataQuery += " AND fe.PlantID = @UserPlantID";
                            }
                                
                            dataQuery += " ORDER BY fe.DateExpired ASC";
                            break;
                            
                        case "expiringSoon":
                            dataQuery = @"
                                SELECT 
                                    fe.FEID,
                                    fe.SerialNumber,
                                    fe.AreaCode,
                                    p.PlantName,
                                    l.LevelName,
                                    fe.Location,
                                    t.TypeName,
                                    fe.DateExpired,
                                    s.StatusName,
                                    DATEDIFF(day, GETDATE(), fe.DateExpired) as DaysLeft
                                FROM FETS.FireExtinguishers fe
                                INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                                INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                                INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                                INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                                WHERE fe.DateExpired >= GETDATE() AND fe.DateExpired <= DATEADD(day, 60, GETDATE()) AND s.StatusName != 'Under Service'";
                                
                            // Add plant restriction for non-admin users
                            if (!IsAdministrator && UserPlantID.HasValue)
                            {
                                dataQuery += " AND fe.PlantID = @UserPlantID";
                            }
                                
                            dataQuery += " ORDER BY fe.DateExpired ASC";
                            break;
                            
                        case "underService":
                            dataQuery = @"
                                SELECT 
                                    fe.FEID,
                                    fe.SerialNumber,
                                    fe.AreaCode,
                                    p.PlantName,
                                    l.LevelName,
                                    fe.Location,
                                    t.TypeName,
                                    fe.DateExpired,
                                    s.StatusName,
                                    fe.DateSentService
                                FROM FETS.FireExtinguishers fe
                                INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                                INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                                INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                                INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                                WHERE s.StatusName = 'Under Service'";
                                
                            // Add plant restriction for non-admin users
                            if (!IsAdministrator && UserPlantID.HasValue)
                            {
                                dataQuery += " AND fe.PlantID = @UserPlantID";
                            }
                                
                            dataQuery += " ORDER BY fe.DateExpired ASC";
                            break;
                    }
                    
                    // Execute the query and bind data to the appropriate grid
                    using (SqlCommand dataCmd = new SqlCommand(dataQuery, conn))
                    {
                        // Add user plant parameter if needed
                        if (!IsAdministrator && UserPlantID.HasValue)
                        {
                            dataCmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                        }
                        
                        using (SqlDataAdapter adapter = new SqlDataAdapter(dataCmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            
                            switch (activeTab)
                            {
                                case "expired":
                                    gvExpired.DataSource = dt;
                                    gvExpired.DataBind();
                                    break;
                                case "expiringSoon":
                                    gvExpiringSoon.DataSource = dt;
                                    gvExpiringSoon.DataBind();
                                    break;
                                case "underService":
                                    gvUnderService.DataSource = dt;
                                    gvUnderService.DataBind();
                                    break;
                            }
                        }
                    }
                    
                    // Update UI with count badges
                    btnExpiredTab.DataBind();
                    btnExpiringSoonTab.DataBind();
                    btnUnderServiceTab.DataBind();
                    upMonitoring.Update();
                }
                catch (Exception ex)
                {
                    // Log the error
                    System.Diagnostics.Debug.WriteLine($"Error in LoadTabData: {ex.Message}");
                    // Show error notification to user
                    ScriptManager.RegisterStartupScript(this, GetType(), "loadDataError", 
                        $"showNotification('❌ Error loading monitoring data: {ex.Message.Replace("'", "\\'")}', 'error');", true);
                }
            }
        }

        /// <summary>
        /// Override of the OnLoad event.
        /// Initializes the page and loads all necessary data.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!IsPostBack)
            {
                LoadDropDownLists();
                LoadFireExtinguishers();
                LoadMonitoringPanels();
                divResultCount.Visible = false; // Hide result count initially
            }
        }

        private void hideSendToServicePanel()
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "hideServicePanel", "hideSendToServicePanel();", true);
        }

        /// <summary>
        /// Event handler for the main grid's page index changing.
        /// Handles pagination for the main fire extinguishers grid.
        /// </summary>
        protected void gvFireExtinguishers_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvFireExtinguishers.PageIndex = e.NewPageIndex;
            LoadFireExtinguishers();
        }

        /// <summary>
        /// Event handler for the main grid's sorting.
        /// Handles column sorting for the main fire extinguishers grid.
        /// </summary>
        protected void gvFireExtinguishers_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (SortExpression == e.SortExpression)
            {
                SortDirection = SortDirection == "ASC" ? "DESC" : "ASC";
            }
            else
            {
                SortExpression = e.SortExpression;
                SortDirection = "ASC";
            }

            LoadFireExtinguishers();
        }

        /// <summary>
        /// Event handler for the main grid's row commands.
        /// Handles actions like editing, deleting fire extinguishers or sending them for service.
        /// </summary>
        protected void gvFireExtinguishers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditRow")
            {
                int feId = Convert.ToInt32(e.CommandArgument);
                LoadFireExtinguisherForEdit(feId);
            }
            else if (e.CommandName == "SendForService")
            {
                int feId = Convert.ToInt32(e.CommandArgument);
                hdnSelectedFEIDForService.Value = feId.ToString();
                LoadServiceConfirmationGrid("single");
                upServiceConfirmation.Update();
            }
            else if (e.CommandName == "DeleteRow")
            {
                int feId = Convert.ToInt32(e.CommandArgument);
                DeleteFireExtinguisher(feId);
                LoadMonitoringPanels();
                LoadFireExtinguishers();
                upMonitoring.Update();
                upMainGrid.Update();
            }
        }

        /// <summary>
        /// Deletes a fire extinguisher from the database.
        /// Refreshes the grid after successful deletion.
        /// </summary>
        private void DeleteFireExtinguisher(int feId)
        {
            string connectionString = GetConnectionString();
            
            // First get the fire extinguisher details for logging
            string serialNumber = "";
            string plantName = "";
            string location = "";
            
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // Get fire extinguisher details before deletion
                    using (SqlCommand cmdGetDetails = new SqlCommand(
                        @"SELECT 
                            fe.SerialNumber, 
                            p.PlantName,
                            fe.Location
                        FROM FETS.FireExtinguishers fe
                        JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                        WHERE fe.FEID = @FEID", conn))
                    {
                        cmdGetDetails.Parameters.AddWithValue("@FEID", feId);
                        using (SqlDataReader reader = cmdGetDetails.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                serialNumber = reader["SerialNumber"].ToString();
                                plantName = reader["PlantName"].ToString();
                                location = reader["Location"].ToString();
                            }
                        }
                    }
                    
                    // First, delete related records from FETS.ServiceReminders table
                    using (SqlCommand cmdDeleteReminders = new SqlCommand(
                        "DELETE FROM FETS.ServiceReminders WHERE FEID = @FEID", conn))
                    {
                        cmdDeleteReminders.Parameters.AddWithValue("@FEID", feId);
                        cmdDeleteReminders.ExecuteNonQuery();
                    }
                    
                    // Then delete the fire extinguisher
                    using (SqlCommand cmdDeleteFE = new SqlCommand(
                        "DELETE FROM FETS.FireExtinguishers WHERE FEID = @FEID", conn))
                    {
                        cmdDeleteFE.Parameters.AddWithValue("@FEID", feId);
                        cmdDeleteFE.ExecuteNonQuery();
                    }
                    
                    // Log the deletion activity
                    string description = $"Deleted fire extinguisher SN: {serialNumber}, Plant: {plantName}, Location: {location}";
                    FETS.Models.ActivityLogger.LogActivity(
                        action: "Delete", 
                        description: description, 
                        entityType: "FireExtinguisher", 
                        entityId: feId.ToString());
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with UI update
                System.Diagnostics.Debug.WriteLine($"Error in DeleteFireExtinguisher: {ex.Message}");
                ScriptManager.RegisterStartupScript(this, GetType(), "deleteError",
                    $"showNotification('❌ Error deleting fire extinguisher: {ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
            
            // Reload the grids after deletion
            LoadFireExtinguishers();
            LoadMonitoringPanels();
        }

        /// <summary>
        /// Updates a fire extinguisher's status in the database.
        /// Refreshes the grids after successful update.
        /// </summary>
        private void UpdateFireExtinguisherStatus(int feId, string statusName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE FETS.FireExtinguishers 
                    SET StatusID = (SELECT StatusID FROM FETS.Status WHERE StatusName = @StatusName)
                    WHERE FEID = @FEID", conn))
                {
                    cmd.Parameters.AddWithValue("@FEID", feId);
                    cmd.Parameters.AddWithValue("@StatusName", statusName);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadFireExtinguishers();
            LoadMonitoringPanels();
        }
    


            public class EmailService
        {
            public static (bool Success, string Message) SendEmail(string recipient, string subject, string body, string notificationType = "Service")
            {
                try
                {
                    var smtpHost = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as System.Net.Configuration.SmtpSection;
                    
                    if (smtpHost == null)
                    {
                        // Handle configuration error
                        Page page = HttpContext.Current.CurrentHandler as Page;
                        if (page != null)
                        {
                            ScriptManager.RegisterStartupScript(
                                page, page.GetType(), "emailErrorPopup",
                                "showNotification('❌ Failed to load mail settings.', 'error');", true
                            );
                        }
                        return (false, "Failed to load mail settings from configuration.");
                    }

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("FETS@INARI", smtpHost.From));
                    
                    // If a specific recipient is provided, use it
                    if (!string.IsNullOrEmpty(recipient))
                    {
                    message.To.Add(new MailboxAddress("", recipient));
                    }
                    else
                    {
                        // Otherwise get recipients from the database
                        List<EmailRecipient> recipients = GetEmailRecipientsFromDB(notificationType, "All");
                        
                        if (recipients.Count == 0)
                        {
                            // Fallback to default recipient if none found
                            message.To.Add(new MailboxAddress("", "danishaiman3b@gmail.com"));
                        }
                        else
                        {
                            foreach (var emailRecipient in recipients)
                            {
                                message.To.Add(new MailboxAddress(emailRecipient.RecipientName, emailRecipient.EmailAddress));
                            }
                        }
                    }
                    
                    message.Subject = subject;

                    var bodyBuilder = new BodyBuilder { HtmlBody = body };
                    message.Body = bodyBuilder.ToMessageBody();

                    using (var client = new SmtpClient())
                    {
                        client.Connect(smtpHost.Network.Host, smtpHost.Network.Port, 
                                    smtpHost.Network.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

                        if (!string.IsNullOrEmpty(smtpHost.Network.UserName))
                        {
                            client.Authenticate(smtpHost.Network.UserName, smtpHost.Network.Password);
                        }

                        client.Send(message);
                        client.Disconnect(true);
                        
                        // Success notification with the new system
                        Page page = HttpContext.Current.CurrentHandler as Page;
                        if (page != null)
                        {
                            ScriptManager.RegisterStartupScript(
                                page, page.GetType(), "emailSentPopup",
                                "showNotification('✅ Email sent successfully!');", true
                            );
                        }

                        return (true, "Email sent successfully!");
                    }
                }
                catch (Exception ex)
                {
                    // Error notification with the new system
                    Page page = HttpContext.Current.CurrentHandler as Page;
                    if (page != null)
                    {
                        ScriptManager.RegisterStartupScript(
                            page, page.GetType(), "emailErrorPopup",
                            "showNotification('❌ " + ex.Message.Replace("'", "\\'") + "', 'error');", true
                        );
                    }
                    return (false, $"Email Error: {ex.Message}");
                }
            }
            
            private static List<EmailRecipient> GetEmailRecipientsFromDB(string notificationType, string fallbackType = null)
            {
                List<EmailRecipient> recipients = new List<EmailRecipient>();
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                
                try
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        
                        // First check if the EmailRecipients table exists
                        bool tableExists = false;
                        string checkTableQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'FETS' AND TABLE_NAME = 'EmailRecipients'";
                        using (SqlCommand checkCmd = new SqlCommand(checkTableQuery, conn))
                        {
                            int tableCount = (int)checkCmd.ExecuteScalar();
                            tableExists = (tableCount > 0);
                        }
                        
                        if (!tableExists)
                        {
                            return recipients; // Return empty list to use fallback
                        }
                        
                        // Query to get recipients for this notification type or "All" type
                        string query = @"
                            SELECT EmailAddress, RecipientName, NotificationType 
                            FROM FETS.EmailRecipients 
                            WHERE IsActive = 1 AND (NotificationType = @NotificationType OR NotificationType = 'All'";
                        
                        // Add fallback type if specified
                        if (!string.IsNullOrEmpty(fallbackType) && fallbackType != notificationType)
                        {
                            query += " OR NotificationType = @FallbackType";
                        }
                        
                        query += ") ORDER BY RecipientName";
                        
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@NotificationType", notificationType);
                            if (!string.IsNullOrEmpty(fallbackType) && fallbackType != notificationType)
                            {
                                cmd.Parameters.AddWithValue("@FallbackType", fallbackType);
                            }
                            
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    recipients.Add(new EmailRecipient
                                    {
                                        EmailAddress = reader["EmailAddress"].ToString(),
                                        RecipientName = reader["RecipientName"].ToString(),
                                        NotificationType = reader["NotificationType"].ToString()
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error retrieving email recipients: {ex.Message}");
                    // Return empty list to use fallback
                }
                
                return recipients;
            }
        }
        
        // Email recipient class
        public class EmailRecipient
        {
            public string EmailAddress { get; set; }
            public string RecipientName { get; set; }
            public string NotificationType { get; set; }
        }

         protected void btnSendToService_Click(object sender, EventArgs e)
        {
            LinkButton btn = (LinkButton)sender;
            string extinguisherId = btn.CommandArgument;
            int feId = Convert.ToInt32(extinguisherId);
            string serialNumber = "";
            string plantName = "";
            string levelName = "";
            string location = "";
            string typeName = "";

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // First update the status to "Under Service" and set DateSentService
                int underServiceStatusId;
                using (SqlCommand cmd = new SqlCommand("SELECT StatusID FROM FETS.Status WHERE StatusName = 'Under Service'", conn))
                {
                    underServiceStatusId = (int)cmd.ExecuteScalar();
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE FETS.FireExtinguishers SET StatusID = @StatusID, DateSentService = @DateSentService WHERE FEID = @FEID", conn))
                {
                    cmd.Parameters.AddWithValue("@StatusID", underServiceStatusId);
                    cmd.Parameters.AddWithValue("@FEID", extinguisherId);
                    cmd.Parameters.AddWithValue("@DateSentService", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }

                // Then get the updated fire extinguisher details
                string query = @"   
                    SELECT 
                        fe.SerialNumber,
                        p.PlantName,
                        l.LevelName,
                        fe.Location,
                        t.TypeName,
                        fe.Remarks,
                        s.StatusName
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    WHERE fe.FEID = @FEID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FEID", extinguisherId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            serialNumber = reader["SerialNumber"].ToString();
                            plantName = reader["PlantName"].ToString();
                            levelName = reader["LevelName"].ToString();
                            location = reader["Location"].ToString();
                            typeName = reader["TypeName"].ToString();
                            string remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : null;
                            string status = reader["StatusName"].ToString();

                            // Now send the email with the updated details
                            string subject = $"Fire Extinguisher {serialNumber} Sent for Service";
                            string body = EmailTemplateManager.GetServiceEmailTemplate(
                                serialNumber,
                                plantName,
                                levelName,
                                location,
                                typeName,
                                remarks
                            );

                            var (success, message) = EmailService.SendEmail("", subject, body, "Service");

                            if (success)
                            {
                                ScriptManager.RegisterStartupScript(this, GetType(), "emailSentPopup",
                                    "showNotification('✅ Email sent successfully!'); setTimeout(function() { window.location.reload(); }, 2000);", true);
                            }
                            else
                            {
                                ScriptManager.RegisterStartupScript(this, GetType(), "emailErrorPopup",
                                    $"showNotification('❌ Email failed: {message.Replace("'", "\'")}', 'error');", true);
                            }
                        }
                    }
                }
                
                // Log the send to service activity
                string description = $"Sent fire extinguisher to service - SN: {serialNumber}, Plant: {plantName}, Level: {levelName}, Location: {location}, Type: {typeName}";
                FETS.Models.ActivityLogger.LogActivity(
                    action: "SendToService", 
                    description: description, 
                    entityType: "FireExtinguisher", 
                    entityId: feId.ToString());
            }

            // Refresh the page data
            LoadMonitoringPanels();
            LoadFireExtinguishers();
            upMonitoring.Update();
            upMainGrid.Update();
        }

        private void LoadServiceSelectionGrid()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        fe.FEID,
                        fe.SerialNumber,
                        fe.AreaCode,
                        p.PlantName,
                        l.LevelName,
                        fe.Location,
                        t.TypeName,
                        fe.Remarks
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    WHERE s.StatusName != 'Under Service'
                    AND (
                        fe.DateExpired < GETDATE() -- Expired
                        OR 
                        (fe.DateExpired >= GETDATE() AND fe.DateExpired <= DATEADD(day, 60, GETDATE())) -- Expiring Soon (within 60 days)
                    )";
                    
                // Add plant restriction for non-admin users
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    query += " AND fe.PlantID = @UserPlantID";
                }
                
                query += " ORDER BY fe.DateExpired ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Add user plant parameter if needed
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                    }
                    
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvServiceSelection.DataSource = dt;
                        gvServiceSelection.DataBind();
                    }
                }
            }
        }

        protected void btnShowSelection_Click(object sender, EventArgs e)
        {
            LoadServiceSelectionGrid();
            pnlServiceSelection.Visible = true;
            upServiceSelection.Update();
        }

        protected void btnConfirmSelection_Click(object sender, EventArgs e)
        {
            List<int> selectedFEIDs = new List<int>();
            Dictionary<int, string> feRemarks = new Dictionary<int, string>();
            Dictionary<int, string> feReplacements = new Dictionary<int, string>();
            
            foreach (GridViewRow row in gvServiceSelection.Rows)
            {
                CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                if (chkSelect != null && chkSelect.Checked)
                {
                    int feId = Convert.ToInt32(gvServiceSelection.DataKeys[row.RowIndex].Value);
                    selectedFEIDs.Add(feId);
                    
                    // Get the remarks from the textbox
                    TextBox txtRemarks = (TextBox)row.FindControl("txtRemarks");
                    if (txtRemarks != null)
                    {
                        feRemarks[feId] = txtRemarks.Text.Trim();
                    }
                    
                    // Get the replacement from the dropdown
                    DropDownList ddlReplacement = (DropDownList)row.FindControl("ddlReplacement");
                    if (ddlReplacement != null)
                    {
                        feReplacements[feId] = ddlReplacement.SelectedValue;
                    }
                }
            }

            if (selectedFEIDs.Count > 0)
            {
                // Get details for all selected fire extinguishers
                List<FireExtinguisherServiceInfo> extinguisherDetails = new List<FireExtinguisherServiceInfo>();
                Dictionary<int, string> extinguisherDescriptions = new Dictionary<int, string>();
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string feIdList = string.Join(",", selectedFEIDs);
                    string query = $@"   
                        SELECT 
                            fe.FEID,
                            fe.SerialNumber,
                            fe.AreaCode,
                            p.PlantName,
                            l.LevelName,
                            fe.Location,
                            t.TypeName,
                            fe.Remarks,
                            fe.Replacement
                        FROM FETS.FireExtinguishers fe
                        INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                        INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                        INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                        WHERE fe.FEID IN ({feIdList})";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int feId = Convert.ToInt32(reader["FEID"]);
                                string serialNumber = reader["SerialNumber"].ToString();
                                string plant = reader["PlantName"].ToString();
                                string level = reader["LevelName"].ToString();
                                string location = reader["Location"].ToString();
                                string type = reader["TypeName"].ToString();
                                
                                string remarks = feRemarks.ContainsKey(feId) ? feRemarks[feId] : 
                                               (reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : null);
                                
                                string replacement = feReplacements.ContainsKey(feId) ? feReplacements[feId] : 
                                                    (reader["Replacement"] != DBNull.Value ? reader["Replacement"].ToString() : null);
                                
                                extinguisherDetails.Add(new FireExtinguisherServiceInfo
                                {
                                    SerialNumber = serialNumber,
                                    AreaCode = reader["AreaCode"].ToString(),
                                    Plant = plant,
                                    Level = level,
                                    Location = location,
                                    Type = type,
                                    Remarks = remarks,
                                    Replacement = replacement
                                });
                                
                                // Create description for activity log
                                string description = $"Sent fire extinguisher to service - SN: {serialNumber}, Plant: {plant}, Level: {level}, Location: {location}, Type: {type}";
                                if (!string.IsNullOrEmpty(remarks))
                                {
                                    description += $", Remarks: {remarks}";
                                }
                                if (!string.IsNullOrEmpty(replacement) && replacement != "No")
                                {
                                    description += $", Replacement: {replacement}";
                                }
                                extinguisherDescriptions[feId] = description;
                            }
                        }
                    }
                    
                    // Update each fire extinguisher individually to set status and remarks
                    foreach (int feId in selectedFEIDs)
                    {
                        string updateQuery = @"
                            UPDATE FETS.FireExtinguishers
                            SET StatusID = (SELECT StatusID FROM FETS.Status WHERE StatusName = 'Under Service'),
                                Remarks = @Remarks,
                                Replacement = @Replacement,
                                DateSentService = @DateSentService
                            WHERE FEID = @FEID";
                        
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@FEID", feId);
                            updateCmd.Parameters.AddWithValue("@Remarks", 
                                feRemarks.ContainsKey(feId) ? (object)feRemarks[feId] : DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@Replacement", 
                                feReplacements.ContainsKey(feId) ? (object)feReplacements[feId] : DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@DateSentService", DateTime.Now);
                            updateCmd.ExecuteNonQuery();
                            
                            // Log the activity for each fire extinguisher
                            if (extinguisherDescriptions.ContainsKey(feId))
                            {
                                FETS.Models.ActivityLogger.LogActivity(
                                    action: "SendToService", 
                                    description: extinguisherDescriptions[feId], 
                                    entityType: "FireExtinguisher", 
                                    entityId: feId.ToString());
                            }
                        }
                    }
                    
                    // Log the batch operation
                    string batchDescription = $"Sent {selectedFEIDs.Count} fire extinguishers to service in batch";
                    FETS.Models.ActivityLogger.LogActivity(
                        action: "BatchSendToService", 
                        description: batchDescription, 
                        entityType: "FireExtinguisher", 
                        entityId: string.Join(",", selectedFEIDs));
                }
                
                // Send email with the professional template
                string subject = $"{extinguisherDetails.Count} Fire Extinguishers Sent for Service";
                string body = EmailTemplateManager.GetMultipleServiceEmailTemplate(extinguisherDetails);

                var (success, message) = EmailService.SendEmail("", subject, body, "Service");

                if (success)
                {
                    Session["NotificationMessage"] = "✅ Email sent successfully!";
                    Response.Redirect(Request.Url.PathAndQuery, false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    lblExpiryStats.Text = $"Email failed: {message}";
                }
            }
            else
            {
                // Show warning notification
                ScriptManager.RegisterStartupScript(this, GetType(), "noSelectionError", 
                    "showNotification('❌ Please select at least one fire extinguisher to send for service.', 'error');", true);
            }
        }
        
        protected void btnCancelSelection_Click(object sender, EventArgs e)
        {
            pnlServiceSelection.Visible = false;
            upServiceSelection.Update();
        }

        /// <summary>
        /// IPostBackEventHandler implementation to handle custom events
        /// </summary>
        public void RaisePostBackEvent(string eventArgument)
        {
            if (eventArgument.StartsWith("LoadFireExtinguisherDetails:"))
            {
                // Extract the FEID from the postback argument
                string feIdStr = eventArgument.Replace("LoadFireExtinguisherDetails:", "");
                int feId;
                
                if (int.TryParse(feIdStr, out feId))
                {
                    LoadFireExtinguisherForEdit(feId);
                    ScriptManager.RegisterStartupScript(this, GetType(), "showEditPanel", "document.getElementById('" + pnlEditFireExtinguisher.ClientID + "').style.display = 'flex';", true);
                }
            }
        }

        /// <summary>
        /// Event handler for Plant dropdown change during edit
        /// </summary>
        protected void ddlPlant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlPlant.SelectedValue))
            {
                // Clear levels dropdown if no plant is selected
                ddlLevel.Items.Clear();
                ddlLevel.Items.Add(new ListItem("-- Select Level --", ""));
                return;
            }

            // Load levels based on selected plant
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "SELECT LevelID, LevelName FROM FETS.Levels WHERE PlantID = @PlantID ORDER BY LevelName", conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", ddlPlant.SelectedValue);
                    ddlLevel.Items.Clear();
                    ddlLevel.Items.Add(new ListItem("-- Select Level --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlLevel.Items.Add(new ListItem(
                                reader["LevelName"].ToString(),
                                reader["LevelID"].ToString()
                            ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load fire extinguisher details for editing
        /// </summary>
        private void LoadFireExtinguisherForEdit(int feId)
        {
            // Load dropdown options
            LoadDropdownsForEdit();

            // Load fire extinguisher details
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        fe.SerialNumber, 
                        fe.AreaCode,
                        fe.PlantID, 
                        fe.LevelID, 
                        fe.Location, 
                        fe.TypeID, 
                        fe.StatusID, 
                        fe.DateExpired, 
                        fe.Remarks,
                        fe.Replacement
                    FROM FETS.FireExtinguishers fe
                    WHERE fe.FEID = @FEID";

                // For non-admin users with assigned plant, verify they can only edit their own plant's extinguishers
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    query += " AND fe.PlantID = @UserPlantID";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FEID", feId);
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                    }

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            hdnEditFEID.Value = feId.ToString();
                            txtSerialNumber.Text = reader["SerialNumber"].ToString();
                            // Check if we have access to the AreaCode field in the UI
                            if (txtAreaCode != null)
                            {
                                txtAreaCode.Text = reader["AreaCode"].ToString();
                            }
                            
                            // Set the plant dropdown
                            string plantId = reader["PlantID"].ToString();
                            ddlPlant.SelectedValue = plantId;
                            
                            // Get levels for selected plant then set level dropdown
                            LoadLevelsForPlant(plantId);
                            ddlLevel.SelectedValue = reader["LevelID"].ToString();
                            
                            txtLocation.Text = reader["Location"].ToString();
                            ddlType.SelectedValue = reader["TypeID"].ToString();
                            ddlStatus.SelectedValue = reader["StatusID"].ToString();
                            
                            // Format date for the date input
                            DateTime expiryDate = Convert.ToDateTime(reader["DateExpired"]);
                            txtExpiryDate.Text = expiryDate.ToString("yyyy-MM-dd");
                            
                            txtRemarks.Text = reader["Remarks"] as string ?? string.Empty;
                            
                            // Set replacement value if it exists
                            string replacement = reader["Replacement"] as string;
                            if (!string.IsNullOrEmpty(replacement))
                            {
                                ddlReplacement.SelectedValue = replacement;
                            }
                            else
                            {
                                ddlReplacement.SelectedIndex = 0; // Select the -- Select -- option
                            }
                        }
                        else if (!IsAdministrator && UserPlantID.HasValue)
                        {
                            // If the user tries to edit a fire extinguisher from another plant
                            ScriptManager.RegisterStartupScript(this, GetType(), "accessError", 
                                "showNotification('❌ You can only edit fire extinguishers from your assigned plant.', 'error'); hideEditPanel();", true);
                            return;
                        }
                    }
                }
            }

            // Update the panel
            upEditFireExtinguisher.Update();
            
            // Show the panel with JavaScript
            ScriptManager.RegisterStartupScript(this, GetType(), "showEditPanel", 
                "document.getElementById('" + pnlEditFireExtinguisher.ClientID + "').style.display = 'flex';" +
                "document.getElementById('modalOverlay').style.display = 'block';", true);
        }

        /// <summary>
        /// Load dropdown lists for the edit form
        /// </summary>
        private void LoadDropdownsForEdit()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Load Plants - with restriction based on user's assigned plant
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    // For users with assigned plant, only load their plant and disable the dropdown
                    using (SqlCommand cmd = new SqlCommand("SELECT PlantID, PlantName FROM FETS.Plants WHERE PlantID = @PlantID", conn))
                    {
                        cmd.Parameters.AddWithValue("@PlantID", UserPlantID.Value);
                        ddlPlant.Items.Clear();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ddlPlant.Items.Add(new ListItem(reader["PlantName"].ToString(), reader["PlantID"].ToString()));
                            }
                        }
                    }
                    ddlPlant.Enabled = false;
                }
                else
                {
                    // For administrators or users without assigned plant, load all plants
                    using (SqlCommand cmd = new SqlCommand("SELECT PlantID, PlantName FROM FETS.Plants ORDER BY PlantName", conn))
                    {
                        ddlPlant.Items.Clear();
                        ddlPlant.Items.Add(new ListItem("-- Select Plant --", ""));
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ddlPlant.Items.Add(new ListItem(
                                    reader["PlantName"].ToString(),
                                    reader["PlantID"].ToString()
                                ));
                            }
                        }
                    }
                }

                // Load Fire Extinguisher Types
                using (SqlCommand cmd = new SqlCommand("SELECT TypeID, TypeName FROM FETS.FireExtinguisherTypes ORDER BY TypeName", conn))
                {
                    ddlType.Items.Clear();
                    ddlType.Items.Add(new ListItem("-- Select Type --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlType.Items.Add(new ListItem(
                                reader["TypeName"].ToString(),
                                reader["TypeID"].ToString()
                            ));
                        }
                    }
                }

                // Load Status
                using (SqlCommand cmd = new SqlCommand("SELECT StatusID, StatusName FROM FETS.Status ORDER BY StatusName", conn))
                {
                    ddlStatus.Items.Clear();
                    ddlStatus.Items.Add(new ListItem("-- Select Status --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlStatus.Items.Add(new ListItem(
                                reader["StatusName"].ToString(),
                                reader["StatusID"].ToString()
                            ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load levels for a specific plant
        /// </summary>
        private void LoadLevelsForPlant(string plantId)
        {
            if (string.IsNullOrEmpty(plantId))
            {
                ddlLevel.Items.Clear();
                ddlLevel.Items.Add(new ListItem("-- Select Level --", ""));
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT LevelID, LevelName FROM FETS.Levels WHERE PlantID = @PlantID ORDER BY LevelName", conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", plantId);
                    ddlLevel.Items.Clear();
                    ddlLevel.Items.Add(new ListItem("-- Select Level --", ""));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ddlLevel.Items.Add(new ListItem(
                                reader["LevelName"].ToString(),
                                reader["LevelID"].ToString()
                            ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save edited fire extinguisher details
        /// </summary>
        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            try
            {
                int feId;
                if (!int.TryParse(hdnEditFEID.Value, out feId))
                {
                    throw new Exception("Invalid fire extinguisher ID.");
                }

                // First, get the original values to compare for change tracking
                Dictionary<string, string> originalValues = new Dictionary<string, string>();
                Dictionary<string, string> newValues = new Dictionary<string, string>();
                List<string> changedFields = new List<string>();
                string serialNumber = string.Empty;

                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get original values
                    string getQuery = @"
                        SELECT 
                            fe.SerialNumber,
                            fe.AreaCode,
                            fe.PlantID,
                            p.PlantName,
                            fe.LevelID,
                            l.LevelName,
                            fe.Location,
                            fe.TypeID,
                            t.TypeName,
                            fe.StatusID,
                            s.StatusName,
                            fe.DateExpired,
                            fe.Remarks,
                            fe.Replacement
                        FROM FETS.FireExtinguishers fe
                        LEFT JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                        LEFT JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                        LEFT JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                        LEFT JOIN FETS.Status s ON fe.StatusID = s.StatusID
                        WHERE fe.FEID = @FEID";

                    using (SqlCommand cmd = new SqlCommand(getQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FEID", feId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Store original values for comparison later
                                originalValues["SerialNumber"] = reader["SerialNumber"].ToString();
                                originalValues["AreaCode"] = reader["AreaCode"].ToString();
                                originalValues["PlantID"] = reader["PlantID"].ToString();
                                originalValues["PlantName"] = reader["PlantName"].ToString();
                                originalValues["LevelID"] = reader["LevelID"].ToString();
                                originalValues["LevelName"] = reader["LevelName"].ToString();
                                originalValues["Location"] = reader["Location"].ToString();
                                originalValues["TypeID"] = reader["TypeID"].ToString();
                                originalValues["TypeName"] = reader["TypeName"].ToString();
                                originalValues["StatusID"] = reader["StatusID"].ToString();
                                originalValues["StatusName"] = reader["StatusName"].ToString();
                                originalValues["DateExpired"] = Convert.ToDateTime(reader["DateExpired"]).ToString("yyyy-MM-dd");
                                originalValues["Remarks"] = reader["Remarks"] as string ?? string.Empty;
                                originalValues["Replacement"] = reader["Replacement"] as string ?? string.Empty;

                                serialNumber = originalValues["SerialNumber"];
                            }
                            else
                            {
                                throw new Exception("Fire extinguisher not found.");
                            }
                        }
                    }

                    // Now get the new values from form controls
                    newValues["SerialNumber"] = txtSerialNumber.Text.Trim();
                    newValues["AreaCode"] = txtAreaCode != null ? txtAreaCode.Text.Trim() : string.Empty;
                    newValues["PlantID"] = ddlPlant.SelectedValue;
                    newValues["PlantName"] = ddlPlant.SelectedItem.Text;
                    newValues["LevelID"] = ddlLevel.SelectedValue;
                    newValues["LevelName"] = ddlLevel.SelectedItem.Text;
                    newValues["Location"] = txtLocation.Text.Trim();
                    newValues["TypeID"] = ddlType.SelectedValue;
                    newValues["TypeName"] = ddlType.SelectedItem.Text;
                    newValues["StatusID"] = ddlStatus.SelectedValue;
                    newValues["StatusName"] = ddlStatus.SelectedItem.Text;
                    
                    DateTime expiryDate;
                    if (DateTime.TryParse(txtExpiryDate.Text, out expiryDate))
                    {
                        newValues["DateExpired"] = expiryDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        throw new Exception("Invalid expiry date format.");
                    }
                    
                    newValues["Remarks"] = txtRemarks.Text.Trim();
                    newValues["Replacement"] = string.IsNullOrEmpty(ddlReplacement.SelectedValue) ? string.Empty : ddlReplacement.SelectedValue;

                    // Compare old and new values to build change log
                    if (originalValues["SerialNumber"] != newValues["SerialNumber"])
                        changedFields.Add($"Serial Number: {originalValues["SerialNumber"]} → {newValues["SerialNumber"]}");
                    
                    if (originalValues["AreaCode"] != newValues["AreaCode"])
                        changedFields.Add($"Area Code: {originalValues["AreaCode"]} → {newValues["AreaCode"]}");
                    
                    if (originalValues["PlantID"] != newValues["PlantID"])
                        changedFields.Add($"Plant: {originalValues["PlantName"]} → {newValues["PlantName"]}");
                    
                    if (originalValues["LevelID"] != newValues["LevelID"])
                        changedFields.Add($"Level: {originalValues["LevelName"]} → {newValues["LevelName"]}");
                    
                    if (originalValues["Location"] != newValues["Location"])
                        changedFields.Add($"Location: {originalValues["Location"]} → {newValues["Location"]}");
                    
                    if (originalValues["TypeID"] != newValues["TypeID"])
                        changedFields.Add($"Type: {originalValues["TypeName"]} → {newValues["TypeName"]}");
                    
                    if (originalValues["StatusID"] != newValues["StatusID"])
                        changedFields.Add($"Status: {originalValues["StatusName"]} → {newValues["StatusName"]}");
                    
                    if (originalValues["DateExpired"] != newValues["DateExpired"])
                        changedFields.Add($"Expiry Date: {originalValues["DateExpired"]} → {newValues["DateExpired"]}");
                    
                    if (originalValues["Remarks"] != newValues["Remarks"])
                        changedFields.Add($"Remarks: {originalValues["Remarks"]} → {newValues["Remarks"]}");
                    
                    if (originalValues["Replacement"] != newValues["Replacement"])
                        changedFields.Add($"Replacement: {originalValues["Replacement"]} → {newValues["Replacement"]}");

                    // Now update the database with new values
                    string updateQuery = @"
                        UPDATE FETS.FireExtinguishers 
                        SET 
                            SerialNumber = @SerialNumber,
                            AreaCode = @AreaCode,
                            PlantID = @PlantID,
                            LevelID = @LevelID,
                            Location = @Location,
                            TypeID = @TypeID,
                            StatusID = @StatusID,
                            DateExpired = @DateExpired,
                            Remarks = @Remarks,
                            Replacement = @Replacement
                        WHERE FEID = @FEID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FEID", feId);
                        cmd.Parameters.AddWithValue("@SerialNumber", newValues["SerialNumber"]);
                        cmd.Parameters.AddWithValue("@AreaCode", newValues["AreaCode"]);
                        cmd.Parameters.AddWithValue("@PlantID", newValues["PlantID"]);
                        cmd.Parameters.AddWithValue("@LevelID", newValues["LevelID"]);
                        cmd.Parameters.AddWithValue("@Location", newValues["Location"]);
                        cmd.Parameters.AddWithValue("@TypeID", newValues["TypeID"]);
                        cmd.Parameters.AddWithValue("@StatusID", newValues["StatusID"]);
                        cmd.Parameters.AddWithValue("@DateExpired", expiryDate);
                        
                        if (string.IsNullOrEmpty(newValues["Remarks"]))
                        {
                            cmd.Parameters.AddWithValue("@Remarks", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Remarks", newValues["Remarks"]);
                        }
                        
                        if (string.IsNullOrEmpty(newValues["Replacement"]))
                        {
                            cmd.Parameters.AddWithValue("@Replacement", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Replacement", newValues["Replacement"]);
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            throw new Exception("Fire extinguisher not found or could not be updated.");
                        }
                    }
                }

                // Log the changes to activity log if any changes were made
                if (changedFields.Count > 0)
                {
                    string changes = string.Join("; ", changedFields);
                    string description = $"Updated fire extinguisher with serial number {serialNumber}. Changes: {changes}";
                    
                    // Log the activity
                    ActivityLogger.LogActivity("Update", description, "FireExtinguisher", feId.ToString());
                }

                // Set success message in session
                Session["NotificationMessage"] = $"✅ Fire extinguisher updated successfully!";
                
                // Redirect to the same page to refresh all data
                Response.Redirect(Request.Url.PathAndQuery, false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                // Show error notification
                ScriptManager.RegisterStartupScript(this, GetType(), "saveError", 
                    $"showNotification('❌ Error: {ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        /// <summary>
        /// Handles the click event for the Complete Service button.
        /// Loads and displays the panel showing all fire extinguishers under service.
        /// </summary>
        protected void btnCompleteServiceList_Click(object sender, EventArgs e)
        {
            LoadCompleteServiceGrid();
            pnlCompleteService.Visible = true;
            upCompleteService.Update();
            
            // Show the modal overlay
            ScriptManager.RegisterStartupScript(this, GetType(), "showCompleteServicePanel", 
                "showCompleteServicePanel();", true);
        }
        
        /// <summary>
        /// Loads the complete service grid with fire extinguishers that are under service.
        /// </summary>
        private void LoadCompleteServiceGrid()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        fe.FEID,
                        fe.SerialNumber,
                        fe.AreaCode,
                        p.PlantName,
                        l.LevelName,
                        fe.Location,
                        t.TypeName,
                        fe.DateExpired,
                        s.StatusName
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    WHERE s.StatusName = 'Under Service'";
                    
                // Add plant restriction for non-admin users
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    query += " AND fe.PlantID = @UserPlantID";
                }
                        
                query += " ORDER BY fe.SerialNumber";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Add user plant parameter if needed
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                    }
                    
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvCompleteService.DataSource = dt;
                        gvCompleteService.DataBind();
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles row data binding for the Complete Service grid.
        /// </summary>
        protected void gvCompleteService_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Set default value for new expiry date (1 year from today)
                TextBox txtNewExpiryDate = (TextBox)e.Row.FindControl("txtNewExpiryDate");
                if (txtNewExpiryDate != null)
                {
                    txtNewExpiryDate.Text = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); // Keep this as yyyy-MM-dd for HTML5 date input
                    // Note: HTML5 date inputs require ISO format yyyy-MM-dd regardless of display format
                }
            }
        }
        
        /// <summary>
        /// Handles the click event for confirming service completion.
        /// Updates the status and expiry dates for all fire extinguishers.
        /// </summary>
        protected void btnConfirmCompleteService_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;
                
            bool anySuccess = false;
            List<string> errors = new List<string>();
            List<FireExtinguisherServiceInfo> completedExtinguishers = new List<FireExtinguisherServiceInfo>();
            List<int> completedFeIds = new List<int>(); // Track completed FEIDs for batch logging
            Dictionary<int, string> completionDescriptions = new Dictionary<int, string>(); // Track descriptions for activity logging
            DateTime serviceDate = DateTime.Now;
            DateTime newExpiryDate = DateTime.Now.AddYears(1); // Default value
            
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // Begin a single transaction for all updates
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (GridViewRow row in gvCompleteService.Rows)
                        {
                            if (row.RowType == DataControlRowType.DataRow)
                            {
                                // Check if this row is selected
                                CheckBox chkSelectForComplete = (CheckBox)row.FindControl("chkSelectForComplete");
                                if (chkSelectForComplete == null || !chkSelectForComplete.Checked)
                                {
                                    // Skip if the checkbox is not checked
                                    continue;
                                }
                                
                                int feId = Convert.ToInt32(gvCompleteService.DataKeys[row.RowIndex].Value);
                                completedFeIds.Add(feId); // Add to list for batch logging
                                TextBox txtNewExpiryDate = (TextBox)row.FindControl("txtNewExpiryDate");
                                
                                if (txtNewExpiryDate != null && !string.IsNullOrEmpty(txtNewExpiryDate.Text))
                                {
                                    if (DateTime.TryParse(txtNewExpiryDate.Text, out newExpiryDate))
                                    {
                                        // Get the fire extinguisher details
                                        string query = @"
                                            SELECT 
                                                fe.SerialNumber,
                                                fe.AreaCode,
                                                p.PlantName AS Plant,
                                                l.LevelName AS Level,
                                                fe.Location,
                                                t.TypeName AS Type,
                                                fe.Remarks
                                            FROM FETS.FireExtinguishers fe
                                            INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                                            INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                                            INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                                            WHERE fe.FEID = @FEID";
                                            
                                        using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@FEID", feId);
                                            using (SqlDataReader reader = cmd.ExecuteReader())
                                            {
                                                if (reader.Read())
                                                {
                                                    string serialNumber = reader["SerialNumber"].ToString();
                                                    string plant = reader["Plant"].ToString();
                                                    string level = reader["Level"].ToString();
                                                    string location = reader["Location"].ToString();
                                                    string type = reader["Type"].ToString();
                                                    string remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : null;
                                                    
                                                    // Add to the list of completed extinguishers
                                                    completedExtinguishers.Add(new FireExtinguisherServiceInfo
                                                    {
                                                        SerialNumber = serialNumber,
                                                        AreaCode = reader["AreaCode"].ToString(),
                                                        Plant = plant,
                                                        Level = level,
                                                        Location = location,
                                                        Type = type,
                                                        Remarks = remarks
                                                    });
                                                    
                                                    // Create description for activity log
                                                    string description = $"Completed service for fire extinguisher - SN: {serialNumber}, Plant: {plant}, Level: {level}, Location: {location}, Type: {type}, New Expiry Date: {newExpiryDate.ToString("yyyy-MM-dd")}";
                                                    if (!string.IsNullOrEmpty(remarks))
                                                    {
                                                        description += $", Remarks: {remarks}";
                                                    }
                                                    completionDescriptions[feId] = description;
                                                }
                                            }
                                        }

                                        // Update the fire extinguisher status, expiry date, and clear replacement
                                        string updateQuery = @"
                                            UPDATE FETS.FireExtinguishers
                                            SET StatusID = (SELECT StatusID FROM FETS.Status WHERE StatusName = 'Active'),
                                                DateExpired = @NewExpiryDate,
                                                Replacement = NULL
                                            WHERE FEID = @FEID";
                                            
                                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@FEID", feId);
                                            cmd.Parameters.AddWithValue("@NewExpiryDate", newExpiryDate);
                                            cmd.ExecuteNonQuery();
                                        }

                                        // Add service reminder for follow-up
                                        DateTime reminderDate = serviceDate.AddDays(7); // Remind in one week
                                        using (SqlCommand cmd = new SqlCommand(
                                            "INSERT INTO FETS.ServiceReminders (FEID, DateCompleteService, ReminderDate) VALUES (@FEID, @DateCompleteService, @ReminderDate)", 
                                            conn, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@FEID", feId);
                                            cmd.Parameters.AddWithValue("@DateCompleteService", serviceDate);
                                            cmd.Parameters.AddWithValue("@ReminderDate", reminderDate);
                                            cmd.ExecuteNonQuery();
                                        }
                                        
                                        anySuccess = true;
                                    }
                                    else
                                    {
                                        errors.Add($"Invalid expiry date format for extinguisher ID {feId}");
                                    }
                                }
                                else
                                {
                                    errors.Add($"No expiry date provided for extinguisher ID {feId}");
                                }
                            }
                        }
                        
                        // Log activities for each completed service
                        foreach (int feId in completedFeIds)
                        {
                            if (completionDescriptions.ContainsKey(feId))
                            {
                                FETS.Models.ActivityLogger.LogActivity(
                                    action: "CompleteService", 
                                    description: completionDescriptions[feId], 
                                    entityType: "FireExtinguisher", 
                                    entityId: feId.ToString());
                            }
                        }
                        
                        // Log batch operation if multiple extinguishers were processed
                        if (completedFeIds.Count > 1)
                        {
                            string batchDescription = $"Completed service for {completedFeIds.Count} fire extinguishers in batch";
                            FETS.Models.ActivityLogger.LogActivity(
                                action: "BatchCompleteService", 
                                description: batchDescription, 
                                entityType: "FireExtinguisher", 
                                entityId: string.Join(",", completedFeIds));
                        }
                        
                        // If we have any completed extinguishers, send the email
                        if (completedExtinguishers.Count > 0)
                        {
                            string subject = completedExtinguishers.Count == 1 
                                ? $"Fire Extinguisher Service Completed - {completedExtinguishers[0].SerialNumber}"
                                : $"{completedExtinguishers.Count} Fire Extinguishers Service Completed";
                            
                            string emailBody;
                            if (completedExtinguishers.Count == 1)
                            {
                                // Single extinguisher email
                                var fe = completedExtinguishers[0];
                                emailBody = EmailTemplateManager.GetServiceCompletionEmailTemplate(
                                    fe.SerialNumber,
                                    fe.Plant,
                                    fe.Level,
                                    fe.Location,
                                    fe.Type,
                                    serviceDate,
                                    newExpiryDate,
                                    fe.Remarks
                                );
                            }
                            else
                            {
                                // Multiple extinguishers email
                                emailBody = EmailTemplateManager.GetMultipleServiceCompletionEmailTemplate(
                                    completedExtinguishers,
                                    serviceDate,
                                    newExpiryDate
                                );
                            }
                            
                            try
                            {
                                // Send the email
                                var (success, emailMessage) = EmailService.SendEmail("", subject, emailBody, "Service");
                                if (!success)
                                {
                                    // Log the email failure but continue with the transaction
                                    System.Diagnostics.Debug.WriteLine($"Failed to send completion email: {emailMessage}");
                                    errors.Add($"Email could not be sent: {emailMessage}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log the error but continue with the transaction
                                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
                                errors.Add($"Email error: {ex.Message}");
                            }
                        }
                        
                        // Commit all changes if successful
                        transaction.Commit();
                        
                    }
                    catch (Exception ex)
                    {
                        // Roll back any changes if there was an error
                        transaction.Rollback();
                        errors.Add($"Transaction error: {ex.Message}");
                    }
                }
            }
            
            // Hide the panel and refresh the data
            pnlCompleteService.Visible = false;
            ScriptManager.RegisterStartupScript(this, GetType(), "hideCompleteServicePanel", 
                "hideCompleteServicePanel();", true);
            
            // Refresh the data
            LoadMonitoringPanels();
            LoadFireExtinguishers();
            upMonitoring.Update();
            upMainGrid.Update();
            
            // Show appropriate message
            if (anySuccess)
            {
                string message = $"Service completed successfully for {completedExtinguishers.Count} fire extinguisher(s).";
                if (errors.Count > 0)
                {
                    message += " However, some errors occurred: " + string.Join("; ", errors);
                }
                
                ScriptManager.RegisterStartupScript(this, GetType(), "successMessage", 
                    $"showNotification('{message.Replace("'", "\\'")}', '{(errors.Count > 0 ? "warning" : "success")}');", true);
            }
            else if (errors.Count > 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "errorMessage", 
                    $"showNotification('❌ Errors occurred: {string.Join("; ", errors).Replace("'", "\\'")}', 'error');", true);
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "noSelectionMessage", 
                    $"showNotification('Please select at least one fire extinguisher to complete service for.', 'error');", true);
            }
        }
        
        /// <summary>
        /// Handles the click event for canceling service completion.
        /// </summary>
        protected void btnCancelCompleteService_Click(object sender, EventArgs e)
        {
            pnlCompleteService.Visible = false;
            upCompleteService.Update();
            
            ScriptManager.RegisterStartupScript(this, GetType(), "hideCompleteServicePanel", 
                "hideCompleteServicePanel();", true);
        }

        /// <summary>
        /// Exports the current filtered fire extinguisher list to a CSV file
        /// </summary>
        protected void btnExportToExcel_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the data that needs to be exported
                DataTable dt = GetFireExtinguishersForExport();
                
                if (dt != null && dt.Rows.Count > 0)
                {
                    // Set the content type and attachment header for CSV
                    Response.Clear();
                    Response.Buffer = true;
                    Response.ContentType = "text/csv";
                    Response.AddHeader("content-disposition", "attachment;filename=FireExtinguishers_" + DateTime.Now.ToString("ddMMyy") + ".csv");
                    Response.Charset = "";
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    
                    using (StringWriter sw = new StringWriter())
                    {
                        // Create header row
                        bool firstColumn = true;
                        foreach (DataColumn column in dt.Columns)
                        {
                            // Skip internal ID columns
                            if (column.ColumnName == "FEID" || column.ColumnName == "StatusID" || 
                                column.ColumnName == "PlantID" || column.ColumnName == "LevelID" || 
                                column.ColumnName == "TypeID")
                                continue;
                                
                            if (!firstColumn)
                                sw.Write(",");
                            
                            // Wrap column names in quotes to handle commas in names
                            sw.Write("\"" + column.ColumnName.Replace("\"", "\"\"") + "\"");
                            firstColumn = false;
                        }
                        sw.WriteLine();
                        
                        // Add data rows
                        foreach (DataRow row in dt.Rows)
                        {
                            firstColumn = true;
                            foreach (DataColumn column in dt.Columns)
                            {
                                // Skip internal ID columns
                                if (column.ColumnName == "FEID" || column.ColumnName == "StatusID" || 
                                    column.ColumnName == "PlantID" || column.ColumnName == "LevelID" || 
                                    column.ColumnName == "TypeID")
                                    continue;
                                
                                if (!firstColumn)
                                    sw.Write(",");
                                
                                // Format dates nicely
                                if (column.ColumnName == "DateExpired" && row[column] != DBNull.Value)
                                {
                                    DateTime dateValue = Convert.ToDateTime(row[column]);
                                    sw.Write("\"" + dateValue.ToString("dd/MM/yy") + "\"");
                                }
                                else
                                {
                                    // Properly escape the value for CSV format
                                    string value = row[column].ToString();
                                    // If the value contains commas, quotes or newlines, wrap it in quotes and escape any quotes
                                    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                                    {
                                        sw.Write("\"" + value.Replace("\"", "\"\"") + "\"");
                                    }
                                    else
                                    {
                                        sw.Write(value);
                                    }
                                }
                                
                                firstColumn = false;
                            }
                            sw.WriteLine();
                        }
                        
                        // Output the CSV to the response
                        Response.Write(sw.ToString());
                        Response.End();
                    }
                }
                else
                {
                    // Show an alert since we're doing a full postback
                    string script = "alert('No data available to export.');";
                    ClientScript.RegisterStartupScript(this.GetType(), "noDataAlert", script, true);
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                string script = $"alert('Error exporting data: {ex.Message.Replace("'", "\\'")}');";
                ClientScript.RegisterStartupScript(this.GetType(), "exportError", script, true);
            }
        }
        
        /// <summary>
        /// Helper method to count visible columns (excluding ID columns)
        /// </summary>
        private int CountVisibleColumns(DataTable dt)
        {
            int count = 0;
            foreach (DataColumn column in dt.Columns)
            {
                if (column.ColumnName != "FEID" && column.ColumnName != "PlantID" && column.ColumnName != "LevelID" && 
                    column.ColumnName != "TypeID" && column.ColumnName != "StatusID" && column.ColumnName != "IsVisible")
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Gets the fire extinguishers data for export based on current filters
        /// </summary>
        private DataTable GetFireExtinguishersForExport()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            DataTable dt = new DataTable();
            
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // Create query similar to LoadFireExtinguishers but without paging
                string query = @"
                    SELECT fe.FEID, fe.SerialNumber, fe.Location, fe.DateExpired, fe.Remarks,
                           p.PlantID, p.PlantName, l.LevelID, l.LevelName, 
                           s.StatusID, s.StatusName, t.TypeID, t.TypeName
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.Plants p ON fe.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON fe.LevelID = l.LevelID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    WHERE 1=1";
                
                // Apply the same filters used in the UI
                List<SqlParameter> parameters = new List<SqlParameter>();
                
                // Plant filter
                if (!string.IsNullOrEmpty(ddlFilterPlant.SelectedValue))
                {
                    query += " AND fe.PlantID = @PlantID";
                    parameters.Add(new SqlParameter("@PlantID", ddlFilterPlant.SelectedValue));
                }
                
                // Level filter
                if (!string.IsNullOrEmpty(ddlFilterLevel.SelectedValue))
                {
                    query += " AND fe.LevelID = @LevelID";
                    parameters.Add(new SqlParameter("@LevelID", ddlFilterLevel.SelectedValue));
                }
                
                // Status filter
                if (!string.IsNullOrEmpty(ddlFilterStatus.SelectedValue))
                {
                    query += " AND fe.StatusID = @StatusID";
                    parameters.Add(new SqlParameter("@StatusID", ddlFilterStatus.SelectedValue));
                }
                
                // Search filter
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    query += " AND (fe.SerialNumber LIKE @Search OR fe.Location LIKE @Search)";
                    parameters.Add(new SqlParameter("@Search", "%" + txtSearch.Text + "%"));
                }
                
                // Apply the current sort
                query += " ORDER BY " + SortExpression + " " + SortDirection;
                
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Add all parameters
                    cmd.Parameters.AddRange(parameters.ToArray());
                    
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            
            return dt;
        }
        
        /// <summary>
        /// Loads maps for the user's assigned plant to display below the fire extinguisher list
        /// </summary>
        private void LoadPlantMaps()
        {
            if (!UserPlantID.HasValue)
                return;
                
            string connectionString = GetConnectionString();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT TOP 100 m.MapID, m.PlantID, m.LevelID, m.ImagePath, m.UploadDate,
                           p.PlantName, l.LevelName
                    FROM FETS.MapImages m WITH (NOLOCK)
                    INNER JOIN FETS.Plants p WITH (NOLOCK) ON m.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l WITH (NOLOCK) ON m.LevelID = l.LevelID AND m.PlantID = l.PlantID
                    WHERE m.PlantID = @PlantID
                    ORDER BY l.LevelName ASC, m.UploadDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", UserPlantID.Value);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        
                        // Log the number of maps found
                        System.Diagnostics.Debug.WriteLine($"Found {dt.Rows.Count} maps for plant ID {UserPlantID.Value}");
                        
                        pnlMapLayout.Visible = true; // Always show the map section for regular users
                        
                        if (dt.Rows.Count > 0)
                        {
                            // Log the first map's details
                            var firstMap = dt.Rows[0];
                            System.Diagnostics.Debug.WriteLine($"First map: Level={firstMap["LevelName"]}, ImagePath={firstMap["ImagePath"]}");
                            
                            rptMaps.DataSource = dt;
                            rptMaps.DataBind();
                            rptMaps.Visible = true;
                            
                            // Hide the "no maps" message
                            if (pnlNoMaps != null)
                                pnlNoMaps.Visible = false;
                            
                            // Register client script to initialize the map carousel
                            string script = @"
                            var currentMapIndex = 0;
                            var mapItems = [];
                            
                            function initMapCarousel() {
                                mapItems = document.querySelectorAll('.map-data-item');
                                if (mapItems.length > 0) {
                                    loadMapByIndex(0);
                                    var plantNameField = document.getElementById('plantNameHidden');
                                    if (plantNameField) {
                                        plantNameField.value = mapItems[0].getAttribute('data-plant');
                                    }
                                    updateNavButtons();
                                } else {
                                    var prevBtn = document.getElementById('btnPrevMap');
                                    var nextBtn = document.getElementById('btnNextMap');
                                    if (prevBtn) prevBtn.style.display = 'none';
                                    if (nextBtn) nextBtn.style.display = 'none';
                                }
                            }
                            
                            function loadMapByIndex(index) {
                                if (index >= 0 && index < mapItems.length) {
                                    var mapItem = mapItems[index];
                                    var imageUrl = mapItem.getAttribute('data-image-url');
                                    var levelName = mapItem.getAttribute('data-level');
                                    var updateDate = mapItem.getAttribute('data-update-date');
                                    
                                    var imgElement = document.getElementById('currentMapImage');
                                    var titleElement = document.getElementById('mapLevelTitle');
                                    var dateElement = document.getElementById('mapLastUpdated');
                                    
                                    if (imgElement) imgElement.src = imageUrl;
                                    if (titleElement) titleElement.innerText = levelName;
                                    if (dateElement) dateElement.innerText = 'Last Updated: ' + updateDate;
                                    
                                    currentMapIndex = index;
                                    updateNavButtons();
                                }
                            }
                            
                            function updateNavButtons() {
                                var prevBtn = document.getElementById('btnPrevMap');
                                var nextBtn = document.getElementById('btnNextMap');
                                
                                if (prevBtn) {
                                    prevBtn.disabled = (currentMapIndex === 0);
                                    prevBtn.style.opacity = (currentMapIndex === 0) ? '0.5' : '1';
                                }
                                
                                if (nextBtn) {
                                    nextBtn.disabled = (currentMapIndex === mapItems.length - 1);
                                    nextBtn.style.opacity = (currentMapIndex === mapItems.length - 1) ? '0.5' : '1';
                                }
                            }
                            
                            function prevMap() {
                                if (currentMapIndex > 0) {
                                    loadMapByIndex(currentMapIndex - 1);
                                }
                            }
                            
                            function nextMap() {
                                if (currentMapIndex < mapItems.length - 1) {
                                    loadMapByIndex(currentMapIndex + 1);
                                }
                            }
                            
                            // Initialize on page load
                            window.addEventListener('load', function() {
                                setTimeout(initMapCarousel, 500);
                            });";
                            
                            // Register the script with the page
                            ScriptManager.RegisterStartupScript(this, GetType(), "MapCarouselScript", script, true);
                            
                            // Load the first map data to initialize the display
                            if (dt.Rows.Count > 0)
                            {
                                string imageUrl = ResolveUrl($"~/Uploads/Maps/{firstMap["ImagePath"]}");
                                string levelName = firstMap["LevelName"].ToString();
                                string updateDate = Convert.ToDateTime(firstMap["UploadDate"]).ToString("MM/dd/yyyy");
                                
                                // Set initial values using JavaScript
                                string initScript = $@"
                                window.addEventListener('load', function() {{
                                    var imgElement = document.getElementById('currentMapImage');
                                    var titleElement = document.getElementById('mapLevelTitle');
                                    var dateElement = document.getElementById('mapLastUpdated');
                                    var plantNameField = document.getElementById('plantNameHidden');
                                    
                                    if (imgElement) imgElement.src = '{imageUrl}';
                                    if (titleElement) titleElement.innerText = '{levelName}';
                                    if (dateElement) dateElement.innerText = 'Last Updated: {updateDate}';
                                    if (plantNameField) plantNameField.value = '{firstMap["PlantName"]}';
                                }});";
                                
                                ScriptManager.RegisterStartupScript(this, GetType(), "InitMapDisplay", initScript, true);
                            }
                        }
                        else
                        {
                            // No maps found - show the "no maps" message
                            rptMaps.Visible = false;
                            if (pnlNoMaps != null)
                                pnlNoMaps.Visible = true;
                            
                            // Hide navigation buttons
                            string hideNavScript = @"
                            window.addEventListener('load', function() {
                                var prevBtn = document.getElementById('btnPrevMap');
                                var nextBtn = document.getElementById('btnNextMap');
                                if (prevBtn) prevBtn.style.display = 'none';
                                if (nextBtn) nextBtn.style.display = 'none';
                            });";
                            
                            ScriptManager.RegisterStartupScript(this, GetType(), "HideNavButtons", hideNavScript, true);
                        }
                    }
                }
            }
        }
        
        
        /// <summary>
        /// Gets the URL for a map image based on its file path
        /// </summary>
        protected string GetMapImageUrl(string imagePath)
        {
            return $"~/Uploads/Maps/{imagePath}";
        }
        
        /// <summary>
        /// Handles clicking on the View Map button in the map repeater
        /// </summary>
        protected void rptMaps_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "ViewMap")
            {
                string[] arguments = e.CommandArgument.ToString().Split('|');
                if (arguments.Length == 3)
                {
                    string mapUrl = arguments[0];
                    string plantName = arguments[1];
                    string levelName = arguments[2];
                    
                    // Use JavaScript to open the map modal
                    string script = $"openMapModal('{mapUrl}', '{plantName}', '{levelName}');";
                    ScriptManager.RegisterStartupScript(this, GetType(), "openMapModal", script, true);
                }
            }
        }
    }
}

