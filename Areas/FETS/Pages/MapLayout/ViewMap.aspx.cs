using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Security;
using System.Web.UI.WebControls;

namespace FETS.Pages.MapLayout
{
    public partial class ViewMap : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Areas/FETS/Pages/Login/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                lblUsername.Text = User.Identity.Name;
                LoadMapAndInfo();
            }
        }

        private void LoadMapAndInfo()
        {
            string plantId = Request.QueryString["PlantID"];
            string levelId = Request.QueryString["LevelID"];

            if (string.IsNullOrEmpty(plantId) || string.IsNullOrEmpty(levelId))
            {
                Response.Redirect("~/Areas/FETS/Pages/MapLayout/MapLayout.aspx");
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Load plant and level names
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT p.PlantName, l.LevelName, m.ImagePath, m.UploadDate
                    FROM FETS.Plants p
                    INNER JOIN FETS.Levels l ON p.PlantID = l.PlantID
                    LEFT JOIN FETS.MapImages m ON l.PlantID = m.PlantID AND l.LevelID = m.LevelID
                    WHERE p.PlantID = @PlantID AND l.LevelID = @LevelID", conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", plantId);
                    cmd.Parameters.AddWithValue("@LevelID", levelId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lblPlantName.Text = reader["PlantName"].ToString();
                            lblLevelName.Text = reader["LevelName"].ToString();
                            if (!reader.IsDBNull(reader.GetOrdinal("ImagePath")))
                            {
                                string imagePath = reader["ImagePath"].ToString();
                                imgMap.ImageUrl = $"~/Uploads/Maps/{imagePath}";
                                lblLastUpdated.Text = Convert.ToDateTime(reader["UploadDate"]).ToString("MMM dd, yyyy HH:mm");
                            }
                            else
                            {
                                imgMap.ImageUrl = "~/Assets/images/no-map.png";
                                lblLastUpdated.Text = "No map uploaded";
                            }
                        }
                    }
                }

                // Load fire extinguisher count and details
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        fe.SerialNumber,
                        fe.Location,
                        t.TypeName,
                        fe.DateExpired,
                        s.StatusName,
                        s.ColorCode
                    FROM FETS.FireExtinguishers fe
                    INNER JOIN FETS.FireExtinguisherTypes t ON fe.TypeID = t.TypeID
                    INNER JOIN FETS.Status s ON fe.StatusID = s.StatusID
                    WHERE fe.PlantID = @PlantID AND fe.LevelID = @LevelID
                    ORDER BY fe.Location", conn))
                {
                    cmd.Parameters.AddWithValue("@PlantID", plantId);
                    cmd.Parameters.AddWithValue("@LevelID", levelId);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        lblFECount.Text = dt.Rows.Count.ToString();
                        gvFireExtinguishers.DataSource = dt;
                        gvFireExtinguishers.DataBind();
                    }
                }
            }
        }

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
            }
        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
                            Response.Redirect("~/Areas/FETS/Pages/MapLayout/MapLayout.aspx");
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Response.Redirect("~/Areas/FETS/Pages/Login/Login.aspx");
        }
    }
} 