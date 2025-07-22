using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FETS.Pages.PublicDashboard
{
    public partial class PublicDashboard : System.Web.UI.Page
    {
        // Declare fields to match the .aspx file
        protected System.Web.UI.WebControls.HiddenField hdnTypeChartData;
        protected System.Web.UI.WebControls.HiddenField hdnExpiryChartData;
        protected System.Web.UI.WebControls.HiddenField hdnPlantChartData;
        protected System.Web.UI.WebControls.HiddenField hdnNextExpiryChartData;
        protected System.Web.UI.WebControls.Literal litTotalFE;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadTotalStatusCounts();
                LoadPlantStatistics();
                LoadFeTypeDistribution();
                LoadPlantComparison();
                LoadNextExpiryDates();
            }
        }

        /// <summary>
        /// Loads total counts of fire extinguishers by status across all plants
        /// </summary>
        private void LoadTotalStatusCounts()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(fe.FEID) as TotalFE,
                        SUM(CASE WHEN s.StatusName = 'Active' THEN 1 ELSE 0 END) as TotalActive,
                        SUM(CASE WHEN s.StatusName = 'Under Service' THEN 1 ELSE 0 END) as TotalUnderService,
                        SUM(CASE WHEN s.StatusName = 'Expired' THEN 1 ELSE 0 END) as TotalExpired,
                        SUM(CASE WHEN s.StatusName = 'Expiring Soon' THEN 1 ELSE 0 END) as TotalExpiringSoon
                    FROM FETS.FireExtinguishers fe
                    JOIN FETS.Status s ON fe.StatusID = s.StatusID", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Set Total FE count
                            litTotalFE.Text = reader.IsDBNull(reader.GetOrdinal("TotalFE")) ? "0" : reader.GetInt32(reader.GetOrdinal("TotalFE")).ToString();
                            
                            // Set other status counts
                            litActiveCount.Text = reader.IsDBNull(reader.GetOrdinal("TotalActive")) ? "0" : reader.GetInt32(reader.GetOrdinal("TotalActive")).ToString();
                            litServiceCount.Text = reader.IsDBNull(reader.GetOrdinal("TotalUnderService")) ? "0" : reader.GetInt32(reader.GetOrdinal("TotalUnderService")).ToString();
                            litExpiredCount.Text = reader.IsDBNull(reader.GetOrdinal("TotalExpired")) ? "0" : reader.GetInt32(reader.GetOrdinal("TotalExpired")).ToString();
                            litExpiringSoonCount.Text = reader.IsDBNull(reader.GetOrdinal("TotalExpiringSoon")) ? "0" : reader.GetInt32(reader.GetOrdinal("TotalExpiringSoon")).ToString();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads fire extinguisher statistics for each plant into the repeater control
        /// </summary>
        private void LoadPlantStatistics()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
SELECT 
    p.PlantID,
    p.PlantName,
    COUNT(fe.FEID) as TotalFE,
    SUM(CASE WHEN s.StatusName = 'Active' THEN 1 ELSE 0 END) as InUse,
    SUM(CASE WHEN s.StatusName = 'Under Service' THEN 1 ELSE 0 END) as UnderService,
    SUM(CASE WHEN s.StatusName = 'Expired' THEN 1 ELSE 0 END) as Expired,
    SUM(CASE WHEN s.StatusName = 'Expiring Soon' THEN 1 ELSE 0 END) as ExpiringSoon,
    MIN(CASE WHEN fe.DateExpired >= GETDATE() THEN fe.DateExpired ELSE NULL END) as NextExpiryDate
