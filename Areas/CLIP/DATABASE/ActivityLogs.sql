USE [ESH]
GO

/****** Object:  Table [CLIP].[ActivityLogs]    Script Date: 19/6/2025 11:27:10 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [CLIP].[ActivityLogs](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [nvarchar](128) NULL,
	[UserName] [nvarchar](256) NULL,
	[Action] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[EntityName] [nvarchar](100) NULL,
	[EntityID] [nvarchar](100) NULL,
	[OldValue] [nvarchar](max) NULL,
	[NewValue] [nvarchar](max) NULL,
	[IPAddress] [nvarchar](50) NULL,
	[UserAgent] [nvarchar](500) NULL,
	[CreatedAt] [datetime] NOT NULL,
	[PageUrl] [nvarchar](500) NULL,
	[SessionID] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [CLIP].[ActivityLogs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO


