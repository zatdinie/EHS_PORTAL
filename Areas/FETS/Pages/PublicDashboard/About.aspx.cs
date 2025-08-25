using System;

namespace FETS.Pages.PublicDashboard
{
    public partial class About : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Set page title
                Page.Title = "About FETS - Fire Extinguisher Tracking System";
                
                // Add any additional initialization logic here if needed
                // For example, you could load dynamic content or statistics
            }
        }
    }
}
