using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public string ScanQueuePath { get; set; } = "AppData/scan-queue";
    public bool LocalScannerEnabled { get; set; } = true;
    public int ScanRetryLimit { get; set; } = 3;
}

public enum FileScanStatus { Clean, Malicious, Unavailable }
public sealed record FileScanResult(FileScanStatus Status, string? Reason = null);
public sealed record DocumentScanMessage(Guid DocumentScanJobId, int DocumentId, string FilePath, DateTime EnqueuedDate);
public sealed class DocumentQuery
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "date";
    public bool Descending { get; set; } = true;
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
}

public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

public interface IDocumentScanQueue
{
    Task EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken = default);
}

public interface IDocumentService
{
    Task<Document> UploadAsync(int userId, string title, string? description, string category, string? tags, int? projectId, int? taskId, Stream content, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<Document?> GetAuthorizedAsync(int documentId, int userId, bool includePending = false);
    Task<List<Document>> SearchAsync(int userId, DocumentQuery query);
    Task<bool> ShareAsync(int documentId, int requestingUserId, int? recipientUserId, string? recipientDepartment);
    Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags);
    Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream content, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<FileScanResult> ProcessScanAsync(DocumentScanMessage message, CancellationToken cancellationToken = default);
}