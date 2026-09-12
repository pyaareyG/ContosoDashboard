using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IFileScanner _scanner;
    private readonly IDocumentScanQueue _queue;
    private readonly DocumentStorageOptions _options;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IFileScanner scanner, IDocumentScanQueue queue, IOptions<DocumentStorageOptions> options, ILogger<DocumentService> logger)
    {
        _context = context;
        _storage = storage;
        _scanner = scanner;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Document> UploadAsync(int userId, string title, string? description, string category, string? tags, int? projectId, int? taskId, Stream content, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        ValidateUpload(title, category, fileName, contentType, fileSize);
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken) ?? throw new UnauthorizedAccessException();
        Project? project = null;
        if (projectId.HasValue)
        {
            project = await _context.Projects.Include(p => p.ProjectMembers).FirstOrDefaultAsync(p => p.ProjectId == projectId.Value, cancellationToken);
            if (project is null || (project.ProjectManagerId != userId && !project.ProjectMembers.Any(member => member.UserId == userId))) throw new UnauthorizedAccessException();
        }
        if (taskId.HasValue)
        {
            var task = await _context.Tasks.FindAsync(new object[] { taskId.Value }, cancellationToken) ?? throw new ArgumentException("Task was not found.");
            if (task.ProjectId != projectId) throw new ArgumentException("Task and project associations must match.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var jobId = Guid.NewGuid();
        var relativePath = $"{userId}/{projectId?.ToString() ?? "personal"}/{jobId:N}{extension}";
        await _storage.UploadAsync(content, relativePath, contentType, cancellationToken);
        var document = new Document
        {
            Title = title.Trim(), Description = description, Category = category, Tags = tags,
            OriginalFileName = Path.GetFileName(fileName), FilePath = relativePath, FileType = contentType,
            FileSize = fileSize, UploadedByUserId = userId, ProjectId = projectId, TaskId = taskId,
            ScanStatus = DocumentScanStatus.PendingScan, ScanJobId = jobId, ScanUpdatedDate = DateTime.UtcNow
        };
        var job = new DocumentScanJob { DocumentScanJobId = jobId, Document = document, DocumentId = document.DocumentId, FilePath = relativePath };
        _context.Documents.Add(document);
        _context.DocumentScanJobs.Add(job);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _queue.EnqueueAsync(new DocumentScanMessage(jobId, document.DocumentId, relativePath, DateTime.UtcNow), cancellationToken);
            return document;
        }
        catch
        {
            _context.Entry(document).State = EntityState.Detached;
            await _storage.DeleteAsync(relativePath, cancellationToken);
            throw;
        }
    }

    public async Task<Document?> GetAuthorizedAsync(int documentId, int userId, bool includePending = false)
    {
        var document = await _context.Documents.Include(d => d.Project).ThenInclude(p => p!.ProjectMembers).FirstOrDefaultAsync(d => d.DocumentId == documentId, CancellationToken.None);
        if (document is null || (!includePending && document.ScanStatus != DocumentScanStatus.Available)) return null;
        var isAdmin = await _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator);
        var isOwner = document.UploadedByUserId == userId;
        var isManager = document.Project?.ProjectManagerId == userId;
        var isMember = document.Project?.ProjectMembers.Any(member => member.UserId == userId) == true;
        var shared = await _context.DocumentShares.AnyAsync(share => share.DocumentId == documentId && (share.SharedWithUserId == userId || (share.SharedWithDepartment != null && _context.Users.Any(u => u.UserId == userId && u.Department == share.SharedWithDepartment))));
        return isAdmin || isOwner || isManager || isMember || shared ? document : null;
    }

    public async Task<List<Document>> SearchAsync(int userId, DocumentQuery query)
    {
        var user = await _context.Users.FindAsync(new object[] { userId });
        if (user is null) return new List<Document>();
        var accessibleProjects = _context.ProjectMembers.Where(member => member.UserId == userId).Select(member => member.ProjectId);
        var shares = _context.DocumentShares.Where(share => share.SharedWithUserId == userId || (share.SharedWithDepartment != null && share.SharedWithDepartment == user.Department)).Select(share => share.DocumentId);
        var documents = _context.Documents.Include(document => document.Project).Include(document => document.UploadedByUser)
            .Where(document => document.ScanStatus == DocumentScanStatus.Available &&
                (user.Role == UserRole.Administrator || document.UploadedByUserId == userId ||
                 (document.ProjectId.HasValue && (document.Project!.ProjectManagerId == userId || accessibleProjects.Contains(document.ProjectId.Value))) || shares.Contains(document.DocumentId)));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            documents = documents.Where(document => document.Title.Contains(search) || (document.Description != null && document.Description.Contains(search)) || (document.Tags != null && document.Tags.Contains(search)) || document.OriginalFileName.Contains(search) || document.UploadedByUser.DisplayName.Contains(search) || (document.Project != null && document.Project.Name.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(query.Category)) documents = documents.Where(document => document.Category == query.Category);
        if (query.ProjectId.HasValue) documents = documents.Where(document => document.ProjectId == query.ProjectId);
        if (query.FromDate.HasValue) documents = documents.Where(document => document.UploadedDate >= query.FromDate);
        if (query.ToDate.HasValue) documents = documents.Where(document => document.UploadedDate < query.ToDate.Value.AddDays(1));
        documents = query.SortBy.ToLowerInvariant() switch
        {
            "title" => query.Descending ? documents.OrderByDescending(document => document.Title) : documents.OrderBy(document => document.Title),
            "category" => query.Descending ? documents.OrderByDescending(document => document.Category) : documents.OrderBy(document => document.Category),
            "size" => query.Descending ? documents.OrderByDescending(document => document.FileSize) : documents.OrderBy(document => document.FileSize),
            _ => query.Descending ? documents.OrderByDescending(document => document.UploadedDate) : documents.OrderBy(document => document.UploadedDate)
        };
        return await documents.Take(500).ToListAsync();
    }

    public async Task<bool> ShareAsync(int documentId, int requestingUserId, int? recipientUserId, string? recipientDepartment)
    {
        if ((recipientUserId.HasValue ? 1 : 0) + (!string.IsNullOrWhiteSpace(recipientDepartment) ? 1 : 0) != 1) return false;
        var document = await _context.Documents.Include(d => d.Project).FirstOrDefaultAsync(d => d.DocumentId == documentId && d.ScanStatus == DocumentScanStatus.Available);
        if (document is null) return false;
        var user = await _context.Users.FindAsync(requestingUserId);
        if (user is null) return false;
        var authorized = document.UploadedByUserId == requestingUserId || document.Project?.ProjectManagerId == requestingUserId || user.Role == UserRole.Administrator;
        if (!authorized) return false;
        var duplicate = await _context.DocumentShares.AnyAsync(share => share.DocumentId == documentId && share.SharedWithUserId == recipientUserId && share.SharedWithDepartment == recipientDepartment);
        if (duplicate) return true;
        _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, SharedWithUserId = recipientUserId, SharedWithDepartment = recipientDepartment, SharedByUserId = requestingUserId });
        if (recipientUserId.HasValue)
        {
            _context.Notifications.Add(new Notification { UserId = recipientUserId.Value, Title = "Document shared with you", Message = $"A document has been shared with you: {document.Title}", Type = NotificationType.DocumentShared, Priority = NotificationPriority.Informational });
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags)
    {
        if (string.IsNullOrWhiteSpace(title) || !DocumentCategory.Values.Contains(category, StringComparer.Ordinal)) return false;
        var document = await _context.Documents.Include(d => d.Project).FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (document is null || (document.UploadedByUserId != requestingUserId && document.Project?.ProjectManagerId != requestingUserId)) return false;
        document.Title = title.Trim(); document.Description = description; document.Category = category; document.Tags = tags;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Edit" });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.Project).FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document is null || (document.UploadedByUserId != requestingUserId && document.Project?.ProjectManagerId != requestingUserId)) return false;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Delete" });
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(document.FilePath, cancellationToken);
        return true;
    }

    public async Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream content, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.Project).FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document is null || (document.UploadedByUserId != requestingUserId && document.Project?.ProjectManagerId != requestingUserId)) return false;
        ValidateUpload(document.Title, document.Category, fileName, contentType, fileSize);
        var extension = Path.GetExtension(fileName).ToLowerInvariant(); var jobId = Guid.NewGuid(); var newPath = $"{document.UploadedByUserId}/{document.ProjectId?.ToString() ?? "personal"}/{jobId:N}{extension}";
        await _storage.UploadAsync(content, newPath, contentType, cancellationToken);
        var oldPath = document.FilePath; document.FilePath = newPath; document.OriginalFileName = Path.GetFileName(fileName); document.FileType = contentType; document.FileSize = fileSize; document.ScanJobId = jobId; document.ScanStatus = DocumentScanStatus.PendingScan; document.ScanAttempts = 0; document.ScanUpdatedDate = DateTime.UtcNow;
        _context.DocumentScanJobs.Add(new DocumentScanJob { DocumentScanJobId = jobId, DocumentId = documentId, FilePath = newPath });
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Replace" });
        await _context.SaveChangesAsync(cancellationToken);
        await _queue.EnqueueAsync(new DocumentScanMessage(jobId, documentId, newPath, DateTime.UtcNow), cancellationToken);
        await _storage.DeleteAsync(oldPath, cancellationToken);
        return true;
    }

    public async Task<FileScanResult> ProcessScanAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.ScanJob).FirstOrDefaultAsync(d => d.DocumentId == message.DocumentId, cancellationToken);
        if (document is null || document.ScanJobId != message.DocumentScanJobId || document.ScanStatus != DocumentScanStatus.PendingScan) return new FileScanResult(FileScanStatus.Unavailable, "Job is already complete or invalid.");
        try
        {
            await using var stream = await _storage.DownloadAsync(document.FilePath, cancellationToken);
            var result = await _scanner.ScanAsync(stream, document.OriginalFileName, document.FileType, cancellationToken);
            document.ScanAttempts++;
            document.ScanUpdatedDate = DateTime.UtcNow;
            document.ScanFailureReason = result.Reason;
            document.ScanStatus = result.Status switch
            {
                FileScanStatus.Clean => DocumentScanStatus.Available,
                FileScanStatus.Malicious => DocumentScanStatus.Rejected,
                _ => DocumentScanStatus.ScanUnavailable
            };
            if (document.ScanJob is not null)
            {
                document.ScanJob.Status = document.ScanStatus;
                document.ScanJob.CompletedDate = DateTime.UtcNow;
                document.ScanJob.Attempt = document.ScanAttempts;
            }
            await _context.SaveChangesAsync(cancellationToken);
            if (result.Status == FileScanStatus.Clean && document.ProjectId.HasValue)
            {
                var memberIds = await _context.ProjectMembers.Where(member => member.ProjectId == document.ProjectId && member.UserId != document.UploadedByUserId).Select(member => member.UserId).ToListAsync(cancellationToken);
                foreach (var memberId in memberIds)
                {
                    var alreadyNotified = await _context.Notifications.AnyAsync(notification => notification.UserId == memberId && notification.Type == NotificationType.ProjectDocumentAdded && notification.Message.Contains(document.Title), cancellationToken);
                    if (!alreadyNotified) _context.Notifications.Add(new Notification { UserId = memberId, Title = "New project document", Message = $"A new document is available: {document.Title}", Type = NotificationType.ProjectDocumentAdded, Priority = NotificationPriority.Informational });
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Document scan failed for document {DocumentId}.", message.DocumentId);
            document.ScanAttempts++;
            document.ScanStatus = document.ScanAttempts >= _options.ScanRetryLimit ? DocumentScanStatus.ScanUnavailable : DocumentScanStatus.PendingScan;
            document.ScanFailureReason = "The scan could not be completed.";
            await _context.SaveChangesAsync(cancellationToken);
            return new FileScanResult(FileScanStatus.Unavailable, document.ScanFailureReason);
        }
    }

    private void ValidateUpload(string title, string category, string fileName, string contentType, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Document title is required.");
        if (!DocumentCategory.Values.Contains(category, StringComparer.Ordinal)) throw new ArgumentException("Document category is invalid.");
        if (fileSize <= 0 || fileSize > _options.MaxFileSizeBytes) throw new ArgumentException("File exceeds the 25 MB size limit.");
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("File type is not supported.");
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("File type is required.");
    }
}