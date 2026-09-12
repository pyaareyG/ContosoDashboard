# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/002-document-upload-management/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/document-storage-and-access.md](contracts/document-storage-and-access.md), [quickstart.md](quickstart.md)

**Tests**: No separate test tasks were requested in the feature specification. Each story includes an independent validation checkpoint and the final phase includes the documented quickstart/security checks.

**Organization**: Tasks are grouped by user story so each increment can be implemented and validated independently after the foundational phase.

## Phase 1: Setup

**Purpose**: Establish configuration, project structure, and runtime boundaries needed by the feature.

- [X] T001 Add configurable document storage, quarantine, upload-size, allowed-type, scan-queue, and local worker settings to `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`.
- [X] T002 [P] Add `AppData/uploads/.gitkeep` and ignore runtime document/quarantine contents in `ContosoDashboard/.gitignore`.
- [X] T003 [P] Add the optional isolated-process Azure Functions project scaffold in `ContosoDashboard.DocumentScanFunction/ContosoDashboard.DocumentScanFunction.csproj` with Functions host v4 and Queue Storage trigger dependencies.
- [X] T004 [P] Add Azure Functions local/runtime configuration placeholders in `ContosoDashboard.DocumentScanFunction/host.json` and `ContosoDashboard.DocumentScanFunction/local.settings.json` without committing secrets.

---

## Phase 2: Foundational

**Purpose**: Establish shared persistence, storage, scan contracts, and authorization primitives before story work begins.

**Checkpoint**: Foundation ready; user story implementation can proceed in priority order or in parallel where file ownership permits.

- [X] T005 Create `Document`, `DocumentShare`, `DocumentActivity`, and `DocumentScanJob` entities with integer keys, required fields, text categories, scan-state fields, and the stated 25 MB/MIME constraints in `ContosoDashboard/Models/Document.cs`, `ContosoDashboard/Models/DocumentShare.cs`, `ContosoDashboard/Models/DocumentActivity.cs`, and `ContosoDashboard/Models/DocumentScanJob.cs`.
- [X] T006 [P] Add document, share, activity, and scan-job navigation properties plus task/project association collections in `ContosoDashboard/Models/User.cs`, `ContosoDashboard/Models/Project.cs`, and `ContosoDashboard/Models/TaskItem.cs`.
- [X] T007 Configure DbSets, relationships, delete behavior, unique share constraints, and indexes for document ownership, project/task scope, scan status/job identity, recipients, and activity in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [X] T008 [P] Define `IFileStorageService`, `IFileScanner`, `IDocumentScanQueue`, scan result types, scan messages, and document access DTOs in `ContosoDashboard/Services/DocumentContracts.cs`.
- [X] T009 Implement path-safe local storage rooted outside `wwwroot`, GUID-based relative paths, parent-directory creation, read/delete/exists operations, and traversal protection in `ContosoDashboard/Services/FileStorageService.cs`.
- [X] T010 [P] Implement the local scanner adapter with explicit `Clean`, `Malicious`, and `Unavailable` outcomes and fail-closed configuration behavior in `ContosoDashboard/Services/FileScannerService.cs`.
- [X] T011 Implement a durable local scan queue and hosted background worker with bounded retries, duplicate-message idempotency, poison/review handling, and `PendingScan` to terminal-state transitions in `ContosoDashboard/Services/DocumentScanQueue.cs` and `ContosoDashboard/Services/DocumentScanWorker.cs`.
- [X] T012 Register storage, scanner, local queue, hosted worker, and document services with dependency injection while preserving offline defaults in `ContosoDashboard/Program.cs`.
- [X] T013 Add the document scan-state values and configuration contract to `ContosoDashboard/Services/DocumentScanOptions.cs`, including `PendingScan`, `Available`, `Rejected`, and `ScanUnavailable` access rules.

---

## Phase 3: User Story 1

**Goal**: Let a signed-in employee upload supported files with required metadata, quarantine them, enqueue asynchronous scanning, and see clear status/results.

**Independent Test**: Upload a supported file as a permitted seeded user, confirm `PendingScan`, confirm it cannot be downloaded while pending, then confirm a clean local scan changes it to `Available`; invalid files never become available.

### Implementation for User Story 1

