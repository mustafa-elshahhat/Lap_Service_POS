# Tasks: [FEATURE_NAME]

**Spec**: `.specify/memory/[FEATURE_SLUG]/spec.md`
**Plan**: `.specify/memory/[FEATURE_SLUG]/plan.md`

## Principle Tags

Each task MUST be tagged with one or more constitution principle identifiers:

- **[D]** Principle I — Domain Integrity
- **[M]** Principle II — MVVM Discipline
- **[R]** Principle III — Data Reliability
- **[S]** Principle IV — Security & Role-Based Access
- **[T]** Principle V — Test Coverage

## Tasks

### Phase 1: Domain & Data Model

- [ ] [D] [TASK_DOMAIN_1]
- [ ] [D][R] [TASK_DOMAIN_2]

### Phase 2: Application Services

- [ ] [TASK_SERVICE_1]

### Phase 3: Infrastructure / Persistence

- [ ] [R] [TASK_INFRA_1]

### Phase 4: Presentation / ViewModel

- [ ] [M] [TASK_VM_1]

### Phase 5: Tests

- [ ] [T] [TASK_TEST_1]

## Definition of Done

- [ ] All tasks completed and code-reviewed.
- [ ] Constitution compliance self-assessment passed (all five principle checkboxes in plan).
- [ ] xUnit tests green for all new financial/repository logic (Principle V).
- [ ] No domain logic introduced outside `Domain/` (Principle I).
- [ ] No View referencing services or repositories directly (Principle II).
- [ ] Financial writes wrapped in transactions (Principle III).
- [ ] Privileged operations gated behind role authorization (Principle IV).
