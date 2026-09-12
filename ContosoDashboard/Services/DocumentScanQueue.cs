using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class LocalDocumentScanQueue : IDocumentScanQueue
{
    private readonly string _queuePath;
    private readonly ConcurrentQueue<DocumentScanMessage> _messages = new();
    private readonly SemaphoreSlim _signal = new(0);

    public LocalDocumentScanQueue(IOptions<DocumentStorageOptions> options, IWebHostEnvironment environment)
    {
        var configured = options.Value.ScanQueuePath;
        _queuePath = Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(_queuePath);
        foreach (var file in Directory.EnumerateFiles(_queuePath, "*.json"))
        {
            try
            {
                var message = JsonSerializer.Deserialize<DocumentScanMessage>(File.ReadAllText(file));
                if (message is not null) { _messages.Enqueue(message); _signal.Release(); }
            }
            catch { }
        }
    }

    public async Task EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_queuePath, $"{message.DocumentScanJobId:N}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(message), cancellationToken);
        _messages.Enqueue(message);
        _signal.Release();
    }

    public async Task<DocumentScanMessage> DequeueAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _signal.WaitAsync(cancellationToken);
            if (_messages.TryDequeue(out var message)) return message;
        }
    }
}