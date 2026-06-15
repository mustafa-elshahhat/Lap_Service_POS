# Data Model: Global Responsive Layout and Scrolling Fix

**Feature**: `001-fix-responsive-layout`
**Phase**: 1 – Design
**Note**: This feature is XAML-layout-only. There are no new data entities, database schema changes, or ViewModel changes. This document models the **UI layout elements** (as defined in the spec's Key Entities section) and their before/after property values.

---

## UI Layout Entities

### 1. Application Shell (`MainWindow.xaml`)

The outermost window frame. Contains the top bar, sidebar, and main content area.

| Property | Current Value | Target Value | Requirement |
|----------|--------------|-------------|-------------|
| `WindowState` | `Normal` | `Maximized` | FR-013 |
| `Height` | `768` | `768` (unchanged – initial value before maximize) | — |
| `Width` | `1366` | `1366` (unchanged – initial value before maximize) | — |
| `MinHeight` | `700` | `700` (unchanged) | FR-011 |
| `MinWidth` | `1024` | `1024` (unchanged) | FR-011 |
| `FlowDirection` | `RightToLeft` | `RightToLeft` (unchanged) | FR-010 |

**Validation rules**:
- `WindowState` must be `Maximized` at launch; user can restore/resize freely.
- `MinWidth` and `MinHeight` must not be reduced.

**State transitions**:
```
Launch → Maximized
User click Maximize/Restore → toggles between Normal ↔ Maximized (existing DataTrigger handles CornerRadius)
```

---

### 2. Navigation Sidebar

The right-hand vertical panel. Always visible, fixed width, with logo and nav buttons.

| Element | Property | Current Value | Target Value | Requirement |
|---------|----------|--------------|-------------|-------------|
| Sidebar `ColumnDefinition` | `Width` | `260` | `220` | FR-002 |
| Logo `StackPanel` | `Margin` | `"0,50,0,30"` | `"0,20,0,16"` | FR-003 |
| Logo `Image` | `Width` | `190` | `160` | FR-004 |
| Logo `Image` | `Height` | `90` | `76` | FR-004 |

**Validation rules**:
- Sidebar must be exactly 220 px wide at all window sizes ≥ 1024 px.
- No collapse/hide toggle must be added.
- Logo `Stretch="Uniform"` is preserved (aspect ratio maintained).

---

### 3. Main Content Area

The left-hand region where each `Page` is rendered inside a WPF `Frame`.

| Element | Property | Current Value | Target Value | Requirement |
|---------|----------|--------------|-------------|-------------|
| Content `Grid.Column="1"` | Child element | `Frame` directly | `ScrollViewer` wrapping `Frame` | FR-001 / FR-009 |
| `ScrollViewer` | `VerticalScrollBarVisibility` | N/A (new) | `Auto` | FR-001 |
| `ScrollViewer` | `HorizontalScrollBarVisibility` | N/A (new) | `Disabled` | FR-009 |
| `Frame` | All existing attributes | (unchanged) | (unchanged) | FR-010 / FR-012 |

**Validation rules**:
- The entire page (title, toolbar, data) scrolls as one unit — no inner per-page scroll container is added.
- Horizontal scrollbar must not appear at the window level for any page at width ≥ 1024 px.

---

### 4. POS Cart Panel (`POSPage.xaml`)

The right-hand fixed section of the POS page showing the current sale's cart, totals, and payment controls.

| Element | Property | Current Value | Target Value | Requirement |
|---------|----------|--------------|-------------|-------------|
| Cart `ColumnDefinition` | `Width` | `380` | `320` | FR-008 |
| Product grid `ColumnDefinition` | `Width` | `*` (star) | `*` (star, unchanged) | FR-008 |

**Validation rules**:
- Cart panel must be exactly 320 px at all window widths.
- Product grid must show ≥ 2 card columns at 1024 px window width with 220 px sidebar (content area ≥ 804 px, 804 − 320 = 484 px for grid).

---

### 5. KPI Summary Cards (`ReportsPage.xaml`)

Summary tiles displayed in a `WrapPanel` inside an `ItemsControl`.

| Element | Property | Current Value | Target Value | Requirement |
|---------|----------|--------------|-------------|-------------|
| `ItemsPanelTemplate` | Panel type | `WrapPanel Orientation="Horizontal"` | **No change** | FR-006 |
| KPI card `Border` | `Width` | `240` | **No change** | — |

**Validation rules**:
- Cards wrap to a new row rather than overflow when total card width exceeds the container (satisfied by existing `WrapPanel`).
- Vertical scroll of the page (provided by the main-area `ScrollViewer`) allows users to reach wrapped rows.

---

### 6. Page Toolbar Buttons (Multiple Pages)

Action buttons in page toolbars that currently use a fixed `Width`, preventing graceful sizing.

| File | Button | Current | Target | Requirement |
|------|--------|---------|--------|-------------|
| `CustomersPage.xaml` | "عميل جديد" | `Width="120"` | `MinWidth="120"` | FR-007 |
| `CustomersPage.xaml` | "عرض الكل" | `Width="140"` | `MinWidth="140"` | FR-007 |
| `ExpensesPage.xaml` | "مصروف جديد" | `Width="140"` | `MinWidth="140"` | FR-007 |
| `ExpensesPage.xaml` | "عرض الكل" | `Width="140"` | `MinWidth="140"` | FR-007 |
| Other pages | Any `Button` with explicit `Width` in toolbar | `Width="N"` | `MinWidth="N"` | FR-007 |

**Validation rules**:
- Buttons must not overflow their toolbar container at any window width ≥ 1024 px.
- Buttons must remain at least their `MinWidth` wide (Arabic text must not truncate).

---

## Entities NOT Changed

The following spec entities require no structural change:

| Entity | Reason |
|--------|--------|
| Top bar | Already correct at 64 px (FR-005) |
| Sidebar internal scroll | `ScrollViewer` already present for nav list (FR — no change needed) |
| `MinWidth` / `MinHeight` | Already set to 1024 / 700 (FR-011) |
| All `FlowDirection` attributes | Preserved throughout all changes (FR-010) |
| Business logic / ViewModels | Out of scope (FR-012) |
| Popup dialogs / modal windows | Out of scope (spec Assumption 9) |
| Typography / fonts | Out of scope (spec Assumption 10) |
