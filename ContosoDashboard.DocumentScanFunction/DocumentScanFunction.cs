using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ContosoDashboard.Services;

namespace ContosoDashboard.DocumentScanFunction;

public sealed class DocumentScanFunction
{
    private readonly ILogger<DocumentScanFunction> _logger;
    private readonly IDocumentService _documents;
    public DocumentScanFunction(ILogger<DocumentScanFunction> logger, IDocumentService documents) { _logger = logger; _documents = documents; }

    [Function("ProcessDocumentScan")]
    public async Task RunAsync(
        [QueueTrigger("%DocumentScanQueueName%", Connection = "AzureWebJobsStorage")] string message,
        FunctionContext executionContext)
    {
        var scanMessage = JsonSerializer.Deserialize<DocumentScanMessage>(message)
            ?? throw new InvalidOperationException("Invalid document scan message.");
        _logger.LogInformation("Processing document scan job {JobId} for document {DocumentId}.", scanMessage.DocumentScanJobId, scanMessage.DocumentId);
        await _documents.ProcessScanAsync(scanMessage);
    }
}