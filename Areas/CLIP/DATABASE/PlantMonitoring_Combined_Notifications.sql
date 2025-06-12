USE [ESH]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlantMonitoring_Combined_Notifications] AS 
BEGIN 
SET NOCOUNT ON;

-- PART 1: EXPIRING ITEMS SECTION
-- Find expiring items in plant monitoring
SELECT 
    PM.Id,
    PM.PlantID,
    PM.Area,
    P.PlantName,
    M.MonitoringName,
    M.MonitoringCategory,
    PM.ExpDate,
    DATEDIFF(DAY, GETDATE(), PM.ExpDate) AS DaysUntilExpiry,
    PM.Remarks,
    CASE 
        WHEN DATEDIFF(DAY, GETDATE(), PM.ExpDate) <= 7 THEN 'critical'
        WHEN DATEDIFF(DAY, GETDATE(), PM.ExpDate) <= 15 THEN 'warning'
        ELSE 'info'
    END AS SeverityClass
INTO #ExpiringItems
FROM 
    [ESH].[CLIP].[PlantMonitoring] PM
LEFT JOIN 
    [ESH].[CLIP].[Plants] P ON PM.PlantID = P.Id
LEFT JOIN 
    [ESH].[CLIP].[Monitoring] M ON PM.MonitoringID = M.MonitoringID
WHERE 
    PM.ExpDate IS NOT NULL
    AND DATEDIFF(DAY, GETDATE(), PM.ExpDate) <= 30
    AND DATEDIFF(DAY, GETDATE(), PM.ExpDate) >= 0
ORDER BY 
    DaysUntilExpiry;

-- PART 2: STATUS UPDATES SECTION
-- Create a temporary table to store status changes from the last 24 hours
SELECT 
    PM.Id,
    PM.PlantID,
    PM.MonitoringID,
    PM.Area,
    P.PlantName,
    M.MonitoringName,
    M.MonitoringCategory,
    PM.ProcStatus,
    PM.ExpDate,
    PM.QuoteDate,
    PM.QuoteCompleteDate,
    PM.QuoteUserAssign,
    PM.EprDate,
    PM.EprCompleteDate,
    PM.EprUserAssign,
    PM.WorkDate,
    PM.WorkCompleteDate,
    PM.WorkUserAssign,
    PM.Remarks,
    CASE 
        WHEN PM.WorkCompleteDate IS NOT NULL THEN 'Completed'
        WHEN PM.WorkDate IS NOT NULL THEN 'Work In Progress'
        WHEN PM.EprDate IS NOT NULL THEN 'ePR Raised'
        WHEN PM.QuoteDate IS NOT NULL THEN 'Quotation Requested'
        ELSE 'Not Started'
    END AS CurrentStatus,
    CASE
        WHEN (PM.QuoteDate IS NOT NULL AND DATEDIFF(HOUR, PM.QuoteDate, GETDATE()) <= 24)
            OR (PM.QuoteCompleteDate IS NOT NULL AND DATEDIFF(HOUR, PM.QuoteCompleteDate, GETDATE()) <= 24)
            OR (PM.EprDate IS NOT NULL AND DATEDIFF(HOUR, PM.EprDate, GETDATE()) <= 24)
            OR (PM.EprCompleteDate IS NOT NULL AND DATEDIFF(HOUR, PM.EprCompleteDate, GETDATE()) <= 24)
            OR (PM.WorkDate IS NOT NULL AND DATEDIFF(HOUR, PM.WorkDate, GETDATE()) <= 24)
            OR (PM.WorkCompleteDate IS NOT NULL AND DATEDIFF(HOUR, PM.WorkCompleteDate, GETDATE()) <= 24)
        THEN 1
        ELSE 0
    END AS HasRecentChanges
INTO #StatusItems
FROM 
    [ESH].[CLIP].[PlantMonitoring] PM
LEFT JOIN 
    [ESH].[CLIP].[Plants] P ON PM.PlantID = P.Id
LEFT JOIN 
    [ESH].[CLIP].[Monitoring] M ON PM.MonitoringID = M.MonitoringID;

-- Get all plants that need notifications (either expiring items or status updates)
SELECT DISTINCT PlantID, PlantName
INTO #AffectedPlants
FROM (
    SELECT PlantID, PlantName FROM #ExpiringItems
    UNION
    SELECT PlantID, PlantName FROM #StatusItems WHERE HasRecentChanges = 1
) AS CombinedPlants;

