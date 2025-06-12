USE [ESH]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlantMonitoring_Status_Updates] AS 
BEGIN 
SET NOCOUNT ON;

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
INTO #AllItems
FROM 
    [ESH].[CLIP].[PlantMonitoring] PM
LEFT JOIN 
    [ESH].[CLIP].[Plants] P ON PM.PlantID = P.Id
LEFT JOIN 
    [ESH].[CLIP].[Monitoring] M ON PM.MonitoringID = M.MonitoringID
WHERE 
    -- Only include items that are not completed
    PM.WorkCompleteDate IS NULL;

-- Get distinct plants with incomplete items
SELECT DISTINCT PlantID, PlantName
INTO #AffectedPlants
FROM #AllItems;

-- Define base URL for the application
DECLARE @baseUrl nvarchar(100) = 'https://inariportal.inari-amertron.com.my/CLIP/PlantMonitoring/Details/';

-- Count how many items we have
DECLARE @totalItems int = (SELECT COUNT(*) FROM #AllItems);
DECLARE @recentChanges int = (SELECT COUNT(*) FROM #AllItems WHERE HasRecentChanges = 1);

-- Exit if no items
IF @totalItems = 0
BEGIN
    DROP TABLE #AllItems;
    DROP TABLE #AffectedPlants;
    RETURN;
END

-- Loop through each affected plant and send emails to users assigned to that plant
DECLARE @plantID int;
DECLARE @plantName nvarchar(100);

DECLARE plant_cursor CURSOR FOR 
SELECT PlantID, PlantName FROM #AffectedPlants;

OPEN plant_cursor;
FETCH NEXT FROM plant_cursor INTO @plantID, @plantName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Get items for this plant
    SELECT *
    INTO #PlantItems
    FROM #AllItems
    WHERE PlantID = @plantID;
    
    -- Count items for this plant
    DECLARE @plantItemCount int = (SELECT COUNT(*) FROM #PlantItems);
    DECLARE @plantRecentChanges int = (SELECT COUNT(*) FROM #PlantItems WHERE HasRecentChanges = 1);
    
    -- First, generate HTML rows for items with recent changes
    DECLARE @recentChangesHTML nvarchar(max) = '';
    
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
    FROM #PlantItems
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
    
    -- Then, generate HTML rows for other incomplete items
    DECLARE @otherItemsHTML nvarchar(max) = '';
    
    SELECT @otherItemsHTML = @otherItemsHTML + '
    <tr>
        <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + CAST(Id AS varchar) + '</td>
        <td style="padding: 8px; border: 1px solid #ddd;">' + MonitoringName + '</td>
        <td style="padding: 8px; border: 1px solid #ddd;">' + MonitoringCategory + '</td>
        <td style="padding: 8px; border: 1px solid #ddd;">' + ISNULL(Area, '') + '</td>
        <td style="padding: 8px; text-align: center; border: 1px solid #ddd;">' + CurrentStatus + '</td>
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
    FROM #PlantItems
    WHERE HasRecentChanges = 0
    ORDER BY 
        CASE CurrentStatus
            WHEN 'Not Started' THEN 1
            WHEN 'Quotation Requested' THEN 2
            WHEN 'ePR Raised' THEN 3
            WHEN 'Work In Progress' THEN 4
            WHEN 'Completed' THEN 5
            ELSE 6
        END;
    
    -- Create email content
    DECLARE @htmlContent nvarchar(max) = '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Plant Monitoring Daily Summary</title>
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
            background-color: #5cb85c;
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
        .action-button {
            display: inline-block;
            padding: 10px 15px;
            background-color: #5cb85c;
            color: white;
            text-decoration: none;
            border-radius: 4px;
            margin-top: 15px;
        }
        .action-button:hover {
            background-color: #449d44;
        }
        .note {
            background-color: #f5f5f5;
            padding: 10px;
            border-left: 3px solid #5cb85c;
            margin: 15px 0;
        }
        .section-header {
            background-color: #dff0d8;
            color: #3c763d;
            padding: 10px;
            margin-top: 20px;
            border-radius: 3px;
            font-weight: bold;
        }
        .status-count {
            display: inline-block;
            margin-right: 15px;
            padding: 5px 10px;
            border-radius: 3px;
            background-color: #f5f5f5;
            font-weight: bold;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Plant Monitoring Daily Summary</h1>
            <p>' + @plantName + ' - ' + CONVERT(varchar, GETDATE(), 103) + '</p>
        </div>
        
        <div class="content">
            
            <p>Dear Team,</p>
            
            <p>This is your daily summary of plant monitoring items for <strong>' + @plantName + '</strong>. Below you will find all incomplete items, with recently updated items highlighted.</p>
            
            <div class="note">
                <p><strong>Summary:</strong></p>
                <div class="status-count">Total Incomplete Items: ' + CAST(@plantItemCount AS varchar) + '</div>
                <div class="status-count">Recently Updated: ' + CAST(@plantRecentChanges AS varchar) + '</div>
            </div>';
            
    -- Add recent changes section if there are any
    IF @plantRecentChanges > 0
    BEGIN
        SET @htmlContent = @htmlContent + '
            <div class="section-header">Recently Updated Items (Last 24 Hours)</div>
            
            <table border="1" cellpadding="8" cellspacing="0" style="width: 100%; border-collapse: collapse; border: 1px solid #ddd;">
                <thead>
                    <tr>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 5%;">ID</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 15%;">Monitoring Name</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Category</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Area</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 12%;">Status</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Expiry Date</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 18%;">Assigned To</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Action</th>
                    </tr>
                </thead>
                <tbody>
                    ' + @recentChangesHTML + '
                </tbody>
            </table>';
    END
    
    -- Add other incomplete items section
    IF @plantItemCount - @plantRecentChanges > 0
    BEGIN
        SET @htmlContent = @htmlContent + '
            <div class="section-header">Other Incomplete Items</div>
            
            <table border="1" cellpadding="8" cellspacing="0" style="width: 100%; border-collapse: collapse; border: 1px solid #ddd;">
                <thead>
                    <tr>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 5%;">ID</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 15%;">Monitoring Name</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Category</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Area</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 12%;">Status</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Expiry Date</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 18%;">Assigned To</th>
                        <th style="padding: 8px; background-color: #f2f2f2; border: 1px solid #ddd; text-align: center; width: 10%;">Action</th>
                    </tr>
                </thead>
                <tbody>
                    ' + @otherItemsHTML + '
                </tbody>
            </table>';
    END
    
    -- Complete the HTML content
    SET @htmlContent = @htmlContent + '
            <div class="note">
                <p><strong>Note:</strong> Please review these items and take appropriate action. Items highlighted in green have been updated in the last 24 hours.</p>
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
        
        -- Get count of status changes for this plant
        DECLARE @emailTitle varchar(200) = 'Plant Monitoring Daily Summary - ' + @plantName + ' - ' + CAST(@plantItemCount AS varchar) + ' incomplete items';
        
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
                ''Plant Monitoring Daily Summary for ' + @plantName + ''', 
                ''' + @plantEmailList + ''', 
                '''', 
                ''' + @emailTitle + ''', 
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
        END CATCH
    END
    
    -- Drop the temporary table for this plant
    DROP TABLE #PlantItems;
    
    FETCH NEXT FROM plant_cursor INTO @plantID, @plantName;
END

CLOSE plant_cursor;
DEALLOCATE plant_cursor;

-- Clean up
DROP TABLE #AllItems;
DROP TABLE #AffectedPlants;

END 