- [X] T014 [US1] Implement upload validation for the 25 MB limit, supported extension/MIME allowlist, required title/category, optional metadata, project membership, and task/project consistency in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T015 [US1] Implement the ordered upload workflow in `ContosoDashboard/Services/DocumentService.cs`: copy the browser stream, generate a unique quarantine path, save the file, persist `PendingScan` metadata and a stable scan job, enqueue the message, and clean up on partial failure.
- [X] T016 [US1] Add the shared scan-processing method in `ContosoDashboard/Services/DocumentService.cs` so local workers and Azure Functions can atomically apply idempotent clean, malicious, unavailable, retry, and poison outcomes without exposing non-available documents.
- [X] T017 [US1] Implement `DocumentController` POST `/api/documents` in `ContosoDashboard/Controllers/DocumentController.cs`, delegating to `DocumentService` and returning clear validation errors for files over 25 MB or unsupported file types.
- [X] T018 [US1] Implement the multi-file upload form with `InputFile`, `@key` reset, metadata fields, progress/status messaging, per-file errors, and queued/available/rejected/unavailable states in `ContosoDashboard/Pages/Documents.razor`.
- [X] T019 [US1] Add authorized document list navigation and the upload entry point to `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Pages/Documents.razor`.
- [X] T020 [US1] Map authenticated download and preview endpoints that call the document service before opening storage streams, return safe filenames/MIME types, block all non-`Available` states, and record activity in `ContosoDashboard/Services/DocumentEndpoint.cs` and `ContosoDashboard/Program.cs`.
- [X] T021 [US1] Add document status, upload error, and protected delivery styling in `ContosoDashboard/wwwroot/css/site.css`.

**Checkpoint**: A user can upload, observe scan status, and access only clean files without direct filesystem exposure.

---

## Phase 4: User Story 2

**Goal**: Provide My Documents, shared/project scope filtering, metadata search, sorting, and protected retrieval within current authorization boundaries.

**Independent Test**: Seed documents for multiple users/projects, search and filter as different roles, and verify unauthorized documents are absent from results and direct retrieval is denied.

### Implementation for User Story 2

- [X] T022 [US2] Implement authorization predicates for owner, administrator, project manager, current project membership, explicit user share, and department share in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T023 [US2] Implement paged authorized document queries with title/description/tag/uploader/project search, title/date/category/size sorting, category/project/date filters, and `Available`-only access in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T024 [US2] Build the My Documents and Shared with Me browse/search/filter/sort UI with non-disclosing not-found behavior in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/DocumentDetails.razor`.
- [X] T025 [US2] Add project document loading and authorized download/preview links to `ContosoDashboard/Pages/ProjectDetails.razor`.
- [X] T026 [US2] Add query indexes and projection choices needed to meet the 500-document list and 2-second metadata-search targets in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`.
- [X] T027 [US2] Record authorized Preview and Download activity and ensure the delivery endpoints recheck current identity, role, ownership, membership, and share permissions in `ContosoDashboard/Services/DocumentEndpoint.cs` and `ContosoDashboard/Services/DocumentService.cs`.

**Checkpoint**: Authorized users can find and retrieve clean documents while unauthorized users receive no protected document details.

---

## Phase 5: User Story 3

**Goal**: Let document owners and authorized project managers share clean documents with users/teams and notify recipients while preserving current project access.

**Independent Test**: Share an available document with a user and department, confirm notifications and Shared with Me visibility, then verify unauthorized shares and stale project membership do not grant access.

### Implementation for User Story 3

- [X] T028 [US3] Implement duplicate-safe share creation, exactly-one recipient validation, owner/project-manager authorization, and current access reevaluation in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T029 [US3] Create document-share notifications using existing notification types and recipient preferences in `ContosoDashboard/Services/NotificationService.cs` and `ContosoDashboard/Models/Notification.cs`.
- [X] T030 [US3] Notify eligible project members after a document becomes `Available` for a project and avoid duplicate notifications on repeated scan delivery in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T031 [US3] Add share-user/team controls, recipient selection, validation messages, and share history/status to `ContosoDashboard/Pages/DocumentDetails.razor`.
- [X] T032 [US3] Add project document counts and available-document sections to `ContosoDashboard/Pages/ProjectDetails.razor`.

**Checkpoint**: Authorized sharing and project collaboration work through notifications and current access rules.

---

## Phase 6: User Story 4

**Goal**: Support metadata edits, safe replacement, permanent deletion, recent-document dashboard visibility, counts, and task attachments.

**Independent Test**: An owner updates metadata, replaces a file through a new scan, deletes a document after confirmation, and accesses authorized documents from dashboard/project/task views.

