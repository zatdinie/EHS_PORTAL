USE [ESH]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[Competency_Points_Check]
AS
BEGIN
    SET NOCOUNT ON;

    ---------------------------
    -- PART 1: DOE_CPD Check --
    ---------------------------

    -- Step 1: Identify users who have Environment competency type
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        U.DOE_CPD,
        CASE 
            WHEN U.DOE_CPD >= 40 THEN 'Compliant'
            ELSE 'Non-Compliant'
        END AS ComplianceStatus,
        (40 - ISNULL(U.DOE_CPD, 0)) AS PointsNeeded
    INTO #EnvironmentUsers
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.CompetencyType = 'Environment'
    AND UC.Status = 'Active';

    -- Step 2: Check if it's January 1st and reset DOE_CPD points if needed
    IF (MONTH(GETDATE()) = 1 AND DAY(GETDATE()) = 1)
    BEGIN
        -- Reset DOE_CPD points for all users with Environment competency
        UPDATE U
        SET U.DOE_CPD = 0
        FROM [CLIP].[AspNetUsers] U
        INNER JOIN #EnvironmentUsers EU ON U.Id = EU.UserId;
    END

    -- Step 3: Collect email addresses for non-compliant DOE_CPD users
    DECLARE @nonCompliantDOEUsers NVARCHAR(MAX);
    SELECT @nonCompliantDOEUsers = STUFF((
        SELECT DISTINCT ' ; ' + Email
        FROM #EnvironmentUsers
        WHERE ComplianceStatus = 'Non-Compliant'
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');
    
    -- Step 3.1: Collect email addresses for Admin users
    DECLARE @adminUsers NVARCHAR(MAX);
    SELECT @adminUsers = STUFF((
        SELECT DISTINCT ' ; ' + U.Email
        FROM [CLIP].[AspNetUsers] U
        INNER JOIN [CLIP].[AspNetUserRoles] UR ON U.Id = UR.UserId
        INNER JOIN [CLIP].[AspNetRoles] R ON UR.RoleId = R.Id
        WHERE R.Name = 'Admin'
        AND U.Email IS NOT NULL
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');
    
    -- Step 4: Send DOE_CPD email if there are non-compliant users
    IF EXISTS (SELECT 1 FROM #EnvironmentUsers WHERE ComplianceStatus = 'Non-Compliant')
    BEGIN
        -- Step 4.1: Combine email lists
        DECLARE @allDOERecipients NVARCHAR(MAX);
        IF @adminUsers IS NOT NULL AND @adminUsers <> ''
        BEGIN
            IF @nonCompliantDOEUsers IS NOT NULL AND @nonCompliantDOEUsers <> ''
                SET @allDOERecipients = @nonCompliantDOEUsers + ' ; ' + @adminUsers;
            ELSE
                SET @allDOERecipients = @adminUsers;
        END
        ELSE
            SET @allDOERecipients = @nonCompliantDOEUsers;

        -- Step 4.2: Build the HTML rows for the email
        DECLARE @doeUserRowsHTML NVARCHAR(MAX) = '';
        SELECT @doeUserRowsHTML = STUFF((
        SELECT '
            <tr>
                <td>' + UserName + '</td>
                <td>' + Email + '</td>
                <td>' + CAST(ISNULL(DOE_CPD, 0) AS VARCHAR) + '</td>
                <td>' + CAST(PointsNeeded AS VARCHAR) + '</td>
                <td>' + ComplianceStatus + '</td>
            </tr>'
        FROM #EnvironmentUsers
        WHERE ComplianceStatus = 'Non-Compliant'
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

        -- Step 4.3: Compose email content
        DECLARE @doeHtmlContent NVARCHAR(MAX);
        SET @doeHtmlContent = '
        <html>
        <head>
            <style>
                table { border-collapse: collapse; width: 100%; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                th { background-color: #f2f2f2; }
                h2, h3 { color: #333366; }
                .highlight { background-color: #fff3cd; }
            </style>
        </head>
        <body>
            <h2>DOE CPD Points Compliance Report</h2>
            <p>The following users have not met the required 40 DOE_CPD points for their Environment competency:</p>
            <table>
                <thead>
                    <tr>
                        <th>Username</th>
                        <th>Email</th>
                        <th>Current Points</th>
                        <th>Points Needed</th>
                        <th>Status</th>
                    </tr>
                </thead>
                    <tbody>' + @doeUserRowsHTML + '</tbody>
            </table>
            <p class="highlight">Note: Users with Environment competency are required to maintain 40 DOE_CPD points annually.</p>
        </body>
        </html>';

        -- Step 4.4: Insert into email queue
        DECLARE @totalDOENonCompliant INT = (SELECT COUNT(*) FROM #EnvironmentUsers WHERE ComplianceStatus = 'Non-Compliant');
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
            'DOE CPD Points Compliance Report', 
            @allDOERecipients, 
            '', 
            'DOE CPD Points Alert - ' + CAST(@totalDOENonCompliant AS VARCHAR) + ' users non-compliant (' + CONVERT(VARCHAR, GETDATE(), 103) + ')', 
            @doeHtmlContent, 
            'INARI PORTAL', 
            GETDATE(), 
            'N'
        );
    END

    ----------------------------
    -- PART 2: Dosh_CEP Check --
    ----------------------------
    
    -- Step 1: Identify users who have SHO competency module
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        U.Dosh_CEP,
        CM.AnnualPointDeduction AS RequiredPoints,
        UC.ExpiryDate,
        CASE 
            WHEN U.Dosh_CEP >= CM.AnnualPointDeduction THEN 'Compliant'
            ELSE 'Non-Compliant'
        END AS ComplianceStatus,
        (CM.AnnualPointDeduction - ISNULL(U.Dosh_CEP, 0)) AS PointsNeeded
    INTO #SHOUsers
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.ModuleName = 'SHO'
    AND UC.Status = 'Active';

    -- Step 2: Process annual deductions based on ExpiryDate
    -- Process each user's deduction date
    DECLARE @userId NVARCHAR(128)
    DECLARE @expiryDate DATE
    DECLARE @requiredPoints INT
    DECLARE @currentPoints INT
    DECLARE @carriedOverPoints INT

    -- Get users who need deductions processed today
    DECLARE user_cursor CURSOR FOR
    SELECT UserId, ExpiryDate, RequiredPoints, Dosh_CEP
    FROM #SHOUsers
    WHERE DAY(ExpiryDate) = DAY(GETDATE())
      AND MONTH(ExpiryDate) = MONTH(GETDATE());

    OPEN user_cursor
    FETCH NEXT FROM user_cursor INTO @userId, @expiryDate, @requiredPoints, @currentPoints

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Calculate carry-over (max 15 points)
        IF (@currentPoints >= @requiredPoints)
            SET @carriedOverPoints = CASE WHEN (@currentPoints - @requiredPoints) > 15 THEN 15 ELSE (@currentPoints - @requiredPoints) END
        ELSE
            SET @carriedOverPoints = 0
        
        -- Update the user's points
        UPDATE U
        SET U.Dosh_CEP = @carriedOverPoints
        FROM [CLIP].[AspNetUsers] U
        WHERE U.Id = @userId;
        
        FETCH NEXT FROM user_cursor INTO @userId, @expiryDate, @requiredPoints, @currentPoints
    END

    CLOSE user_cursor
    DEALLOCATE user_cursor

    -- Step 3: Identify users with upcoming deductions in the next 90 days
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        U.Dosh_CEP,
        CM.AnnualPointDeduction AS RequiredPoints,
        UC.ExpiryDate,
        DATEDIFF(DAY, GETDATE(), 
            DATEFROMPARTS(
                YEAR(GETDATE()), 
                MONTH(UC.ExpiryDate), 
                DAY(UC.ExpiryDate)
            )
        ) AS DaysToDeduction,
        CASE 
            WHEN U.Dosh_CEP >= CM.AnnualPointDeduction THEN 'Compliant'
            ELSE 'Non-Compliant'
        END AS ComplianceStatus,
        (CM.AnnualPointDeduction - ISNULL(U.Dosh_CEP, 0)) AS PointsNeeded
    INTO #UpcomingDeductions
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.ModuleName = 'SHO'
    AND UC.Status = 'Active'
    AND DATEDIFF(DAY, GETDATE(), 
            DATEFROMPARTS(
                YEAR(GETDATE()), 
                MONTH(UC.ExpiryDate), 
                DAY(UC.ExpiryDate)
            )
        ) BETWEEN 0 AND 90
    AND U.Dosh_CEP < CM.AnnualPointDeduction;

    -- Step 4: Send Dosh_CEP email if there are upcoming deductions
    IF EXISTS (SELECT 1 FROM #UpcomingDeductions)
    BEGIN
        -- Step 4.1: Collect email addresses for non-compliant Dosh_CEP users
        DECLARE @nonCompliantDoshUsers NVARCHAR(MAX);
        SELECT @nonCompliantDoshUsers = STUFF((
            SELECT DISTINCT ' ; ' + Email
            FROM #UpcomingDeductions
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');
        
        -- Step 4.2: Combine email lists
        DECLARE @allDoshRecipients NVARCHAR(MAX);
        IF @adminUsers IS NOT NULL AND @adminUsers <> ''
        BEGIN
            IF @nonCompliantDoshUsers IS NOT NULL AND @nonCompliantDoshUsers <> ''
                SET @allDoshRecipients = @nonCompliantDoshUsers + ' ; ' + @adminUsers;
            ELSE
                SET @allDoshRecipients = @adminUsers;
        END
        ELSE
            SET @allDoshRecipients = @nonCompliantDoshUsers;

        -- Step 4.3: Build the HTML rows for the email
        DECLARE @doshUserRowsHTML NVARCHAR(MAX) = '';
        SELECT @doshUserRowsHTML = STUFF((
            SELECT '
                <tr>
                    <td>' + UserName + '</td>
                    <td>' + Email + '</td>
                    <td>' + CAST(ISNULL(Dosh_CEP, 0) AS VARCHAR) + '</td>
                    <td>' + CAST(RequiredPoints AS VARCHAR) + '</td>
                    <td>' + CAST(PointsNeeded AS VARCHAR) + '</td>
                    <td>' + CONVERT(VARCHAR, ExpiryDate, 103) + '</td>
                    <td>' + CAST(DaysToDeduction AS VARCHAR) + '</td>
                </tr>'
            FROM #UpcomingDeductions
            ORDER BY DaysToDeduction
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

        -- Step 4.4: Compose email content
        DECLARE @doshHtmlContent NVARCHAR(MAX);
        SET @doshHtmlContent = '
            <html>
            <head>
                <style>
                    table { border-collapse: collapse; width: 100%; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    h2, h3 { color: #333366; }
                    .highlight { background-color: #fff3cd; }
                </style>
            </head>
            <body>
                <h2>DOSH CEP Points Compliance Report</h2>
                <p>The following users have upcoming DOSH CEP point deductions within the next 90 days and do not have enough points:</p>
                <table>
                    <thead>
                        <tr>
                            <th>Username</th>
                            <th>Email</th>
                            <th>Current Points</th>
                            <th>Required Points</th>
                            <th>Points Needed</th>
                            <th>Deduction Date</th>
                            <th>Days Remaining</th>
                        </tr>
                    </thead>
                    <tbody>' + @doshUserRowsHTML + '</tbody>
                </table>
                <p class="highlight">Note: Users with SHO competency need to maintain sufficient DOSH CEP points before their annual deduction date.</p>
            </body>
            </html>';

        -- Step 4.5: Insert into email queue
        DECLARE @totalDoshNonCompliant INT = (SELECT COUNT(*) FROM #UpcomingDeductions);
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
            'DOSH CEP Points Compliance Report', 
            @allDoshRecipients, 
            '', 
            'DOSH CEP Points Alert - ' + CAST(@totalDoshNonCompliant AS VARCHAR) + ' users with upcoming deductions (' + CONVERT(VARCHAR, GETDATE(), 103) + ')', 
            @doshHtmlContent, 
            'INARI PORTAL', 
            GETDATE(), 
            'N'
        );
    END

    ----------------------------
    -- PART 3: Atom_CEP Check --
    ----------------------------
    
    -- Step 1: Identify users who have RPO competency module
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        U.Atom_CEP,
        CM.AnnualPointDeduction AS RequiredPoints,
        UC.ExpiryDate,
        CASE 
            WHEN U.Atom_CEP >= CM.AnnualPointDeduction THEN 'Compliant'
            ELSE 'Non-Compliant'
        END AS ComplianceStatus,
        (CM.AnnualPointDeduction - ISNULL(U.Atom_CEP, 0)) AS PointsNeeded
    INTO #RPOUsers
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.ModuleName = 'RPO'
    AND UC.Status = 'Active';

    -- Step 2: Process annual deductions based on ExpiryDate
    -- Process each user's deduction date
    DECLARE @atomUserId NVARCHAR(128)
    DECLARE @atomExpiryDate DATE
    DECLARE @atomRequiredPoints INT
    DECLARE @atomCurrentPoints INT
    DECLARE @atomCarriedOverPoints INT

    -- Get users who need deductions processed today
    DECLARE atom_cursor CURSOR FOR
    SELECT UserId, ExpiryDate, RequiredPoints, Atom_CEP
    FROM #RPOUsers
    WHERE DAY(ExpiryDate) = DAY(GETDATE())
      AND MONTH(ExpiryDate) = MONTH(GETDATE());

    OPEN atom_cursor
    FETCH NEXT FROM atom_cursor INTO @atomUserId, @atomExpiryDate, @atomRequiredPoints, @atomCurrentPoints

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Calculate carry-over (max 15 points)
        IF (@atomCurrentPoints >= @atomRequiredPoints)
            SET @atomCarriedOverPoints = CASE WHEN (@atomCurrentPoints - @atomRequiredPoints) > 15 THEN 15 ELSE (@atomCurrentPoints - @atomRequiredPoints) END
        ELSE
            SET @atomCarriedOverPoints = 0
        
        -- Update the user's points
        UPDATE U
        SET U.Atom_CEP = @atomCarriedOverPoints
        FROM [CLIP].[AspNetUsers] U
        WHERE U.Id = @atomUserId;
        
        FETCH NEXT FROM atom_cursor INTO @atomUserId, @atomExpiryDate, @atomRequiredPoints, @atomCurrentPoints
    END

    CLOSE atom_cursor
    DEALLOCATE atom_cursor

    -- Step 3: Identify users with upcoming deductions in the next 90 days
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        U.Atom_CEP,
        CM.AnnualPointDeduction AS RequiredPoints,
        UC.ExpiryDate,
        DATEDIFF(DAY, GETDATE(), 
            DATEFROMPARTS(
                YEAR(GETDATE()), 
                MONTH(UC.ExpiryDate), 
                DAY(UC.ExpiryDate)
            )
        ) AS DaysToDeduction,
        CASE 
            WHEN U.Atom_CEP >= CM.AnnualPointDeduction THEN 'Compliant'
            ELSE 'Non-Compliant'
        END AS ComplianceStatus,
        (CM.AnnualPointDeduction - ISNULL(U.Atom_CEP, 0)) AS PointsNeeded
    INTO #UpcomingAtomDeductions
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.ModuleName = 'RPO'
    AND UC.Status = 'Active'
    AND DATEDIFF(DAY, GETDATE(), 
            DATEFROMPARTS(
                YEAR(GETDATE()), 
                MONTH(UC.ExpiryDate), 
                DAY(UC.ExpiryDate)
            )
        ) BETWEEN 0 AND 90
    AND U.Atom_CEP < CM.AnnualPointDeduction;

    -- Step 4: Send Atom_CEP email if there are upcoming deductions
    IF EXISTS (SELECT 1 FROM #UpcomingAtomDeductions)
    BEGIN
        -- Step 4.1: Collect email addresses for non-compliant Atom_CEP users
        DECLARE @nonCompliantAtomUsers NVARCHAR(MAX);
        SELECT @nonCompliantAtomUsers = STUFF((
            SELECT DISTINCT ' ; ' + Email
            FROM #UpcomingAtomDeductions
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');
        
        -- Step 4.2: Combine email lists
        DECLARE @allAtomRecipients NVARCHAR(MAX);
        IF @adminUsers IS NOT NULL AND @adminUsers <> ''
        BEGIN
            IF @nonCompliantAtomUsers IS NOT NULL AND @nonCompliantAtomUsers <> ''
                SET @allAtomRecipients = @nonCompliantAtomUsers + ' ; ' + @adminUsers;
            ELSE
                SET @allAtomRecipients = @adminUsers;
        END
        ELSE
            SET @allAtomRecipients = @nonCompliantAtomUsers;

        -- Step 4.3: Build the HTML rows for the email
        DECLARE @atomUserRowsHTML NVARCHAR(MAX) = '';
        SELECT @atomUserRowsHTML = STUFF((
            SELECT '
                <tr>
                    <td>' + UserName + '</td>
                    <td>' + Email + '</td>
                    <td>' + CAST(ISNULL(Atom_CEP, 0) AS VARCHAR) + '</td>
                    <td>' + CAST(RequiredPoints AS VARCHAR) + '</td>
                    <td>' + CAST(PointsNeeded AS VARCHAR) + '</td>
                    <td>' + CONVERT(VARCHAR, ExpiryDate, 103) + '</td>
                    <td>' + CAST(DaysToDeduction AS VARCHAR) + '</td>
                </tr>'
            FROM #UpcomingAtomDeductions
            ORDER BY DaysToDeduction
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

        -- Step 4.4: Compose email content
        DECLARE @atomHtmlContent NVARCHAR(MAX);
        SET @atomHtmlContent = '
            <html>
            <head>
                <style>
                    table { border-collapse: collapse; width: 100%; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    h2, h3 { color: #333366; }
                    .highlight { background-color: #fff3cd; }
                </style>
            </head>
            <body>
                <h2>ATOM CEP Points Compliance Report</h2>
                <p>The following users have upcoming ATOM CEP point deductions within the next 90 days and do not have enough points:</p>
                <table>
                    <thead>
                        <tr>
                            <th>Username</th>
                            <th>Email</th>
                            <th>Current Points</th>
                            <th>Required Points</th>
                            <th>Points Needed</th>
                            <th>Deduction Date</th>
                            <th>Days Remaining</th>
                        </tr>
                    </thead>
                    <tbody>' + @atomUserRowsHTML + '</tbody>
                </table>
                <p class="highlight">Note: Users with RPO competency need to maintain sufficient ATOM CEP points before their annual deduction date.</p>
            </body>
            </html>';

        -- Step 4.5: Insert into email queue
        DECLARE @totalAtomNonCompliant INT = (SELECT COUNT(*) FROM #UpcomingAtomDeductions);
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
            'ATOM CEP Points Compliance Report', 
            @allAtomRecipients, 
            '', 
            'ATOM CEP Points Alert - ' + CAST(@totalAtomNonCompliant AS VARCHAR) + ' users with upcoming deductions (' + CONVERT(VARCHAR, GETDATE(), 103) + ')', 
            @atomHtmlContent, 
            'INARI PORTAL', 
            GETDATE(), 
            'N'
        );
    END

    ----------------------------------------
    -- PART 4: Competency Expiry Reminder --
    ----------------------------------------
    
    -- Step 1: Identify users with SHO or RPO competencies expiring within 90 days
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        CM.ModuleName AS CompetencyType,
        UC.ExpiryDate,
        DATEDIFF(DAY, GETDATE(), UC.ExpiryDate) AS DaysToExpiry
    INTO #ExpiringCompetencies
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.ModuleName IN ('SHO', 'RPO')
    AND UC.Status = 'Active'
    AND DATEDIFF(DAY, GETDATE(), UC.ExpiryDate) BETWEEN 0 AND 90;

    -- Step 2: Send email if there are expiring competencies
    IF EXISTS (SELECT 1 FROM #ExpiringCompetencies)
    BEGIN
        -- Step 2.1: Collect email addresses for users with expiring competencies
        DECLARE @expiringCompetencyUsers NVARCHAR(MAX);
        SELECT @expiringCompetencyUsers = STUFF((
            SELECT DISTINCT ' ; ' + Email
            FROM #ExpiringCompetencies
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');
        
        -- Step 2.2: Combine email lists
        DECLARE @allExpiryRecipients NVARCHAR(MAX);
        IF @adminUsers IS NOT NULL AND @adminUsers <> ''
        BEGIN
            IF @expiringCompetencyUsers IS NOT NULL AND @expiringCompetencyUsers <> ''
                SET @allExpiryRecipients = @expiringCompetencyUsers + ' ; ' + @adminUsers;
            ELSE
                SET @allExpiryRecipients = @adminUsers;
        END
        ELSE
            SET @allExpiryRecipients = @expiringCompetencyUsers;

        -- Step 2.3: Build the HTML rows for the email
        DECLARE @expiryRowsHTML NVARCHAR(MAX) = '';
        SELECT @expiryRowsHTML = STUFF((
            SELECT '
                <tr>
                    <td>' + UserName + '</td>
                    <td>' + Email + '</td>
                    <td>' + CompetencyType + '</td>
                    <td>' + CONVERT(VARCHAR, ExpiryDate, 103) + '</td>
                    <td>' + CAST(DaysToExpiry AS VARCHAR) + '</td>
                </tr>'
            FROM #ExpiringCompetencies
            ORDER BY DaysToExpiry
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

        -- Step 2.4: Compose email content
        DECLARE @expiryHtmlContent NVARCHAR(MAX);
        SET @expiryHtmlContent = '
            <html>
            <head>
                <style>
                    table { border-collapse: collapse; width: 100%; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    h2, h3 { color: #333366; }
                    .highlight { background-color: #fff3cd; }
                </style>
            </head>
            <body>
                <h2>Competency Expiry Reminder</h2>
                <p>The following users have SHO or RPO competencies expiring within the next 90 days:</p>
                <table>
                    <thead>
                        <tr>
                            <th>Username</th>
                            <th>Email</th>
                            <th>Competency Type</th>
                            <th>Expiry Date</th>
                            <th>Days Remaining</th>
                        </tr>
                    </thead>
                    <tbody>' + @expiryRowsHTML + '</tbody>
                </table>
                <p class="highlight">Note: Please ensure these competencies are renewed before they expire.</p>
            </body>
            </html>';

        -- Step 2.5: Insert into email queue
        DECLARE @totalExpiring INT = (SELECT COUNT(*) FROM #ExpiringCompetencies);
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
            'Competency Expiry Reminder', 
            @allExpiryRecipients, 
            '', 
            'Competency Expiry Alert - ' + CAST(@totalExpiring AS VARCHAR) + ' competencies expiring soon (' + CONVERT(VARCHAR, GETDATE(), 103) + ')', 
            @expiryHtmlContent, 
            'INARI PORTAL', 
            GETDATE(), 
            'N'
        );
    END

    -- Clean up
    DROP TABLE #EnvironmentUsers;
    DROP TABLE #SHOUsers;
    DROP TABLE #UpcomingDeductions;
    DROP TABLE #RPOUsers;
    DROP TABLE #UpcomingAtomDeductions;
    DROP TABLE #ExpiringCompetencies;

    ---------------------------------------
    -- PART 5: Expired Competencies Alert --
    ---------------------------------------
    
    -- Step 1: Identify users with SHO or RPO competencies that have already expired
    SELECT DISTINCT 
        U.Id AS UserId,
        U.UserName,
        U.Email,
        CM.ModuleName AS CompetencyType,
        UC.ExpiryDate,
        UC.Id AS UserCompetencyId,
        DATEDIFF(DAY, UC.ExpiryDate, GETDATE()) AS DaysExpired
    INTO #ExpiredCompetencies
    FROM [CLIP].[AspNetUsers] U
    INNER JOIN [CLIP].[UserCompetencies] UC ON U.Id = UC.UserId
    INNER JOIN [CLIP].[CompetencyModules] CM ON UC.CompetencyModuleId = CM.Id
    WHERE CM.ModuleName IN ('SHO', 'RPO')
    AND UC.Status = 'Active'
    AND UC.ExpiryDate < GETDATE();
    
    -- Step 1.1: Update the status of expired competencies to "Expired"
    UPDATE UC
    SET UC.Status = 'Expired'
    FROM [CLIP].[UserCompetencies] UC
    INNER JOIN #ExpiredCompetencies EC ON UC.Id = EC.UserCompetencyId;

    -- Step 2: Send email if there are expired competencies
    IF EXISTS (SELECT 1 FROM #ExpiredCompetencies)
    BEGIN
        -- Step 2.1: Collect email addresses for users with expired competencies
        DECLARE @expiredCompetencyUsers NVARCHAR(MAX);
        SELECT @expiredCompetencyUsers = STUFF((
            SELECT DISTINCT ' ; ' + Email
            FROM #ExpiredCompetencies
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 3, '');
        
        -- Step 2.2: Combine email lists with admin users
        DECLARE @allExpiredRecipients NVARCHAR(MAX);
        IF @adminUsers IS NOT NULL AND @adminUsers <> ''
        BEGIN
            IF @expiredCompetencyUsers IS NOT NULL AND @expiredCompetencyUsers <> ''
                SET @allExpiredRecipients = @expiredCompetencyUsers + ' ; ' + @adminUsers;
            ELSE
                SET @allExpiredRecipients = @adminUsers;
        END
        ELSE
            SET @allExpiredRecipients = @expiredCompetencyUsers;

        -- Step 2.3: Build the HTML rows for the email
        DECLARE @expiredRowsHTML NVARCHAR(MAX) = '';
        SELECT @expiredRowsHTML = STUFF((
            SELECT '
                <tr>
                    <td>' + UserName + '</td>
                    <td>' + Email + '</td>
                    <td>' + CompetencyType + '</td>
                    <td>' + CONVERT(VARCHAR, ExpiryDate, 103) + '</td>
                    <td>' + CAST(DaysExpired AS VARCHAR) + '</td>
                </tr>'
            FROM #ExpiredCompetencies
            ORDER BY DaysExpired DESC
            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '');

        -- Step 2.4: Compose email content
        DECLARE @expiredHtmlContent NVARCHAR(MAX);
        SET @expiredHtmlContent = '
            <html>
            <head>
                <style>
                    table { border-collapse: collapse; width: 100%; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #f2f2f2; }
                    h2, h3 { color: #333366; }
                    .highlight { background-color: #ffcccc; }
                </style>
            </head>
            <body>
                <h2>EXPIRED Competencies Alert</h2>
                <p class="highlight">The following users have competencies that have ALREADY EXPIRED and require immediate attention:</p>
                <table>
                    <thead>
                        <tr>
                            <th>Username</th>
                            <th>Email</th>
                            <th>Competency Type</th>
                            <th>Expiry Date</th>
                            <th>Days Expired</th>
                        </tr>
                    </thead>
                    <tbody>' + @expiredRowsHTML + '</tbody>
                </table>
                <p class="highlight">URGENT: These competencies must be renewed immediately as they are already expired.</p>
            </body>
            </html>';

        -- Step 2.5: Insert into email queue
        DECLARE @totalExpired INT = (SELECT COUNT(*) FROM #ExpiredCompetencies);
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
            'Expired Competencies Alert', 
            @allExpiredRecipients, 
            '', 
            'URGENT: ' + CAST(@totalExpired AS VARCHAR) + ' Competencies EXPIRED (' + CONVERT(VARCHAR, GETDATE(), 103) + ')', 
            @expiredHtmlContent, 
            'INARI PORTAL', 
            GETDATE(), 
            'N'
        );
    END

    -- Add expired competencies to cleanup
    DROP TABLE #ExpiredCompetencies;
END
GO
