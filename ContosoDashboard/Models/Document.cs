using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required, MaxLength(255)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Tags { get; set; }
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string FilePath { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string FileType { get; set; } = string.Empty;
    [Required] public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    [Required] public int UploadedByUserId { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    [Required, MaxLength(32)] public string ScanStatus { get; set; } = DocumentScanStatus.PendingScan;
    [Required] public Guid ScanJobId { get; set; }
    public int ScanAttempts { get; set; }
    public DateTime ScanUpdatedDate { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? ScanFailureReason { get; set; }

    [ForeignKey(nameof(UploadedByUserId))] public virtual User UploadedByUser { get; set; } = null!;
    [ForeignKey(nameof(ProjectId))] public virtual Project? Project { get; set; }
    [ForeignKey(nameof(TaskId))] public virtual TaskItem? Task { get; set; }
    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
    public virtual DocumentScanJob? ScanJob { get; set; }
}

public static class DocumentCategory
{
    public static readonly string[] Values = { "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other" };
}

public static class DocumentScanStatus
{
    public const string PendingScan = "PendingScan";
    public const string Available = "Available";
    public const string Rejected = "Rejected";
    public const string ScanUnavailable = "ScanUnavailable";
}