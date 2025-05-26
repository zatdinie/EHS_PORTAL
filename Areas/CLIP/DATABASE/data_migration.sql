-- Script to migrate data from CLIP and FETS databases to ESH database schemas
-- This script should be run after the ESH database has been created with all tables and constraints

-- Set up database context
USE [master]
GO

-- Step 1: Disable constraints in ESH database to allow data import
PRINT 'Disabling constraints in ESH database...'
USE [ESH]
GO

-- Disable all constraints in CLIP schema
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL', @whereand = ' AND SCHEMA_NAME(schema_id) = ''CLIP''';

-- Disable all constraints in FETS schema
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL', @whereand = ' AND SCHEMA_NAME(schema_id) = ''FETS''';

-- Step 2: Merge data from CLIP database to ESH.CLIP schema
PRINT 'Merging data from CLIP database to ESH.CLIP schema...'

-- Delete all existing data in destination tables first (to avoid primary key conflicts)
-- We'll do this in reverse order of dependencies to avoid foreign key issues
PRINT 'Cleaning existing data in target tables...'

-- Clear existing CLIP schema data
DELETE FROM [ESH].[CLIP].[CertificateOfFitness];
DELETE FROM [ESH].[CLIP].[PlantMonitoring];
DELETE FROM [ESH].[CLIP].[UserCompetencies];
DELETE FROM [ESH].[CLIP].[UserPlants];
DELETE FROM [ESH].[CLIP].[ActivityTrainings];
DELETE FROM [ESH].[CLIP].[AspNetUserClaims];
DELETE FROM [ESH].[CLIP].[AspNetUserLogins];
DELETE FROM [ESH].[CLIP].[AspNetUserRoles];
DELETE FROM [ESH].[CLIP].[AspNetUsers];
DELETE FROM [ESH].[CLIP].[AspNetRoles];
DELETE FROM [ESH].[CLIP].[CompetencyModules];
DELETE FROM [ESH].[CLIP].[Monitoring];
DELETE FROM [ESH].[CLIP].[Plants];
DELETE FROM [ESH].[CLIP].[__MigrationHistory];

-- Clear existing FETS schema data
DELETE FROM [ESH].[FETS].[ActivityLogs];
DELETE FROM [ESH].[FETS].[EmailRecipients];
DELETE FROM [ESH].[FETS].[ServiceReminders];
DELETE FROM [ESH].[FETS].[MapImages];
DELETE FROM [ESH].[FETS].[FireExtinguishers];
DELETE FROM [ESH].[FETS].[Users];
DELETE FROM [ESH].[FETS].[Levels];
DELETE FROM [ESH].[FETS].[FireExtinguisherTypes];
DELETE FROM [ESH].[FETS].[Status];
DELETE FROM [ESH].[FETS].[Plants];

PRINT 'Existing data cleared. Starting data migration...'

-- Migrate __MigrationHistory
INSERT INTO [ESH].[CLIP].[__MigrationHistory] ([MigrationId], [ContextKey], [Model], [ProductVersion])
SELECT [MigrationId], [ContextKey], [Model], [ProductVersion]
FROM [CLIP].[dbo].[__MigrationHistory];

-- Migrate AspNetRoles
INSERT INTO [ESH].[CLIP].[AspNetRoles] ([Id], [Name])
SELECT [Id], [Name]
FROM [CLIP].[dbo].[AspNetRoles];

-- Migrate AspNetUsers
INSERT INTO [ESH].[CLIP].[AspNetUsers] ([Id], [Email], [EmailConfirmed], [PasswordHash], [SecurityStamp], 
                                        [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], 
                                        [LockoutEndDateUtc], [LockoutEnabled], [AccessFailedCount], 
                                        [UserName], [EmpID], [DOE_CPD], [Atom_CEP], [Dosh_CEP])
SELECT [Id], [Email], [EmailConfirmed], [PasswordHash], [SecurityStamp], 
       [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], 
       [LockoutEndDateUtc], [LockoutEnabled], [AccessFailedCount], 
       [UserName], [EmpID], [DOE_CPD], [Atom_CEP], [Dosh_CEP]
