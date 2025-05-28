using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using FETS.Models;
using System.Configuration;

namespace FETS.Pages.Admin
{
    public partial class ActivityLogs : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Check if user is authenticated and has admin rights
            if (!Request.IsAuthenticated || !RoleHelper.IsUserInRole("Administrator"))
            {
                Response.Redirect("~/Areas/FETS/Pages/Dashboard/Dashboard.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // Load filter dropdowns
                LoadUsernames();
                LoadActions();
                LoadEntityTypes();

                // Load activity logs with default filters
                LoadActivityLogs();
            }
        }

        /// <summary>
        /// Loads the list of usernames for the filter dropdown
        /// </summary>
        private void LoadUsernames()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT DISTINCT u.Username, al.UserID 
                        FROM FETS.ActivityLogs al
                        INNER JOIN FETS.Users u ON al.UserID = u.UserID
                        ORDER BY u.Username";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string username = reader["Username"].ToString();
                                string userId = reader["UserID"].ToString();
                                ddlUsername.Items.Add(new ListItem(username, userId));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue loading the page
                System.Diagnostics.Debug.WriteLine(string.Format("Error loading usernames: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Loads the list of actions for the filter dropdown
        /// </summary>
        private void LoadActions()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT Action FROM FETS.ActivityLogs WHERE Action IS NOT NULL ORDER BY Action";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string action = reader["Action"].ToString();
                                ddlAction.Items.Add(new ListItem(action, action));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error loading actions: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Loads the list of entity types for the filter dropdown
        /// </summary>
        private void LoadEntityTypes()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT EntityType FROM FETS.ActivityLogs WHERE EntityType IS NOT NULL ORDER BY EntityType";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string entityType = reader["EntityType"].ToString();
                                ddlEntityType.Items.Add(new ListItem(entityType, entityType));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error loading entity types: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Loads activity logs based on selected filters
        /// </summary>
        private void LoadActivityLogs()
        {
            try
            {
                string connectionString = WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    StringBuilder queryBuilder = new StringBuilder(@"
                        SELECT al.LogID, al.Action, al.Description, al.EntityType, al.EntityID, 
                               al.IPAddress, al.Timestamp, u.Username
                        FROM FETS.ActivityLogs al
                        LEFT JOIN FETS.Users u ON al.UserID = u.UserID
                        WHERE 1=1");
                    
                    List<SqlParameter> parameters = new List<SqlParameter>();

                    // Apply username filter
                    if (!string.IsNullOrEmpty(ddlUsername.SelectedValue))
                    {
                        queryBuilder.Append(" AND al.UserID = @UserID");
                        parameters.Add(new SqlParameter("@UserID", ddlUsername.SelectedValue));
                    }

                    // Apply action filter
                    if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                    {
                        queryBuilder.Append(" AND al.Action = @Action");
                        parameters.Add(new SqlParameter("@Action", ddlAction.SelectedValue));
                    }

                    // Apply entity type filter
                    if (!string.IsNullOrEmpty(ddlEntityType.SelectedValue))
                    {
                        queryBuilder.Append(" AND al.EntityType = @EntityType");
                        parameters.Add(new SqlParameter("@EntityType", ddlEntityType.SelectedValue));
                    }

                    // Apply date range filter
                    string dateRange = ddlDateRange.SelectedValue;
                    if (dateRange != "all")
                    {
                        DateTime fromDate = DateTime.Now;
                        switch (dateRange)
                        {
                            case "today":
                                fromDate = DateTime.Today;
                                break;
                            case "week":
                                fromDate = DateTime.Today.AddDays(-7);
                                break;
                            case "month":
                                fromDate = DateTime.Today.AddDays(-30);
                                break;
                        }
                        queryBuilder.Append(" AND al.Timestamp >= @FromDate");
                        parameters.Add(new SqlParameter("@FromDate", fromDate));
                    }

                    // First get total count for display
                    string countQuery = @"
                        SELECT COUNT(*)
                        FROM FETS.ActivityLogs al
                        LEFT JOIN FETS.Users u ON al.UserID = u.UserID
                        WHERE 1=1";
                        
                    // Apply the same filters to the count query
                    if (!string.IsNullOrEmpty(ddlUsername.SelectedValue))
                    {
                        countQuery += " AND al.UserID = @UserID";
                    }
                    if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                    {
                        countQuery += " AND al.Action = @Action";
                    }
                    if (!string.IsNullOrEmpty(ddlEntityType.SelectedValue))
                    {
                        countQuery += " AND al.EntityType = @EntityType";
                    }
                    if (dateRange != "all")
                    {
                        countQuery += " AND al.Timestamp >= @FromDate";
                    }
                    
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        // Add parameters to the count query
                        foreach (SqlParameter parameter in parameters)
                        {
                            countCommand.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.Value));
                        }
                        
                        int totalRecords = (int)countCommand.ExecuteScalar();
                        lblTotalRecords.Text = totalRecords.ToString();
                    }

                    // Add order by clause
                    queryBuilder.Append(" ORDER BY al.Timestamp DESC");

                    // Execute the query
                    using (SqlCommand command = new SqlCommand(queryBuilder.ToString(), connection))
                    {
                        foreach (SqlParameter parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);
                            gvActivityLogs.DataSource = dataTable;
                            gvActivityLogs.DataBind();
                            
                            // Debug information
                            System.Diagnostics.Debug.WriteLine($"Query executed: {queryBuilder.ToString()}");
                            System.Diagnostics.Debug.WriteLine($"Rows returned: {dataTable.Rows.Count}");
                            System.Diagnostics.Debug.WriteLine($"Date range selected: {dateRange}");
                            if (dateRange != "all")
                            {
                                System.Diagnostics.Debug.WriteLine($"From date: {parameters.First(p => p.ParameterName == "@FromDate").Value}");
                            }
                            
                            // If no records and this isn't the first page, go back to first page
                            if (dataTable.Rows.Count == 0 && gvActivityLogs.PageIndex > 0)
                            {
                                gvActivityLogs.PageIndex = 0;
                                LoadActivityLogs();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine("Error loading activity logs: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Error stack trace: " + ex.StackTrace);
            }
        }

        /// <summary>
        /// Handles changes to any of the filter controls
        /// </summary>
        protected void ApplyFilters_Changed(object sender, EventArgs e)
        {
            // Reset to first page when filters change
            gvActivityLogs.PageIndex = 0;
            LoadActivityLogs();
        }

        /// <summary>
        /// Handles paging in the GridView
        /// </summary>
        protected void gvActivityLogs_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvActivityLogs.PageIndex = e.NewPageIndex;
            LoadActivityLogs();
        }

        /// <summary>
        /// Exports activity logs to CSV file
        /// </summary>
        protected void btnExportCsv_Click(object sender, EventArgs e)
        {
            try
            {
                // Get the data with filters applied
                string connectionString = WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    StringBuilder queryBuilder = new StringBuilder(@"
                        SELECT al.LogID, al.Action, al.Description, al.EntityType, al.EntityID, 
                               al.IPAddress, al.Timestamp, u.Username
                        FROM FETS.ActivityLogs al
                        LEFT JOIN FETS.Users u ON al.UserID = u.UserID
                        WHERE 1=1");
                        
                    List<SqlParameter> parameters = new List<SqlParameter>();

                    // Apply username filter
                    if (!string.IsNullOrEmpty(ddlUsername.SelectedValue))
                    {
                        queryBuilder.Append(" AND al.UserID = @UserID");
                        parameters.Add(new SqlParameter("@UserID", ddlUsername.SelectedValue));
                    }

                    // Apply action filter
                    if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                    {
                        queryBuilder.Append(" AND al.Action = @Action");
                        parameters.Add(new SqlParameter("@Action", ddlAction.SelectedValue));
                    }

                    // Apply entity type filter
                    if (!string.IsNullOrEmpty(ddlEntityType.SelectedValue))
                    {
                        queryBuilder.Append(" AND al.EntityType = @EntityType");
                        parameters.Add(new SqlParameter("@EntityType", ddlEntityType.SelectedValue));
                    }

                    // Apply date range filter
                    string dateRange = ddlDateRange.SelectedValue;
                    if (dateRange != "all")
                    {
                        DateTime fromDate = DateTime.Now;
                        switch (dateRange)
                        {
                            case "today":
                                fromDate = DateTime.Today;
                                break;
                            case "week":
                                fromDate = DateTime.Today.AddDays(-7);
                                break;
                            case "month":
                                fromDate = DateTime.Today.AddDays(-30);
                                break;
                        }
                        queryBuilder.Append(" AND al.Timestamp >= @FromDate");
                        parameters.Add(new SqlParameter("@FromDate", fromDate));
                    }

                    // Add order by clause
                    queryBuilder.Append(" ORDER BY al.Timestamp DESC");

                    // Execute the query
                    using (SqlCommand command = new SqlCommand(queryBuilder.ToString(), connection))
                    {
                        foreach (SqlParameter parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);

                            // Create CSV
                            StringBuilder csv = new StringBuilder();

                            // Add header
                            List<string> headerRow = new List<string>();
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                headerRow.Add(column.ColumnName);
                            }
                            csv.AppendLine(string.Join(",", headerRow.Select(h => "\"" + h + "\"")));

                            // Add rows
                            foreach (DataRow row in dataTable.Rows)
                            {
                                List<string> rowValues = new List<string>();
                                foreach (DataColumn column in dataTable.Columns)
                                {
                                    // Format timestamp value
                                    if (column.ColumnName == "Timestamp" && row[column] != DBNull.Value)
                                    {
                                        DateTime timestamp = Convert.ToDateTime(row[column]);
                                        rowValues.Add("\"" + timestamp.ToString("yyyy-MM-dd HH:mm:ss") + "\"");
                                    }
                                    else
                                    {
                                        rowValues.Add("\"" + (row[column] == DBNull.Value ? "" : row[column].ToString().Replace("\"", "\"\"")) + "\"");
                                    }
                                }
                                csv.AppendLine(string.Join(",", rowValues));
                            }

                            // Download the CSV file
                            Response.Clear();
                            Response.Buffer = true;
                            Response.AddHeader("content-disposition", "attachment;filename=ActivityLogs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                            Response.Charset = "";
                            Response.ContentType = "application/text";
                            Response.Output.Write(csv.ToString());
                            Response.Flush();
                            Response.End();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine("Error exporting CSV: " + ex.Message);
            }
        }

        // Method to determine badge class based on action
        protected string GetActionBadgeClass(string action)
        {
            switch (action.ToLower())
            {
                case "login":
                case "logout":
                    return "secondary";
                case "add":
                case "create":
                    return "success";
                case "update":
                case "edit":
                    return "primary";
                case "delete":
                case "remove":
                    return "danger";
                case "view":
                case "read":
                    return "info";
                case "service":
                case "maintenance":
                    return "warning";
                default:
                    return "secondary";
            }
        }
    }
} 