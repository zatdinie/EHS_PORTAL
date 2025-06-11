USE [ESH]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlantMonitoring_Expiry_Email] AS 
BEGIN 
SET NOCOUNT ON;

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

-- Count statistics
DECLARE @totalCount int = (SELECT COUNT(*) FROM #ExpiringItems);
DECLARE @criticalCount int = (SELECT COUNT(*) FROM #ExpiringItems WHERE SeverityClass = 'critical');
DECLARE @warningCount int = (SELECT COUNT(*) FROM #ExpiringItems WHERE SeverityClass = 'warning');
DECLARE @infoCount int = (SELECT COUNT(*) FROM #ExpiringItems WHERE SeverityClass = 'info');

-- Exit if no expiring items
IF @totalCount = 0
BEGIN
    DROP TABLE #ExpiringItems;
    RETURN;
END

-- Get distinct plants with expiring items
SELECT DISTINCT PlantID, PlantName
INTO #AffectedPlants
FROM #ExpiringItems;

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
    -- Get items for this specific plant only
    SELECT *
    INTO #PlantSpecificItems
    FROM #ExpiringItems
    WHERE PlantID = @plantID;
    
    -- Count statistics for this plant only
    DECLARE @plantItemCount int = (SELECT COUNT(*) FROM #PlantSpecificItems);
    DECLARE @plantCriticalCount int = (SELECT COUNT(*) FROM #PlantSpecificItems WHERE SeverityClass = 'critical');
    DECLARE @plantWarningCount int = (SELECT COUNT(*) FROM #PlantSpecificItems WHERE SeverityClass = 'warning');
    DECLARE @plantInfoCount int = (SELECT COUNT(*) FROM #PlantSpecificItems WHERE SeverityClass = 'info');
    
    -- Generate HTML rows for this plant only
    DECLARE @itemRowsHTML nvarchar(max) = '';
    SELECT @itemRowsHTML = @itemRowsHTML + '
        <tr class="' + SeverityClass + '-row">
            <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><strong>ID:</strong> ' + CAST(Id AS varchar) + '</td>
            <td style="padding: 8px; border: 1px solid #ddd;"><strong>Plant:</strong> ' + PlantName + '</td>
            <td style="padding: 8px; border: 1px solid #ddd;"><strong>Monitoring:</strong> ' + MonitoringName + '</td>
            <td style="padding: 8px; border: 1px solid #ddd;"><strong>Category:</strong> ' + MonitoringCategory + '</td>
            <td style="padding: 8px; border: 1px solid #ddd;"><strong>Area:</strong> ' + ISNULL(Area, '') + '</td>
            <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><strong>Expiry:</strong> ' + CONVERT(varchar, ExpDate, 103) + '</td>
            <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><strong>Days Left:</strong> ' + CAST(DaysUntilExpiry AS varchar) + '</td>
            <td style="padding: 8px; border: 1px solid #ddd;"><strong>Remarks:</strong> ' + ISNULL(Remarks, '') + '</td>
            <td style="padding: 8px; text-align: center; border: 1px solid #ddd;"><a href="' + @baseUrl + CAST(Id AS varchar) + '" style="display: inline-block; padding: 5px 10px; background-color: #337ab7; color: white; text-decoration: none; border-radius: 3px; font-size: 12px;">View Details</a></td>
        </tr>'
    FROM #PlantSpecificItems
    ORDER BY DaysUntilExpiry;

    -- Create email content in smaller chunks to avoid truncation
    DECLARE @htmlHeader nvarchar(max) = '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Plant Monitoring Expiry Notification</title>
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
            border: 1px solid #ddd;
        }
        th, td {
            padding: 8px;
            text-align: left;
            border: 1px solid #ddd;
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
        .remarks {
            background-color: #f5f5f5;
            padding: 10px;
            border-left: 3px solid #337ab7;
            margin: 15px 0;
        }
        .action-button {
            display: inline-block;
            padding: 10px 15px;
            background-color: #337ab7;
            color: white;
            text-decoration: none;
            border-radius: 4px;
            margin-top: 15px;
        }
        .action-button:hover {
            background-color: #23527c;
        }
        .column-label {
            font-weight: bold;
            display: block;
            margin-bottom: 3px;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Plant Monitoring Expiry Alert</h1>
            <p>' + @plantName + ' - ' + CONVERT(varchar, GETDATE(), 103) + '</p>
        </div>
        
        <div class="content">
            
            <p>Dear Team,</p>
            
            <p>This is an <strong>important notification</strong> regarding plant monitoring items for <strong>' + @plantName + '</strong> that will expire within the next 30 days. Please take action to ensure continued compliance.</p>
            
            <div class="summary-box">
                <h3>Summary:</h3>
                <div class="summary-item critical-count">Critical (≤7 days): ' + CAST(@plantCriticalCount AS varchar) + '</div>
                <div class="summary-item warning-count">Warning (8-15 days): ' + CAST(@plantWarningCount AS varchar) + '</div>
                <div class="summary-item info-count">Upcoming (16-30 days): ' + CAST(@plantInfoCount AS varchar) + '</div>
                <div class="summary-item">Total: ' + CAST(@plantItemCount AS varchar) + '</div>
            </div>
            
            <h2>Expiring Plant Monitoring Items:</h2>
            
            <table border="1" cellpadding="8" cellspacing="0" style="width: 100%; border-collapse: collapse; border: 1px solid #ddd;">
                <thead>
                    <tr>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 5%;">ID</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Plant</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 15%;">Monitoring Name</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Category</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Area</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Expiry Date</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 8%;">Days Left</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 22%;">Remarks</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Action</th>
                    </tr>
                </thead>
                <tbody>';

    DECLARE @htmlFooter nvarchar(max) = '
                </tbody>
            </table>
            
            <div class="remarks">
                <p><strong>Required Action:</strong></p>
                <p>Please schedule maintenance or renewal for these items before their expiry dates to maintain compliance with safety regulations.</p>
                <p><strong>Color Code:</strong></p>
                <ul>
                    <li><span style="color: #a94442; font-weight: bold;">Red</span> - Critical (7 days or less)</li>
                    <li><span style="color: #8a6d3b; font-weight: bold;">Yellow</span> - Warning (8-15 days)</li>
                    <li><span style="color: #31708f;">Blue</span> - Upcoming (16-30 days)</li>
                </ul>
            </div>
            
            <a href="https://inariportal.inari-amertron.com.my/CLIP/PlantMonitoring?plantId=' + CAST(@plantID AS varchar) + '" class="action-button">Go to Plant Monitoring System</a>
            
        </div>
        
        <div class="footer">
            <p>This is an automated message from the Plant Monitoring System.</p>
            <p>' + CAST(YEAR(GETDATE()) AS varchar) + ' INARI AMERTRON BHD. - Environment, Health and Safety Department (EHS)</p>
        </div>
    </div>
</body>
</html>';

    -- Combine HTML parts
    DECLARE @htmlContent nvarchar(max) = @htmlHeader + @itemRowsHTML + @htmlFooter;

    -- Get email list for users assigned to this plant
    DECLARE @plantEmailList varchar(max) = '';
    
    SELECT @plantEmailList = @plantEmailList + U.Email + '; '
    FROM [ESH].[CLIP].[AspNetUsers] U
    INNER JOIN [ESH].[CLIP].[UserPlants] UP ON U.Id = UP.UserId
    WHERE UP.PlantId = @plantID
    AND U.Email IS NOT NULL;
    
    -- Remove trailing separator
    IF LEN(@plantEmailList) > 0
    BEGIN
        SET @plantEmailList = LEFT(@plantEmailList, LEN(@plantEmailList) - 2);
        
        -- Convert counts to variables to avoid CAST in dynamic SQL
        DECLARE @itemCountStr varchar(10) = CAST(@plantItemCount AS varchar);
        DECLARE @currentDate varchar(20) = CONVERT(varchar, GETDATE(), 103);
        
        -- Insert into email table with error handling
        BEGIN TRY
            -- Try to insert into AUTOREPORT.dbo.TAUTO_EMAIL
            DECLARE @sqlCmd nvarchar(max) = N'
            INSERT INTO [AUTOREPORT].[dbo].TAUTO_EMAIL (
                EMAIL_DESC, 
                TOLIST, 
                CCLIST, 
                EMAIL_TITLE, 
                EMAIL_CONTENT, 
                CREATE_BY, 
                CREATE_DATE, 
                UPDATE_FLAG
            ) 
            VALUES (
                ''Plant Monitoring Expiry Alert for ' + @plantName + ''', 
                ''' + @plantEmailList + ''', 
                '''', 
                ''Plant Monitoring Expiry Alert - ' + @plantName + ' - ' + @itemCountStr + ' items expiring soon (' + @currentDate + ')'', 
                ''' + REPLACE(@htmlContent, '''', '''''') + ''', 
                ''INARI PORTAL'', 
                GETDATE(), 
                ''N''
            )';
            
            EXEC sp_executesql @sqlCmd;
        END TRY
        BEGIN CATCH
            -- Log error to a table
            INSERT INTO [ESH].[FETS].[ErrorLog] (
                ErrorMessage,
                ErrorLine,
                ErrorProcedure,
                ErrorDateTime
            )
            VALUES (
                ERROR_MESSAGE(),
                ERROR_LINE(),
                ERROR_PROCEDURE(),
                GETDATE()
            );
            
            -- Try alternative method - direct email if available
            -- This is a placeholder for an alternative email method
            -- You might want to implement a direct email sending mechanism here
            -- using sp_send_dbmail or a custom CLR procedure
        END CATCH
    END
    
    -- Drop the temporary table for this plant
    DROP TABLE #PlantSpecificItems;
    
    FETCH NEXT FROM plant_cursor INTO @plantID, @plantName;
END

CLOSE plant_cursor;
DEALLOCATE plant_cursor;

-- Clean up
DROP TABLE #ExpiringItems;
DROP TABLE #AffectedPlants;

END 