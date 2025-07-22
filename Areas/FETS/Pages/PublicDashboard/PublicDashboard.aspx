<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PublicDashboard.aspx.cs" Inherits="FETS.Pages.PublicDashboard.PublicDashboard" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>INARI - FETS
    </title>
    <!-- Favicon -->
    <link rel="shortcut icon" href="../../favicon.ico" type="image/x-icon" />
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css" />
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
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

        body {
            background-color: var(--background-color);
            font-family: 'Poppins', sans-serif;
            margin: 0;
            padding: 0;
            min-height: 100vh;
        }

        .dashboard-container {
            padding: clamp(1rem, 2vw, 2rem);
            max-width: 1400px;
            margin: 0 auto;
            width: 100%;
            box-sizing: border-box;
            display: flex;
            flex-direction: column;
            align-items: stretch;
        }

        .dashboard-header {
            width: 100%;
            text-align: center;
            background-color: #fff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            padding: clamp(1.2rem, 2.5vw, 1.8rem);
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
            font-size: clamp(1.5rem, 2.5vw, 1.8rem);
            font-weight: 600;
            padding-bottom: 0.75rem;
            text-align: center;
            width: 100%;
            border-bottom: 2px solid var(--primary-color);
            font-weight: bold;
        }

        .dashboard-header h3 {
            margin: 0.75rem 0 0 0;
            color: red;
            font-size: clamp(1.2rem, 2vw, 1.5rem);
            font-weight: 500;
            text-align: center;
            width: 100%;
            font-weight: bold;  
        }

        .logo-container {
            margin-bottom: 1rem;
        }

        .logo {
            max-height: 80px;
            width: auto;
        }

        .dashboard-grid {
            display: flex;
            flex-direction: column;
            gap: 2rem;
            width: 100%;
        }

        .status-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 1.5rem;
            width: 100%;
        }

        .status-card {
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            padding: 1.5rem;
            display: flex;
            flex-direction: column;
            text-align: center;
            transition: var(--transition);
        }

        .status-card:hover {
            transform: translateY(-5px);
        }

        .status-card.active {
            border-top: 4px solid var(--success-color);
        }

        .status-card.service {
            border-top: 4px solid var(--primary-color);
        }

        .status-card.expired {
            border-top: 4px solid var(--danger-color);
        }

        .status-card.soon {
            border-top: 4px solid var(--warning-color);
        }

        .status-card h3 {
            margin: 0 0 0.5rem 0;
            font-size: 1.1rem;
            color: var(--text-secondary);
        }

        .status-card .count {
            font-size: 2.5rem;
            font-weight: 700;
            margin: 0.5rem 0;
            color: var(--text-color);
        }

        .plants-section {
            background-color: #fff;
            padding: clamp(1.5rem, 3vw, 2rem);
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            width: 100%;
            margin: 2rem auto 0;
            box-sizing: border-box;
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
            grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            gap: 1.5rem;
            width: 100%;
        }

        .plant-card {
            background-color: #fff;
            border-radius: var(--radius);
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
            padding: 1.5rem;
            display: flex;
            flex-direction: column;
            transition: var(--transition);
            border: 1px solid var(--border-color);
            align-items: center;
        }

        .plant-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 6px 12px rgba(0, 0, 0, 0.1);
        }

        .plant-header {
            margin-bottom: 1.5rem;
            padding-bottom: 0.75rem;
            border-bottom: 2px solid var(--primary-color);
        }

        .plant-title {
            margin: 0;
            color: var(--text-color);
            font-size: 1.2rem;
            font-weight: 600;
        }

        .plant-stats {
            display: flex;
            flex-direction: column;
            align-items: flex-start;
            gap: 0.75rem;
            text-align: left;
            width: 100%;
        }

        .stat-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            width: 100%;
            padding: 0.3rem 0;
            border-bottom: 1px dashed var(--border-color);
        }

        .stat-item:last-child {
            border-bottom: none;
        }

        .stat-label {
            margin: 0;
            color: var(--text-secondary);
            font-size: 0.9rem;
        }

        .stat-value {
            margin: 0;
            color: var(--text-color);
            font-size: 1rem;
            font-weight: 600;
            min-width: 2.5rem;
            text-align: right;
        }

        .stat-value.active {
            color: var(--success-color);
        }

        .stat-value.service {
            color: var(--primary-color);
        }

        .stat-value.expired {
            color: var(--danger-color);
        }

        .stat-value.expiring {
            color: var(--warning-color);
        }

        .stat-item.expiry-date-item {
            margin-top: 0.5rem;
            padding-top: 0.5rem;
            border-top: 1px solid var(--primary-color);
            border-bottom: none;
        }

        .stat-value.next-expiry {
            color: var(--primary-color);
            font-family: 'Courier New', monospace;
            letter-spacing: 0.5px;
        }

        .login-section {
            background-color: #fff;
            padding: clamp(1.5rem, 3vw, 2rem);
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            width: 100%;
            margin: 2rem auto 0;
            box-sizing: border-box;
            text-align: center;
        }

        .login-section h3 {
            margin: 0 0 1.5rem 0;
            color: var(--text-color);
            font-size: clamp(1.2rem, 2vw, 1.5rem);
            text-align: center;
            padding-bottom: 0.75rem;
            border-bottom: 2px solid var(--primary-color);
            width: 100%;
        }

        .login-section p {
            margin-bottom: 1.5rem;
            color: var(--text-secondary);
        }

        .btn-login {
            display: inline-block;
            padding: 10px 25px;
            background-color: var(--primary-color);
            color: white;
            text-decoration: none;
            border-radius: var(--radius);
            font-weight: 500;
            transition: var(--transition);
            margin: 0 10px;
        }

        .btn-login:hover {
            background-color: #0056b3;
            transform: translateY(-2px);
        }

        .footer {
            text-align: center;
            padding: 20px;
            margin-top: 40px;
            color: var(--text-secondary);
            font-size: 0.9rem;
        }

        /* Chart sections */
        .chart-section {
            background-color: #fff;
            padding: clamp(1.5rem, 3vw, 2rem);
            border-radius: var(--radius);
            box-shadow: var(--card-shadow);
            width: 100%;
            margin: 0 auto;
            box-sizing: border-box;
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
            margin: 0 auto;
            position: relative;
        }

        .chart-row {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(450px, 1fr));
            gap: 2rem;
            width: 100%;
        }

        @media (max-width: 767px) {
            .chart-row {
                grid-template-columns: 1fr;
            }
        }
        
        /* Total FE Counter styles */
        .total-fe-container {
            width: 100%;
            margin: 0 0 2rem 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        .total-fe {
            background-color: #fff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            padding: 1.5rem;
            display: flex;
            flex-direction: column;
            text-align: center;
            transition: var(--transition);
            border-top: 4px solid #000000;
            width: 100%;
            box-sizing: border-box;
        }
        
        .total-fe:hover {
            transform: translateY(-5px);
        }
        
        .total-fe h3 {
            margin: 0 0 0.5rem 0;
            font-size: 1.1rem;
            color: var(--text-secondary);
        }
        
        .total-fe .count {
            font-size: 2.5rem;
            font-weight: 700;
            margin: 0.5rem 0;
            color: var(--text-color);
        }

        /* Add uniform container class */
        .uniform-width-container {
            width: 100%;
            max-width: 100%;
            margin-left: auto;
            margin-right: auto;
            box-sizing: border-box;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <asp:HiddenField ID="hdnTypeChartData" runat="server" />
        <asp:HiddenField ID="hdnExpiryChartData" runat="server" />
        <asp:HiddenField ID="hdnPlantChartData" runat="server" />
        <asp:HiddenField ID="hdnNextExpiryChartData" runat="server" />
        
        <div class="dashboard-container">
            <div class="dashboard-header uniform-width-container">
                <div style="display: flex; justify-content: flex-end; width: 100%;">
                    <a href="UserGuideHandler.ashx" class="btn-login" target="_blank" style="margin-bottom: 10px;">
                        <i class="fas fa-file-pdf"></i> User Guide
                    </a>
                </div>
                <div class="logo-container">
                    <img src="<%=ResolveUrl("~/Areas/FETS/Uploads/misc/logo.png")%>" alt="INARI Logo" class="logo" />
                </div>
                <h2>ENVIRONMENT, HEALTH AND SAFETY DEPARTMENT (EHS)</h2>
                <h3>FIRE EXTINGUISHER TRACKING DASHBOARD</h3>
            </div>

            <!-- Total FE counter section -->
            <div class="total-fe-container uniform-width-container">
                <div class="total-fe">
                    <h3>Total Fire Extinguishers</h3>
                    <div class="count"><asp:Literal ID="litTotalFE" runat="server"></asp:Literal></div>
                </div>
            </div>

            <div class="dashboard-grid">
                <div class="status-cards">
                    <div class="status-card active">
                        <h3>Active</h3>
                        <div class="count"><asp:Literal ID="litActiveCount" runat="server"></asp:Literal></div>
                    </div>
                    <div class="status-card service">
                        <h3>Under Service</h3>
                        <div class="count"><asp:Literal ID="litServiceCount" runat="server"></asp:Literal></div>
                    </div>
                    <div class="status-card expired">
                        <h3>Expired</h3>
                        <div class="count"><asp:Literal ID="litExpiredCount" runat="server"></asp:Literal></div>
                    </div>
                    <div class="status-card soon">
                        <h3>Expiring Soon</h3>
                        <div class="count"><asp:Literal ID="litExpiringSoonCount" runat="server"></asp:Literal></div>
                    </div>
                </div>

                <!-- New Chart Row -->
                <div class="chart-row">
                    <!-- Fire Extinguisher Type Distribution -->
                    <div class="chart-section">
                        <h3>Fire Extinguisher Type Distribution</h3>
                        <div class="chart-container">
                            <canvas id="typeDistributionChart"></canvas>
                        </div>
                    </div>
                    
                    <!-- Plant Comparison Chart (replacing Monthly Expirations) -->
                    <div class="chart-section">
                        <h3>Plant Distribution</h3>
                        <div class="chart-container">
                            <canvas id="plantComparisonChart"></canvas>
                        </div>
                    </div>
                </div>

                <!-- Next Expiry Dates Chart -->
                <div class="chart-section">
                    <h3>Next Expiry Date</h3>
                    <div class="chart-container">
                        <canvas id="nextExpiryChart"></canvas>
                    </div>
                </div>

                <div class="plants-section">
                <h3>Plant Statistics </h3>
                    <div class="plants-grid">
                        <asp:Repeater ID="rptPlants" runat="server">
                            <ItemTemplate>
                                <div class="plant-card">
                                    <div class="plant-header">
                                        <h4 class="plant-title"><%# Eval("PlantName") %></h4>
                                    </div>
                                    <div class="plant-stats">
                                        <div class="stat-item">
                                            <span class="stat-label">Total Extinguishers:</span>
                                            <span class="stat-value"><%# Eval("TotalFE") %></span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-label">Active:</span>
                                            <span class="stat-value active"><%# Eval("InUse") %></span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-label">Under Service:</span>
                                            <span class="stat-value service"><%# Eval("UnderService") %></span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-label">Expired:</span>
                                            <span class="stat-value expired"><%# Eval("Expired") %></span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-label">Expiring Soon:</span>
                                            <span class="stat-value expiring"><%# Eval("ExpiringSoon") %></span>
                                        </div>
                                        <div class="stat-item expiry-date-item">
                                            <span class="stat-label">Next Expiry:</span>
                                            <span class="stat-value next-expiry"><%# Eval("NextExpiryDateFormatted") %></span>
                                        </div>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>

                <div class="login-section">
                    <h3>Authorized Access</h3>
                    <p>Log in to access complete system features including data entry, map view, and service management.</p>
                    <a href="<%=ResolveUrl("~/Areas/FETS/Pages/Login/Login.aspx")%>" class="btn-login">Login to System</a>
                </div>
            </div>

            <div class="footer">
                <p>&copy; <%= DateTime.Now.Year %> INARI AMERTRON BHD. - Environment, Health and Safety Department (EHS)</p>
            </div>
        </div>

        <script>
            document.addEventListener('DOMContentLoaded', function() {
                // Initialize the Fire Extinguisher Type Distribution Chart
                const typeChartData = JSON.parse(document.getElementById('<%= hdnTypeChartData.ClientID %>').value || '{"labels":[],"data":[]}');
                const typeCtx = document.getElementById('typeDistributionChart').getContext('2d');
                
                new Chart(typeCtx, {
                    type: 'doughnut',
                    data: {
                        labels: typeChartData.labels,
                        datasets: [{
                            data: typeChartData.data,
                            backgroundColor: [
                                '#3498db',  // blue
                                '#2ecc71',  // green
                                '#e74c3c',  // red
                                '#f39c12',  // orange
                                '#9b59b6'   // purple
                            ],
                            borderWidth: 2,
                            borderColor: '#ffffff'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        cutout: '65%',
                        plugins: {
                            legend: {
                                position: 'bottom',
                                labels: {
                                    padding: 20,
                                    font: {
                                        size: 12,
                                        weight: '500'
                                    }
                                }
                            },
                            tooltip: {
                                callbacks: {
                                    label: function(context) {
                                        const label = context.label || '';
                                        const value = context.raw || 0;
                                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                        const percentage = Math.round((value / total) * 100);
                                        return ` ${label}: ${value} units (${percentage}%)`;
                                    }
                                }
                            }
                        }
                    }
                });

                // Initialize Plant Comparison Chart
                const plantChartData = JSON.parse(document.getElementById('<%= hdnPlantChartData.ClientID %>').value || '{"labels":[],"totalData":[],"abcData":[],"co2Data":[]}');
                const plantCtx = document.getElementById('plantComparisonChart').getContext('2d');
                
                new Chart(plantCtx, {
                    type: 'bar',
                    data: {
                        labels: plantChartData.labels,
                        datasets: [
                            {
                                label: 'ABC',
                                data: plantChartData.abcData,
                                backgroundColor: 'rgba(52, 152, 219, 0.8)',
                                borderColor: 'rgb(52, 152, 219)',
                                borderWidth: 1,
                                borderRadius: 4,
                                barPercentage: 0.8
                            },
                            {
                                label: 'CO2',
                                data: plantChartData.co2Data,
                                backgroundColor: 'rgba(46, 204, 113, 0.8)',
                                borderColor: 'rgb(46, 204, 113)',
                                borderWidth: 1,
                                borderRadius: 4,
                                barPercentage: 0.8
                            }
                        ]
                    },
                    options: {
                        indexAxis: 'x',
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: 'top',
                                labels: {
                                    font: {
                                        size: 12
                                    }
                                }
                            },
                            tooltip: {
                                callbacks: {
                                    afterTitle: function(context) {
                                        const idx = context[0].dataIndex;
                                        const total = plantChartData.totalData[idx];
                                        return `Total: ${total}`;
                                    }
                                }
                            }
                        },
                        scales: {
                            x: {
                                stacked: true,
                                title: {
                                    display: true,
                                    text: 'Plants'
                                }
                            },
                            y: {
                                stacked: true,
                                beginAtZero: true,
                                ticks: {
                                    precision: 0
                                },
                                title: {
                                    display: true,
                                    text: 'Number of Fire Extinguishers'
                                }
                            }
                        }
                    }
                });
                
                // Initialize Next Expiry Date Chart
                const nextExpiryChartData = JSON.parse(document.getElementById('<%= hdnNextExpiryChartData.ClientID %>').value || '{"labels":[],"expiryDates":[],"daysTillExpiry":[]}');
                const nextExpiryCtx = document.getElementById('nextExpiryChart').getContext('2d');
                
                new Chart(nextExpiryCtx, {
                    type: 'bar',
                    data: {
                        labels: nextExpiryChartData.labels,
                        datasets: [{
                            label: 'Days Until Expiry',
                            data: nextExpiryChartData.daysTillExpiry,
                            backgroundColor: function(context) {
                                const value = context.dataset.data[context.dataIndex];
                                // Return color based on days remaining
                                if (value < 30) return 'rgba(220, 53, 69, 0.8)'; // Red for < 30 days
                                if (value < 60) return 'rgba(255, 193, 7, 0.8)'; // Yellow for < 60 days
                                return 'rgba(40, 167, 69, 0.8)'; // Green for > 60 days
                            },
                            borderWidth: 1,
                            borderRadius: 4
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        indexAxis: 'y',  // Horizontal bars
                        plugins: {
                            legend: {
                                display: false
                            },
                            tooltip: {
                                callbacks: {
                                    title: function(context) {
                                        return context[0].label;
                                    },
                                    label: function(context) {
                                        const idx = context.dataIndex;
                                        const days = Math.round(nextExpiryChartData.daysTillExpiry[idx]);
                                        const expiryDate = nextExpiryChartData.expiryDates[idx];
                                        return [
                                            `Expires: ${expiryDate}`,
                                            `Days remaining: ${days}`
                                        ];
                                    }
                                }
                            }
                        },
                        scales: {
                            x: {
                                beginAtZero: true,
                                title: {
                                    display: true,
                                    text: 'Days Until Expiry'
                                }
                            },
                            y: {
                                title: {
                                    display: true,
                                    text: 'Plants'
                                }
                            }
                        }
                    }
                });
                
                // Add color legend below the Next Expiry Chart
                const legendContainer = document.createElement('div');
                legendContainer.style.display = 'flex';
                legendContainer.style.justifyContent = 'center';
                legendContainer.style.marginTop = '15px';
                legendContainer.style.gap = '15px';
                legendContainer.style.flexWrap = 'wrap';
                
                const legendItems = [
                    { color: 'rgba(220, 53, 69, 0.8)', text: 'Less than 30 days' },
                    { color: 'rgba(255, 193, 7, 0.8)', text: '30-60 days' },
                    { color: 'rgba(40, 167, 69, 0.8)', text: 'More than 60 days' }
                ];
                
                legendItems.forEach(item => {
                    const legendItem = document.createElement('div');
                    legendItem.style.display = 'flex';
                    legendItem.style.alignItems = 'center';
                    legendItem.style.gap = '5px';
                    
                    const colorBox = document.createElement('div');
                    colorBox.style.width = '16px';
                    colorBox.style.height = '16px';
                    colorBox.style.backgroundColor = item.color;
                    colorBox.style.borderRadius = '3px';
                    
                    const text = document.createElement('span');
                    text.textContent = item.text;
                    text.style.fontSize = '12px';
                    text.style.color = 'var(--text-secondary)';
                    
                    legendItem.appendChild(colorBox);
                    legendItem.appendChild(text);
                    legendContainer.appendChild(legendItem);
                });
                
                document.getElementById('nextExpiryChart').parentNode.appendChild(legendContainer);
            });
        </script>
    </form>
</body>
</html> 