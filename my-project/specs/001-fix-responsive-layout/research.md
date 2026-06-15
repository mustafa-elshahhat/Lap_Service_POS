# Research: Global Responsive Layout and Scrolling Fix

**Feature**: `001-fix-responsive-layout`
**Phase**: 0 – Research
**Status**: Complete – all unknowns resolved from codebase inspection

---

## 1. Main Content Area Scrolling

**Decision**: Wrap the existing `<Frame>` in `MainWindow.xaml` with a `<ScrollViewer>`.

**Rationale**:
- The `Frame` (Grid.Column="1" in the content Grid) currently has no scroll container — pages taller than the viewport are clipped.
- Adding `<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">` as a single wrapper in `MainWindow.xaml` applies scrolling to every page uniformly, without modifying any individual page XAML.
- `VerticalScrollBarVisibility="Auto"` shows the scrollbar only when content overflows; `HorizontalScrollBarVisibility="Disabled"` prevents horizontal scroll at the window level (per FR-009, FR-001).
- `Frame` inside `ScrollViewer` works correctly in WPF: the Frame measures its content and the ScrollViewer honours the measured height.

**Alternatives considered**:
- Adding `ScrollViewer` to each page individually — rejected because it requires 15+ file edits and creates maintenance overhead; a single wrapper is sufficient (spec Assumption 4).
- Using `VirtualizingPanel` — not applicable; this is a page-swap navigation model, not an items collection.

---

## 2. Sidebar Width Reduction (260 → 220 px)

**Decision**: Change `<ColumnDefinition Width="260"/>` to `<ColumnDefinition Width="220"/>` in the two-column `Grid` inside `MainWindow.xaml` (Grid.Row="1").

**Rationale**:
- Current sidebar `ColumnDefinition` is 260 px; FR-002 mandates exactly 220 px.
- The `NavButton` style uses `Margin="12,2"` and `Padding="16,12"` — text fits comfortably at 220 px.
- No collapse/hide toggle required (spec assumption: sidebar always visible).

**Alternatives considered**:
- Proportional `Width="0.17*"` — rejected; FR-002 mandates a fixed 220 px, not a proportion.

---

## 3. Sidebar Logo Spacing and Size

**Decision**:
- Logo `StackPanel` `Margin`: change `"0,50,0,30"` → `"0,20,0,16"` (reduces top dead zone from 50 px to 20 px).
- Logo `Image` dimensions: change `Width="190" Height="90"` → `Width="160" Height="76"` (proportional ~16 % reduction).

**Rationale**:
- At 700 px window height (minimum), the top bar takes 64 px, leaving 636 px for the sidebar. A 50 px top margin before the logo wastes space. 20 px is sufficient visual breathing room (FR-003).
- The logo border uses `Padding="8"`, so the effective image display area reduces from 190×90 to 160×76 — still clearly recognisable at 80 % original size (FR-004).
- `Stretch="Uniform"` on the `Image` means aspect ratio is preserved regardless of container.

**Alternatives considered**:
- Dynamic margin via `LayoutTransform` — over-engineered for a simple spacing fix.
- Hiding the logo entirely on short screens — rejected; FR-004 requires the logo to remain visible.

---

## 4. Window Startup State (Normal → Maximized)

**Decision**: Change `WindowState="Normal"` to `WindowState="Maximized"` in `MainWindow.xaml`.

**Rationale**:
- FR-013 mandates the application launches maximised on first run.
- WPF `WindowState="Maximized"` is the idiomatic, single-property solution — no code-behind required.
- The existing `DataTrigger` on `WindowState=Maximized` (which removes `CornerRadius` from the border) already handles the maximised visual correctly.
- `WindowChrome` with `ResizeBorderThickness="5"` still works in maximised state.

**Alternatives considered**:
- Setting `Left`, `Top`, `Width`, `Height` to screen bounds in `Window_Loaded` — over-engineered; `WindowState="Maximized"` is the correct declarative approach.

---

## 5. POS Cart Panel Width (380 → 320 px)

