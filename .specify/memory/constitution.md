# ContosoDashboard Constitution
<!-- Sync Impact Report
Version change: 0.0.0 -> 1.0.0
Modified principles: N/A (new constitution established)
Added sections: Security and Data Handling, Development Workflow
Removed sections: Placeholder template scaffolding replaced with project-specific rules
Deferred items: None
-->

## Core Principles

### I. Security and Access Boundaries
ContosoDashboard MUST treat security as a non-negotiable requirement for every feature and change. Authentication and authorization decisions MUST enforce the least-privilege model, user isolation, and explicit permission checks before data or actions are exposed. Any route, page, service, or API that exposes user-specific content MUST validate identity and authorization at the service layer, even when UI-level restrictions already exist. This protects learning scenarios from IDOR, accidental data leakage, and misconfigured role access.

### II. Training-Ready Simplicity
The system MUST prioritize clear, understandable patterns over unnecessary abstraction. Features MUST be implemented in a way that can be explained and reviewed easily by learners, with a single obvious responsibility for each component, page, and service. The project MUST avoid production-only dependencies or complex infrastructure unless a feature specifically requires them, and any additional complexity MUST be documented with a clear rationale. This keeps the codebase suitable for Spec-Driven Development training while still demonstrating professional engineering habits.

### III. User Isolation and Data Integrity
Each user MUST be able to view and modify only the records and actions authorized for their role and team context. Service methods MUST validate ownership, membership, and project scope before returning data or mutating state. Data creation and updates MUST preserve referential integrity, enforce required fields, and prevent duplicate or orphaned records. This ensures the application remains faithful to the mock business scenario and prevents accidental cross-user leakage.

### IV. Quality Gates Before Completion
All feature work MUST follow a test-and-review workflow: requirements must be clear, changes must be verifiable, and the application MUST be run or tested before a task is considered complete. New behavior MUST be validated for the relevant path, including authentication and authorization paths where applicable, and regressions MUST be avoided by preserving existing user flows. If a change affects business rules or security boundaries, the implementation MUST include direct validation of that behavior. This keeps the project reliable in a training environment and supports disciplined iteration.

### V. Offline-First Architecture with Clear Migration Paths
The application MUST remain runnable without cloud services so it can work in a self-contained training environment. Infrastructure dependencies MUST be isolated behind clear abstractions and configuration boundaries so the product can be adapted to Azure or another production platform later without rewriting business logic. Any migration path or cloud-specific change MUST preserve the current offline mode and document the required configuration or service swap. This supports learning, portability, and a realistic evolution path.

## Technical and Design Constraints
ContosoDashboard MUST remain aligned with its training scope and the repository's stated limitations:

- The project uses ASP.NET Core 8, Blazor Server, and Entity Framework Core and MUST preserve compatibility with that stack.
- Authentication is mock-based for training; production-grade identity and secret handling MUST not be inferred as complete security controls.
- Data access and business logic MUST be separated from UI concerns; services MUST own authorization rules and domain operations.
- Local development and offline execution MUST remain the default path, and any cloud or external dependency MUST be optional and clearly isolated.
- File and data handling MUST avoid duplicate names or ambiguous ownership and MUST use explicit identifiers when storing content.

## Development Workflow
The repository MUST follow a disciplined workflow for changes:

- Requirements and constraints MUST be captured before implementation so work remains aligned with the training scenario.
- Small, reviewable changes are preferred; broad refactors without a clear need are prohibited.
- Security-sensitive and role-sensitive changes MUST be validated against the relevant user scenarios before completion.
- Documentation and comments MUST explain intent where the logic is not self-evident, especially around mock auth, user scoping, and migration boundaries.
- Changes that affect the constitution, process, or project standards MUST be reviewed and versioned through the governance section below.

## Governance
This Constitution governs all project decisions, design reviews, and implementation work for ContosoDashboard. It supersedes informal practices when conflict exists. Amendment proposals MUST document the change, justify the rationale, and identify any compatibility implications for the training scenario. All substantive changes MUST be reviewed before approval, and the project MUST preserve a clear record of the effective rules.

Versioning follows semantic versioning: MAJOR changes remove or redefine core principles or governance requirements, MINOR changes add or materially expand guidance, and PATCH changes clarify wording or improve non-semantic precision. The project MUST update the version and amendment date whenever the constitution changes.

Compliance review expectations:

- Each change MUST be checked against the principles above before merge or completion.
- Security and authorization changes MUST be reviewed with explicit attention to least privilege, user isolation, and role boundaries.
- Feature work MUST remain aligned with the training-first design and cannot silently add unsupported production assumptions.
- Exceptions MUST be rare, documented, and approved by project maintainers before being accepted.

**Version**: 1.0.0 | **Ratified**: 2026-09-11 | **Last Amended**: 2026-09-11
