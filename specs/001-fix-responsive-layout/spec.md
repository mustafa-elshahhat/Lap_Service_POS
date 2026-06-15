# Feature Specification: Global Responsive Layout and Scrolling Fix

**Feature Branch**: `001-fix-responsive-layout`

**Created**: 2026-05-24

**Status**: Clarified

**Input**: User description: "Improve the entire application layout so all pages are responsive, readable, and scroll correctly across common screen sizes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - App Scrolls and Shows All Content (Priority: P1)

A technician opens the application on a 1280×800 laptop screen. Every page they navigate to (POS, Customers, Expenses, Invoices, Returns, Maintenance) shows its full content without anything being cut off. When a page has more content than fits on screen, a scrollbar appears and they can scroll down to see the rest.

**Why this priority**: Content clipping prevents users from accessing data or completing tasks. This is the most critical defect and must be fixed first.

**Independent Test**: Open the app at 1280×800, navigate to each page, resize the window to 900px height, and confirm no content is clipped and a scrollbar appears.

**Acceptance Scenarios**:

1. **Given** the window is 1280×800, **When** the user opens the Customers page, **Then** the full customer list and all action buttons are visible without horizontal or vertical clipping.
2. **Given** the window height is reduced to 700px, **When** any page contains content taller than the viewport, **Then** a vertical scrollbar appears and content below the fold can be reached by scrolling.
3. **Given** any page is loaded, **When** the user scrolls inside the main content area, **Then** the sidebar and top bar remain fixed while only the content scrolls.

---

### User Story 2 - Sidebar Adapts to Smaller Window Widths (Priority: P2)

A user running the application in a 1280px-wide window finds that the sidebar occupies a fixed 220px, always visible, with no collapse toggle. Navigation remains fully usable without horizontal overflow, and the main content area receives the remaining width to display tables and forms.

**Why this priority**: A fixed wide sidebar on smaller screens compresses the content area, making tables and forms unreadable.

**Independent Test**: Resize the window to 1280px wide and confirm that the sidebar is proportionally sized and the content area still shows table columns without horizontal overflow.

**Acceptance Scenarios**:

1. **Given** the window is 1280px wide, **When** the sidebar is visible, **Then** its width is exactly 220px (always visible, no collapse toggle) and the content area receives the remaining width.
2. **Given** the window is resized below 1280px, **When** the user navigates to any page, **Then** no horizontal scrollbar appears at the window level due to sidebar overflow.
3. **Given** sidebar navigation items are displayed, **When** the window width is reduced, **Then** all navigation labels remain readable and no text is truncated or hidden.

---

### User Story 3 - Pages Resize Their Content Proportionally (Priority: P3)

A manager opens the Reports page and the Inventory page on a 1366×768 screen. KPI cards wrap cleanly to the next row when the window is narrowed. Tables and data grids do not overflow the page horizontally. Fixed-width elements such as buttons and cards adapt or wrap to fit the available space.

**Why this priority**: Improves usability across the most common laptop resolutions used in the target environment.

**Independent Test**: Open Reports and Inventory pages at 1366×768 and confirm that KPI cards wrap to a second row rather than overflow, and that no horizontal scrollbar appears at the page level.

**Acceptance Scenarios**:

1. **Given** the Reports page is open at 1366×768, **When** the KPI row is rendered, **Then** cards wrap to a new row rather than overflow horizontally.
2. **Given** any page with a DataGrid is open, **When** the window is narrowed, **Then** all columns remain visible and reachable; the DataGrid container scrolls horizontally so no column is ever hidden or removed.
3. **Given** action buttons are placed in a toolbar, **When** the toolbar is narrower than the sum of fixed button widths, **Then** buttons wrap or resize rather than overflow the toolbar.

---

### User Story 4 - POS Page Cart Panel Adapts (Priority: P4)

A cashier uses the POS page on a 1280px screen. The currently fixed 380px cart panel on the right does not consume an excessive proportion of the screen, and the product grid on the left still shows multiple product cards per row without being uncomfortably narrow.

**Why this priority**: POS is the most-used page; a layout that is unusable on common screen sizes directly impacts daily operations.

**Independent Test**: Open POS at 1280px total window width, confirm the cart panel is proportionally sized (not exceeding one-third of the content area) and the product grid shows at least two columns of products.

**Acceptance Scenarios**:

1. **Given** the POS page is open at any window width, **When** the cart panel is displayed, **Then** it occupies exactly 320px (fixed), regardless of total window width.
2. **Given** the product grid is displayed, **When** the window is narrowed to 1024px, **Then** the product grid still shows at least two product card columns.

---

### Edge Cases

