# Tasks: Global Responsive Layout and Scrolling Fix

**Spec**: `specs/001-fix-responsive-layout/spec.md`
**Plan**: `my-project/specs/001-fix-responsive-layout/plan.md`
**Branch**: `001-fix-responsive-layout`
**Tech**: WPF / XAML only — C# 12.0 / .NET 10.0-windows. No business logic, ViewModel, or database changes.

---

## Principle Tags

All tasks in this feature are View-layer XAML changes only. The single applicable principle is:

- **[M]** Principle II — MVVM Discipline (all changes remain in `Presentation/Views/` XAML files; no ViewModels or services are touched)

---

## Tasks

### Phase 1: Setup

- [x] T001 [M] Confirm baseline project builds without errors by running `dotnet build AlJohary.ServiceHub.sln` and resolving any pre-existing build warnings before applying layout changes

---

### Phase 2: User Story 1 — App Scrolls and Shows All Content (P1)

**Story goal**: Every page at 1280×800 shows full content and scrolls vertically when content exceeds the viewport; app launches maximised.

**Independent test**: Launch app, resize window to 900 px height, navigate to each page — scrollbar appears and no content is clipped.

- [x] T002 [M] [US1] Set `WindowState="Maximized"` (replacing `"Normal"`) on the root `<Window>` element in `Presentation/Views/MainWindow.xaml` (FR-013 / SC-008)
- [x] T003 [M] [US1] Change logo `StackPanel` `Margin` from `"0,50,0,30"` to `"0,20,0,16"` in `Presentation/Views/MainWindow.xaml` (FR-003)
- [x] T004 [P] [M] [US1] Change logo `Image` dimensions from `Width="190" Height="90"` to `Width="160" Height="76"` in `Presentation/Views/MainWindow.xaml` (FR-004)
- [x] T005 [M] [US1] Wrap the main-content `<Frame>` in `Grid.Column="1"` with `<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">` in `Presentation/Views/MainWindow.xaml` (FR-001 / FR-009 / SC-001)

---

### Phase 3: User Story 2 — Sidebar Adapts to Smaller Window Widths (P2)

**Story goal**: Sidebar is exactly 220 px wide and always visible; no horizontal overflow at any window width ≥ 1024 px.

**Independent test**: Resize window to 1280 px wide — sidebar is 220 px, content area fills the remaining width, no horizontal scrollbar appears.

- [x] T006 [M] [US2] Change sidebar `<ColumnDefinition Width="260"/>` to `Width="220"` in the two-column content `Grid` at `Grid.Row="1"` in `Presentation/Views/MainWindow.xaml` (FR-002 / SC-002 / SC-005)

---

### Phase 4: User Story 3 — Pages Resize Content Proportionally (P3)

**Story goal**: Toolbar buttons flex rather than overflow; KPI cards wrap on the Reports page; no horizontal scrollbar at the page level on any page at ≥ 1024 px.

**Independent test**: Open Reports at 1366×768 — KPI cards wrap to a second row; open any page at 1024 px wide — no horizontal overflow; no toolbar button is cut off.

- [x] T007 [P] [M] [US3] Change toolbar `Button` `Width="120"` → `MinWidth="120"` and `Width="140"` → `MinWidth="140"` on the two action buttons in `Presentation/Views/CustomersPage.xaml` (FR-007)
- [x] T008 [P] [M] [US3] Change toolbar `Button` `Width="140"` → `MinWidth="140"` on both action buttons in `Presentation/Views/ExpensesPage.xaml` (FR-007)
- [x] T009 [P] [M] [US3] Audit `Presentation/Views/InvoicesPage.xaml` for any `<Button ... Width="N"` inside a toolbar `StackPanel` and change each `Width` to `MinWidth` (FR-007)
- [x] T010 [P] [M] [US3] Audit `Presentation/Views/ReturnsPage.xaml` for any `<Button ... Width="N"` inside a toolbar `StackPanel` and change each `Width` to `MinWidth` (FR-007)
- [x] T011 [P] [M] [US3] Audit `Presentation/Views/MaintenancePage.xaml` for any `<Button ... Width="N"` inside a toolbar `StackPanel` and change each `Width` to `MinWidth` (FR-007)
- [x] T012 [P] [M] [US3] Audit `Presentation/Views/InventoryPage.xaml` for any `<Button ... Width="N"` inside a toolbar `StackPanel` and change each `Width` to `MinWidth` (FR-007)
- [x] T013 [P] [M] [US3] Audit `Presentation/Views/SuppliersPage.xaml` for any `<Button ... Width="N"` inside a toolbar `StackPanel` and change each `Width` to `MinWidth` (FR-007)
- [x] T014 [P] [M] [US3] Verify that the KPI `ItemsControl` in `Presentation/Views/ReportsPage.xaml` already uses `<WrapPanel Orientation="Horizontal">` as its `ItemsPanelTemplate` and confirm no structural change is needed (FR-006 / SC-003)

