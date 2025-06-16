# 🛡️ EHS Portal

## 🌟 Overview
The EHS (Environment, Health, and Safety) Portal is a comprehensive web application built with ASP.NET MVC that manages safety compliance and monitoring across multiple plants. The system includes two main modules:

- 🔍 **CLIP (Certification & Licensing Inspection Portal)**: Manages certificates of fitness, competency tracking, and plant monitoring
- 🧯 **FETS (Fire Equipment Tracking System)**: Tracks and manages fire safety equipment and related activities

## ✨ Features

### CLIP Module
- 📋 Certificate of Fitness management with expiry tracking
- 🏭 Multi-plant support with user-plant associations
- 📊 Monitoring and notification system
- 🎓 Competency tracking and training management
- 📅 Automatic status updates for certificates (Active, Expiring Soon, Expired)

### FETS Module
- 🧯 Fire extinguisher inventory and maintenance
- 📝 Activity logging for audit trails
- 👥 Role-based access control
- 📧 Email notifications
- 📱 Responsive interface for desktop and mobile access

## 🛠️ Technology Stack
- **Framework**: ASP.NET MVC
- **Database**: SQL Server
- **Frontend**: HTML5, CSS3, JavaScript
- **Authentication**: ASP.NET Identity
- **ORM**: Entity Framework

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2019 or newer
- .NET Framework 4.7.2 or newer
- SQL Server 2016 or newer

### Installation
1. Clone this repository
2. Open the solution file `EHS_PORTAL.sln` in Visual Studio
3. Restore NuGet packages
4. Update the connection string in `Web.config` to point to your database
5. Run database migrations using Package Manager Console:
   ```
   Update-Database -ContextTypeName ApplicationDbContext
   ```
6. Build and run the application

## 🔒 Security
The application implements role-based security with the following roles:
- Administrator: Full system access
- Plant Manager: Access to specific plant data
- User: Limited access to view and report

## 📊 Reporting
Generate comprehensive reports on:
- Certificate expiry status
- Compliance metrics
- Training completion rates
- Fire equipment inspection status

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License
This project is licensed under the [Your License] License - see the LICENSE file for details.
