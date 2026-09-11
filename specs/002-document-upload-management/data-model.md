# Data Model: Document Upload and Management

## Document

Represents one accepted uploaded file.

- `DocumentId`: integer primary key.
- `Title`: required, bounded text shown to users.
- `Description`: optional text.
- `Category`: required text constrained to the six predefined categories.
- `Tags`: optional normalized text representation for custom tags.
- `OriginalFileName`: display-only name; never used as a path.
- `FilePath`: required relative storage path containing user/project scope and a generated identifier.
- `FileType`: required MIME type with capacity for 255 characters.
- `FileSize`: required byte count, maximum 25 MB.
- `UploadedDate`: UTC timestamp.
- `UploadedByUserId`: required foreign key to `User`.
- `ProjectId`: optional foreign key to `Project`.
- `TaskId`: optional foreign key to `TaskItem`; when present, it must match the task's project context.
- `ScanStatus`: required text state: `PendingScan`, `Available`, `Rejected`, or `ScanUnavailable`.
- `ScanJobId`: stable identifier for the queued scan request.
- `ScanAttempts`: bounded retry count.
- `ScanUpdatedDate`: UTC timestamp of the latest scan state transition.
- `ScanFailureReason`: optional operator-facing diagnostic that must not expose file contents.

Relationships:

- One user uploads many documents.
- One project has many documents.
- One task has many document associations.
- One document has many shares and activity records.

## DocumentShare

Represents explicit sharing with a user or team.

- `DocumentShareId`: integer primary key.
- `DocumentId`: required foreign key.
- `SharedWithUserId`: optional user recipient.
- `SharedWithDepartment`: optional team/department recipient.
- `SharedByUserId`: required actor.
- `CreatedDate`: UTC timestamp.

Validation rules:

- Exactly one recipient type must be populated.
- A share cannot grant access beyond the owner's or project manager's authority.
- Access is reevaluated against current project membership and recipient identity when the document is opened.
- Duplicate active shares for the same document and recipient are rejected.

## DocumentActivity

Immutable audit record for document operations.

- `DocumentActivityId`: integer primary key.
- `DocumentId`: required foreign key, retained for the activity lifetime supported by the feature.
- `UserId`: required actor foreign key.
- `Action`: required text such as Upload, Download, Preview, Share, Edit, Replace, or Delete.
- `CreatedDate`: UTC timestamp.

## DocumentScanJob

Represents the durable work item consumed by either the local worker or Azure Function.

- `DocumentScanJobId`: stable identifier used for idempotency.
- `DocumentId`: required document identifier.
- `FilePath`: quarantine-relative path captured when queued; the worker verifies it before scanning.
- `Attempt`: delivery/processing attempt number.
- `EnqueuedDate`: UTC timestamp.
- `CompletedDate`: optional UTC timestamp.
- `Status`: queued, processing, clean, rejected, or poison/review.

The queue message should contain identifiers and paths only, never file bytes or sensitive metadata. The database remains the source of truth for document availability.

## Existing entity updates

- `Project` gets a document collection.
- `TaskItem` gets a document association collection.
- `User` gets uploaded-document and share navigation collections as needed for EF relationships.
- `DashboardSummary` gains the document count; a dashboard query returns the five newest documents in authorized user scope.

## State transitions

1. Upload request is `PendingScan` while metadata, size, extension, and initial validation run.
2. A unique quarantine-relative path is generated and the file is written to local storage or blob quarantine.
3. Metadata and a stable scan job are persisted, then the job is enqueued. The document is not available to browse, preview, download, share, or attach while pending.
4. A local worker or Azure Queue-triggered Function processes the job. A clean result transitions the document to `Available`; malware transitions it to `Rejected`; unavailable or exhausted retries transition it to `ScanUnavailable` and keep it inaccessible.
5. Replacement creates a new pending scan and retains the currently available version until the replacement is clean; rejected replacements do not replace the available file.
6. Delete removes the database record and quarantined/available stored file as one service operation; cleanup is retried or surfaced as a failure rather than silently losing auditability.

## Indexes and integrity

- Index `UploadedByUserId`, `ProjectId`, `UploadedDate`, and `Category` for list and filter queries.
- Index `DocumentShare(DocumentId, SharedWithUserId)` and recipient department for access checks.
- Index `DocumentActivity(DocumentId, CreatedDate)` for audit reporting.
- Index `Document(ScanStatus, ScanJobId)` for worker polling, status views, and idempotency checks.
- Restrict cascade behavior where deletion could remove audit or unrelated user/project records.
- Validate project membership and task/project consistency in the service before saving associations.
- Enforce state transitions in the service/worker so only `Available` documents enter access queries.