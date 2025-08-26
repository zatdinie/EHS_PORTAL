<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="FETS.Pages.PublicDashboard.About" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>About - INARI FETS</title>
    <!-- Favicon -->
    <link rel="shortcut icon" href="../../favicon.ico" type="image/x-icon" />
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css" />
    <style>
        :root {
            --primary-color: #007bff;
            --success-color: #28a745;
            --warning-color: #ffc107;
            --danger-color: #dc3545;
            --text-color: #333;
            --text-secondary: #666;
            --border-color: #dee2e6;
            --shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            --radius: 8px;
            --background-color: #f5f7fa;
            --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
            --transition: all 0.3s ease;
        }

        body {
            background-color: var(--background-color);
            font-family: 'Poppins', sans-serif;
            margin: 0;
            padding: 0;
            min-height: 100vh;
            line-height: 1.6;
        }

        .about-container {
            padding: clamp(1rem, 2vw, 2rem);
            max-width: 1200px;
            margin: 0 auto;
            width: 100%;
            box-sizing: border-box;
        }

        .about-header {
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            padding: clamp(1.5rem, 3vw, 2rem);
            margin-bottom: 2rem;
            text-align: center;
            position: relative;
        }

        .back-button {
            position: absolute;
            top: 20px;
            left: 20px;
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 8px 16px;
            background-color: var(--primary-color);
            color: white;
            text-decoration: none;
            border-radius: var(--radius);
            font-size: 0.9rem;
            font-weight: 500;
            transition: var(--transition);
            box-shadow: var(--shadow);
        }

        .back-button:hover {
            background-color: #0056b3;
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
        }

        .logo-container {
            margin-bottom: 1.5rem;
        }

        .logo {
            max-height: 80px;
            width: auto;
        }

        .about-header h1 {
            margin: 0 0 0.5rem 0;
            color: var(--text-color);
            font-size: clamp(1.8rem, 3vw, 2.2rem);
            font-weight: 700;
        }

        .about-header h2 {
            margin: 0 0 1rem 0;
            color: var(--primary-color);
            font-size: clamp(1.2rem, 2.5vw, 1.5rem);
            font-weight: 600;
        }

        .subtitle {
            color: var(--text-secondary);
            font-size: clamp(1rem, 1.5vw, 1.1rem);
            margin: 0;
        }

        .content-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
            gap: 2rem;
            margin-bottom: 2rem;
        }

        .content-card {
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            padding: 2rem;
            transition: var(--transition);
        }

        .content-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 20px rgba(0, 0, 0, 0.12);
        }

        .content-card h3 {
            margin: 0 0 1rem 0;
            color: var(--text-color);
            font-size: 1.4rem;
            font-weight: 600;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .content-card h3 i {
            color: var(--primary-color);
            font-size: 1.2rem;
        }

        .content-card p {
            margin: 0 0 1rem 0;
            color: var(--text-secondary);
            text-align: justify;
        }

        .content-card ul {
            margin: 0 0 1rem 1.5rem;
            color: var(--text-secondary);
        }

        .content-card ul li {
            margin-bottom: 0.5rem;
        }

        .feature-list {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 1rem;
            margin-top: 1rem;
        }

        .feature-item {
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 0.8rem;
            background-color: rgba(0, 123, 255, 0.05);
            border-radius: var(--radius);
            border-left: 3px solid var(--primary-color);
        }

        .feature-item i {
            color: var(--primary-color);
            font-size: 1.1rem;
            width: 20px;
            text-align: center;
        }

        .feature-item span {
            color: var(--text-color);
            font-weight: 500;
        }

        .stats-section {
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            padding: 2rem;
            margin-bottom: 2rem;
        }

        .stats-section h3 {
            margin: 0 0 1.5rem 0;
            color: var(--text-color);
            font-size: 1.4rem;
            font-weight: 600;
            text-align: center;
            padding-bottom: 0.75rem;
            border-bottom: 2px solid var(--primary-color);
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1.5rem;
        }

        .stat-card {
            text-align: center;
            padding: 1.5rem;
            background: linear-gradient(135deg, rgba(0, 123, 255, 0.1), rgba(0, 123, 255, 0.05));
            border-radius: var(--radius);
            border: 1px solid rgba(0, 123, 255, 0.2);
        }

        .stat-card i {
            font-size: 2.5rem;
            color: var(--primary-color);
            margin-bottom: 0.5rem;
        }

        .stat-card h4 {
            margin: 0 0 0.5rem 0;
            color: var(--text-color);
            font-size: 1.1rem;
            font-weight: 600;
        }

        .stat-card p {
            margin: 0;
            color: var(--text-secondary);
            font-size: 0.9rem;
        }

        .contact-section {
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            padding: 2rem;
            text-align: center;
        }

        .contact-section h3 {
            margin: 0 0 1.5rem 0;
            color: var(--text-color);
            font-size: 1.4rem;
            font-weight: 600;
        }

        .contact-info {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 2rem;
            margin-top: 1.5rem;
        }

        .contact-item {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
            padding: 1rem;
            background-color: rgba(0, 123, 255, 0.05);
            border-radius: var(--radius);
        }

        .contact-item i {
            color: var(--primary-color);
            font-size: 1.2rem;
        }

        .contact-item span {
            color: var(--text-color);
            font-weight: 500;
        }

        .footer {
            text-align: center;
            padding: 20px;
            margin-top: 40px;
            color: var(--text-secondary);
            font-size: 0.9rem;
        }

        @media (max-width: 768px) {
            .back-button {
                position: relative;
                top: 0;
                left: 0;
                margin-bottom: 1rem;
                align-self: flex-start;
            }

            .about-header {
                padding: 1.5rem 1rem 2rem 1rem;
            }

            .content-grid {
                grid-template-columns: 1fr;
                gap: 1.5rem;
            }

            .content-card {
                padding: 1.5rem;
            }

            .feature-list {
                grid-template-columns: 1fr;
            }

            .stats-grid {
                grid-template-columns: repeat(2, 1fr);
            }

            .contact-info {
                grid-template-columns: 1fr;
                gap: 1rem;
            }
        }

        @media (max-width: 480px) {
            .stats-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="about-container">
            <div class="about-header">
                <a href="PublicDashboard.aspx" class="back-button">
                    <i class="fas fa-arrow-left"></i>
                    Back to Dashboard
                </a>
                <div class="logo-container">
                    <img src="<%=ResolveUrl("~/Areas/FETS/Uploads/misc/logo.png")%>" alt="INARI Logo" class="logo" />
                </div>
                <h1>About FETS</h1>
                <h2>Fire Extinguisher Tracking System</h2>
                <p class="subtitle">Environment, Health and Safety Department (EHS) - INARI AMERTRON BHD</p>
            </div>

            <div class="content-grid">
                <div class="content-card">
                    <h3><i class="fas fa-info-circle"></i>System Overview</h3>
                    <p>
                        The Fire Extinguisher Tracking System (FETS) is a comprehensive digital solution designed to monitor, 
                        manage, and maintain fire safety equipment across all INARI AMERTRON facilities. Our system ensures 
                        compliance with safety regulations while providing real-time visibility into the status and location 
                        of fire extinguishers.
                    </p>
                    <p>
                        Built with modern web technologies, FETS provides an intuitive interface for both public viewing 
                        and authorized personnel management, ensuring that fire safety remains a top priority throughout 
                        our organization.
                    </p>
                </div>

                <div class="content-card">
                    <h3><i class="fas fa-bullseye"></i>Mission & Purpose</h3>
                    <p>
                        Our mission is to enhance workplace safety by providing a robust, reliable system for tracking 
                        fire extinguisher maintenance, expiration dates, and service schedules. We aim to:
                    </p>
                    <ul>
                        <li>Prevent fire safety equipment failures through proactive monitoring</li>
                        <li>Ensure regulatory compliance across all facilities</li>
                        <li>Reduce administrative overhead through automation</li>
                        <li>Provide transparent reporting for stakeholders</li>
                        <li>Support continuous improvement in safety protocols</li>
                    </ul>
                </div>

                <div class="content-card">
                    <h3><i class="fas fa-cogs"></i>Key Features</h3>
                    <p>FETS offers a comprehensive suite of features designed for efficiency and reliability:</p>
                    <div class="feature-list">
                        <div class="feature-item">
                            <i class="fas fa-chart-bar"></i>
                            <span>Real-time Dashboard</span>
                        </div>
                        <div class="feature-item">
                            <i class="fas fa-map-marked-alt"></i>
                            <span>Interactive Plant Maps</span>
                        </div>
                        <div class="feature-item">
                            <i class="fas fa-calendar-alt"></i>
                            <span>Automated Alerts</span>
                        </div>

                        <div class="feature-item">
                            <i class="fas fa-shield-alt"></i>
                            <span>Secure Access Control</span>
                        </div>
                        <div class="feature-item">
                            <i class="fas fa-file-export"></i>
                            <span>Comprehensive Reporting</span>
                        </div>
                    </div>
                </div>
            </div>

            <div class="stats-section">
                <h3>System Capabilities</h3>
                <div class="stats-grid">
                    <div class="stat-card">
                        <i class="fas fa-fire-extinguisher"></i>
                        <h4>Equipment Tracking</h4>
                        <p>Monitor all fire extinguishers across multiple facilities</p>
                    </div>
                    <div class="stat-card">
                        <i class="fas fa-clock"></i>
                        <h4>24/7 Monitoring</h4>
                        <p>Round-the-clock system availability and alerts</p>
                    </div>
                    <div class="stat-card">
                        <i class="fas fa-chart-line"></i>
                        <h4>Analytics</h4>
                        <p>Comprehensive reporting and trend analysis</p>
                    </div>
                    <div class="stat-card">
                        <i class="fas fa-lock"></i>
                        <h4>Data Security</h4>
                        <p>Secure data handling and access control</p>
                    </div>
                </div>
            </div>
            <div class="footer">
                <p>&copy; <%= DateTime.Now.Year %> INARI AMERTRON BHD. - Environment, Health and Safety Department (EHS)</p>
                <p>Fire Extinguisher Tracking System (FETS) - Launched on April 2025 version 1.0.0</p>
            </div>
        </div>
    </form>
</body>
</html>
