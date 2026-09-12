using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentScanJob
{
    [Key] public Guid DocumentScanJobId { get; set; }
    [Required] public int DocumentId { get; set; }
    [Required, MaxLength(500)] public string FilePath { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public DateTime EnqueuedDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }
    [Required, MaxLength(32)] public string Status { get; set; } = "Queued";
    [ForeignKey(nameof(DocumentId))] public virtual Document Document { get; set; } = null!;
}