using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContosoDashboard.Services;

namespace ContosoDashboard.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;
    public DocumentController(IDocumentService documentService) => _documentService = documentService;

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null) return BadRequest(new { error = "A file is required." });
        if (request.File.Length > 25 * 1024 * 1024) return BadRequest(new { error = "File exceeds the 25 MB size limit." });
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try
        {
            await using var stream = request.File.OpenReadStream();
            var document = await _documentService.UploadAsync(userId, request.Title, request.Description, request.Category, request.Tags, request.ProjectId, request.TaskId, stream, request.File.FileName, request.File.ContentType, request.File.Length, cancellationToken);
            return Accepted(new { documentId = document.DocumentId, status = document.ScanStatus, scanJobId = document.ScanJobId });
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}

public sealed class DocumentUploadRequest
{
    [FromForm(Name = "title")] public string Title { get; set; } = string.Empty;
    [FromForm(Name = "description")] public string? Description { get; set; }
    [FromForm(Name = "category")] public string Category { get; set; } = string.Empty;
    [FromForm(Name = "tags")] public string? Tags { get; set; }
    [FromForm(Name = "projectId")] public int? ProjectId { get; set; }
    [FromForm(Name = "taskId")] public int? TaskId { get; set; }
    [FromForm(Name = "file")] public IFormFile? File { get; set; }
}