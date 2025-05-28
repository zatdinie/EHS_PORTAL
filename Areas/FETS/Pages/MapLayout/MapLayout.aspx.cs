using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Caching;

namespace FETS.Pages.MapLayout
{
    public partial class MapLayout : System.Web.UI.Page
    {
        // Properties to store user's assigned plant and role
        private int? UserPlantID { get; set; }
        private bool IsAdministrator { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                // Redirect to login page
                Response.Redirect("~/Areas/FETS/Pages/Login/Login.aspx");
            }

            // Get user's assigned plant and role
            GetUserPlantAndRole();

            if (!IsPostBack)
            {
                LoadDropDownLists();
                LoadMaps();
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

        private void LoadDropDownLists()
        {
            try
            {
                // Try to get plants from cache first
                DataTable dtPlants = Cache["FETS.Plants"] as DataTable;
                if (dtPlants == null)
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                    
                    // For upload dropdown - restrict to user's plant if not admin
                    string uploadPlantQuery = @"SELECT PlantID, PlantName FROM FETS.Plants WITH (NOLOCK)";
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        uploadPlantQuery += " WHERE PlantID = @UserPlantID";
                    }
                    uploadPlantQuery += " ORDER BY PlantName";
                    
                    DataTable dtUploadPlants = new DataTable();
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(uploadPlantQuery, conn))
                        {
                            if (!IsAdministrator && UserPlantID.HasValue)
                            {
                                cmd.Parameters.AddWithValue("@UserPlantID", UserPlantID.Value);
                            }
                            
                            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                            {
                                adapter.Fill(dtUploadPlants);
                            }
                        }
                        
                        // For filter dropdown - always get all plants
                        using (SqlCommand cmd = new SqlCommand(
                            @"SELECT PlantID, PlantName FROM FETS.Plants WITH (NOLOCK) ORDER BY PlantName", conn))
                        {
                            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                            {
                                dtPlants = new DataTable();
                                adapter.Fill(dtPlants);
                                
                                // Cache all plants for the filter dropdown
                                Cache.Insert("FETS.Plants", dtPlants, null, DateTime.Now.AddHours(1), Cache.NoSlidingExpiration);
                            }
                        }
                    }
                    
                    // Clear dropdowns
                    ddlPlant.Items.Clear();
                    ddlFilterPlant.Items.Clear();
                    
                    // Configure upload plant dropdown (restricted for non-admin)
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        // No "Select Plant" option for non-admin users
                        ddlPlant.Enabled = false;
                    }
                    else
                    {
                        // For admins, add the select option
                        ddlPlant.Items.Add(new ListItem("-- Select Plant --", ""));
                        ddlPlant.Enabled = true;
                    }
                    
                    // Add plants to the upload dropdown
                    foreach (DataRow row in dtUploadPlants.Rows)
                    {
                        ddlPlant.Items.Add(new ListItem(
                            row["PlantName"].ToString(),
                            row["PlantID"].ToString()
                        ));
                    }
                    
                    // Configure filter plant dropdown (always show all plants)
                    ddlFilterPlant.Items.Add(new ListItem("-- All Plants --", ""));
                    ddlFilterPlant.Enabled = true;
                    
                    // Add all plants to the filter dropdown
                    foreach (DataRow row in dtPlants.Rows)
                    {
                        ddlFilterPlant.Items.Add(new ListItem(
                            row["PlantName"].ToString(),
                            row["PlantID"].ToString()
                        ));
                    }
                    
                    // For regular users with assigned plant, auto-select their plant in the upload dropdown
                    if (!IsAdministrator && UserPlantID.HasValue)
                    {
                        // Set the selected value to user's plant for upload dropdown
                        if (ddlPlant.Items.FindByValue(UserPlantID.Value.ToString()) != null)
                        {
                            ddlPlant.SelectedValue = UserPlantID.Value.ToString();
                            
                            // Trigger the plant change event to load levels
                            ddlPlant_SelectedIndexChanged(ddlPlant, EventArgs.Empty);
                        }
                    }
                    