**Decision**: Change `<ColumnDefinition Width="380"/>` to `<ColumnDefinition Width="320"/>` in `POSPage.xaml` (column index 1, the right-hand cart panel).

**Rationale**:
- At 1280 px total window width, with 220 px sidebar, the content area is 1060 px. A 380 px cart panel leaves 680 px for the product grid. At 320 px, the product grid gets 740 px — enough for at least two product card columns (FR-008, SC-004).
- The cart panel's inner content (item list, totals, payment controls) is vertical and does not require the extra 60 px.

**Alternatives considered**:
- Using a `*`-based proportion — rejected; FR-008 explicitly mandates a fixed 320 px.

---

## 6. Toolbar Button Fixed Width → MinWidth (FR-007)

**Decision**: Replace explicit `Width="N"` on toolbar `Button` elements in individual page XAML files with `MinWidth="N"`.

**Rationale**:
- Pages confirmed to have fixed-width toolbar buttons: `CustomersPage.xaml` (Width=120, 140), `ExpensesPage.xaml` (Width=140).
- Other pages (InvoicesPage, ReturnsPage, MaintenancePage, InventoryPage, SuppliersPage) to be audited; same treatment applied wherever fixed `Width` is found on a `Button` in a toolbar `StackPanel`.
- `MinWidth` preserves the minimum clickable area while allowing buttons to grow on wider screens and not overflow on narrower ones.
- `BaseButtonStyle` in `Buttons.xaml` already uses `Padding="16,0"` without a fixed `Width` — per-page overrides are the source of the problem.

**Alternatives considered**:
- Removing the width constraint entirely — rejected; buttons would collapse to content width, which may be too narrow for Arabic text labels.
- Moving buttons into a `WrapPanel` — over-engineered; a simple `MinWidth` swap achieves FR-007.

---

## 7. KPI Card WrapPanel (ReportsPage)

**Decision**: No structural change required. The `WrapPanel Orientation="Horizontal"` is already present as the `ItemsPanelTemplate` for the KPI `ItemsControl` in `ReportsPage.xaml`. Adding the main `ScrollViewer` (item 1 above) provides the vertical scroll needed when cards wrap to a second row.

**Rationale**:
- Each KPI card has `Width="240"` and `Margin="0,0,16,16"`. At a 1366 px window with 220 px sidebar = 1146 px content area, 4 cards × 256 px = 1024 px — they fit on one row. If the window is narrower, the `WrapPanel` wraps them, and the page-level `ScrollViewer` allows the user to scroll down.
- FR-006 (cards wrap instead of overflow) is satisfied by the existing `WrapPanel`.

**Alternatives considered**:
- Changing KPI card `Width` to `Auto` — unnecessary; `WrapPanel` already handles wrapping at the container boundary.

---

## Summary Table

| FR | Root Cause | Fix Location | Change |
|----|-----------|-------------|--------|
| FR-001 / FR-009 | No `ScrollViewer` on main content | `MainWindow.xaml` | Wrap `Frame` in `ScrollViewer` |
| FR-002 | Sidebar `ColumnDefinition Width="260"` | `MainWindow.xaml` | `Width="220"` |
| FR-003 | Logo `StackPanel Margin="0,50,0,30"` | `MainWindow.xaml` | `Margin="0,20,0,16"` |
| FR-004 | Logo `Width="190" Height="90"` | `MainWindow.xaml` | `Width="160" Height="76"` |
| FR-005 | Top bar height 64 px — already correct | — | No change |
| FR-006 | KPI `WrapPanel` already present | `ReportsPage.xaml` | No change |
| FR-007 | Toolbar buttons have fixed `Width` | Per-page XAML files | `Width` → `MinWidth` |
| FR-008 | Cart panel `ColumnDefinition Width="380"` | `POSPage.xaml` | `Width="320"` |
| FR-010 | RTL intact across all changes | All files | Verify `FlowDirection` unchanged |
| FR-011 | `MinWidth=1024` / `MinHeight=700` — already set | — | No change |
| FR-012 | Business logic — out of scope | — | No change |
| FR-013 | `WindowState="Normal"` | `MainWindow.xaml` | `WindowState="Maximized"` |
