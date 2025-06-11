USE [ESH]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[CF_Monthly_Expiry]
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: Update Certificate Statuses
    UPDATE [CLIP].[CertificateOfFitness]
    SET Status = CASE 
        WHEN ExpiryDate < GETDATE() THEN 'Expired'
        WHEN DATEDIFF(DAY, GETDATE(), ExpiryDate) BETWEEN 0 AND 90 THEN 'Expiring Soon'
        ELSE Status
    END
    WHERE ExpiryDate IS NOT NULL
    AND Status NOT IN ('Expired', 'Expiring Soon');

    -- Step 2: Get Expiring Certificates
    SELECT 
        COF.Id,
        COF.PlantId,
        COF.RegistrationNo,
        COF.ExpiryDate,
        COF.MachineName,
        COF.Status,
        COF.Remarks,
        COF.DocumentPath,
        COF.Location,
        COF.HostInfo,
        COF.Department,
        P.PlantName,
        DATEDIFF(DAY, GETDATE(), COF.ExpiryDate) AS DaysUntilExpiry,
        CASE 
            WHEN COF.ExpiryDate < GETDATE() THEN 'expired'
            WHEN DATEDIFF(DAY, GETDATE(), COF.ExpiryDate) <= 30 THEN 'critical'
            WHEN DATEDIFF(DAY, GETDATE(), COF.ExpiryDate) <= 60 THEN 'warning'
            ELSE 'info'
        END AS SeverityClass
    INTO #ExpiringCertificates
    FROM [CLIP].[CertificateOfFitness] COF
    LEFT JOIN [CLIP].[Plants] P ON COF.PlantId = P.Id
    WHERE COF.ExpiryDate IS NOT NULL
    AND (COF.ExpiryDate < GETDATE() OR DATEDIFF(DAY, GETDATE(), COF.ExpiryDate) <= 90);

    -- Step 3: Exit early if no certs
    IF NOT EXISTS (SELECT 1 FROM #ExpiringCertificates)
    BEGIN
        DROP TABLE #ExpiringCertificates;
        RETURN;
    END

    -- Step 4: Collect email addresses of users assigned to plants with expiring certificates
    DECLARE @emailList NVARCHAR(MAX);
    SELECT @emailList = STUFF((
        SELECT DISTINCT ' ; ' + U.Email
        FROM [CLIP].[AspNetUsers] U
        INNER JOIN [CLIP].[UserPlants] UP ON U.Id = UP.UserId
        INNER JOIN #ExpiringCertificates EC ON UP.PlantId = EC.PlantId
        WHERE U.Email IS NOT NULL 
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');

    -- Step 5: Exit if no users found for the affected plants
    IF @emailList IS NULL OR @emailList = ''
    BEGIN
        DROP TABLE #ExpiringCertificates;
        RETURN;
    END

    -- Step 6: Build the HTML rows
    DECLARE @certificateRowsHTML NVARCHAR(MAX) = '';
    SELECT @certificateRowsHTML = STUFF((
        SELECT '
            <tr class="' + SeverityClass + '-row">
                <td>' + RegistrationNo + '</td>
                <td>' + ISNULL(MachineName, '') + '</td>
                <td>' + ISNULL(PlantName, '') + '</td>
                <td>' + CONVERT(VARCHAR, ExpiryDate, 103) + '</td>
                <td>' + 
                    CASE 
                        WHEN DaysUntilExpiry < 0 THEN 'EXPIRED (' + CAST(ABS(DaysUntilExpiry) AS VARCHAR) + ' days ago)'
                        ELSE CAST(DaysUntilExpiry AS VARCHAR) + ' days'
                    END + '</td>
                <td>' + Status + '</td>
                <td>' + ISNULL(Remarks, '') + '</td>
            </tr>'
        FROM #ExpiringCertificates
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

    -- Step 7: Compose email content
    DECLARE @htmlContent NVARCHAR(MAX);
    SET @htmlContent = '
        <html>
        <head>
            <style>
                table { border-collapse: collapse; width: 100%; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f2f2f2; }
                .expired-row { background-color: #f8d7da; }
                .critical-row { background-color: #fff3cd; }
                .warning-row { background-color: #d1ecf1; }
                .info-row { background-color: #e2e3e5; }
            </style>
        </head>
        <body>
            <h2>Certificate of Fitness Expiry Report</h2>
            <p>The following certificates from your assigned plants require attention:</p>
            <table>
                <thead>
                    <tr>
                        <th>Registration No</th>
                        <th>Machine Name</th>
                        <th>Plant Name</th>
                        <th>Expiry Date</th>
                        <th>Days Until Expiry</th>
                        <th>Status</th>
                        <th>Remarks</th>
                    </tr>
                </thead>
                <tbody>' + @certificateRowsHTML + '</tbody>
            </table>
        </body>
        </html>';

    -- Step 8: Insert into email queue
    DECLARE @totalCount INT = (SELECT COUNT(*) FROM #ExpiringCertificates);
    INSERT INTO [AUTOREPORT].dbo.[TAUTO_EMAIL] (
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
        'CLIP Certificate of Fitness Expiry Report', 
        @emailList, 
        '', 
        'Certificate of Fitness Expiry Alert - ' + CAST(@totalCount AS VARCHAR) + ' certificates require attention (' + CONVERT(VARCHAR, GETDATE(), 103) + ')', 
        @htmlContent, 
        'INARI PORTAL', 
        GETDATE(), 
        'N'
    );

    DROP TABLE #ExpiringCertificates;
END
GO