                    return;
                }

                // Using cached plants data
                ddlPlant.Items.Clear();
                ddlFilterPlant.Items.Clear();
                
                // For non-admin users with assigned plant, restrict upload dropdown
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    // Don't add "Select Plant" option for upload dropdown
                    ddlPlant.Enabled = false;
                    
                    // For filter dropdown - allow all plants
                    ddlFilterPlant.Items.Add(new ListItem("-- All Plants --", ""));
                    ddlFilterPlant.Enabled = true;
                    
                    // Add only the assigned plant to the upload dropdown
                    foreach (DataRow row in dtPlants.Rows)
                    {
                        if (Convert.ToInt32(row["PlantID"]) == UserPlantID.Value)
                        {
                            ddlPlant.Items.Add(new ListItem(
                                row["PlantName"].ToString(),
                                row["PlantID"].ToString()
                            ));
                        }
                        
                        // Add all plants to the filter dropdown
                        ddlFilterPlant.Items.Add(new ListItem(
                            row["PlantName"].ToString(),
                            row["PlantID"].ToString()
                        ));
                    }
                }
                else
                {
                    // For admins or users without assigned plant - no restrictions
                    ddlPlant.Items.Add(new ListItem("-- Select Plant --", ""));
                    ddlPlant.Enabled = true;
                    
                    ddlFilterPlant.Items.Add(new ListItem("-- All Plants --", ""));
                    ddlFilterPlant.Enabled = true;
                    
                    // Add all plants to both dropdowns
                    foreach (DataRow row in dtPlants.Rows)
                    {
                        ListItem item = new ListItem(
                            row["PlantName"].ToString(),
                            row["PlantID"].ToString()
                        );
                        ddlPlant.Items.Add(item);
                        ddlFilterPlant.Items.Add(item);
                    }
                }
                
                // For regular users with assigned plant, auto-select their plant in the upload dropdown
                if (!IsAdministrator && UserPlantID.HasValue)
                {
                    // Set the selected value to user's plant for the upload dropdown
                    if (ddlPlant.Items.FindByValue(UserPlantID.Value.ToString()) != null)
                    {
                        ddlPlant.SelectedValue = UserPlantID.Value.ToString();
                        
                        // Trigger the plant change event to load levels
                        ddlPlant_SelectedIndexChanged(ddlPlant, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadDropDownLists: {ex.Message}");
                throw;
            }
        }

        protected void ddlPlant_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Plant selection changed to: {ddlPlant.SelectedValue}");
            LoadLevels(ddlPlant, ddlLevel);
            System.Diagnostics.Debug.WriteLine($"Number of levels loaded: {ddlLevel.Items.Count - 1}"); // -1 for the default item
        }

        protected void ddlFilterPlant_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLevels(ddlFilterPlant, ddlFilterLevel);
            LoadMaps();
        }

        protected void ddlFilterLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadMaps();
        }

        private void LoadLevels(DropDownList plantDropDown, DropDownList levelDropDown)
        {
            try
            {
                levelDropDown.Items.Clear();
                levelDropDown.Items.Add(new ListItem(plantDropDown == ddlFilterPlant ? "-- All Levels --" : "-- Select Level --", ""));

                if (string.IsNullOrEmpty(plantDropDown.SelectedValue))
                    return;

                // Try to get levels from cache first
                string cacheKey = $"Levels_{plantDropDown.SelectedValue}";
                DataTable dtLevels = Cache[cacheKey] as DataTable;

                if (dtLevels == null)
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(@"
                            SELECT LevelID, LevelName 
                            FROM FETS.Levels WITH (NOLOCK)
                            WHERE PlantID = @PlantID 
                            ORDER BY LevelName", conn))
                        {
                            cmd.Parameters.AddWithValue("@PlantID", plantDropDown.SelectedValue);
                            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                            {
                                dtLevels = new DataTable();
                                adapter.Fill(dtLevels);
                                
                                // Cache the results for 1 hour
                                Cache.Insert(cacheKey, dtLevels, null, DateTime.Now.AddHours(1), Cache.NoSlidingExpiration);
                            }
                        }
                    }
                }

                // Populate dropdown from DataTable
                foreach (DataRow row in dtLevels.Rows)
                {
                    levelDropDown.Items.Add(new ListItem(
                        row["LevelName"].ToString(),
                        row["LevelID"].ToString()
                    ));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadLevels: {ex.Message}");
                throw;
            }
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            if (!fuMapImage.HasFile)
            {
                lblMessage.Text = "Please select a file to upload.";
                lblMessage.CssClass = "message error";
                return;
            }

            string fileExtension = Path.GetExtension(fuMapImage.FileName).ToLower();
            if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
            {
                lblMessage.Text = "Only JPG and PNG files are allowed.";
                lblMessage.CssClass = "message error";
                return;
            }

            try
            {
                string fileName = $"{Guid.NewGuid()}{fileExtension}";
                string uploadPath = Server.MapPath("~/Uploads/Maps");
                
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                string filePath = Path.Combine(uploadPath, fileName);
                fuMapImage.SaveAs(filePath);

                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string insertQuery = @"
                        INSERT INTO FETS.MapImages (PlantID, LevelID, ImagePath, UploadDate)
                        VALUES (@PlantID, @LevelID, @ImagePath, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@PlantID", ddlPlant.SelectedValue);
                        cmd.Parameters.AddWithValue("@LevelID", ddlLevel.SelectedValue);
                        cmd.Parameters.AddWithValue("@ImagePath", fileName);

                        // Get the inserted MapID for activity logging
                        int mapId = Convert.ToInt32(cmd.ExecuteScalar());

                        // Get plant and level names for the log description
                        string plantName = ddlPlant.SelectedItem.Text;
                        string levelName = ddlLevel.SelectedItem.Text;
                        
                        // Log the upload activity
                        string description = $"Uploaded new map for Plant: {plantName}, Level: {levelName}";
                        FETS.Models.ActivityLogger.LogActivity(
                            action: "Upload", 
                            description: description, 
                            entityType: "Map", 
                            entityId: mapId.ToString());

                        lblMessage.Text = "Map uploaded successfully.";
                        lblMessage.CssClass = "message success";
                        
                        // Clear form
                        ddlPlant.SelectedIndex = 0;
                        ddlLevel.Items.Clear();
                        ddlLevel.Items.Add(new ListItem("-- Select Level --", ""));
                        
                        // Reload maps grid
                        LoadMaps();
                    }
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error uploading map: " + ex.Message;
                lblMessage.CssClass = "message error";
            }
        }

        public void LoadMaps()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT m.MapID, m.PlantID, m.LevelID, m.ImagePath, m.UploadDate,
                           p.PlantName, l.LevelName
                    FROM FETS.MapImages m
                    INNER JOIN FETS.Plants p ON m.PlantID = p.PlantID
                    INNER JOIN FETS.Levels l ON m.LevelID = l.LevelID
                    WHERE (@PlantID IS NULL OR m.PlantID = @PlantID)
                    AND (@LevelID IS NULL OR m.LevelID = @LevelID)
                    ORDER BY m.UploadDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", ddlFilterPlant.SelectedValue == "" ? DBNull.Value : (object)int.Parse(ddlFilterPlant.SelectedValue));
                    cmd.Parameters.AddWithValue("@LevelID", ddlFilterLevel.SelectedValue == "" ? DBNull.Value : (object)int.Parse(ddlFilterLevel.SelectedValue));

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        gvMaps.DataSource = dt;
                        gvMaps.DataBind();
                    }
                }
            }
        }

        protected string GetMapImageUrl(string imagePath)
        {
            return $"~/Uploads/Maps/{imagePath}";
        }

        protected void gvMaps_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvMaps.PageIndex = e.NewPageIndex;
            LoadMaps();
        }

        protected void gvMaps_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewMap")
            {
                // Note: We're now handling map viewing through client-side JavaScript
                // No server-side action needed here as we're showing the map in a modal
                // The map URL, plant name, and level name are passed directly to the JavaScript openMapModal function
            }
            else if (e.CommandName == "DeleteMap")
            {
                DeleteMap(Convert.ToInt32(e.CommandArgument));
            }
        }

        private void DeleteMap(int mapId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Get the image path and plant details first
                string imagePath = string.Empty;
                string plantName = string.Empty;
                string levelName = string.Empty;
                
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT m.ImagePath, p.PlantName, l.LevelName 
                    FROM FETS.MapImages m
                    JOIN FETS.Plants p ON m.PlantID = p.PlantID
                    JOIN FETS.Levels l ON m.LevelID = l.LevelID
                    WHERE m.MapID = @MapID", conn))
                {
                    cmd.Parameters.AddWithValue("@MapID", mapId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            imagePath = reader["ImagePath"].ToString();
                            plantName = reader["PlantName"].ToString();
                            levelName = reader["LevelName"].ToString();
                        }
                    }
                }

                // Delete the database record
                using (SqlCommand cmd = new SqlCommand("DELETE FROM FETS.MapImages WHERE MapID = @MapID", conn))
                {
                    cmd.Parameters.AddWithValue("@MapID", mapId);
                    cmd.ExecuteNonQuery();
                }

                // Delete the physical file
                if (!string.IsNullOrEmpty(imagePath))
                {
                    string filePath = Server.MapPath($"~/Uploads/Maps/{imagePath}");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                
                // Log the map deletion activity
                string description = $"Deleted map for Plant: {plantName}, Level: {levelName}";
                FETS.Models.ActivityLogger.LogActivity(
                    action: "Delete", 
                    description: description, 
                    entityType: "Map", 
                    entityId: mapId.ToString());

                LoadMaps();
            }
        }
    }
} 