FROM [CLIP].[dbo].[AspNetUsers];

-- Migrate AspNetUserClaims
INSERT INTO [ESH].[CLIP].[AspNetUserClaims] ([UserId], [ClaimType], [ClaimValue])
SELECT [UserId], [ClaimType], [ClaimValue]
FROM [CLIP].[dbo].[AspNetUserClaims];

-- Migrate AspNetUserLogins
INSERT INTO [ESH].[CLIP].[AspNetUserLogins] ([LoginProvider], [ProviderKey], [UserId])
SELECT [LoginProvider], [ProviderKey], [UserId]
FROM [CLIP].[dbo].[AspNetUserLogins];

-- Migrate AspNetUserRoles
INSERT INTO [ESH].[CLIP].[AspNetUserRoles] ([UserId], [RoleId])
SELECT [UserId], [RoleId]
FROM [CLIP].[dbo].[AspNetUserRoles];

-- Migrate Plants
SET IDENTITY_INSERT [ESH].[CLIP].[Plants] ON;
INSERT INTO [ESH].[CLIP].[Plants] ([Id], [PlantName])
SELECT [Id], [PlantName]
FROM [CLIP].[dbo].[Plants];
SET IDENTITY_INSERT [ESH].[CLIP].[Plants] OFF;

-- Migrate CompetencyModules
SET IDENTITY_INSERT [ESH].[CLIP].[CompetencyModules] ON;
INSERT INTO [ESH].[CLIP].[CompetencyModules] ([Id], [ModuleName], [Description], [AnnualPointDeduction], [CompetencyType])
SELECT [Id], [ModuleName], [Description], [AnnualPointDeduction], [CompetencyType]
FROM [CLIP].[dbo].[CompetencyModules];
SET IDENTITY_INSERT [ESH].[CLIP].[CompetencyModules] OFF;

-- Migrate UserCompetencies
SET IDENTITY_INSERT [ESH].[CLIP].[UserCompetencies] ON;
INSERT INTO [ESH].[CLIP].[UserCompetencies] ([Id], [UserId], [CompetencyModuleId], [Status], [CompletionDate], [ExpiryDate], [Remarks], [Building])
SELECT [Id], [UserId], [CompetencyModuleId], [Status], [CompletionDate], [ExpiryDate], [Remarks], [Building]
FROM [CLIP].[dbo].[UserCompetencies];
SET IDENTITY_INSERT [ESH].[CLIP].[UserCompetencies] OFF;

-- Migrate UserPlants
SET IDENTITY_INSERT [ESH].[CLIP].[UserPlants] ON;
INSERT INTO [ESH].[CLIP].[UserPlants] ([Id], [UserId], [PlantId])
SELECT [Id], [UserId], [PlantId]
FROM [CLIP].[dbo].[UserPlants];
SET IDENTITY_INSERT [ESH].[CLIP].[UserPlants] OFF;

-- Migrate ActivityTrainings
INSERT INTO [ESH].[CLIP].[ActivityTrainings] ([Id], [UserId], [ActivityName], [ActivityDate], [Document], [CEPPointsGained], [CPDPointsGained])
SELECT [Id], [UserId], [ActivityName], [ActivityDate], [Document], [CEPPointsGained], [CPDPointsGained]
FROM [CLIP].[dbo].[ActivityTrainings];

-- Migrate Monitoring
SET IDENTITY_INSERT [ESH].[CLIP].[Monitoring] ON;
INSERT INTO [ESH].[CLIP].[Monitoring] ([MonitoringID], [MonitoringName], [MonitoringCategory], [MonitoringFreq])
SELECT [MonitoringID], [MonitoringName], [MonitoringCategory], [MonitoringFreq]
FROM [CLIP].[dbo].[Monitoring];
SET IDENTITY_INSERT [ESH].[CLIP].[Monitoring] OFF;

