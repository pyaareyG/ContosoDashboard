using Microsoft.Extensions.Hosting;

namespace ContosoDashboard.Services;

public sealed class DocumentScanWorker : BackgroundService
{
    private readonly LocalDocumentScanQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentScanWorker> _logger;

    public DocumentScanWorker(LocalDocumentScanQueue queue, IServiceScopeFactory scopeFactory, ILogger<DocumentScanWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queue.DequeueAsync(stoppingToken);
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var result = await service.ProcessScanAsync(message, stoppingToken);
                if (result.Status == FileScanStatus.Unavailable && result.Reason == "The scan could not be completed.")
                {
                    await _queue.EnqueueAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Document scan worker failed while processing a queued item.");
            }
        }
    }
}