- What happens when a page is opened on a screen narrower than the defined `MinWidth` of 1024px? The window enforces the minimum width and a window-level horizontal scroll appears, which is acceptable.
- How does the system handle a page that has no scrollable content but whose root container is taller than the viewport? The root container should still allow vertical scrolling rather than clipping.
- What happens when the sidebar logo section has a very large fixed top margin on a short window? The logo area shrinks its margin proportionally, preventing it from consuming the entire sidebar height.
- How does the system behave when the window is maximized on a 4K screen? All layout proportions should scale correctly; no element should appear excessively large or spaced.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The main content area MUST support vertical scrolling so that any page whose content exceeds the visible height can be scrolled without content being clipped. The entire page — title, toolbar, and data content — scrolls as a single unit; there is no pinned page header within the content area.
- **FR-002**: The navigation sidebar MUST have a fixed width of exactly 220px and MUST always remain visible (no collapse toggle, no auto-hide). It MUST NOT exceed 220px at any window size.
- **FR-003**: The sidebar logo section MUST NOT consume an excessive portion of the sidebar's vertical space on short screens; the spacing above the logo MUST be reduced to allow navigation items to remain visible without scrolling on typical screen heights.
- **FR-004**: The sidebar logo MUST be sized proportionally smaller to free vertical space, while remaining clearly recognizable.
- **FR-005**: The application top bar height is already appropriate and MUST NOT be changed.
- **FR-006**: KPI summary cards on the Reports page MUST wrap to the next row when the available horizontal space is insufficient, rather than overflowing outside the visible area.
- **FR-007**: Action buttons in page toolbars MUST define a minimum usable width rather than a fixed absolute width, so they can grow on wider screens and do not overflow on narrower screens.
- **FR-008**: The shopping cart panel on the POS page MUST have a fixed width of exactly 320px at all window sizes. This replaces the current fixed 380px width and ensures the product grid receives adequate space at 1280px window width.
- **FR-009**: Every page in the application MUST be scrollable vertically when its content exceeds the viewport height; no page content MUST be permanently hidden due to lack of a scroll mechanism. The entire page (title, toolbar, and data) scrolls as one unit.
- **FR-010**: The RTL (right-to-left) text and layout direction MUST remain correct on all pages and the application shell after every layout change.
- **FR-011**: The application minimum usable window size MUST remain at 1024px wide by 700px tall; no change to the minimum size is required.
- **FR-012**: No business logic, data operations, or user-facing data-binding behavior MUST be altered as part of this fix; only visual layout and sizing constraints are in scope.
- **FR-013**: The application MUST launch in a maximized window state by default, replacing the current fixed startup size, so that users on any screen resolution immediately see the full available screen area.

### Key Entities

- **Application Shell**: The outermost window frame containing the top bar, the sidebar, and the main content area. All pages are displayed inside the content area. This is the root of all layout changes.
- **Navigation Sidebar**: The vertical navigation panel on the right edge of the window that holds the application logo, navigation buttons grouped by category, and a version label.
- **Main Content Area**: The region to the left of the sidebar where each page is rendered. Pages are swapped in and out as the user navigates. This area must support vertical scrolling.
- **Page Layout**: The root-level layout of each individual page (POS, Customers, Invoices, etc.) that organizes the page's header, toolbar, content, and any other sections.
- **KPI Summary Card**: A summary tile displayed on the Reports page that shows a key business metric (e.g., total sales, total expenses). Cards are displayed in a row that wraps automatically when space is limited.
- **Cart Panel**: The right-hand section of the POS page that shows the current sale's item list, totals, and payment controls. Its width directly affects how much space is left for the product search grid.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At a window size of 1280×768, no page content is visually clipped; all page content is reachable by scrolling.
- **SC-002**: At any window width ≥1024px, the sidebar is exactly 220px (always visible), and the content area receives the full remaining width. At 1280px the content area is ≥1044px usable.
- **SC-003**: On the Reports page at 1366×768, KPI cards wrap to a second row rather than overflow the container horizontally.
- **SC-004**: On the POS page at any window width ≥1024px, the cart panel is exactly 320px (fixed) and the product grid shows a minimum of two product card columns.
- **SC-005**: Resizing the window from 1024px to 1920px width produces no horizontal scrollbar at the window level for any page.
- **SC-006**: RTL direction is preserved on all modified pages and the global window; Arabic text alignment remains correct after all layout changes.
- **SC-007**: All existing navigation, data display, and user interactions remain fully functional after layout changes (no functional regressions).
- **SC-008**: On first launch after the fix, the application window opens in a maximized state on the user's primary monitor, regardless of screen resolution.

## Assumptions

- The application runs on Windows as a native desktop application; this fix targets desktop window layout only, not web or mobile UI.
- The minimum supported window size is 1024×700; no change to this minimum is required.
- Users typically run the application at 1280×800, 1366×768, or 1440×900; these are the primary target resolutions for validation.
- The main content area currently does not provide automatic vertical scrolling for page content; the fix introduces a single scroll container in the content area so all pages scroll as a unit without per-page changes to each page's internal structure.
- The navigation sidebar already has its own internal scroll for the navigation list and does not need further changes to sidebar-internal scrolling.
- The sidebar will not gain a collapse/expand toggle; it remains always visible at 220px.
- Page headers and toolbars are not pinned; the full page content scrolls together as one unit.
- The application will launch maximized so users immediately see the full screen area regardless of their resolution.
- Popup dialogs and modal windows are out of scope for this fix unless severe clipping is discovered during layout review.
- Typography and font sizes are not in scope; only layout containers, spacing, and element sizing constraints are targeted.
- The existing visual identity — colors, icons, corner radii, borders, and typography — must not change.
