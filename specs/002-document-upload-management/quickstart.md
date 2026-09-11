# Document Management Validation Guide

## Prerequisites

- .NET 8 SDK and SQL Server LocalDB.
- A clean local database or a database created by the application's existing startup initialization.
- A locally configured upload root outside `wwwroot` and a working local file scanner implementation.
- For the Azure path: an Azure Storage Queue, Function App using the isolated .NET worker model, configured queue trigger, and Application Insights; Azure deployment is optional for offline validation.
- Run commands from `ContosoDashboard/`.

## Setup

```powershell
dotnet restore
dotnet build
dotnet run
```

Open the HTTPS URL printed by the application and use the seeded mock users from the repository README.

## Acceptance scenarios

1. Log in as Ni Kang, upload a supported PDF under 25 MB with a title, category, tags, and project. Confirm progress, success, metadata, and the generated storage path outside `wwwroot`.
2. Confirm the upload initially shows `PendingScan` and is not downloadable or previewable. After the local worker completes a clean scan, confirm it transitions to `Available`.
3. Attempt an unsupported extension and a file over 25 MB. Confirm each is rejected, no document is listed, and no usable file remains on disk.
4. Simulate scanner failure and a transient queue failure. Confirm retries occur, the document remains inaccessible, and an exhausted job is isolated for operator review.
5. Search and filter as the uploader by title, category, project, and date; verify sorting by title, date, category, and size.
6. Log in as another project member and confirm available project documents are visible and downloadable. Log in as a non-member and confirm the same document is absent and direct download is denied.
7. Share a document with a user and with a department. Confirm the recipient notification and shared-document view; attempt an unauthorized share and confirm no access change.
8. Edit metadata, replace the file, preview a PDF/image, download it, and delete it after confirmation. Confirm each operation is reflected in the UI and audit records.
9. Open the dashboard and confirm the document count and five newest uploads. Open a project and task detail view and confirm authorized document associations and task project inheritance.
10. As an administrator, generate an activity report. As an employee, attempt the same action and confirm access is denied.

## Azure Functions path

1. Configure the web app with Azure Queue Storage and the quarantine/available storage settings.
2. Upload a file and verify one message is created containing only the stable document/job identifiers and quarantine path.
3. Run the isolated-process Azure Function locally or deploy it to a Function App. Verify the Queue Storage trigger changes a clean document to `Available` and a malicious document to `Rejected`.
4. Force a transient scanner/storage failure and verify bounded retries, visibility timeout behavior, and movement to the poison queue after the retry limit.
5. Verify Application Insights contains scan outcome and latency telemetry without file contents, and verify the Function is not configured for anonymous access.

## Performance and security checks

- Measure a 25 MB upload, a 500-document list, metadata search, and PDF/image preview against the specification targets.
- Verify files cannot be fetched through static URLs or path traversal.
- Verify service calls and direct delivery endpoints enforce current claims, ownership, role, project membership, and share permissions.
- Verify a failed storage, database, or scanner operation leaves no available orphan record and that cleanup failures are visible.
- Verify duplicate delivery of the same queue message does not rescan or incorrectly transition a completed document.

The entity relationships and validation rules are defined in [data-model.md](data-model.md); interface behavior is defined in [contracts/document-storage-and-access.md](contracts/document-storage-and-access.md).