FROM FETS.Plants p
LEFT JOIN FETS.FireExtinguishers fe ON p.PlantID = fe.PlantID
LEFT JOIN FETS.Status s ON fe.StatusID = s.StatusID
GROUP BY p.PlantID, p.PlantName
ORDER BY p.PlantName", conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Create a new column for the formatted date string
                        dt.Columns.Add("NextExpiryDateFormatted", typeof(string));

                        // Replace null values with zero to prevent rendering issues
                        foreach (DataRow row in dt.Rows)
                        {
                            if (row["TotalFE"] == DBNull.Value) row["TotalFE"] = 0;
                            if (row["InUse"] == DBNull.Value) row["InUse"] = 0;
                            if (row["UnderService"] == DBNull.Value) row["UnderService"] = 0;
                            if (row["Expired"] == DBNull.Value) row["Expired"] = 0;
                            if (row["ExpiringSoon"] == DBNull.Value) row["ExpiringSoon"] = 0;
                            
                            // Format the expiry date or set to "N/A" if null
                            if (row["NextExpiryDate"] == DBNull.Value)
                            {
                                row["NextExpiryDateFormatted"] = "N/A";
                            }
                            else
                            {
                                DateTime expiryDate = Convert.ToDateTime(row["NextExpiryDate"]);
                                row["NextExpiryDateFormatted"] = expiryDate.ToString("dd/MM/yyyy");
                            }
                        }

                        rptPlants.DataSource = dt;
                        rptPlants.DataBind();
                    }
                }
            }
        }

        /// <summary>
        /// Loads fire extinguisher type distribution data for chart visualization
        /// </summary>
        private void LoadFeTypeDistribution()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT fet.TypeName as Type, COUNT(fe.FEID) as Count
                        FROM FETS.FireExtinguisherTypes fet
                        LEFT JOIN FETS.FireExtinguishers fe ON fet.TypeID = fe.TypeID
                        GROUP BY fet.TypeName
                        ORDER BY fet.TypeName", conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                        
                        // Create arrays for chart
                        List<string> labels = new List<string>();
                        List<int> data = new List<int>();
                        
                        foreach (DataRow row in dt.Rows)
                        {
                            string type = row["Type"].ToString();
                            int count = row["Count"] == DBNull.Value ? 0 : Convert.ToInt32(row["Count"]);
                            
                            labels.Add(type);
                            data.Add(count);
                        }
                        
                        // Serialize to JSON for JavaScript
                        var typeChartData = new { labels = labels, data = data };
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        hdnTypeChartData.Value = serializer.Serialize(typeChartData);
                    }
                }
            }
            catch (Exception)
            {
                // Simple error handling - in production you might log this
                hdnTypeChartData.Value = "{}";
            }
        }

        /// <summary>
        /// Loads plant comparison data for vertical bar chart
        /// </summary>
        private void LoadPlantComparison()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            p.PlantName,
                            COUNT(fe.FEID) as TotalCount,
                            SUM(CASE WHEN fet.TypeName = 'ABC' THEN 1 ELSE 0 END) as ABCCount,
                            SUM(CASE WHEN fet.TypeName = 'CO2' THEN 1 ELSE 0 END) as CO2Count
                        FROM FETS.Plants p
                        LEFT JOIN FETS.FireExtinguishers fe ON p.PlantID = fe.PlantID
                        LEFT JOIN FETS.FireExtinguisherTypes fet ON fe.TypeID = fet.TypeID
                        GROUP BY p.PlantName
                        ORDER BY TotalCount DESC", conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                        
                        // Create arrays for chart data
                        List<string> plants = new List<string>();
                        List<int> totalCounts = new List<int>();
                        List<int> abcCounts = new List<int>();
                        List<int> co2Counts = new List<int>();
                        
                        foreach (DataRow row in dt.Rows)
                        {
                            string plantName = row["PlantName"].ToString();
                            int totalCount = row["TotalCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["TotalCount"]);
                            int abcCount = row["ABCCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["ABCCount"]);
                            int co2Count = row["CO2Count"] == DBNull.Value ? 0 : Convert.ToInt32(row["CO2Count"]);
                            
                            plants.Add(plantName);
                            totalCounts.Add(totalCount);
                            abcCounts.Add(abcCount);
                            co2Counts.Add(co2Count);
                        }
                        
                        // Serialize to JSON for JavaScript
                        var plantChartData = new { 
                            labels = plants, 
                            totalData = totalCounts,
                            abcData = abcCounts,
                            co2Data = co2Counts
                        };
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        hdnPlantChartData.Value = serializer.Serialize(plantChartData);
                    }
                }
            }
            catch (Exception)
            {
                // Simple error handling
                hdnPlantChartData.Value = "{}";
            }
        }

        /// <summary>
        /// Loads next expiry date data for each plant for the chart
        /// </summary>
        private void LoadNextExpiryDates()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT 
                            p.PlantName,
                            MIN(CASE WHEN fe.DateExpired >= GETDATE() AND s.StatusName <> 'Under Service' THEN fe.DateExpired ELSE NULL END) as NextExpiryDate
                        FROM FETS.Plants p
                        LEFT JOIN FETS.FireExtinguishers fe ON p.PlantID = fe.PlantID
                        LEFT JOIN FETS.Status s ON fe.StatusID = s.StatusID
                        GROUP BY p.PlantName
                        ORDER BY p.PlantName", conn))
                    {
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                        
                        // Create arrays for chart data
                        List<string> plants = new List<string>();
                        List<string> expiryDates = new List<string>();
                        List<double> daysTillExpiry = new List<double>();
                        
                        DateTime now = DateTime.Now.Date;
                        
                        foreach (DataRow row in dt.Rows)
                        {
                            string plantName = row["PlantName"].ToString();
                            
                            if (row["NextExpiryDate"] != DBNull.Value)
                            {
                                DateTime expiryDate = Convert.ToDateTime(row["NextExpiryDate"]);
                                double days = (expiryDate - now).TotalDays;
                                
                                plants.Add(plantName);
                                expiryDates.Add(expiryDate.ToString("dd/MM/yyyy"));
                                daysTillExpiry.Add(days);
                            }
                        }
                        
                        // Serialize to JSON for JavaScript
                        var nextExpiryChartData = new { 
                            labels = plants, 
                            expiryDates = expiryDates,
                            daysTillExpiry = daysTillExpiry
                        };
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        hdnNextExpiryChartData.Value = serializer.Serialize(nextExpiryChartData);
                    }
                }
            }
            catch (Exception)
            {
                // Simple error handling
                hdnNextExpiryChartData.Value = "{}";
            }
        }
    }
} 