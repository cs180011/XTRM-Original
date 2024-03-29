USE [master]
GO
/****** Object:  Database [XTRM]    Script Date: 11/5/2015 6:29:53 PM ******/
CREATE DATABASE [XTRM]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'XTRM', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\XTRM.mdf' , SIZE = 4096KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'XTRM_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\XTRM_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [XTRM] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [XTRM].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [XTRM] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [XTRM] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [XTRM] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [XTRM] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [XTRM] SET ARITHABORT OFF 
GO
ALTER DATABASE [XTRM] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [XTRM] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [XTRM] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [XTRM] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [XTRM] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [XTRM] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [XTRM] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [XTRM] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [XTRM] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [XTRM] SET  DISABLE_BROKER 
GO
ALTER DATABASE [XTRM] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [XTRM] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [XTRM] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [XTRM] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [XTRM] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [XTRM] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [XTRM] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [XTRM] SET RECOVERY FULL 
GO
ALTER DATABASE [XTRM] SET  MULTI_USER 
GO
ALTER DATABASE [XTRM] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [XTRM] SET DB_CHAINING OFF 
GO
ALTER DATABASE [XTRM] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [XTRM] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [XTRM] SET DELAYED_DURABILITY = DISABLED 
GO
USE [XTRM]
GO
/****** Object:  Table [dbo].[events]    Script Date: 11/5/2015 6:29:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[events](
	[event_serial] [int] IDENTITY(1,1) NOT NULL,
	[event_UUID] [nvarchar](50) NULL,
	[host] [nvarchar](50) NOT NULL,
	[tag] [nvarchar](50) NOT NULL,
	[source] [nvarchar](50) NOT NULL,
	[action] [nvarchar](50) NOT NULL,
	[state] [int] NOT NULL,
	[user_id] [nvarchar](50) NOT NULL,
	[event_date] [datetime] NOT NULL,
	[eval_date] [datetime] NULL,
	[chain] [int] NULL,
	[parm1] [nvarchar](max) NULL,
	[parm2] [nvarchar](max) NULL,
	[parm3] [nvarchar](max) NULL,
	[parm4] [nvarchar](max) NULL,
	[parm5] [nvarchar](max) NULL,
	[parm6] [nvarchar](max) NULL,
	[parm7] [nvarchar](max) NULL,
	[parm8] [nvarchar](max) NULL,
	[parm9] [nvarchar](max) NULL,
	[parm10] [nvarchar](max) NULL,
	[parm11] [nvarchar](max) NULL,
	[parm12] [nvarchar](max) NULL,
	[parm13] [nvarchar](max) NULL,
	[parm14] [nvarchar](max) NULL,
	[parm15] [nvarchar](max) NULL,
	[retention] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[event_serial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[tasklog]    Script Date: 11/5/2015 6:29:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tasklog](
	[log_serial] [int] IDENTITY(1,1) NOT NULL,
	[task_serial] [int] NOT NULL,
	[log_time] [datetime] NOT NULL,
	[log_id] [int] NOT NULL,
	[log_text] [nvarchar](max) NULL,
	[user_id] [nvarchar](255) NOT NULL,
	[result] [int] NOT NULL,
	[esource] [nvarchar](max) NULL,
	[eseverity] [int] NULL,
	[estate] [int] NULL,
	[enumber] [int] NULL,
	[eprocedure] [nvarchar](max) NULL,
	[eline] [int] NULL,
	[emessage] [nvarchar](max) NULL,
	[userrole] [nvarchar](255) NULL,
	[hostname] [nvarchar](255) NULL,
	[dbname] [nvarchar](255) NULL,
	[appname] [nvarchar](255) NULL,
	[retention] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[log_serial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[workjobdata]    Script Date: 11/5/2015 6:29:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[workjobdata](
	[data_serial] [int] IDENTITY(1,1) NOT NULL,
	[job_serial] [int] NOT NULL,
	[key_name] [nvarchar](50) NOT NULL,
	[key_value] [nvarchar](max) NOT NULL,
	[entry_date] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[data_serial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[workjobs]    Script Date: 11/5/2015 6:29:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[workjobs](
	[job_serial] [int] IDENTITY(1,1) NOT NULL,
	[host] [nvarchar](255) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[desc] [nvarchar](max) NOT NULL,
	[type] [int] NOT NULL,
	[start] [datetime] NULL,
	[stop] [datetime] NULL,
	[sequence] [int] NULL,
	[status] [int] NOT NULL,
	[result] [int] NOT NULL,
	[priority] [int] NULL,
	[limit] [int] NULL,
	[display] [nvarchar](255) NULL,
	[event_serial] [int] NULL,
	[retention] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[job_serial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[worktasks]    Script Date: 11/5/2015 6:29:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[worktasks](
	[job_serial] [int] NOT NULL,
	[task_serial] [int] IDENTITY(1,1) NOT NULL,
	[sequence] [int] NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[pid] [int] NOT NULL,
	[status] [int] NOT NULL,
	[result] [int] NOT NULL,
	[start] [datetime] NULL,
	[stop] [datetime] NULL,
	[condition] [int] NULL,
	[priority] [int] NULL,
	[limit] [int] NULL,
	[display] [nvarchar](255) NULL,
	[path] [nvarchar](512) NULL,
	[executable] [nvarchar](255) NULL,
	[parm0] [nvarchar](255) NULL,
	[parm1] [nvarchar](255) NULL,
	[parm2] [nvarchar](255) NULL,
	[parm3] [nvarchar](255) NULL,
	[parm4] [nvarchar](255) NULL,
	[parm5] [nvarchar](255) NULL,
	[parm6] [nvarchar](255) NULL,
	[parm7] [nvarchar](255) NULL,
	[parm8] [nvarchar](255) NULL,
	[parm9] [nvarchar](255) NULL,
	[parm10] [nvarchar](255) NULL,
	[task_event] [int] NULL,
	[retention] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[task_serial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[events]  WITH CHECK ADD  CONSTRAINT [FK_Events_Event] FOREIGN KEY([chain])
REFERENCES [dbo].[events] ([event_serial])
GO
ALTER TABLE [dbo].[events] CHECK CONSTRAINT [FK_Events_Event]
GO
ALTER TABLE [dbo].[tasklog]  WITH CHECK ADD  CONSTRAINT [FK_tasklog_worktasks] FOREIGN KEY([task_serial])
REFERENCES [dbo].[worktasks] ([task_serial])
GO
ALTER TABLE [dbo].[tasklog] CHECK CONSTRAINT [FK_tasklog_worktasks]
GO
ALTER TABLE [dbo].[workjobdata]  WITH CHECK ADD  CONSTRAINT [FK_WorkJobData_Job] FOREIGN KEY([job_serial])
REFERENCES [dbo].[workjobs] ([job_serial])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[workjobdata] CHECK CONSTRAINT [FK_WorkJobData_Job]
GO
ALTER TABLE [dbo].[workjobs]  WITH CHECK ADD  CONSTRAINT [FK_workjobs_events] FOREIGN KEY([event_serial])
REFERENCES [dbo].[events] ([event_serial])
GO
ALTER TABLE [dbo].[workjobs] CHECK CONSTRAINT [FK_workjobs_events]
GO
ALTER TABLE [dbo].[worktasks]  WITH CHECK ADD  CONSTRAINT [FK_WorkJobs] FOREIGN KEY([job_serial])
REFERENCES [dbo].[workjobs] ([job_serial])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[worktasks] CHECK CONSTRAINT [FK_WorkJobs]
GO
ALTER TABLE [dbo].[worktasks]  WITH CHECK ADD  CONSTRAINT [FK_worktasks_events] FOREIGN KEY([task_event])
REFERENCES [dbo].[events] ([event_serial])
GO
ALTER TABLE [dbo].[worktasks] CHECK CONSTRAINT [FK_worktasks_events]
GO
/****** Object:  StoredProcedure [dbo].[XTRMSelectCandidateJobs]    Script Date: 11/5/2015 6:29:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[XTRMSelectCandidateJobs]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	print 'XTRMSelectCandidateJobs';
END

GO
USE [master]
GO
ALTER DATABASE [XTRM] SET  READ_WRITE 
GO
