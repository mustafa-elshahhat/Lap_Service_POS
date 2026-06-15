# Implementation Plan: Global Responsive Layout and Scrolling Fix

**Branch**: `001-fix-responsive-layout` | **Date**: 2026-05-24 | **Spec**: [spec.md](../../specs/001-fix-responsive-layout/spec.md)

**Input**: Feature specification from `specs/001-fix-responsive-layout/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Fix the WPF desktop application (AlJohary ServiceHub POS) layout so all pages scroll correctly, the sidebar is exactly 220 px wide, the application launches maximised, and the POS cart panel narrows from 380 px to 320 px. All changes are XAML-only; no business logic, data-binding, or C# code is altered.

The primary technical approach is: (1) wrap the main-content `Frame` in a `ScrollViewer` in `MainWindow.xaml` to give every page unified vertical scrolling without touching individual pages, and (2) apply targeted property changes in `MainWindow.xaml` and `POSPage.xaml` for sidebar width, logo spacing/size, window startup state, and cart-panel width. Toolbar buttons in all pages replace fixed `Width` with `MinWidth` to satisfy FR-007.

## Technical Context

**Language/Version**: C# 12.0 / .NET 10.0-windows

**Primary Dependencies**: WPF (built-in, `UseWPF=true`); Microsoft.Data.Sqlite 9.0.0 (SQLite ORM – not touched by this feature)

**Storage**: SQLite via `Infrastructure/Data/DatabaseManager.cs` – not touched by this feature

**Testing**: xUnit 2.9.2 (`Tests/AlJohary.ServiceHub.Tests.csproj`) – layout-only changes; no new unit tests required; manual visual regression checklist covers acceptance scenarios

**Target Platform**: Windows Desktop, WPF, self-contained win-x64 single-file executable

**Project Type**: Desktop application (WPF / MVVM)

**Performance Goals**: Smooth resize and scroll at 1280×800, 1366×768, 1440×900 display resolutions; layout recalculation must not cause visible jank (WPF default 60 fps render loop)

**Constraints**: RTL layout (`FlowDirection="RightToLeft"` on Window and all Pages must remain intact); `MinWidth=1024` / `MinHeight=700` (FR-011 – no change); no functional regressions in navigation, data display, or user interactions (FR-012)

**Scale/Scope**: 1 `MainWindow`, ~15 navigable Pages, 1 layout resource dictionary (`Presentation/Resources/Buttons.xaml`); purely visual XAML changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The `constitution.md` in `.specify/memory/` contains only the blank scaffold template — no principles have been ratified. **No constitutional gates apply.** The following project-level quality invariants are treated as implicit gates for this feature:

| Gate | Status | Notes |
|------|--------|-------|
| No business-logic change | ✅ PASS | All changes are XAML property values only |
| No data-binding change | ✅ PASS | ViewModel bindings not touched |
| RTL preserved | ✅ PASS (design intent) | All modified XAML nodes keep `FlowDirection="RightToLeft"` or inherit it |
| No regression in navigation | ✅ PASS (design intent) | `Frame` is only wrapped, not replaced |

*Re-check: All gates still pass after Phase 1 design (no C# code changed, no new XAML elements with LTR hardcoding).*

## Project Structure

### Documentation (this feature)

```text
specs/001-fix-responsive-layout/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

*(No `contracts/` directory — this is a purely internal desktop application with no external API surface.)*

### Source Code (repository root)

```text
Presentation/
├── Views/
│   ├── MainWindow.xaml          # PRIMARY: sidebar width, logo, ScrollViewer, WindowState
│   ├── POSPage.xaml             # Cart panel width 380→320
│   ├── CustomersPage.xaml       # Toolbar button Width→MinWidth
│   ├── ExpensesPage.xaml        # Toolbar button Width→MinWidth
│   ├── InvoicesPage.xaml        # Toolbar button Width→MinWidth (if applicable)
│   ├── ReturnsPage.xaml         # Toolbar button Width→MinWidth (if applicable)
│   ├── MaintenancePage.xaml     # Toolbar button Width→MinWidth (if applicable)
│   ├── InventoryPage.xaml       # Toolbar button Width→MinWidth (if applicable)
│   ├── SuppliersPage.xaml       # Toolbar button Width→MinWidth (if applicable)
│   └── ReportsPage.xaml         # Verify KPI WrapPanel + scroll (already uses WrapPanel)
└── Resources/
    └── Buttons.xaml             # BaseButtonStyle uses Padding only (no Width) — no change needed

Tests/                           # xUnit project — no new tests (visual-only changes)
```

**Structure Decision**: Single-project WPF desktop app. All changes reside in `Presentation/Views/` XAML files. No new files are created; all edits are minimal property-value replacements.

## Complexity Tracking

> No Constitution violations exist — this section is intentionally empty.
