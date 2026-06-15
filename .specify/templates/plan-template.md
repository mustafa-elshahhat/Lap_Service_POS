# Plan: [FEATURE_NAME]

**Spec**: `.specify/memory/[FEATURE_SLUG]/spec.md`
**Status**: [PLAN_STATUS]
**Last Updated**: [DATE]

## Constitution Check

Verify this feature complies with all five principles before planning:

- [ ] **I. Domain Integrity** — all new business logic lives in `Domain/`; no domain rules leak
  into `Infrastructure`, `Presentation`, or `Shared`.
- [ ] **II. MVVM Discipline** — Views bind only to ViewModels; no direct service/repository
  references in code-behind.
- [ ] **III. Data Reliability** — financial/inventory writes are wrapped in SQLite transactions;
  migrations preserve backup/restore functionality.
- [ ] **IV. Security & Role-Based Access** — privileged operations require authenticated,
  role-authorized sessions; no plain-text credentials.
- [ ] **V. Test Coverage** — new financial calculations and repository operations have xUnit
  regression tests; tests do not share mutable DB state.

## Design Decisions

[DESIGN_DECISIONS]

## Component Breakdown

[COMPONENT_BREAKDOWN]

## Data Model Changes

[DATA_MODEL_CHANGES]

## Migration Plan

[MIGRATION_PLAN]

## Risks & Open Questions

[RISKS_AND_OPEN_QUESTIONS]