-- Migrate PlantMonitoring
SET IDENTITY_INSERT [ESH].[CLIP].[PlantMonitoring] ON;
INSERT INTO [ESH].[CLIP].[PlantMonitoring] ([Id], [PlantID], [MonitoringID], [Area], [ExpDate], [QuoteDate], [QuoteSubmitDate], 
                                            [QuoteCompleteDate], [QuoteUserAssign], [EprDate], [EprSubmitDate], [EprCompleteDate], 
                                            [EprUserAssign], [WorkDate], [WorkSubmitDate], [WorkCompleteDate], [WorkUserAssign], 
                                            [Remarks], [RenewDate], [QuoteDoc], [ePRDoc], [WorkDoc], [ProcStatus], [ExpStatus])
SELECT [Id], [PlantID], [MonitoringID], [Area], [ExpDate], [QuoteDate], [QuoteSubmitDate], 
       [QuoteCompleteDate], [QuoteUserAssign], [EprDate], [EprSubmitDate], [EprCompleteDate], 
       [EprUserAssign], [WorkDate], [WorkSubmitDate], [WorkCompleteDate], [WorkUserAssign], 
       [Remarks], [RenewDate], [QuoteDoc], [ePRDoc], [WorkDoc], [ProcStatus], [ExpStatus]
FROM [CLIP].[dbo].[PlantMonitoring];
SET IDENTITY_INSERT [ESH].[CLIP].[PlantMonitoring] OFF;

-- Migrate CertificateOfFitness
SET IDENTITY_INSERT [ESH].[CLIP].[CertificateOfFitness] ON;
INSERT INTO [ESH].[CLIP].[CertificateOfFitness] ([Id], [PlantId], [RegistrationNo], [ExpiryDate], [MachineName], [Status], 
                                                [Remarks], [DocumentPath], [Location], [HostInfo], [Department], [ResidentInfo])
SELECT [Id], [PlantId], [RegistrationNo], [ExpiryDate], [MachineName], [Status], 
       [Remarks], [DocumentPath], [Location], [HostInfo], [Department], [ResidentInfo]
FROM [CLIP].[dbo].[CertificateOfFitness];
SET IDENTITY_INSERT [ESH].[CLIP].[CertificateOfFitness] OFF;

-- Step 3: Migrate data from FETS database to ESH.FETS schema
PRINT 'Migrating data from FETS database to ESH.FETS schema...'

-- Migrate Plants
SET IDENTITY_INSERT [ESH].[FETS].[Plants] ON;
INSERT INTO [ESH].[FETS].[Plants] ([PlantID], [PlantName])
SELECT [PlantID], [PlantName]
FROM [FETS].[dbo].[Plants];
SET IDENTITY_INSERT [ESH].[FETS].[Plants] OFF;

-- Migrate Users
SET IDENTITY_INSERT [ESH].[FETS].[Users] ON;
INSERT INTO [ESH].[FETS].[Users] ([UserID], [Username], [PasswordHash], [Role], [PlantID])
SELECT [UserID], [Username], [PasswordHash], [Role], [PlantID]
FROM [FETS].[dbo].[Users];
SET IDENTITY_INSERT [ESH].[FETS].[Users] OFF;

-- Migrate Status
SET IDENTITY_INSERT [ESH].[FETS].[Status] ON;
INSERT INTO [ESH].[FETS].[Status] ([StatusID], [StatusName], [ColorCode])
SELECT [StatusID], [StatusName], [ColorCode]
FROM [FETS].[dbo].[Status];
SET IDENTITY_INSERT [ESH].[FETS].[Status] OFF;

-- Migrate FireExtinguisherTypes
SET IDENTITY_INSERT [ESH].[FETS].[FireExtinguisherTypes] ON;
INSERT INTO [ESH].[FETS].[FireExtinguisherTypes] ([TypeID], [TypeName])
SELECT [TypeID], [TypeName]
FROM [FETS].[dbo].[FireExtinguisherTypes];
SET IDENTITY_INSERT [ESH].[FETS].[FireExtinguisherTypes] OFF;

