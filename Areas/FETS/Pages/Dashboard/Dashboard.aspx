<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="FETS.Pages.Dashboard" MasterPageFile="~/Areas/FETS/Site.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        /* Base styles and variables */
        :root {
            --primary-color: #007bff;
            --success-color: #28a745;
            --warning-color: #ffc107;
            --danger-color: #dc3545;
            --alert-color: #fd7e14;
            --text-color: #333;
            --text-secondary: #666;
            --border-color: #dee2e6;
            --shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
            --radius: 8px;
            --background-color: #f5f7fa;
            --card-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
            --transition: all 0.3s ease;
        }

        /* Layout */
        body {
            background-color: var(--background-color);
            min-height: 100vh;
            margin: 0;
            padding: 0;
        }

        .dashboard-container {
            padding: clamp(1rem, 2vw, 2rem);
            max-width: 1400px;
            margin: 0 auto;
            width: 100%;
            box-sizing: border-box;
        }

        .dashboard-header {
            width: 100%;
            text-align: center;
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            padding: clamp(1.5rem, 3vw, 2rem);
            margin: 0 auto 2rem auto;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            box-sizing: border-box;
        }

        .dashboard-header h2 {
            margin: 0;
            color: var(--text-color);
            font-size: clamp(1.5rem, 3vw, 2rem);
            font-weight: 600;
            padding-bottom: 1rem;
            text-align: center;
            width: 100%;
            border-bottom: 2px solid var(--primary-color);
        }

        .dashboard-grid {
            display: flex;
            flex-direction: column;
            gap: 2rem;
            width: 100%;
        }

        /* Chart section */
        .chart-section {
            background-color: #fff;
            padding: clamp(1.5rem, 3vw, 2rem);
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            width: 100%;
            margin: 0 auto;
            box-sizing: border-box;
            height: auto;
        }

        .chart-section h3 {
            margin: 0 0 1.5rem 0;
            color: var(--text-color);
            font-size: clamp(1.2rem, 2vw, 1.5rem);
            text-align: center;
            padding-bottom: 0.75rem;
            border-bottom: 2px solid var(--primary-color);
            width: 100%;
        }

        .chart-container {
            width: 100%;
            height: clamp(250px, 40vh, 400px);
            max-width: 600px;
            margin: 0 auto;
            display: flex;
            justify-content: center;
            align-items: center;
            position: relative;
        }

        /* Style for crossed-out legend items */
        .chart-legend-item-hidden {
            text-decoration: line-through;
            opacity: 0.5;
        }
        
        /* Add a subtle border around the chart */
        canvas#feTypeChart {
            border-radius: 8px;
            background-color: #f9f9f9;
            padding: 10px;
        }

        /* Plants section */
        .plants-section {
            background-color: #fff;
            padding: clamp(1.5rem, 3vw, 2rem);
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            width: 100%;
            margin: 0 auto;
            box-sizing: border-box;
            padding-bottom: 2rem;
            display: flex;
            flex-direction: column;
            align-items: center;
        }

        .plants-section h3 {
            margin: 0 0 1.5rem 0;
            color: var(--text-color);
            font-size: clamp(1.2rem, 2vw, 1.5rem);
            text-align: center;
            padding-bottom: 0.75rem;
            border-bottom: 2px solid var(--primary-color);
            width: 100%;
        }

        .plants-grid {
            display: grid;
            grid-template-columns: repeat(3, 320px);
            gap: 3rem;
            padding: 3rem;
            justify-content: center;
            max-width: 1200px;
            width: 100%;
        }

        /* Plant Card Styles */
        .plant-card {
            background-color: #fff;
            padding: 0.75rem;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            transition: var(--transition);
            display: flex;
            flex-direction: column;
            height: 100%;
            border: 1px solid var(--border-color);
            position: relative;
            overflow: hidden;
            width: 100%;
        }

        .plant-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
        }

        .plant-card:before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 4px;
            background: linear-gradient(90deg, var(--primary-color), var(--success-color));
        }

        .plant-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 0.5rem;
            padding-bottom: 0.5rem;
            border-bottom: 1px solid var(--border-color);
        }

        .plant-title {
            color: var(--text-color);
            font-size: 0.9rem;
            font-weight: 600;
            text-align: left;
            flex: 1;
        }

        .plant-total {
            display: flex;
            flex-direction: column;
            align-items: center;
            background-color: #f8f9fa;
            border-radius: var(--radius);
            padding: 0.3rem 0.6rem;
            border: 1px solid #e9ecef;
        }

        .total-number {
            font-size: 1rem;
            font-weight: 700;
            color: var(--primary-color);
        }

        .total-label {
            font-size: 0.7rem;
            color: #6c757d;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        /* Status Summary with mini chart */
        .status-summary {
            display: flex;
            justify-content: center;
            margin: 1rem 0;
            padding: 0.5rem;
            background-color: #f8f9fa;
            border-radius: var(--radius);
            border: 1px solid var(--border-color);
        }

        .status-chart {
            width: 140px;
            height: 140px;
            position: relative;
        }

        .status-pie-container {
            width: 100%;
            height: 100%;
            padding: 0.5rem;
        }

        /* Stats list improvements */
        .stats-list {
            list-style: none;
            padding: 0.75rem;
            margin: 0;
            flex-grow: 1;
            display: flex;
            flex-direction: column;
            gap: 1rem;
            background-color: #f8f9fa;
            border-radius: var(--radius);
            border: 1px solid var(--border-color);
        }

        .stat-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 0.75rem 1rem;
            background-color: white;
            border-radius: var(--radius);
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
            transition: var(--transition);
        }

        .stat-item:hover {
            transform: translateX(3px);
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.08);
        }

        .stat-label {
            color: var(--text-color);
            font-size: 0.95rem;
            font-weight: 500;
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }

        .stat-indicator {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            display: inline-block;
            box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.8);
        }

        .stat-indicator.in-use { background-color: var(--success-color); }
        .stat-indicator.under-service { background-color: var(--warning-color); }
        .stat-indicator.expired { background-color: var(--danger-color); }
        .stat-indicator.expiring-soon { background-color: var(--alert-color); }

        .stat-value {
            font-size: 1.1rem;
            font-weight: 600;
            min-width: 45px;
            text-align: center;
            white-space: nowrap;
            padding: 0.3rem 0.75rem;
            border-radius: 6px;
            background-color: #f8f9fa;
        }

        .stat-value.in-use { 
            color: var(--success-color);
            background-color: rgba(40, 167, 69, 0.1);
        }
        .stat-value.under-service { 
            color: var(--warning-color);
            background-color: rgba(255, 193, 7, 0.1);
        }
        .stat-value.expired { 
            color: var(--danger-color);
            background-color: rgba(220, 53, 69, 0.1);
        }
        .stat-value.expiring-soon { 
            color: var(--alert-color);
            background-color: rgba(253, 126, 20, 0.1);
        }

        /* Enhanced responsive design */
        @media (max-width: 1200px) {
            .plants-grid {
                grid-template-columns: repeat(2, 320px);
                gap: 1.75rem;
                max-width: 800px;
            }
        }

        @media (max-width: 768px) {
            .plants-grid {
                grid-template-columns: 320px;
                gap: 1.5rem;
                max-width: 400px;
                padding: 0.5rem;
            }
            
            .plant-header {
                flex-direction: column;
                gap: 0.5rem;
                align-items: flex-start;
            }
            
            .plant-total {
                align-self: flex-end;
            }
            
            .status-chart {
                width: 100px;
                height: 100px;
            }
        }

        @media (max-width: 480px) {
            .plants-grid {
                gap: 1.25rem;
            }
            
            .plant-card {
                padding: 0.6rem;
            }
            
            .next-expiry {
                flex-direction: column;
                gap: 0.4rem;
                align-items: flex-start;
                padding: 0.6rem 0.75rem;
            }

            .expiry-date {
                align-self: flex-end;
                font-size: 0.85rem;
            }
        }

        .next-expiry {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 0.5rem 0.75rem;
            background-color: #f8f9fa;
            border-radius: var(--radius);
            margin-bottom: 0.75rem;
            border: 1px solid #e9ecef;
            transition: var(--transition);
        }

        .next-expiry:hover {
            background-color: #f1f3f5;
        }

        .expiry-label {
            color: var(--text-secondary);
            font-size: 0.8rem;
            font-weight: 500;
        }

        .expiry-date {
            color: var(--primary-color);
            font-weight: 600;
            font-size: 0.85rem;
        }

        .expiry-date.no-expiry {
            color: #6c757d;
            font-style: italic;
        }

        /* Add this in the dashboard-grid div, right after the chart-section section */
        <section class="chart-section total-stats-section">
            <h3>Fire Extinguisher Status Overview</h3>
            <div class="total-stats-container">
                <div class="stat-card">
                    <div class="stat-icon active-icon">
                        <i class="fas fa-check-circle"></i>
                    </div>
                    <div class="stat-info">
                        <h4>Active</h4>
                        <div class="stat-number"><%= TotalActive %></div>
                    </div>
                </div>
                <div class="stat-card">
                    <div class="stat-icon service-icon">
                        <i class="fas fa-tools"></i>
                    </div>
                    <div class="stat-info">
                        <h4>Under Service</h4>
                        <div class="stat-number"><%= TotalUnderService %></div>
                    </div>
                </div>
                <div class="stat-card">
                    <div class="stat-icon expired-icon">
                        <i class="fas fa-exclamation-circle"></i>
                    </div>
                    <div class="stat-info">
                        <h4>Expired</h4>
                        <div class="stat-number"><%= TotalExpired %></div>
                    </div>
                </div>
                <div class="stat-card">
                    <div class="stat-icon expiring-icon">
                        <i class="fas fa-clock"></i>
                    </div>
                    <div class="stat-info">
                        <h4>Expiring Soon</h4>
                        <div class="stat-number"><%= TotalExpiringSoon %></div>
                    </div>
                </div>
            </div>
        </section>

        .total-stats-container {
            display: flex;
            flex-wrap: wrap;
            justify-content: space-between;
            gap: 1rem;
            padding: 1rem 0;
        }

        .stat-card {
            display: flex;
            align-items: center;
            background: white;
            border-radius: var(--radius);
            padding: 1rem;
            flex: 1;
            min-width: 180px;
            border: 1px solid var(--border-color);
            box-shadow: var(--shadow);
            transition: transform 0.2s ease;
        }

        .stat-card:hover {
            transform: translateY(-3px);
        }

        .stat-icon {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 50px;
            height: 50px;
            border-radius: 50%;
            margin-right: 1rem;
            flex-shrink: 0;
        }

        .active-icon {
            background-color: rgba(40, 167, 69, 0.1);
            color: var(--success-color);
        }

        .service-icon {
            background-color: rgba(255, 193, 7, 0.1);
            color: var(--warning-color);
        }

        .expired-icon {
            background-color: rgba(220, 53, 69, 0.1);
            color: var(--danger-color);
        }

        .expiring-icon {
            background-color: rgba(253, 126, 20, 0.1);
            color: var(--alert-color);
        }

        .stat-info {
            display: flex;
            flex-direction: column;
        }

        .stat-info h4 {
            margin: 0;
            font-size: 0.9rem;
            color: var(--text-secondary);
        }

        .stat-number {
            font-size: 1.5rem;
            font-weight: 700;
            color: var(--text-color);
        }

        /* Responsive styling */
        @media (max-width: 768px) {
            .total-stats-container {
                flex-direction: column;
            }
            
            .stat-card {
                min-width: 100%;
            }
        }

        /* Add these styles to the existing style section */
        .chart-row {
            display: flex;
            gap: 2rem;
            margin-bottom: 2rem;
            width: 100%;
        }

        .types-chart-section, .total-stats-section {
            flex: 1;
            min-width: 0; /* Ensures proper flexbox behavior */
            width: calc(50% - 1rem);
        }

        /* Responsive styles for the chart row */
        @media (max-width: 992px) {
            .chart-row {
                flex-direction: column;
                gap: 2rem;
            }
            
            .types-chart-section, .total-stats-section {
                width: 100%;
            }
        }

        /* Adjust the cards to fit better in the status section */
        .total-stats-container {
            flex-wrap: wrap;
            gap: 1rem;
        }

        .stat-card {
            min-width: calc(50% - 0.5rem);
            flex: 0 0 calc(50% - 0.5rem);
            box-sizing: border-box;
        }

        /* Make cards stack on smaller screens */
        @media (max-width: 480px) {
            .stat-card {
                min-width: 100%;
                flex: 0 0 100%;
            }
        }

        /* Add this to the style section */
        .total-fe-counter {
            display: flex;
            justify-content: center;
            align-items: center;
            margin-top: 1rem;
            padding: 0.75rem;
            background-color: #f8f9fa;
            border-radius: var(--radius);
            border: 1px solid var(--border-color);
            gap: 0.75rem;
        }

        .counter-label {
            font-size: 1rem;
            font-weight: 500;
            color: var(--text-secondary);
        }

        .counter-value {
            font-size: 1.5rem;
            font-weight: 700;
            color: var(--primary-color);
            background-color: rgba(0, 123, 255, 0.1);
            padding: 0.25rem 1rem;
            border-radius: var(--radius);
            min-width: 80px;
            text-align: center;
        }

        /* Add button styles for the user guide button */
        .btn-user-guide {
            padding: 8px 15px;
            background-color: var(--primary-color);
            color: white;
            text-decoration: none;
            border-radius: var(--radius);
            font-weight: 500;
            display: inline-flex;
            align-items: center;
            gap: 5px;
            transition: var(--transition);
        }

        .btn-user-guide:hover {
            background-color: #0056b3;
            transform: translateY(-2px);
        }

        .header-actions {
            display: flex;
            justify-content: flex-end;
            width: 100%;
            margin-bottom: 10px;
        }
    </style>
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="dashboard-container">
        <div class="dashboard-header">
            <div class="header-actions">
                <a href="../PublicDashboard/UserGuideHandler.ashx" class="btn-user-guide" target="_blank">
                    <i class="fas fa-file-pdf"></i> User Guide
                </a>
            </div>
            <h2>Fire Extinguisher Tracking System</h2>
        </div>

        <div class="dashboard-grid">
            <!-- Create a flex container for the two sections -->
            <div class="chart-row">
                <!-- Chart section (unchanged) -->
                <section class="chart-section types-chart-section">
                    <h3>Fire Extinguisher Types Distribution</h3>
                    <div class="chart-container">
                        <canvas id="feTypeChart"></canvas>
                    </div>
                    <asp:HiddenField ID="hdnChartData" runat="server" />
                    
                    <!-- Add this total count display below the chart -->
                    <div class="total-fe-counter">
                        <div class="counter-label">Total Fire Extinguishers:</div>
                        <div class="counter-value"><%= TotalFireExtinguishers %></div>
                    </div>
                </section>

                <!-- Status overview section (unchanged) -->
                <section class="chart-section total-stats-section">
                    <h3>Fire Extinguisher Status Overview</h3>
                    <div class="total-stats-container">
                        <div class="stat-card">
                            <div class="stat-icon active-icon">
                                <i class="fas fa-check-circle"></i>
                            </div>
                            <div class="stat-info">
                                <h4>Active</h4>
                                <div class="stat-number"><%= TotalActive %></div>
                            </div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-icon service-icon">
                                <i class="fas fa-tools"></i>
                            </div>
                            <div class="stat-info">
                                <h4>Under Service</h4>
                                <div class="stat-number"><%= TotalUnderService %></div>
                            </div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-icon expired-icon">
                                <i class="fas fa-exclamation-circle"></i>
                            </div>
                            <div class="stat-info">
                                <h4>Expired</h4>
                                <div class="stat-number"><%= TotalExpired %></div>
                            </div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-icon expiring-icon">
                                <i class="fas fa-clock"></i>
                            </div>
                            <div class="stat-info">
                                <h4>Expiring Soon</h4>
                                <div class="stat-number"><%= TotalExpiringSoon %></div>
                            </div>
                        </div>
                    </div>
                </section>
            </div>

            <!-- Plant statistics section (unchanged) -->
            <section class="plants-section">
                <h3>Plant Statistics</h3>
                <div class="plants-grid">
                    <asp:Repeater ID="rptPlants" runat="server">
                        <ItemTemplate>
                            <div class="plant-card">
                                <div class="plant-header">
                                    <div class="plant-title"><%# Eval("PlantName") %></div>
                                    <div class="plant-total">
                                        <span class="total-number"><%# Eval("TotalFE", "{0:N0}") %></span>
                                        <span class="total-label">Total FE</span>
                                    </div>
                                </div>
                                
                                <div class="next-expiry">
                                    <span class="expiry-label">Next Expiry:</span>
                                    <span class="expiry-date <%# Convert.ToDateTime(Eval("NextExpiryDate")) == DateTime.MaxValue ? "no-expiry" : "" %>">
                                        <%# Convert.ToDateTime(Eval("NextExpiryDate")) == DateTime.MaxValue ? "No upcoming expiries" : Convert.ToDateTime(Eval("NextExpiryDate")).ToString("dd/MM/yyyy") %>
                                    </span>
                                </div>
                                
                                <div class="status-summary">
                                    <div class="status-chart">
                                        <div class="status-pie-container">
                                            <canvas class="status-pie" 
                                                   data-in-use="<%# Eval("InUse") %>" 
                                                   data-under-service="<%# Eval("UnderService") %>" 
                                                   data-expired="<%# Eval("Expired") %>" 
                                                   data-expiring-soon="<%# Eval("ExpiringSoon") %>">
                                            </canvas>
                                        </div>
                                    </div>
                                </div>
                                
                                <ul class="stats-list">
                                    <!-- In-Use extinguishers -->
                                    <li class="stat-item">
                                        <span class="stat-label">
                                            <span class="stat-indicator in-use"></span>
                                            Active
                                        </span>
                                        <span class="stat-value in-use"><%# Eval("InUse", "{0:N0}") %></span>
                                    </li>
                                    <!-- Under service extinguishers -->
                                    <li class="stat-item">
                                        <span class="stat-label">
                                            <span class="stat-indicator under-service"></span>
                                            Under Service
                                        </span>
                                        <span class="stat-value under-service"><%# Eval("UnderService", "{0:N0}") %></span>
                                    </li>
                                    <!-- Expired extinguishers -->
                                    <li class="stat-item">
                                        <span class="stat-label">
                                            <span class="stat-indicator expired"></span>
                                            Expired
                                        </span>
                                        <span class="stat-value expired"><%# Eval("Expired", "{0:N0}") %></span>
                                    </li>
                                    <!-- Soon-to-expire extinguishers -->
                                    <li class="stat-item">
                                        <span class="stat-label">
                                            <span class="stat-indicator expiring-soon"></span>
                                            Expiring Soon
                                        </span>
                                        <span class="stat-value expiring-soon"><%# Eval("ExpiringSoon", "{0:N0}") %></span>
                                    </li>
                                </ul>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </section>
        </div>
    </div>

    <script>
        document.addEventListener('DOMContentLoaded', function() {
            // Initialize the FE Type Distribution Chart
            var ctx = document.getElementById('feTypeChart').getContext('2d');
            var chartData = document.getElementById('<%= hdnChartData.ClientID %>').value.split(',').map(Number);
            
            new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['ABC', 'CO2'],
                    datasets: [{
                        data: chartData,
                        backgroundColor: [
                            '#3498db',
                            '#2ecc71'
                        ],
                        borderWidth: 2,
                        borderColor: '#ffffff'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '75%',
                    plugins: {
                        tooltip: {
                            backgroundColor: 'rgba(255, 255, 255, 0.95)',
                            titleColor: '#333',
                            titleFont: {
                                size: 14,
                                weight: 'bold'
                            },
                            bodyColor: '#666',
                            bodyFont: {
                                size: 13
                            },
                            padding: 12,
                            boxPadding: 8,
                            borderColor: '#ddd',
                            borderWidth: 1,
                            displayColors: true,
                            boxWidth: 12,
                            boxHeight: 12,
                            cornerRadius: 8,
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.raw || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = Math.round((value / total) * 100);
                                    return ` ${label}: ${value} units (${percentage}%)`;
                                }
                            }
                        },
                        legend: {
                            position: 'bottom',
                            labels: {
                                padding: 20,
                                font: {
                                    size: 14,
                                    weight: '500'
                                },
                                generateLabels: function(chart) {
                                    const data = chart.data;
                                    const total = data.datasets[0].data.reduce((a, b) => a + b, 0);
                                    return data.labels.map((label, i) => ({
                                        text: `${label}: ${data.datasets[0].data[i]} (${Math.round(data.datasets[0].data[i]/total * 100)}%)`,
                                        fillStyle: data.datasets[0].backgroundColor[i],
                                        index: i,
                                        hidden: !chart.getDataVisibility(i),
                                        lineWidth: 1,
                                        strokeStyle: '#666'
                                    }));
                                }
                            }
                        }
                    }
                }
            });
            
            // Initialize individual status charts for each plant
            document.querySelectorAll('.status-pie').forEach(canvas => {
                const ctx = canvas.getContext('2d');
                const inUse = parseInt(canvas.getAttribute('data-in-use')) || 0;
                const underService = parseInt(canvas.getAttribute('data-under-service')) || 0;
                const expired = parseInt(canvas.getAttribute('data-expired')) || 0;
                const expiringSoon = parseInt(canvas.getAttribute('data-expiring-soon')) || 0;
                
                new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: ['Active', 'Under Service', 'Expired', 'Expiring Soon'],
                        datasets: [{
                            data: [inUse, underService, expired, expiringSoon],
                            backgroundColor: [
                                'rgba(40, 167, 69, 0.85)',  // success-color
                                'rgba(255, 193, 7, 0.85)',  // warning-color
                                'rgba(220, 53, 69, 0.85)',  // danger-color
                                'rgba(253, 126, 20, 0.85)'  // alert-color
                            ],
                            borderColor: [
                                '#28a745',  // success-color
                                '#ffc107',  // warning-color
                                '#dc3545',  // danger-color
                                '#fd7e14'   // alert-color
                            ],
                            borderWidth: 2,
                            hoverBackgroundColor: [
                                'rgba(40, 167, 69, 1)',   // solid success-color
                                'rgba(255, 193, 7, 1)',   // solid warning-color
                                'rgba(220, 53, 69, 1)',   // solid danger-color
                                'rgba(253, 126, 20, 1)'   // solid alert-color
                            ],
                            hoverBorderColor: '#ffffff',
                            hoverBorderWidth: 3
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: true,
                        cutout: '60%',
                        layout: {
                            padding: {
                                top: 20,
                                bottom: 20,
                                left: 20,
                                right: 20
                            }
                        },
                        plugins: {
                            tooltip: {
                                enabled: true,
                                position: 'nearest',
                                backgroundColor: 'rgba(255, 255, 255, 0.95)',
                                titleColor: '#333',
                                titleFont: {
                                    size: 14,
                                    weight: 'bold'
                                },
                                bodyColor: '#666',
                                bodyFont: {
                                    size: 13,
                                    weight: '500'
                                },
                                padding: 12,
                                boxPadding: 8,
                                borderColor: '#ddd',
                                borderWidth: 1,
                                displayColors: true,
                                boxWidth: 12,
                                boxHeight: 12,
                                cornerRadius: 8,
                                usePointStyle: true,
                                caretSize: 8,
                                caretPadding: 6,
                                callbacks: {
                                    title: function(tooltipItems) {
                                        return tooltipItems[0].label;
                                    },
                                    label: function(context) {
                                        const value = context.raw || 0;
                                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                        const percentage = total > 0 ? Math.round((value / total) * 100) : 0;
                                        return ` ${value} units (${percentage}%)`;
                                    }
                                }
                            },
                            legend: {
                                display: false
                            }
                        },
                        animation: {
                            animateScale: true,
                            animateRotate: true,
                            duration: 1000
                        },
                        elements: {
                            arc: {
                                borderWidth: 2
                            }
                        },
                        interaction: {
                            mode: 'nearest',
                            intersect: false,
                            axis: 'xy'
                        }
                    }
                });
            });
            
            // Animate progress bars on page load for visual effect
            setTimeout(() => {
                document.querySelectorAll('.progress').forEach(bar => {
                    const width = bar.style.width;
                    bar.style.width = '0%';
                    setTimeout(() => {
                        bar.style.width = width;
                    }, 100);
                });
            }, 300);
        });
    </script>
</asp:Content>