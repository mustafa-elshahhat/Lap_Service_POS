<!--
SYNC IMPACT REPORT
==================
Version change: N/A → 1.0.0 (initial creation)
Modified principles: N/A — initial creation, no prior principles
Added sections: All (Purpose, Principles I–V, Additional Constraints, Governance)
Removed sections: None

Templates requiring updates:
  - .specify/templates/constitution-template.md  ✅ created
  - .specify/templates/plan-template.md          ✅ created
  - .specify/templates/spec-template.md          ✅ created
  - .specify/templates/tasks-template.md         ✅ created
  - .specify/templates/commands/                 ⚠ pending (command stubs not yet authored)

Follow-up TODOs:
  - TODO(RATIFICATION_DATE): Original project ratification/adoption date is unknown.
    Confirm with project maintainer and replace the TODO line below.
-->

# AlJohary Service Hub — Project Constitution

**Version**: 1.0.0
**Ratification Date**: TODO(RATIFICATION_DATE): Confirm original adoption date (YYYY-MM-DD).
**Last Amended**: 2026-05-24

## Purpose

AlJohary Service Hub is a Windows-native POS and service-management application for laptop and
printer repair workshops. It unifies sales, repair-order tracking, spare-parts inventory, customer
credit, supplier purchasing, and financial reporting in a single self-contained executable.

This constitution establishes the non-negotiable engineering principles that govern every
contribution: domain correctness, UI layer discipline, financial data reliability, role-based
security, and sustained test coverage across the codebase.

## Principles

### I. Domain Integrity

All business entities, validation rules, and domain calculations MUST reside exclusively within
the `Domain/` layer (`Domain/Entities/` and `Domain/Interfaces/`). The `Infrastructure`,
`Presentation`, and `Shared` layers MUST NOT contain domain logic, financial calculations, or
entity validation rules. Cross-layer leakage of domain concerns constitutes a constitution
violation and MUST be corrected before merge.

**Rationale**: Centralising business rules in `Domain/` ensures they are independently verifiable,
prevents rule drift across layers, and keeps the domain model the authoritative source of truth.

### II. MVVM Discipline

Views (`.xaml` and code-behind `.xaml.cs` files under `Presentation/Views/`) MUST bind
exclusively to ViewModel properties and commands. Views MUST NOT directly reference service
interfaces, repository types, `Infrastructure` concrete types, or `Application` service
implementations. All UI-driving logic, commands, and property transformation MUST live in
`Presentation/ViewModels/`. The `AppBootstrapper` and `ServiceContainer` are the only
permitted composition-root touch-points outside ViewModels.

**Rationale**: Enforcing MVVM boundaries decouples the UI from data-access concerns, enables
ViewModel-level testability, and prevents spaghetti dependencies from accumulating in code-behind.

### III. Data Reliability

All write operations that affect financial totals (sales, payments, repair-order balances),
inventory counts, or customer/supplier account balances MUST be executed within SQLite
transactions. Where multiple related rows are written atomically, a single transaction MUST
span all statements. Backup and restore utilities MUST remain fully functional after every
schema migration applied via `Infrastructure/SQLiteMigrations/`.

**Rationale**: A POS system's core value is accurate financial state. Partial writes produce
irrecoverable discrepancies that directly damage business operations and customer trust.

### IV. Security and Role-Based Access

All privileged operations — including user management, database backup/restore, and financial
reporting — MUST be gated behind authenticated, role-authorized sessions. Credentials MUST NOT
be stored in plain text in any configuration file, database column, or log output. The default
administrator password (`admin123`) MUST be changed on first deployment; the application MUST
prompt or warn when the default credential remains active.

**Rationale**: A multi-user POS system processes financial transactions and holds customer credit
data. Unauthorized access represents a direct operational and legal risk to the business.

### V. Test Coverage

The `Domain` and `Application` layers MUST maintain xUnit test coverage for all financial
calculations and repository operations that modify monetary aggregates. Tests MUST NOT share
mutable database state between individual test cases (each test MUST operate on a freshly
initialized in-memory or isolated SQLite instance). Every new repository operation that
modifies financial aggregates MUST be accompanied by at least one regression test before
the corresponding pull request is merged.

**Rationale**: The existing `Tests/` suite already enforces this standard for repair-order and
user repositories. All new financial logic MUST meet the same coverage bar to prevent
silent regressions in money calculations.

## Additional Constraints

- The application MUST target `net10.0-windows` and publish as a self-contained, single-file
  executable for `win-x64`. No runtime installation requirement is permitted for end users.
- C# language version is fixed at `12.0`. Nullable reference types are currently disabled
  (`<Nullable>disable</Nullable>`); new code MUST match this project-wide setting until a
  constitution amendment explicitly enables it.
- SQLite (`Microsoft.Data.Sqlite`) is the only permitted persistence backend. External database
  servers MUST NOT be introduced without a MAJOR constitution amendment.
- The neutral language is `ar-EG`. All user-facing strings and date/number formatting MUST
  accommodate Arabic locale requirements and right-to-left layout where applicable.
- Implicit usings are disabled (`<ImplicitUsings>disable</ImplicitUsings>`); all `using`
  directives MUST be declared explicitly at the top of each file.

## Governance

### Amendment Procedure

1. Open a pull request against the `main` branch containing changes to
   `.specify/memory/constitution.md`.
2. The amended constitution MUST be reviewed and approved by the project maintainer before merge.
3. Run `/speckit.constitution` immediately after approval to propagate updates to all dependent
   templates and command files.
4. Update `LAST_AMENDED_DATE` to the merge date in ISO 8601 format (`YYYY-MM-DD`).
5. Increment `CONSTITUTION_VERSION` according to the Versioning Policy below.

### Versioning Policy

Constitution versions follow semantic versioning (`MAJOR.MINOR.PATCH`):

- **MAJOR**: Removal or backward-incompatible redefinition of an existing principle (e.g.,
  changing MUST to SHOULD, removing a principle entirely).
- **MINOR**: Addition of a new principle, new governance section, or materially expanded
  guidance that imposes new obligations.
- **PATCH**: Clarifications, wording improvements, typo fixes, or non-semantic refinements
  that do not change obligations.

### Compliance Review

Every pull request MUST include a self-assessment confirming no principle is violated. A full
compliance review across all five principles MUST be performed at each MINOR or MAJOR version
bump. The reviewer is responsible for verifying that `.specify/templates/plan-template.md`,
`.specify/templates/spec-template.md`, and `.specify/templates/tasks-template.md` remain
aligned with the current constitution before marking the amendment as complete.