-- Count statistics for overall report
DECLARE @totalExpiringCount int = (SELECT COUNT(*) FROM #ExpiringItems);
DECLARE @criticalCount int = (SELECT COUNT(*) FROM #ExpiringItems WHERE SeverityClass = 'critical');
DECLARE @warningCount int = (SELECT COUNT(*) FROM #ExpiringItems WHERE SeverityClass = 'warning');
DECLARE @infoCount int = (SELECT COUNT(*) FROM #ExpiringItems WHERE SeverityClass = 'info');
DECLARE @recentChangesCount int = (SELECT COUNT(*) FROM #StatusItems WHERE HasRecentChanges = 1);

-- Exit if no items to report
IF @totalExpiringCount = 0 AND @recentChangesCount = 0
BEGIN
    DROP TABLE #ExpiringItems;
    DROP TABLE #StatusItems;
    DROP TABLE #AffectedPlants;
    RETURN;
END

-- Define base URL for the application
DECLARE @baseUrl nvarchar(100) = 'https://inariportal.inari-amertron.com.my/CLIP/PlantMonitoring/Details/';

-- Loop through each affected plant and send emails to users assigned to that plant
DECLARE @plantID int;
DECLARE @plantName nvarchar(100);

DECLARE plant_cursor CURSOR FOR 
SELECT PlantID, PlantName FROM #AffectedPlants;

OPEN plant_cursor;
FETCH NEXT FROM plant_cursor INTO @plantID, @plantName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Get items for this specific plant
    SELECT *
    INTO #PlantExpiringItems
    FROM #ExpiringItems
    WHERE PlantID = @plantID;
    
    SELECT *
    INTO #PlantStatusItems
    FROM #StatusItems
    WHERE PlantID = @plantID;
    
    -- Count statistics for this plant
    DECLARE @plantExpiringCount int = (SELECT COUNT(*) FROM #PlantExpiringItems);
    DECLARE @plantCriticalCount int = (SELECT COUNT(*) FROM #PlantExpiringItems WHERE SeverityClass = 'critical');
    DECLARE @plantWarningCount int = (SELECT COUNT(*) FROM #PlantExpiringItems WHERE SeverityClass = 'warning');
    DECLARE @plantInfoCount int = (SELECT COUNT(*) FROM #PlantExpiringItems WHERE SeverityClass = 'info');
    DECLARE @plantRecentChanges int = (SELECT COUNT(*) FROM #PlantStatusItems WHERE HasRecentChanges = 1);
    
    -- Only proceed if there are items to report for this plant
    IF @plantExpiringCount > 0 OR @plantRecentChanges > 0
    BEGIN
        -- Generate HTML for expiring items
        DECLARE @expiringItemsHTML nvarchar(max) = '';
        
        IF @plantExpiringCount > 0
        BEGIN
            SELECT @expiringItemsHTML = @expiringItemsHTML + '
                <tr class="' + SeverityClass + '-row">
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + CAST(Id AS varchar) + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + MonitoringName + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + MonitoringCategory + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + ISNULL(Area, '') + '</td>
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + CONVERT(varchar, ExpDate, 103) + '</td>
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + CAST(DaysUntilExpiry AS varchar) + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + ISNULL(Remarks, '') + '</td>
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><a href="' + @baseUrl + CAST(Id AS varchar) + '" style="display: inline-block; padding: 5px 10px; background-color: #337ab7; color: white; text-decoration: none; border-radius: 3px; font-size: 12px;">View Details</a></td>
                </tr>'
            FROM #PlantExpiringItems
            ORDER BY DaysUntilExpiry;
        END
        
        -- Generate HTML for recent status changes
        DECLARE @recentChangesHTML nvarchar(max) = '';
        
        IF @plantRecentChanges > 0
        BEGIN
            SELECT @recentChangesHTML = @recentChangesHTML + '
                <tr style="background-color: #dff0d8;">
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + CAST(Id AS varchar) + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + MonitoringName + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + MonitoringCategory + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + ISNULL(Area, '') + '</td>
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><span style="font-weight: bold; color: #3c763d;">' + CurrentStatus + '</span></td>
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + ISNULL(CONVERT(varchar, ExpDate, 103), 'N/A') + '</td>
                    <td style="padding: 8px; border: 1px solid #ddd;">' + 
                        CASE 
                            WHEN CurrentStatus = 'Quotation Requested' THEN ISNULL(QuoteUserAssign, 'Unassigned')
                            WHEN CurrentStatus = 'ePR Raised' THEN ISNULL(EprUserAssign, 'Unassigned')
                            WHEN CurrentStatus IN ('Work In Progress', 'Completed') THEN ISNULL(WorkUserAssign, 'Unassigned')
                            ELSE 'Unassigned'
                        END + '</td>
                    <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><a href="' + @baseUrl + CAST(Id AS varchar) + '" style="display: inline-block; padding: 5px 10px; background-color: #5cb85c; color: white; text-decoration: none; border-radius: 3px; font-size: 12px;">View Details</a></td>
                </tr>'
            FROM #PlantStatusItems
            WHERE HasRecentChanges = 1
            ORDER BY 
                CASE CurrentStatus
                    WHEN 'Not Started' THEN 1
                    WHEN 'Quotation Requested' THEN 2
                    WHEN 'ePR Raised' THEN 3
                    WHEN 'Work In Progress' THEN 4
                    WHEN 'Completed' THEN 5
                    ELSE 6
                END;
        END
        
        -- Create email content
        DECLARE @htmlContent nvarchar(max) = '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Plant Monitoring Notification</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
        }
        .container {
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
        }
        .header {
            background-color: #337ab7;
            color: white;
            padding: 15px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }
        .content {
            background-color: #f9f9f9;
            padding: 20px;
            border-left: 1px solid #dddddd;
            border-right: 1px solid #dddddd;
        }
        .footer {
            background-color: #eeeeee;
            padding: 15px;
            text-align: center;
            font-size: 12px;
            color: #777777;
            border-radius: 0 0 5px 5px;
            border: 1px solid #dddddd;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }
        th, td {
            padding: 8px 10px;
            text-align: left;
            border: 1px solid #dddddd;
        }
        th {
            background-color: #f2f2f2;
            font-weight: bold;
            text-align: center;
        }
        .critical-row {
            background-color: #f2dede;
        }
        .warning-row {
            background-color: #fcf8e3;
        }
        .info-row {
            background-color: #d9edf7;
        }
        .section-header {
            background-color: #337ab7;
            color: white;
            padding: 10px 15px;
            margin: 20px 0 10px 0;
            border-radius: 3px;
        }
        .summary-box {
            background-color: #f5f5f5;
            padding: 15px;
            margin: 15px 0;
            border-radius: 5px;
            border: 1px solid #dddddd;
        }
        .summary-item {
            display: inline-block;
            margin-right: 20px;
            padding: 8px 15px;
            border-radius: 4px;
        }
        .critical-count {
            background-color: #f2dede;
            color: #a94442;
            font-weight: bold;
        }
        .warning-count {
            background-color: #fcf8e3;
            color: #8a6d3b;
            font-weight: bold;
        }
        .info-count {
            background-color: #d9edf7;
            color: #31708f;
        }
        .status-update {
            background-color: #dff0d8;
            color: #3c763d;
            font-weight: bold;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Plant Monitoring Notification</h1>
            <p>' + @plantName + ' - ' + CONVERT(varchar, GETDATE(), 103) + '</p>
        </div>
        
        <div class="content">
            <p>Dear Team,</p>
            
            <p>This is an <strong>important notification</strong> regarding plant monitoring items for <strong>' + @plantName + '</strong>. Please review the information below and take appropriate action.</p>';
            
        -- Add expiring items section if there are any
        IF @plantExpiringCount > 0
        BEGIN
            SET @htmlContent = @htmlContent + '
            <div class="section-header">
                <h2 style="margin: 0;">Expiring Monitoring Items</h2>
            </div>
            
            <div class="summary-box">
                <h3>Expiry Summary:</h3>
                <div class="summary-item critical-count">Critical (≤7 days): ' + CAST(@plantCriticalCount AS varchar) + '</div>
                <div class="summary-item warning-count">Warning (8-15 days): ' + CAST(@plantWarningCount AS varchar) + '</div>
                <div class="summary-item info-count">Upcoming (16-30 days): ' + CAST(@plantInfoCount AS varchar) + '</div>
                <div class="summary-item">Total: ' + CAST(@plantExpiringCount AS varchar) + '</div>
            </div>
            
            <table border="1" cellpadding="8" cellspacing="0" style="width: 100%; border-collapse: collapse; border: 1px solid #ddd;">
                <thead>
                    <tr>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">ID</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Monitoring Name</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Category</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Area</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Expiry Date</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Days Left</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Remarks</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Action</th>
                    </tr>
                </thead>
                <tbody>
                    ' + @expiringItemsHTML + '
                </tbody>
            </table>';
        END
        
        -- Add status updates section if there are any
        IF @plantRecentChanges > 0
        BEGIN
            SET @htmlContent = @htmlContent + '
            <div class="section-header">
                <h2 style="margin: 0;">Recent Status Updates (Last 24 Hours)</h2>
            </div>
            
            <div class="summary-box">
                <div class="summary-item status-update">Status Updates: ' + CAST(@plantRecentChanges AS varchar) + '</div>
            </div>
            
            <table border="1" cellpadding="8" cellspacing="0" style="width: 100%; border-collapse: collapse; border: 1px solid #ddd;">
                <thead>
                    <tr>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">ID</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Monitoring Name</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Category</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Area</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Status</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Expiry Date</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Assigned To</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center;">Action</th>
                    </tr>
                </thead>
                <tbody>
                    ' + @recentChangesHTML + '
                </tbody>
            </table>';
        END
        
        -- Close out the HTML
        SET @htmlContent = @htmlContent + '
            <p>Please review these items and take appropriate action. You can click on the "View Details" button to access the full details of each item.</p>
            
            <p>Thank you for your attention to these important matters.</p>
            
            <p>Best regards,<br>EHS Portal System</p>
        </div>
        
        <div class="footer">
            <p>This is an automated message from the EHS Portal. Please do not reply to this email.</p>
            <p>© ' + CAST(YEAR(GETDATE()) AS varchar) + ' Inari Amertron Berhad - Environment, Health and Safety Department</p>
        </div>
    </div>
</body>
</html>';

        -- Get users assigned to this plant
        DECLARE @recipients nvarchar(max) = '';
        
        SELECT @recipients = @recipients + Email + ' ; '
        FROM [ESH].[CLIP].[UserPlants] UP
        JOIN [ESH].[dbo].[AspNetUsers] U ON UP.UserId = U.Id
        WHERE UP.PlantId = @plantID AND U.Email IS NOT NULL;
        
        -- Add admin recipients
        SELECT @recipients = @recipients + Email + ' ; '
        FROM [ESH].[dbo].[AspNetUsers] U
        JOIN [ESH].[dbo].[AspNetUserRoles] UR ON U.Id = UR.UserId
        JOIN [ESH].[dbo].[AspNetRoles] R ON UR.RoleId = R.Id
        WHERE R.Name = 'Admin' AND U.Email IS NOT NULL;
        
        -- Remove trailing semicolon
        IF LEN(@recipients) > 0
            SET @recipients = LEFT(@recipients, LEN(@recipients) - 1);
        
        -- Send email if we have recipients
        IF LEN(@recipients) > 0
        BEGIN
            DECLARE @subject nvarchar(255) = 'Plant Monitoring Notification - ' + @plantName + ' - ' + CONVERT(varchar, GETDATE(), 103);
            
            -- Use Database Mail to send the email
            EXEC msdb.dbo.sp_send_dbmail
                @profile_name = 'EHS_Mail_Profile',
                @recipients = @recipients,
                @subject = @subject,
                @body = @htmlContent,
                @body_format = 'HTML';
        END
    END
    
    -- Clean up temporary tables for this plant
    DROP TABLE #PlantExpiringItems;
    DROP TABLE #PlantStatusItems;
    
    -- Get next plant
    FETCH NEXT FROM plant_cursor INTO @plantID, @plantName;
END

-- Clean up
CLOSE plant_cursor;
DEALLOCATE plant_cursor;
DROP TABLE #ExpiringItems;
DROP TABLE #StatusItems;
DROP TABLE #AffectedPlants;

END 