- [X] T033 [US4] Implement owner/project-manager authorization for metadata edits, safe replacement with pending-scan retention of the currently available file, and cleanup of rejected/old files in `ContosoDashboard/Services/DocumentService.cs`.
- [X] T034 [US4] Implement permanent deletion with confirmation semantics, stored-file cleanup, audit preservation, and non-disclosing authorization failures in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T035 [US4] Add metadata edit, file replacement, delete confirmation, and status transition UI to `ContosoDashboard/Pages/DocumentDetails.razor`.
- [X] T036 [US4] Add recent-five documents and document-count queries to `ContosoDashboard/Services/DashboardService.cs` and extend `DashboardSummary`.
- [X] T037 [US4] Add the Recent Documents widget, document summary card, and links to `ContosoDashboard/Pages/Index.razor`.
- [ ] T038 [US4] Add task-document associations with task project inheritance, authorized attach/list controls, and task detail navigation in `ContosoDashboard/Models/TaskItem.cs`, `ContosoDashboard/Services/TaskService.cs`, and `ContosoDashboard/Pages/TaskDetails.razor`.
- [ ] T039 [US4] Add project and task document association display and upload context handling to `ContosoDashboard/Pages/ProjectDetails.razor` and `ContosoDashboard/Pages/TaskDetails.razor`.

**Checkpoint**: Authorized lifecycle management and dashboard/task/project context are complete without exposing pending or unauthorized files.

---

## Phase 7: User Story 5

**Goal**: Record document activity and provide administrator-only usage reports.

**Independent Test**: Perform upload, scan, preview, download, share, edit, replace, and delete actions, then verify administrator reports contain actor/document/time and usage summaries while non-administrators are denied.

- [ ] T040 [US5] Complete immutable activity recording for upload, scan outcome, preview, download, share, edit, replacement, and deletion in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/DocumentActivity.cs`.
- [X] T041 [US5] Implement administrator-only document activity, type, uploader, and access-pattern report queries in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T042 [US5] Add the administrator document reporting page with period/scope filters and non-administrator denial handling in `ContosoDashboard/Pages/DocumentReports.razor`.
- [ ] T043 [US5] Add audit indexes and report projections for document/action/time access patterns in `ContosoDashboard/Data/ApplicationDbContext.cs`.

**Checkpoint**: Administrators can audit document behavior without exposing reporting capabilities to other roles.

---

## Phase 8: Optional Azure Queue Scan Worker

**Purpose**: Replace the local queue worker with an Azure Queue Storage-triggered Function without changing document business logic.

- [X] T044 [P] Implement the isolated-process Queue Storage trigger, message deserialization, and call to the shared scan-processing contract in `ContosoDashboard.DocumentScanFunction/DocumentScanFunction.cs`.
- [X] T045 [P] Configure Functions host v4, Queue Storage retry/visibility settings, poison-queue behavior, and non-secret local settings in `ContosoDashboard.DocumentScanFunction/host.json` and `ContosoDashboard.DocumentScanFunction/local.settings.json`.
- [ ] T046 Implement Azure Queue Storage queue access using managed identity/configuration and stable `DocumentScanMessage` serialization in `ContosoDashboard/Services/AzureDocumentScanQueue.cs`.
- [ ] T047 Add the Azure Function project to `ContosoDashboard.sln` or the repository build configuration and document the isolated .NET runtime/queue trigger setup in `ContosoDashboard.DocumentScanFunction/README.md`.
- [ ] T048 Add Application Insights scan outcome, latency, retry, and poison-job telemetry without logging file contents in `ContosoDashboard.DocumentScanFunction/DocumentScanFunction.cs`.
- [ ] T049 Document Flex Consumption (FC1), authenticated non-anonymous access, managed identity, private endpoint/network restrictions, and deployment configuration in `specs/002-document-upload-management/quickstart.md` and `ContosoDashboard.DocumentScanFunction/README.md`.

**Checkpoint**: The optional Azure Function consumes the same queued job contract, is idempotent, fails closed, and can be deployed without weakening the offline implementation.

---

## Phase 9: Polish

**Purpose**: Validate security, performance, failure recovery, and documentation across the complete feature.

- [ ] T050 [P] Add focused authorization, path-traversal, MIME/size validation, scan-state, queue retry, poison-message, and cleanup coverage in the repository's established test location or `ContosoDashboard.Tests/` if created by the implementation.
- [ ] T051 [P] Add database initialization/migration handling and clean-state guidance for the new document tables in `ContosoDashboard/Data/ApplicationDbContext.cs` and `specs/002-document-upload-management/quickstart.md`.
- [ ] T052 Run the complete acceptance, performance, and security scenarios from `specs/002-document-upload-management/quickstart.md`, including 25 MB upload, 500-document list/search, preview timing, direct endpoint denial, duplicate queue delivery, and orphan cleanup.
- [ ] T053 Review all document-facing services and endpoints for current identity, role, owner, project membership, share, and scan-state checks; update `README.md` with the offline/Azure scanning boundary and training-only limitations.
- [ ] T054 Run `dotnet build` from `ContosoDashboard/` and resolve feature-related compile/configuration errors without changing unrelated behavior.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001-T004 can begin immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user-story work. T005-T013 establish the shared model, storage, queue, scanner, worker, and DI boundaries.
- **User Stories (Phases 3-7)**: Depend on Phase 2. US1 is the MVP; US2 depends on the document lifecycle from US1; US3 depends on authorized document queries from US2; US4 depends on the document lifecycle and associations; US5 depends on activity records from US1-US4.
- **Azure Queue Scan Worker (Phase 8)**: Depends on T008, T011, T016, and T046's shared contract; can proceed in parallel with UI story work after the scan-processing contract exists.
- **Polish (Phase 9)**: Depends on all selected story phases and the optional Azure path if Azure deployment is in scope.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; MVP and scan-state foundation.
- **US2 (P1)**: Starts after Phase 2, but requires the document entity/service from US1 for meaningful data; can parallelize UI/query work after T014-T017.
- **US3 (P2)**: Depends on US2 authorization predicates and available-document queries.
- **US4 (P2)**: Depends on US1 lifecycle operations and the US2/US3 authorization model; dashboard work can proceed after T035.
- **US5 (P3)**: Depends on activity creation paths from US1-US4.
- **Optional Azure worker**: Depends on the shared queue and scan-processing contracts, not on Blazor UI completion.

### Parallel Opportunities

- Setup T002-T004 can run in parallel after T001 configuration decisions are agreed.
- Foundational T006, T008, T010, and T013 can run in parallel; T009 and T011 follow the contracts.
- After T005-T013, US1 UI (T018-T021), US2 query/UI (T022-T026), and Azure Function scaffold (T044-T045) can proceed in parallel where developers avoid the same-file tasks.
- Within US3, notification work (T029-T030) and sharing UI (T031) can proceed in parallel after T028.
- Within US4, dashboard work (T036-T037) can proceed in parallel with lifecycle UI (T033-T035) and task integration (T038-T039).
- Azure Function deployment/configuration tasks T044, T045, and T048 can proceed in parallel after the shared contracts are stable.

## Parallel Example: User Story 1

```text
After T014-T017 establish the upload, controller, and scan-processing service:

