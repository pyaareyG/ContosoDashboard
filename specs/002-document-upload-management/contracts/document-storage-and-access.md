# Document Storage and Access Contracts

## `IFileStorageService`

The storage abstraction is implementation-independent and uses relative paths only.

- `Task<string> UploadAsync(Stream content, string relativePath, string contentType, CancellationToken cancellationToken)`
  - Creates parent directories as needed and returns the stored relative path.
  - Must not accept an absolute path or a path containing traversal segments.
- `Task<Stream> DownloadAsync(string relativePath, CancellationToken cancellationToken)`
  - Opens a read-only stream only after the caller has authorized the document.
- `Task DeleteAsync(string relativePath, CancellationToken cancellationToken)`
  - Deletes the stored file and is safe when cleanup is retried for a missing file.
- `Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken)`
  - Supports integrity checks without exposing filesystem paths.

The local implementation resolves all paths beneath the configured upload root and rejects path escape attempts. A future cloud implementation maps the same relative path to a blob name.

## `IFileScanner`

- `Task<FileScanResult> ScanAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)`
- `FileScanResult` must distinguish `Clean`, `Malicious`, and `Unavailable`.

`DocumentService` accepts only `Clean`; `Malicious` and `Unavailable` are rejected without making metadata available.

## `IDocumentScanQueue`

- `Task EnqueueAsync(DocumentScanMessage message, CancellationToken cancellationToken)`
- `DocumentScanMessage` contains `DocumentScanJobId`, `DocumentId`, quarantine-relative `FilePath`, and an enqueue timestamp.
- Queue messages contain no file bytes and no secrets.

Implementations:

- `LocalDocumentScanQueue` persists work for the offline hosted worker.
- `AzureDocumentScanQueue` writes to Azure Queue Storage using configuration and managed identity when deployed.

Azure queue processing requirements:

- The trigger uses the isolated-process Azure Functions model and Queue Storage bindings.
- Processing is idempotent by `DocumentScanJobId` and checks the database state before scanning.
- Transient failures are retried with bounded visibility timeout/backoff; messages exceeding the retry limit are moved to a poison queue.
- Poison jobs leave the document inaccessible and record an operator-review state.
- Function authentication is not anonymous; storage and queue access use managed identity where available.
- Application Insights records outcome, latency, retry count, and poison-job telemetry without logging file contents.

Deployment expectations for the Azure implementation are Functions host v4, .NET isolated process, Flex Consumption (FC1) where available, managed identity for Queue/Blob access, authenticated non-anonymous invocation, and Application Insights enabled. Use private endpoints or equivalent network controls for storage and scanner access when supported by the deployment environment.

## Scan state contract

- `PendingScan`: upload persisted and queued; no user access.
- `Available`: scan completed clean; normal authorization rules apply.
- `Rejected`: scan found malware or the file failed safety validation; no user access.
- `ScanUnavailable`: scan could not complete after bounded retries; no user access until operator resolution.

The UI polls or refreshes document status after upload and must clearly distinguish queued, available, rejected, and unavailable outcomes.

## `IDocumentService`

All methods receive the requesting user identity explicitly and perform authorization internally.

- Upload one or more files with metadata and optional project/task context.
- Query authorized documents with search, sort, filter, and paging inputs.
- Get a single authorized document for display.
- Open an authorized file for preview/download.
- Edit metadata and replace a file.
- Delete a document after authorization and confirmation at the UI boundary.
- Share with a user or department and create notifications.
- Return recent documents, counts, and administrator-only audit summaries.

Failure behavior is non-disclosing: unauthorized document identifiers return not-found/false results rather than protected metadata.

## Protected HTTP delivery

The application maps authenticated endpoints for `/documents/{id}/download` and `/documents/{id}/preview`. Each endpoint:

1. Reads the current authenticated user claims.
2. Calls `IDocumentService` to authorize and resolve the document.
3. Opens the file through `IFileStorageService`.
4. Returns the stored MIME type and a safe download name, or an authorization/not-found response.
5. Records Download or Preview activity only after an authorized open.