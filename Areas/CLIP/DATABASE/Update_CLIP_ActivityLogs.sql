-- Script to update ActivityTrainings table structure
-- Rename CEPPointsGained to ATOM_CEP_Points
-- Rename CPDPointsGained to DOE_CPD_Points
-- Add new column DOSH_CEP_Points

-- Step 1: Rename columns
EXEC sp_rename 'ESH.CLIP.ActivityTrainings.CEPPointsGained', 'ATOM_CEP_Points', 'COLUMN';
EXEC sp_rename 'ESH.CLIP.ActivityTrainings.CPDPointsGained', 'DOE_CPD_Points', 'COLUMN';

-- Step 2: Add new column
ALTER TABLE ESH.CLIP.ActivityTrainings
ADD DOSH_CEP_Points INT NULL;

-- Verification query to check the updated structure
SELECT TOP (1000) [Id]
      ,[UserId]
      ,[ActivityName]
      ,[ActivityDate]
      ,[Document]
      ,[ATOM_CEP_Points]
      ,[DOE_CPD_Points]
      ,[DOSH_CEP_Points]
FROM [ESH].[CLIP].[ActivityTrainings];