Task: T018 Build the Blazor upload form in ContosoDashboard/Pages/Documents.razor
Task: T020 Add protected download/preview endpoints in ContosoDashboard/Services/DocumentEndpoint.cs
Task: T021 Add scan/upload status styling in ContosoDashboard/wwwroot/css/site.css
```

## Parallel Example: Azure Scan Worker

```text
After T008, T011, and T016 establish the shared queue/scan contract:

Task: T044 Implement the Queue Storage-triggered Function in ContosoDashboard.DocumentScanFunction/DocumentScanFunction.cs
Task: T045 Configure host retries and poison behavior in ContosoDashboard.DocumentScanFunction/host.json
Task: T048 Add Application Insights telemetry in ContosoDashboard.DocumentScanFunction/DocumentScanFunction.cs
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational work.
3. Complete Phase 3 User Story 1 with the offline local queue/worker.
4. Stop and validate queued, clean, rejected, unavailable, and direct-access-denied states independently.
5. Add the Azure Function path only when cloud deployment is needed; it is an implementation replacement for the same scan contract.

### Incremental Delivery

1. Deliver US1 upload/quarantine/scan as the MVP.
2. Add US2 authorized discovery and protected retrieval.
3. Add US3 sharing and project notifications.
4. Add US4 maintenance and dashboard/task integration.
5. Add US5 administrator audit reports.
6. Validate the optional Azure worker and then run the cross-cutting security/performance pass.

## Notes

- Every task uses the required checklist format with a sequential ID and exact file path.
- `[P]` marks tasks that can be performed in parallel without depending on incomplete work in the same files.
- User-story labels are present on every story-phase task; setup, foundational, Azure cross-cutting, and polish tasks intentionally have no story label.
- The Azure Function path is optional for offline training but must preserve the same fail-closed and idempotent behavior.