using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class LocalFileScanner : IFileScanner
{
    private readonly DocumentStorageOptions _options;
    public LocalFileScanner(IOptions<DocumentStorageOptions> options) => _options = options.Value;

    public Task<FileScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!_options.LocalScannerEnabled) return Task.FromResult(new FileScanResult(FileScanStatus.Unavailable, "Scanner is unavailable."));
        return Task.FromResult(new FileScanResult(FileScanStatus.Clean));
    }
}