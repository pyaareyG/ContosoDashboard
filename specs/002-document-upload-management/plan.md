# Implementation Plan: Document Upload and Management

**Branch**: `002-document-upload-management` | **Date**: 2026-09-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-document-upload-management/spec.md`

The feature adds secure local document storage and lifecycle management while preserving the existing Blazor Server, EF Core, mock-authentication, and service-layer patterns.

## Summary

Add document metadata, sharing, activity, scan-job, and task/project associations to the existing EF Core model. Add an `IFileStorageService` with a filesystem implementation that stores GUID-named files outside `wwwroot`, an `IFileScanner` boundary, and an `IDocumentScanQueue` boundary. Uploads are written to quarantine, recorded as `PendingScan`, and queued for asynchronous scanning; only a clean result transitions a document to `Available`. In Azure, the queue is Azure Queue Storage and an isolated-process Azure Function consumes queue messages, invokes the scanner, and updates scan state. Offline mode uses local filesystem storage plus local queue/worker implementations. A `DocumentService` owns validation, authorization, lifecycle operations, notifications, and audit records. Blazor pages provide upload, browse, search, preview, download, sharing, and management workflows; protected Razor endpoints stream files only after service authorization.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# on .NET 8.0  
**Primary Dependencies**: ASP.NET Core 8 Blazor Server, Entity Framework Core 8 SQL Server provider, existing Bootstrap UI and cookie-based mock authentication  
**Storage**: SQL Server LocalDB for metadata; local filesystem under configurable `AppData/uploads` outside `wwwroot`; optional Azure Blob Storage and Azure Queue Storage migration path  
**Testing**: `dotnet build`, focused service/component tests, local queue-worker tests, Azure Function trigger tests, and manual browser acceptance scenarios in [quickstart.md](quickstart.md)  
**Target Platform**: Offline-capable ASP.NET Core web application on Windows training environments  
**Project Type**: Single web project  
**Performance Goals**: Upload result within 30 seconds for 25 MB; authorized lists and searches within 2 seconds for 500 documents; previews within 3 seconds  
**Constraints**: Maximum 25 MB per file; allowlisted types; files outside `wwwroot`; service-layer authorization on every operation; cloud services optional for offline mode; integer document keys and text categories; pending documents unavailable until scan completion  
**Scale/Scope**: Existing seeded users, projects, tasks, notifications, and up to 500 documents per list view; initial release is web-only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Passes before research:

- Security and Access Boundaries: storage, browse, download, preview, share, edit, replace, and delete operations are authorized in `DocumentService` and protected file endpoints.
- Training-Ready Simplicity: one document service and two narrow infrastructure abstractions fit the existing service architecture; no repository layer or external cloud SDK is introduced.
- User Isolation and Data Integrity: queries are scoped to ownership, current project membership, role, or explicit share; file persistence precedes metadata availability and cleanup handles failures.
- Quality Gates: service authorization, validation, storage failure, and lifecycle scenarios are included in the validation guide and implementation tasks.
- Offline-First Architecture: local filesystem, local queue, and local scanner worker are the default; Azure Blob/Queue and Function implementations are optional replacements behind interfaces without changing business logic.
- Async Scan Safety: quarantine state, idempotent job handling, bounded retries, poison-queue isolation, and fail-closed delivery are required.

No violations require a complexity exception.

## Project Structure

### Documentation (this feature)

```text
specs/002-document-upload-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── DocumentActivity.cs
│   └── TaskItem.cs, Project.cs, User.cs (association updates)
├── Services/
│   ├── DocumentService.cs
│   ├── FileStorageService.cs
│   ├── FileScannerService.cs
│   ├── DocumentScanQueue.cs
│   ├── DocumentScanWorker.cs
│   ├── DashboardService.cs (recent documents/count)
│   └── TaskService.cs, NotificationService.cs (integration updates)
├── Pages/
│   ├── Documents.razor
│   ├── DocumentDetails.razor
│   ├── ProjectDetails.razor (project documents)
│   ├── TaskDetails.razor (task attachments)
│   └── Index.razor (dashboard widget/count)
├── Services/DocumentEndpoint.cs (authorized download/preview endpoints)
├── wwwroot/css/site.css
└── AppData/uploads/ (runtime-created, outside wwwroot, ignored by source control)

ContosoDashboard.DocumentScanFunction/ (optional Azure deployment project)
├── DocumentScanFunction.cs
├── host.json
├── local.settings.json (local-only secrets/configuration)
└── ContosoDashboard.DocumentScanFunction.csproj
```

**Structure Decision**: Extend the existing `ContosoDashboard` web project for domain, local queue/worker, and UI behavior. Add a separately deployable `ContosoDashboard.DocumentScanFunction` isolated-process Azure Functions project only when the optional Azure path is enabled. The function consumes an Azure Queue Storage trigger, uses managed configuration/identity for storage and scanner access, and calls the shared scan-processing application contract. File delivery uses a small endpoint mapping because files are intentionally outside static web content and every stream request requires authorization.

**Azure Deployment Notes**: Deploy the scan worker to a .NET isolated-process Function App on Flex Consumption (FC1), with Functions host v4, authenticated function access, managed identity for Queue/Blob access, Application Insights enabled for exceptions and dependency telemetry, and private endpoints or equivalent network restrictions where the environment supports them. Keep `local.settings.json` local-only and provide an explicit local queue emulator/configuration for offline development.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | The design conforms to the existing project structure and constitution. |