-- Migrate Levels
SET IDENTITY_INSERT [ESH].[FETS].[Levels] ON;
INSERT INTO [ESH].[FETS].[Levels] ([LevelID], [PlantID], [LevelName])
SELECT [LevelID], [PlantID], [LevelName]
FROM [FETS].[dbo].[Levels];
SET IDENTITY_INSERT [ESH].[FETS].[Levels] OFF;

-- Migrate FireExtinguishers
SET IDENTITY_INSERT [ESH].[FETS].[FireExtinguishers] ON;
INSERT INTO [ESH].[FETS].[FireExtinguishers] ([FEID], [SerialNumber], [PlantID], [LevelID], [Location], [TypeID],
                                             [DateExpired], [Remarks], [StatusID], [StandbyStatus], [ServiceRemarks],
                                             [Replacement], [DateSentService], [AreaCode])
SELECT [FEID], [SerialNumber], [PlantID], [LevelID], [Location], [TypeID],
       [DateExpired], [Remarks], [StatusID], [StandbyStatus], [ServiceRemarks],
       [Replacement], [DateSentService], [AreaCode]
FROM [FETS].[dbo].[FireExtinguishers];
SET IDENTITY_INSERT [ESH].[FETS].[FireExtinguishers] OFF;

-- Migrate ServiceReminders
SET IDENTITY_INSERT [ESH].[FETS].[ServiceReminders] ON;
INSERT INTO [ESH].[FETS].[ServiceReminders] ([ReminderID], [FEID], [ReminderDate], [ReminderSent], [DateCompleteService])
SELECT [ReminderID], [FEID], [ReminderDate], [ReminderSent], [DateCompleteService]
FROM [FETS].[dbo].[ServiceReminders];
SET IDENTITY_INSERT [ESH].[FETS].[ServiceReminders] OFF;

-- Migrate MapImages
SET IDENTITY_INSERT [ESH].[FETS].[MapImages] ON;
INSERT INTO [ESH].[FETS].[MapImages] ([MapID], [PlantID], [LevelID], [ImagePath], [UploadDate])
SELECT [MapID], [PlantID], [LevelID], [ImagePath], [UploadDate]
FROM [FETS].[dbo].[MapImages];
SET IDENTITY_INSERT [ESH].[FETS].[MapImages] OFF;

-- Migrate EmailRecipients
SET IDENTITY_INSERT [ESH].[FETS].[EmailRecipients] ON;
INSERT INTO [ESH].[FETS].[EmailRecipients] ([RecipientID], [EmailAddress], [RecipientName], [NotificationType], [IsActive], [DateAdded])
SELECT [RecipientID], [EmailAddress], [RecipientName], [NotificationType], [IsActive], [DateAdded]
FROM [FETS].[dbo].[EmailRecipients];
SET IDENTITY_INSERT [ESH].[FETS].[EmailRecipients] OFF;

-- Migrate ActivityLogs
SET IDENTITY_INSERT [ESH].[FETS].[ActivityLogs] ON;
INSERT INTO [ESH].[FETS].[ActivityLogs] ([LogID], [UserID], [Action], [Description], [EntityType], [EntityID], [IPAddress], [Timestamp])
SELECT [LogID], [UserID], [Action], [Description], [EntityType], [EntityID], [IPAddress], [Timestamp]
FROM [FETS].[dbo].[ActivityLogs];
SET IDENTITY_INSERT [ESH].[FETS].[ActivityLogs] OFF;

-- Step 4: Re-enable constraints in ESH database
PRINT 'Re-enabling constraints in ESH database...'

-- Re-enable all constraints in CLIP schema
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL', @whereand = ' AND SCHEMA_NAME(schema_id) = ''CLIP''';

-- Re-enable all constraints in FETS schema
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL', @whereand = ' AND SCHEMA_NAME(schema_id) = ''FETS''';

PRINT 'Data migration completed successfully!'
GO 