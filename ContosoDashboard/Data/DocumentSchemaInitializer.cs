using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public static class DocumentSchemaInitializer
{
    public static async Task EnsureCreatedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        const string sql = """
IF OBJECT_ID(N'[Documents]', N'U') IS NULL
BEGIN
    CREATE TABLE [Documents] (
        [DocumentId] int NOT NULL IDENTITY,
        [Title] nvarchar(255) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Category] nvarchar(100) NOT NULL,
        [Tags] nvarchar(1000) NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileType] nvarchar(255) NOT NULL,
        [FileSize] bigint NOT NULL,
        [UploadedDate] datetime2 NOT NULL,
        [UploadedByUserId] int NOT NULL,
        [ProjectId] int NULL,
        [TaskId] int NULL,
        [ScanStatus] nvarchar(32) NOT NULL,
        [ScanJobId] uniqueidentifier NOT NULL,
        [ScanAttempts] int NOT NULL,
        [ScanUpdatedDate] datetime2 NOT NULL,
        [ScanFailureReason] nvarchar(1000) NULL,
        CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]),
        CONSTRAINT [FK_Documents_Users] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_Documents_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([ProjectId]),
        CONSTRAINT [FK_Documents_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [Tasks]([TaskId])
    );
END;
IF OBJECT_ID(N'[DocumentScanJobs]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentScanJobs] (
        [DocumentScanJobId] uniqueidentifier NOT NULL,
        [DocumentId] int NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [Attempt] int NOT NULL,
        [EnqueuedDate] datetime2 NOT NULL,
        [CompletedDate] datetime2 NULL,
        [Status] nvarchar(32) NOT NULL,
        CONSTRAINT [PK_DocumentScanJobs] PRIMARY KEY ([DocumentScanJobId]),
        CONSTRAINT [FK_DocumentScanJobs_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([DocumentId]) ON DELETE CASCADE
    );
END;
IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentShares] (
        [DocumentShareId] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [SharedWithUserId] int NULL,
        [SharedWithDepartment] nvarchar(100) NULL,
        [SharedByUserId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([DocumentShareId]),
        CONSTRAINT [FK_DocumentShares_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentShares_Users_Recipient] FOREIGN KEY ([SharedWithUserId]) REFERENCES [Users]([UserId]),
        CONSTRAINT [FK_DocumentShares_Users_Sender] FOREIGN KEY ([SharedByUserId]) REFERENCES [Users]([UserId])
    );
END;
IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentActivities] (
        [DocumentActivityId] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [UserId] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_DocumentActivities] PRIMARY KEY ([DocumentActivityId]),
        CONSTRAINT [FK_DocumentActivities_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentActivities_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([UserId])
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ScanJobId' AND object_id = OBJECT_ID(N'[Documents]')) CREATE UNIQUE INDEX [IX_Documents_ScanJobId] ON [Documents]([ScanJobId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedByUserId_UploadedDate' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_UploadedByUserId_UploadedDate] ON [Documents]([UploadedByUserId], [UploadedDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId_ScanStatus' AND object_id = OBJECT_ID(N'[Documents]')) CREATE INDEX [IX_Documents_ProjectId_ScanStatus] ON [Documents]([ProjectId], [ScanStatus]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentActivities_DocumentId_CreatedDate' AND object_id = OBJECT_ID(N'[DocumentActivities]')) CREATE INDEX [IX_DocumentActivities_DocumentId_CreatedDate] ON [DocumentActivities]([DocumentId], [CreatedDate]);
""";

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}