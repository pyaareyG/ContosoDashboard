# Research: Document Upload and Management

## Decision: Extend the existing EF Core and service architecture

**Rationale**: The application already centralizes persistence in `ApplicationDbContext` and authorization-sensitive business rules in services such as `ProjectService` and `TaskService`. Adding document entities and a `DocumentService` keeps the feature understandable for training and avoids a broad rewrite.

**Alternatives considered**: A separate document API or repository layer was rejected because the application is a single Blazor Server project and the constitution prioritizes simple, local patterns.

## Decision: Use local filesystem storage behind `IFileStorageService`

**Rationale**: The feature must run offline and files must remain outside `wwwroot`. A configurable root directory with relative, GUID-based paths supports secure local operation and later replacement by an Azure Blob implementation without changing document business rules.

**Alternatives considered**: Storing file bytes in SQL was rejected because it complicates the training database and does not match the requested migration path. Static files were rejected because they bypass service authorization.

## Decision: Generate the storage path before metadata persistence

**Rationale**: The upload workflow is validate and scan, generate a unique relative path, write the file, then insert metadata. If metadata persistence fails, the service deletes the stored file; if storage fails, no available document record is created. User filenames are retained only as display metadata and never used as storage identity.

**Alternatives considered**: Inserting metadata first was rejected because it can leave empty paths or orphaned records after a filesystem failure.

## Decision: Make scanning an explicit fail-closed abstraction

**Rationale**: Malware scanning is a security requirement, but the training app cannot depend on a cloud service. `IFileScanner` allows a locally configured scanner implementation. The service must reject or retain the file in quarantine when scanning reports malware, cannot complete, or is unavailable.

**Alternatives considered**: Treating extension checks as a virus scan was rejected because allowlisting file types does not detect malware.

## Decision: Process scans asynchronously through a queue

**Rationale**: Uploading a 25 MB file and invoking a scanner can exceed an interactive Blazor request's useful lifetime. The application writes the file to quarantine, persists `PendingScan` metadata, and enqueues a stable scan message. The user receives a queued status and can see the status transition to available or rejected. This keeps upload responsiveness separate from scan duration and makes retries explicit.

**Azure implementation**: Use Azure Queue Storage as the queue and an isolated-process .NET Azure Function with a Queue Storage trigger as the worker. The function loads the document by identifier, verifies the document is still pending and the quarantine path matches, scans the content, and applies an idempotent state transition. Configure retry behavior and a poison queue for messages that exceed the retry limit. Protect storage and queue access with managed identity where deployed, require authenticated function access, and emit scan duration, outcome, retry, and poison-message telemetry to Application Insights.

**Offline implementation**: Use the same `IDocumentScanQueue` and scan-processing contract with a local durable queue or hosted `BackgroundService`. The local worker preserves the same pending, clean, rejected, retry, and unavailable states without requiring Azure.

**Alternatives considered**: Synchronous scanning in the upload request was rejected because it increases timeout risk and prevents resilient retry handling. A cloud-only queue was rejected because the constitution requires offline execution.

## Decision: Make scan jobs idempotent and fail closed

**Rationale**: Queue delivery is at-least-once, so duplicate messages are expected. A job includes a stable job/document identifier and the worker ignores already-completed states. Transient storage or scanner errors are retried with bounded backoff; repeated failures move to a poison queue and leave the document unavailable for operator review. Preview and download reject every state except `Available`.

**Alternatives considered**: Assuming exactly-once delivery was rejected because it is not a safe queue processing assumption.

## Decision: Deploy the Azure worker as an isolated .NET Function

**Rationale**: The Azure path should use the current .NET isolated worker model with Functions host v4 and a Queue Storage trigger. Flex Consumption (FC1) provides scale-to-zero behavior appropriate for bursty scan jobs. Managed identity avoids storage credentials in application settings, Application Insights supplies operational visibility, and private endpoints or equivalent network restrictions reduce exposure of quarantined content.

**Alternatives considered**: An always-on web-hosted worker was rejected because it adds operational cost and couples scan throughput to the dashboard process. Anonymous Function access and embedded storage secrets were rejected because they weaken the security boundary.

## Decision: Authorize through one document service and recheck at delivery

**Rationale**: Search and list queries should filter by current ownership, project membership, role, or explicit share. Download and preview endpoints must call the same authorization decision immediately before opening the file so stale UI state cannot create an IDOR path.

**Alternatives considered**: UI-only hiding was rejected because Blazor rendering is not a security boundary.

## Decision: Store categories as text and keys as integers

**Rationale**: This matches the stakeholder constraints and existing model conventions. Category values are validated against a single predefined list in the service, while `DocumentId` and association keys remain integer foreign keys.

**Alternatives considered**: An enum column was rejected because the requested database contract is text-based and categories may evolve.

## Decision: Use metadata search only in the initial release

**Rationale**: The specification names title, description, tags, uploader, and project as search fields and explicitly does not require content indexing. Indexed metadata queries can meet the 2-second target for the stated 500-document list scope without adding a search engine.

**Alternatives considered**: Full-text or external search was rejected as unnecessary infrastructure for the offline training feature.