---

### Phase 5: User Story 4 — POS Page Cart Panel Adapts (P4)

**Story goal**: Cart panel is exactly 320 px; product grid shows ≥ 2 columns at 1280 px window width.

**Independent test**: Open POS at 1280 px total width — cart panel is 320 px and product grid shows at least two product card columns.

- [x] T015 [M] [US4] Change cart `<ColumnDefinition Width="380"/>` to `Width="320"` in the root two-column `Grid` of `Presentation/Views/POSPage.xaml` (FR-008 / SC-004)

---

### Phase 6: Polish & Cross-Cutting

- [x] T016 [M] Verify `FlowDirection="RightToLeft"` is intact on `MainWindow.xaml`, `POSPage.xaml`, `CustomersPage.xaml`, `ExpensesPage.xaml`, and all other modified page XAML files — no attribute was accidentally removed or set to `LeftToRight` (FR-010 / SC-006)
- [x] T017 [M] Perform manual visual regression check against the full verification checklist in `my-project/specs/001-fix-responsive-layout/quickstart.md` covering SC-001 through SC-008 (FR-012 / SC-007)

---

## Dependencies

```
T001 (baseline build)
  └─► T002, T003, T004, T005 (US1 – all touch MainWindow.xaml, apply sequentially)
        └─► T006 (US2 – also touches MainWindow.xaml; apply after US1 is done)
              └─► T007–T014 (US3 – independent page files, fully parallel)
                    └─► T015 (US4 – POSPage.xaml, independent)
                          └─► T016, T017 (Polish – final cross-check)
```

**Story completion order**: US1 → US2 → US3 (parallel batch) → US4 → Polish

**Note**: T002–T005 share `MainWindow.xaml` and must be applied in sequence within that file. T007–T013 each touch a different page file and can be executed in parallel.

---

## Parallel Execution Examples

**US3 parallel batch** (T007–T013 touch distinct files — safe to apply simultaneously):
```
T007  CustomersPage.xaml    ──┐
T008  ExpensesPage.xaml     ──┤
T009  InvoicesPage.xaml     ──┤── all parallel
T010  ReturnsPage.xaml      ──┤
T011  MaintenancePage.xaml  ──┤
T012  InventoryPage.xaml    ──┤
T013  SuppliersPage.xaml    ──┘
T014  ReportsPage.xaml (read-only verify) ──┘
```

**US1–US2 sequential** (T002–T006 all modify `MainWindow.xaml` — apply one at a time):
```
T002 → T003 → T004 → T005 → T006
```

---

## Implementation Strategy

**MVP Scope** (deliver US1 alone for immediate value):
- Complete T001–T005 only.
- This resolves the most critical defect: content clipping on all pages, plus the launch-size improvement.
- US2, US3, US4 are additive improvements that do not block US1 verification.

**Incremental delivery**:
1. **Sprint 1** — US1 (T001–T005): Scrolling + maximised launch. Biggest user impact.
2. **Sprint 2** — US2 (T006): Sidebar 220 px. Quick single-property change.
3. **Sprint 3** — US3 (T007–T014): Button MinWidth + KPI audit. Parallel-friendly.
4. **Sprint 4** — US4 (T015): POS cart 320 px.
5. **Final** — T016–T017: RTL guard + visual regression sign-off.

---

## Definition of Done

- [x] All tasks completed; project builds without errors or new warnings.
- [x] Application launches in maximised state (SC-008).
- [x] All pages scroll vertically at 900 px window height (SC-001).
- [x] Sidebar is exactly 220 px; no horizontal overflow at 1024–1920 px width (SC-002, SC-005).
- [x] KPI cards wrap on the Reports page at 1366×768 (SC-003).
- [x] POS cart panel is 320 px; product grid shows ≥ 2 columns at 1280 px (SC-004).
- [x] RTL / Arabic layout is correct on all modified pages (SC-006).
- [x] No functional regression in navigation, data display, or user interactions (SC-007).
- [x] No ViewModels, services, repositories, or C# business-logic files were modified (FR-012).
