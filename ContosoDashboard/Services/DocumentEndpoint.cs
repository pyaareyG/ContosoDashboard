using System.Security.Claims;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public static class DocumentEndpoint
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/documents/{documentId:int}/download", async (int documentId, ClaimsPrincipal principal, IDocumentService documents, IFileStorageService storage, ApplicationDbContext context, CancellationToken cancellationToken) =>
            await OpenAsync(documentId, principal, false, documents, storage, context, cancellationToken));
        endpoints.MapGet("/documents/{documentId:int}/preview", async (int documentId, ClaimsPrincipal principal, IDocumentService documents, IFileStorageService storage, ApplicationDbContext context, CancellationToken cancellationToken) =>
            await OpenAsync(documentId, principal, true, documents, storage, context, cancellationToken));
        return endpoints;
    }

    private static async Task<IResult> OpenAsync(int documentId, ClaimsPrincipal principal, bool preview, IDocumentService documents, IFileStorageService storage, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Results.Unauthorized();
        var document = await documents.GetAuthorizedAsync(documentId, userId);
        if (document is null) return Results.NotFound();
        if (preview && document.FileType is not ("application/pdf" or "image/jpeg" or "image/png")) return Results.BadRequest("Preview is not supported for this file type.");
        try
        {
            var stream = await storage.DownloadAsync(document.FilePath, cancellationToken);
            context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = preview ? "Preview" : "Download" });
            await context.SaveChangesAsync(cancellationToken);
            return Results.File(stream, document.FileType, preview ? null : document.OriginalFileName, enableRangeProcessing: true);
        }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }
}