# TABLES_UI_UX_IMPLEMENTATION_PLAN

**Project:** AlJohary ServiceHub (Lap_Service POS) — WPF desktop application, Arabic RTL
**Source of truth:** `TABLES_UI_UX_AUDIT_REPORT.md`
**Type:** Implementation **plan only**. No source files are edited here. No code patches included.
**Scope reminder:** Desktop window-size adaptation only (large monitor → minimum safe window). **No** mobile/tablet/card layouts, no column hiding, no web/CSS.

This plan is organized so each phase is independently revertable, lowest-risk first. Every change is justified by a specific audit finding (referenced as H1–H5 / M1–M7 / L1–L5, and §6.x per-table sections of the audit).

---

## 1. Executive summary

### What will be fixed
- **Numeric alignment (H1/H2):** Money, quantity, totals, paid, remaining, debt, balance, salary, and report-amount columns move from centered (`DataGridTextCenter`) to a dedicated **right-aligned numeric style** — applied **per column**, not blanket. This is the single largest readability win and resolves the page-vs-dialog inconsistency.
- **Column order & widths (§7/§8):** Per-table reordering and width tuning, each customized to that table's purpose (directory vs ledger vs work-queue vs invoice-lines).
- **States (H3/H4):** Add empty-state overlays to the dialog/POS grids that lack them, and add a **"no matching search results"** state distinct from "no data" on the searchable list pages, plus a loading overlay where already modeled (Maintenance).
- **Long-text handling (M5):** Trim + tooltip on specific long-text columns (address, description, issue, details, reason).
- **Controlled horizontal scroll (H5):** Enable scroll fallback on Invoices and Maintenance (the two page grids that can overflow at minimum width).
- **Status/width fixes (M1/M2), footers/counts (M6/M7), action consistency (M3):** Per-table.
- **Safe refactors (L1):** Extract the duplicated copyable-cell template.

### What will NOT be touched
- No business logic, commands, DB access, authentication/authorization.
- No financial calculations, report totals, KPI values, supplier balances, inventory quantities, salary, maintenance, or return/refund math.
- No print/export **values, totals, or binding paths**; printed layouts (built independently in `Infrastructure/Printing/*`) are not modified.
- No global, blanket cell styling. No table loses its purpose-specific column order/width/adaptive behavior.
- No page layout outside table containers, except the minimum needed to host an overlay or scroll fallback.

---

## 2. Non-negotiable safety rules

1. **Display-only.** Every change affects presentation (alignment, width, order, overlays, tooltips, scroll). No change to data values, computation, persistence, or control flow.
2. **No binding-path changes.** Do not rename or re-point any `Binding` path (incl. dictionary indexers like `[amount]`). Reordering columns means reordering column *declarations*, never editing bindings.
3. **No converter math changes.** `FlexibleNumberConverter`, `FlexibleCurrencyConverter`, status/color converters are consumed only — never edited in logic.
4. **No report total / KPI / export-value changes.** `ReportsViewModel` calculation methods, `_currentColumns` header text, binding paths, and `Format` values stay byte-for-byte unless the audit explicitly marks a change display-only (it does **not** for any value/total).
5. **Print services are off-limits.** `Infrastructure/Printing/*` is not edited. Screen grid changes must not alter printed output (verified, see §8).
6. **Per-table customization preserved.** Shared styles are **opt-in per column/table**. No shared style is force-applied to all cells.
7. **RTL integrity.** Arabic text columns stay right-aligned; codes/phones/invoice numbers stay LTR; headers stay centered (existing convention).
8. **Each phase builds and runs before the next.** A phase that introduces any XAML runtime error or visual regression is reverted before proceeding.
9. **No new dependencies, no new projects, no NuGet changes.**
10. **The "no-search-result" state must be presentation-only** — implemented via existing `SearchText` + grid `HasItems` (converter/MultiBinding), preferring not to add ViewModel logic; if a presentation-only VM property is unavoidable it must not alter any data or command.

---

## 3. Implementation phases (rollback-safe order)

| Phase | Theme | Risk | Revert unit |
|-------|-------|------|-------------|
| **1** | Shared **safe, opt-in** DataGrid resources (new styles/templates only; nothing wired yet) | Very low | Delete added resource keys |
| **2** | High-priority **numeric right-alignment** + **dialog alignment consistency** (apply Phase-1 numeric style per money/qty column) | Low | Swap `ElementStyle` back per column |
| **3** | **Per-table column order + width** tuning | Low–Med | Per-file XAML revert |
| **4** | **Empty / loading / no-search** states (wire Phase-1 overlays) | Low | Remove overlay elements |
| **5** | **Desktop adaptive** overflow/scroll fixes + optional compact rows (dense tables only) | Low | Remove scroll/compact opt-in |
| **6** | **Reports grid** handling + print/CSV verification | Med (coupled) | Revert `ReportsViewModel` list order + `.xaml.cs` |
| **7** | **Cleanup / refactor-only** (extract copyable-cell template, remove dead row) | Very low | Re-inline template |

**Rationale for order:** Phase 1 adds resources without touching any grid (zero behavioral change). Phases 2–5 consume those resources per table. Phase 6 is isolated last because it is the only place screen/print/CSV are coupled. Phase 7 is pure tidy-up.

---

## 4. Shared / global changes that are safe

> All Phase-1 items are **additive** (new resource keys) and **opt-in**. Nothing changes appearance until a specific column/table references the new key (Phases 2–5).

### 4.1 Shared right-aligned numeric cell style — `DataGridTextNumeric`
- **Purpose:** One canonical style for money/quantity/total/balance/salary columns: right horizontal alignment, vertical center, consistent padding, `NumberSubstitution=European` (Western digits), no wrap. Resolves H1/H2.
- **Files affected:** `Presentation/Resources/DataGrid.xaml` (add the style next to `DataGridTextCenter`).
- **Why safe:** Pure addition; no existing key changes; consumes no data. Mirrors existing `DataGridTextRight` but standardized for numerics with digit substitution.
- **Tables that opt in (per column):** #3 (debt), #4 (total/paid/remaining), #5 (total/paid/remaining), #6 (total), #7 (amount), #8 (salary), #10 (amount/paid/balance), #11 (qty/price/total), #12 (numeric report cols), #13 (price/stock), #14 (qty/total), #15 (qty/unit/total), #16 (qty/price/total), #17 (total/paid/remaining), #18 (qty/unit/total), #21 (qty/price/total). **Not** applied to text/name/category/status columns.
- **Risk:** Very low.
- **Verification:** Build; open each opted-in grid; confirm digits right-align and line up vertically; confirm colored/bold money cells (e.g., debt red, remaining orange) keep their color/weight by deriving the per-column style `BasedOn` the numeric style.

### 4.2 Reusable long-text trim+tooltip style — `DataGridTextTruncate`
- **Purpose:** Right-aligned text with `TextTrimming=CharacterEllipsis` and `ToolTip` bound to the same field, so long free text never breaks row height and is fully readable on hover. Resolves M5.
- **Files affected:** `Presentation/Resources/DataGrid.xaml` (add base style). **Tooltip binding is per column** (each column sets `ToolTip` to its own field), so the shared style provides trimming + alignment and each column supplies its tooltip binding.
- **Why safe:** Additive; the existing Maintenance customer/technician and Employees notes columns already do exactly this — we are standardizing the pattern.
- **Tables that opt in (per column):** #3 `العنوان`, #7 `الوصف`, #19 `المشكلة`, #20 `ملاحظات`, #12 long-text report cols; reuse on existing #5/#8 columns optionally for consistency.
- **Risk:** Very low.
- **Verification:** Long values show ellipsis and full tooltip; row height unchanged.

### 4.3 Reusable empty-state overlay approach
- **Purpose:** Make the existing `DataGridEmptyState` pattern trivially reusable on grids that currently lack it (esp. dialog/POS grids). Resolves H3.
- **Approach (lowest risk):** Keep the established pattern — a sibling `TextBlock` (style `DataGridEmptyState`) placed in the **same parent `Grid`** as the `DataGrid`, with `Visibility` bound to the grid's `HasItems` via the existing `InverseBooleanToVisibilityConverter`. For dialog grids that already sit inside a wrapping `Grid`/`Border`, this is a one-element addition; for grids that don't, wrap the `DataGrid` in a one-cell `Grid` first (container-only change, allowed by scope as required for table usability).
- **Files affected:** the specific dialog/POS view XAML (Phase 4). `DataGrid.xaml` already has the style.
- **Why safe:** No data/logic; overlay is `IsHitTestVisible=False`.
- **Tables that opt in:** #13, #14, #15, #16, #18, #19, #20, #21, and Reports operations/returns/suppliers variants (#12).
- **Risk:** Low.
- **Verification:** Each grid with zero rows shows its message centered over the grid area; with rows, overlay hidden.

### 4.4 "No matching search results" state (presentation-only)
- **Purpose:** Distinguish "no data at all" from "search returned nothing" on searchable list pages. Resolves H4.
- **Approach (preferred, no VM logic):** Two overlay `TextBlock`s in the grid's parent `Grid`:
  - **No data:** visible when `HasItems == false` **and** `SearchText` is empty.
  - **No results:** visible when `HasItems == false` **and** `SearchText` is non-empty.
  Drive visibility with a small **presentation `IMultiValueConverter`** (new, in `Presentation/Converters/`) taking (`HasItems`, `SearchText`) → `Visibility`. No ViewModel/business change. (Fallback option: a presentation-only computed property on the VM that exposes the same boolean — only if MultiBinding proves awkward; must contain no data/command logic.)
- **Files affected:** new converter in `Presentation/Converters/`; the searchable page XAMLs (Phase 4).
- **Tables that opt in:** #1, #2, #3, #4, #5, #6, #7, #8 (the search-enabled list pages). #9 Users has no search box → generic empty only.
- **Risk:** Low.
- **Verification:** Type a non-matching query → "no results" message; clear data with empty search → "no data" message.

### 4.5 Shared loading overlay
- **Purpose:** Surface a loading state where already modeled. Resolves the missing loading state (audit §6.5).
- **Approach:** A reusable overlay style (semi-transparent panel + spinner/text) bound to a boolean. Wire **only** to `MaintenanceViewModel.IsLoading` (already exists) in Phase 4. Do not invent loading flags elsewhere in this pass.
- **Files affected:** `Presentation/Resources/DataGrid.xaml` (or a small shared dictionary); `MaintenancePage.xaml`.
- **Why safe:** Binds to an existing property; no new logic.
- **Risk:** Low.
- **Verification:** Overlay shows during Maintenance load, hides after.

### 4.6 Optional reusable copyable-cell template — `CopyableCellTemplate`
- **Purpose:** Replace the duplicated inline "value + hover copy button" `DataGridTemplateColumn` (in #1, #4, #6, #17) and the **string-built XAML** in `ReportsPage.xaml.cs`. Resolves L1.
- **Approach:** One `DataTemplate` resource parameterized by the bound field; reference it from each copyable column. For Reports, replace the `XamlReader.Parse` string with the shared resource where feasible (Phase 7).
- **Files affected:** `Presentation/Resources/DataGrid.xaml` (add template); consuming views (Phase 7); `ReportsPage.xaml.cs` (Phase 7, refactor only — no column identity/order change).
- **Why safe:** Behavior-preserving refactor; copy command (`CopyTextCommand`) unchanged.
- **Risk:** Low (Reports portion Med — verify in Phase 6/7 that copyable report columns still render and copy).
- **Verification:** Copy buttons appear on hover/selection and copy the correct value in all five locations.

### 4.7 Optional compact DataGrid variant — `CompactDataGrid` (dense tables only)
- **Purpose:** A `BasedOn=MainDataGrid` variant with reduced row/header height for the densest tables, to show more rows on short laptops. **Explicitly NOT global.** Resolves L4 / adaptive §9.
- **Files affected:** `Presentation/Resources/DataGrid.xaml` (add variant).
- **Tables that opt in:** **only** #5 Maintenance, #10 Supplier ledger, #12 Operations report. All other tables keep the standard height.
- **Risk:** Low.
- **Verification:** Only the three dense tables change height; all others identical.

### 4.8 Sticky header / scrollbars — **no change**
- Already inherent in `MainDataGrid`/`DialogDataGrid`. No work; just preserve when editing `DataGrid.xaml`.

---

## 5. Per-table implementation plan

> Each subsection lists: file · problem · target order (RTL right→left) · alignment · width · adaptive · states/footer · print-export impact · risk · verification. Orders/widths come from audit §7/§8; alignment from H1/H2; states from H3/H4.

### 5.1 Customers — `CustomersGrid`
- **File:** `Presentation/Views/CustomersPage.xaml`
- **Problem:** No money columns (low impact); Phone Auto width jitters; no count/no-search state.
- **Target order:** Keep `م · اسم العميل · الهاتف`.
- **Alignment:** Keep header center; Name right; Id center; Phone center **+ keep LTR**. No numeric style needed.
- **Width:** `الهاتف` Auto → **fixed ~150** (stable edge); Name `*`; optionally Name trim+tooltip (4.2).
- **Adaptive:** Natural full-width all sizes; no h-scroll.
- **States/footer:** Add no-search state (4.4); add empty state already present — keep; optional customer-count footer (M7).
- **Print/export:** None.
- **Risk:** Very low.
- **Verify:** Double-click → details; context menu works; phone LTR; no-results vs no-data messages correct.

### 5.2 Inventory / Products — `ProductsGrid`
- **File:** `Presentation/Views/InventoryPage.xaml`
- **Problem:** `الكمية`, `سعر البيع` centered (H1); unused 4th grid row (M6); no count footer.
- **Target order:** Keep `م · اسم المنتج · الفئة · الكمية · سعر البيع`.
- **Alignment:** `الكمية` + `سعر البيع` → **`DataGridTextNumeric`**; `الفئة` center ok; Name right.
- **Width:** Keep; add `min` to `سعر البيع`; Name `*`/min160.
- **Adaptive:** Already has h-scroll `Auto` ✔; fits at 1040px.
- **States/footer:** Add no-search state (4.4); keep empty overlay; use the **unused 4th row** for a product/low-stock count footer (M6/M7).
- **Print/export:** "طباعة الجرد" → `InventoryPrintService` (own columns). **Screen changes do not affect print.**
- **Risk:** Low.
- **Verify:** Numbers right-align; edit/adjust/delete context + double-click work; footer count correct; inventory print unchanged.

### 5.3 Suppliers — `SuppliersGrid`
- **File:** `Presentation/Views/SuppliersPage.xaml`
- **Problem:** `المديونية` centered (H1); `العنوان` clips with no tooltip (M5); debt sits after address.
- **Target order:** `م · اسم المورد · الهاتف · المديونية · العنوان` (move debt before address).
- **Alignment:** `المديونية` → numeric style **derived** to keep red+bold; Phone center (consider LTR); Name right; Address right.
- **Width:** Name `2*`, Address `*` **+ trim+tooltip** (4.2); Phone 150; Debt 150.
- **Adaptive:** Natural full-width; no h-scroll.
- **States/footer:** No-search (4.4); keep empty overlay.
- **Print/export:** None from this grid.
- **Risk:** Low.
- **Verify:** Debt red/bold + right-aligned; address tooltip; double-click → transactions; context menu intact.

### 5.4 Invoices — `InvoicesGrid`
- **File:** `Presentation/Views/InvoicesPage.xaml`
- **Problem:** money trio centered (H1); operator column mid-table; overflow risk at min width (H5); no count footer.
- **Target order:** `م · رقم الفاتورة · التاريخ · العميل · الإجمالي · المدفوع · المتبقي · المستخدم` (operator last; money grouped).
- **Alignment:** `الإجمالي/المدفوع/المتبقي` → numeric style (keep `المتبقي` orange-bold via `BasedOn`); Id/Date/User center; invoice# LTR center; Customer right + trim+tooltip.
- **Width:** money fixed 110 (min ~90); Customer `*`/min160.
- **Adaptive (H5):** **Enable `HorizontalScrollBarVisibility=Auto`**; verify rounded-corner clip still renders with scrollbar.
- **States/footer:** No-search (4.4); keep empty overlay; **add invoice-count footer** (M7).
- **Print/export:** Per-invoice print via `InvoicePrintService` (own columns). **Not affected.**
- **Risk:** Med (widest-but-one page grid + scroll change).
- **Verify:** Right-aligned money; narrow window → controlled h-scroll not crush; view/print context + double-click work; printed invoice identical.

### 5.5 Maintenance — `MaintenanceGrid`
- **File:** `Presentation/Views/MaintenancePage.xaml`
- **Problem:** widest grid, overflow at min width (H5); money centered (H1); status buried; `IsLoading` not surfaced.
- **Target order:** `رقم الطلب · الحالة · العميل · الفني · الأجهزة · الإجمالي · المدفوع · المتبقي · تاريخ الاستلام · التسليم المتوقع` (status up for triage).
- **Alignment:** money trio → numeric style; `الأجهزة` count → numeric/center; status badge centered (keep); dates center; order# LTR code; Customer/Technician keep trim+tooltip.
- **Width:** money 90 each; Customer `*`/min120.
- **Adaptive (H5):** **Enable `HorizontalScrollBarVisibility=Auto`**; **opt into `CompactDataGrid`** (4.7) for small-laptop density.
- **States/footer:** Keep count footer ✔; add no-search (4.4); **wire loading overlay to `IsLoading`** (4.5); keep empty overlay.
- **Print/export:** Intake/invoice via `PrintService` (own columns). **Not affected.**
- **Risk:** Med (10 columns + scroll + compact + loading).
- **Verify:** Status near order#; money right-aligned; loading overlay shows during refresh; h-scroll at min width; deliver/print/cancel toolbar + double-click intact; intake/invoice print unchanged.

### 5.6 Returns — `ReturnsGrid`
- **File:** `Presentation/Views/ReturnsPage.xaml`
- **Problem:** total centered (H1); reference numbers not adjacent; date column over-wide.
- **Target order:** `م · رقم المرتجع · رقم الفاتورة · التاريخ · العميل · الإجمالي · المستخدم` (references adjacent).
- **Alignment:** `الإجمالي` → numeric style (keep red bold); reference numbers LTR center; date center; Customer right.
- **Width:** `التاريخ` 180 → 150; reference cols 150; Customer `*`.
- **Adaptive:** Fits at 1040px; no h-scroll needed.
- **States/footer:** No-search (4.4); keep empty overlay; optional count footer.
- **Print/export:** Return receipt via `PrintService` (own columns). **Not affected.**
- **Risk:** Low.
- **Verify:** References adjacent; total red right-aligned; details/print context + double-click; return print unchanged.

### 5.7 Expenses — `ExpensesGrid`
- **File:** `Presentation/Views/ExpensesPage.xaml`
- **Problem:** amount centered (H1); description clips (M5); date not forward; dictionary-indexer bindings (do not touch).
- **Target order:** `م · التاريخ · الوصف · الفئة · المبلغ · طريقة الدفع · المستخدم`.
- **Alignment:** `المبلغ` → numeric style (keep orange bold); `الفئة`/`طريقة الدفع` center; Description right + trim+tooltip (4.2).
- **Width:** Description `2*`; numerics fixed.
- **Adaptive:** Fits; no h-scroll.
- **States/footer:** No-search (4.4); keep empty overlay. **If** a "visible total" footer is added, label it clearly as display-only and **exclude from print/export** (must not resemble a report total).
- **Print/export:** None from this grid (no export of this grid).
- **Risk:** Low.
- **Verify:** Amount right-aligned orange; description tooltip; delete context works; binding paths unchanged.

### 5.8 Employees — `EmployeesGrid`
- **File:** `Presentation/Views/EmployeesPage.xaml`
- **Problem:** salary centered (H1); role not adjacent to name.
- **Target order:** `م · الاسم الكامل · المسمى الوظيفي · الهاتف · الراتب الأساسي · الحالة · ملاحظات`.
- **Alignment:** `الراتب الأساسي` → numeric style; Phone center (consider LTR); job title center; status badge centered (keep fixed 100); Notes trim+tooltip (already) ✔.
- **Width:** Name `2*`, Notes `*`; status fixed 100.
- **Adaptive:** Fits; no h-scroll.
- **States/footer:** No-search (4.4); keep empty overlay.
- **Print/export:** None.
- **Risk:** Low.
- **Verify:** Salary right-aligned; role beside name; toggle/salary/deduction context + double-click intact.

### 5.9 Users — `UsersGrid`
- **File:** `Presentation/Views/UsersPage.xaml`
- **Problem:** discount % centered (H1); no search box (generic empty only).
- **Target order:** Keep `م · اسم المستخدم · الاسم الكامل · الموظف · الصلاحية · حد الخصم (%)`.
- **Alignment:** `حد الخصم (%)` → numeric style; `اسم المستخدم` LTR only if logins are Latin (verify, else center); role center; full name right.
- **Width:** Name `2*`; `الموظف` 180 → 150.
- **Adaptive:** Fits; no h-scroll.
- **States/footer:** Generic empty overlay (no search box → **no** no-search state); no count needed (small data).
- **Print/export:** None.
- **Risk:** Very low.
- **Verify:** Discount right-aligned; edit/change-password/delete context + double-click intact.

### 5.10 Supplier transactions (ledger) — `TransactionsGrid`
- **File:** `Presentation/Views/SupplierTransactionsPage.xaml`
- **Problem:** amount/paid/balance centered — **critical for a ledger** (H1); inline action column duplicates double-click (M3); raw date string (M4).
- **Target order:** `# · التاريخ · النوع · المبلغ · المدفوع · الرصيد بعد · بواسطة` (+ drop or narrow the `التفاصيل` button column).
- **Alignment:** `المبلغ/المدفوع/الرصيد بعد` → numeric style; **`الرصيد بعد` emphasized** (weight) and **red when negative** (display-only color via converter — value/sign unchanged); `النوع` right; date center.
- **Width:** three money cols 120; `بواسطة` `*`. If action column dropped, give space to `الرصيد بعد`/`بواسطة`. If kept (M3), make `Auto`/narrower, right-aligned; consider `FrozenColumnCount` so it stays visible on h-scroll.
- **Adaptive:** Fits; opt into `CompactDataGrid` (4.7) for long statements.
- **States/footer:** Keep empty overlay + hint footer ✔.
- **Print/export:** "طباعة كشف حساب" → `SupplierStatementPrintService` (own columns, pulls items separately). **Not affected** — safe to reorder/restyle screen ledger.
- **Risk:** Med (ledger semantics; negative-color must be display-only).
- **Verify:** Running balance right-aligned and vertically lined up; negative balance red without sign change; double-click → details; "عرض" (if kept) works; statement print byte-identical.

### 5.11 Supplier transaction items — `ItemsGrid`
- **File:** `Presentation/Views/SupplierTransactionDetailsPage.xaml`
- **Problem:** qty/price/total centered (H1); over-wide price/total/qty; empty text sits below grid not over it.
- **Target order:** Keep `اسم المنتج · الكمية · سعر الشراء · الإجمالي`.
- **Alignment:** `الكمية/سعر الشراء/الإجمالي` → numeric style; product name right.
- **Width:** `سعر الشراء`/`الإجمالي` 160 → 120; `الكمية` 120 → 80; product `*`.
- **Adaptive:** 4 cols — always natural full-width.
- **States/footer:** Convert the existing `EmptyItemsText` to the **overlay-on-grid** pattern (4.3) for consistency.
- **Print/export:** Covered by parent statement print; none here.
- **Risk:** Low.
- **Verify:** Numbers right-aligned; empty overlay centers over grid; summary tiles unaffected.

### 5.12 Reports grid — `ReportDataGrid` (all variants)
- **Files:** `Presentation/Views/ReportsPage.xaml`, `Presentation/Views/ReportsPage.xaml.cs`, `Presentation/ViewModels/ReportsViewModel.cs`
- **Problem:** numeric report columns centered (H1); `التأثير الصافي` negative not flagged; no empty overlay; **screen/print/CSV order coupled** (see §6).
- **Target order (display):** per audit §7 — Operations `التاريخ→…→التأثير الصافي→الموظف`; Returns references adjacent; Inventory/Suppliers keep.
- **Alignment:** switch the code-behind "numeric" set from center → **right** (in `OnColumnsChanged` element-style construction); add **red-on-negative** for `التأثير الصافي` (display-only trigger; value unchanged). Keep `العملية` color-coding and copyable templates.
- **Width:** keep `Auto`+`*` scheme; copyable cols fixed 140.
- **Adaptive:** keep `HorizontalScrollBarVisibility=Auto` + `ForwardMouseWheelToParent` ✔; opt into `CompactDataGrid` for operations.
- **States/footer:** add empty overlay for operations/returns/suppliers variants (4.3); optional "showing N operations" line.
- **Print/export:** **COUPLED — see §6 for the mandatory handling and verification.**
- **Risk:** **Med-High** (only coupled surface in the app).
- **Verify:** see §6 verification list (all 5 variants + CSV + print).

### 5.13 POS product search — `ProductsGrid`
- **File:** `Presentation/Views/POSPage.xaml`
- **Problem:** price/stock centered (H1); no empty/pre-search state (H3).
- **Target order:** Keep `# · اسم المنتج · السعر · المخزون`.
- **Alignment:** `السعر`/`المخزون` → numeric style; name right.
- **Width:** Keep; name `*`/min140.
- **Adaptive:** Left pane (`*`, min 480); v-scroll `Auto` ✔; no h-scroll.
- **States/footer:** Add empty/pre-search overlay (4.3) ("ابحث لإظهار المنتجات").
- **Print/export:** None.
- **Risk:** Low.
- **Verify:** Numbers right-aligned; double-click/add-to-cart context works; empty hint before search.

### 5.14 POS cart — `CartGrid`
- **File:** `Presentation/Views/POSPage.xaml`
- **Problem:** qty/total centered (H1); Auto widths shift edge; no empty-cart state (H3).
- **Target order:** Keep `المنتج · العدد · إجمالي`.
- **Alignment:** `العدد`/`إجمالي` → numeric style (keep total bold); product right.
- **Width:** `العدد`/`إجمالي` Auto → fixed ~50/90 (stable edge); product `*`.
- **Adaptive:** Fixed right pane (320, min 280); v-scroll only.
- **States/footer:** Add empty-cart overlay bound to `Cart.IsEmpty` (already exists) (4.3); total box remains footer.
- **Print/export:** Checkout receipt via `ReceiptPrintService` (own layout). **Not affected.**
- **Risk:** Low.
- **Verify:** Numbers right-aligned; stable column edge; empty-cart message; edit qty/price/remove (context + buttons + double-click) intact; receipt unchanged.

### 5.15 Invoice view / refund lines — refund grid
- **File:** `Presentation/Views/InvoiceViewDialog.xaml`
- **Problem:** qty/unit/total centered though LTR (H1); editable grid (`IsReadOnly=False`) — preserve interaction.
- **Target order:** Keep `# · المنتج · الكمية · سعر الوحدة · الإجمالي · المرتجع` (stepper at end).
- **Alignment:** `الكمية/سعر الوحدة/الإجمالي` → right (keep LTR + bold total); product right + ellipsis ✔; stepper centered.
- **Width:** Keep (stepper 160, money 120).
- **Adaptive:** Fixed dialog chrome; v-scroll `Auto` ✔; no h-scroll.
- **States/footer:** Lines always present (low risk) — empty overlay optional; summary cards/refund preview remain.
- **Print/export:** "طباعة الفاتورة" via service (own layout). **Not affected.** **Do not alter refund quantity logic/commands.**
- **Risk:** Med (editable grid — must not disturb `Inc/DecReturnCommand` or `ReturnQuantity` binding).
- **Verify:** Stepper +/− still updates return qty and refund preview; numbers right-aligned; print + refund buttons unchanged.

### 5.16 Return details items — `ItemsGrid`
- **File:** `Presentation/Views/ReturnDetailsDialog.xaml`
- **Problem:** qty/price/total centered though LTR (H1); no empty state (low risk).
- **Target order:** Keep `المنتج · الكمية · السعر · الإجمالي`.
- **Alignment:** numerics → right (keep LTR + bold total); product right.
- **Width:** Keep.
- **Adaptive:** Fixed dialog (NoResize); always fits.
- **States/footer:** Empty overlay optional (a return always has items); refund banner remains.
- **Print/export:** Return print via service (own layout). **Not affected.**
- **Risk:** Low.
- **Verify:** Numbers right-aligned; banner total unchanged; print unchanged.

### 5.17 Customer invoices — `InvoicesGrid` (dialog)
- **File:** `Presentation/Views/CustomerInvoicesDialog.xaml`
- **Problem:** money inherits default right while page Invoices centers them (M2 inconsistency); status badge `*` floats in big cell (M1); date format differs (M4).
- **Target order:** Keep `# · رقم الفاتورة · التاريخ · النوع · الإجمالي · المدفوع · المتبقي · الحالة`.
- **Alignment:** apply `DataGridTextNumeric` explicitly to `الإجمالي/المدفوع/المتبقي` (keep green/orange/bold via `BasedOn`) so it **matches page Invoices** (right) — resolve M2; status badge centered (keep).
- **Width:** **`الحالة` `*` → `Auto`/100** (M1); money 110.
- **Adaptive:** Dialog 932×632; fits; extra width acceptable once status is tight.
- **States/footer:** Has `IsEmpty` empty state ✔ (keep); sales-count header ✔.
- **Print/export:** "طباعة كشف حساب" via service (own layout). **Not affected.**
- **Risk:** Low.
- **Verify:** Money alignment matches page Invoices; status badge tight; view/print context + double-click intact; statement print unchanged.

### 5.18 Repair parts — parts grid
- **File:** `Presentation/Views/RepairPartsDialog.xaml`
- **Problem:** **no element styles** → all default right incl. numerics (H2 inconsistency); no empty state (H3).
- **Target order:** Keep `القطعة · الكمية · سعر الوحدة · الإجمالي · المصدر`.
- **Alignment:** apply `DataGridTextNumeric` to `الكمية/سعر الوحدة/الإجمالي` (explicit, so it's intentional not fallback); `المصدر` center optional; `القطعة` right.
- **Width:** Keep (`القطعة` `*`, qty 65, money 110, source 90).
- **Adaptive:** Resizable dialog; always fits; sticky header.
- **States/footer:** Add empty overlay (4.3) ("لا توجد قطع مضافة"); keep total footer.
- **Print/export:** Parts print on repair invoice (own layout). **Not affected.**
- **Risk:** Low.
- **Verify:** Numbers explicitly right-aligned; empty overlay on new device; add/delete part + total unchanged.

### 5.19 Repair order · Devices tab — devices grid
- **File:** `Presentation/Views/RepairOrderDialog.xaml`
- **Problem:** no element styles → default right (H2); `المشكلة` long text clips (M5); plain-text status vs badge elsewhere; no empty state (H3).
- **Target order:** Keep `النوع · الجهاز · المشكلة · الحالة · أجر العمل` (status after device optional).
- **Alignment:** `أجر العمل` → `DataGridTextNumeric` (explicit); `المشكلة` trim+tooltip (4.2); optionally render `الحالة` as centered badge for consistency (display-only).
- **Width:** Two `*` cols (device/issue); verify `النوع` 80 doesn't clip Arabic type.
- **Adaptive:** Resizable dialog; fits.
- **States/footer:** Add empty overlay (4.3) (new order's Devices tab empty until saved); keep hint line.
- **Print/export:** Devices print on intake/invoice (own layout). **Not affected.**
- **Risk:** Low.
- **Verify:** Labor right-aligned; issue tooltip; empty overlay; add/parts/edit/remove toolbar intact.

### 5.20 Repair order · Payments tab — payments grid
- **File:** `Presentation/Views/RepairOrderDialog.xaml`
- **Problem:** no element styles → default right (H2); date not forward; notes clip (M5); no empty state (H3).
- **Target order:** `التاريخ · مبلغ التحصيل · طريقة الدفع · ملاحظات` (date first).
- **Alignment:** `مبلغ التحصيل` → `DataGridTextNumeric` (explicit); `ملاحظات` trim+tooltip (4.2); date right/center.
- **Width:** Notes `*`; date 140; amount/method 120.
- **Adaptive:** Resizable dialog; fits.
- **States/footer:** Add empty overlay (4.3) (new order has no payments); keep totals footer.
- **Print/export:** Payments print on repair invoice (own layout). **Not affected.** **Do not alter `RegisterPaymentCommand` or payment totals.**
- **Risk:** Low.
- **Verify:** Amount right-aligned; notes tooltip; empty overlay; register-payment + totals unchanged.

### 5.21 Supplier purchase lines — `Lines` grid
- **File:** `Presentation/Views/SupplierPurchaseDialog.xaml`
- **Problem:** all 4 template columns centered via `DataGridTextCenter` (H1); no empty state — **first thing user sees on a new purchase** (H3, highest value).
- **Target order:** Keep `اسم المنتج · الكمية · سعر الشراء · الإجمالي`.
- **Alignment:** switch `الكمية/سعر الشراء/الإجمالي` template `TextBlock` styles to right-aligned numeric; product name → right (to match other item grids).
- **Width:** Keep (`اسم المنتج` `2*`, qty 90, money 120).
- **Adaptive:** Fixed dialog (NoResize); always fits; sticky header.
- **States/footer:** **Add empty overlay (4.3)** ("أضف أسطر المشتريات") — top priority empty state; keep totals row.
- **Print/export:** **No export of this grid.** Has CSV/XLSX **import** → lines; import path independent of screen column order. **Not affected.**
- **Risk:** Low.
- **Verify:** Numbers right-aligned; empty overlay on open; add/delete line, manual amount, import, AI-prompt, save/cancel all unchanged.

---

## 6. ReportsPage special handling

**Files:** `Presentation/Views/ReportsPage.xaml`, `Presentation/Views/ReportsPage.xaml.cs`, `Presentation/ViewModels/ReportsViewModel.cs`

### 6.1 The risk (from audit §6.12)
- Columns are **generated at runtime** in `ReportsPage.xaml.cs:OnColumnsChanged` from `ReportColumn` lists defined in `ReportsViewModel`.
- The code-behind **iterates the list in reverse** (`for i = Count-1 … 0`) to lay columns out for RTL, so **on-screen order = reverse(ViewModel list)**.
- **CSV export** (`ReportsViewModel.ExportReport`) and **print** (`PrintReport` / `PrintKpiSummary`) read the **same `_currentColumns` list in forward order** via header/binding-path arrays.
- **Therefore screen order, CSV order, and print order are all derived from one list.** Reordering the list to change the screen also reorders CSV and print.

### 6.2 Recommended approach: **keep coupling, reorder intentionally, re-verify both outputs**
- **Do not** split display vs export/print order in this pass. Splitting adds a parallel ordering structure and new code paths — higher regression risk on a financial surface, for marginal benefit. Coupling is acceptable because forward (file/print) and reverse (screen) are both valid presentations of the same column set.
- To change the **display** order (audit §7 targets), edit the **order of items in the `_currentColumns` list** in `ReportsViewModel` for the relevant report method. Because the list also drives CSV/print, the file/print column order changes correspondingly — **this is acceptable as long as headers/paths/formats are unchanged and totals are unaffected** (they are; only order changes).
- **Hard constraints (must not change):** `Header` text, `BindingPath`, `Format`, `IsProperty`, the `FlexibleNumber` formatting, any KPI/summary calculation, `ExportReport`/`PrintReport`/`PrintKpiSummary` logic, and the financial values themselves.
- **Alignment + negative color** are applied in `OnColumnsChanged` element-style construction (presentation only): flip the "numeric" set from center → right; add a red trigger for negative `التأثير الصافي`. These do not touch values.
- **Copyable columns:** keep current behavior; if Phase 7 swaps the string-built XAML for the shared template, verify copy still works for `رقم المرجع`/`رقم الفاتورة`/`رقم المرتجع`/`الهاتف`.

### 6.3 If a stakeholder later requires divergent screen vs export order
- Only then introduce a separate `DisplayOrder` vs `ExportOrder` (e.g., a sort index on `ReportColumn`), implemented as a **presentation-only** ordering — still no value/total/path change. Out of scope for this plan unless explicitly requested.

### 6.4 Required verification (every report variant + outputs)
For **each** of: **Daily Operations**, **Monthly Operations**, **Returns report**, **Inventory low-stock**, **Suppliers-debt**:
- [ ] Screen column **order** matches the §7 target (reverse of list) and renders without XAML runtime error.
- [ ] Numeric columns **right-aligned**; `التأثير الصافي` negative shows red; `العملية` color-coding intact; copyable columns copy correctly.
- [ ] **Empty overlay** appears when the variant has no rows.
- [ ] Date-filter changes still reload correctly (no logic change).
- [ ] **Values, totals, KPI cards unchanged** vs current build (compare numbers before/after).

For **outputs**:
- [ ] **CSV export:** open the file — headers and column order as expected; **every value identical** to pre-change (spot-check sums); encoding/quoting unchanged.
- [ ] **Print output:** print each report — header/column order and **all printed values/totals identical** to pre-change.
- [ ] **Daily/Monthly summary print** (`PrintKpiSummary`, البيان/القيمة two-column) unchanged.

---

## 7. Desktop-size verification matrix

> Verify by resizing `MainWindow` (min enforced 1280×720; default 1366×768). Page content width ≈ window − 220px nav rail.

| Size | Invoices | Maintenance | Reports ops grid | Supplier ledger | POS | Supplier purchase dialog | Repair order dialog |
|------|----------|-------------|------------------|-----------------|-----|--------------------------|---------------------|
| **Min window (1280×720, ~1040px page)** | h-scroll engages, no crush; headers sticky | h-scroll + compact rows; status visible | h-scroll already; wheel handoff works | fits; balance right-aligned; compact ok | panes at min (cart 280 / products 480) usable | fixed dialog (min 820×560) fully usable | resizable min 832×632 usable |
| **1366×768 (small laptop)** | fits; money aligned | tight but no crush | fits/borderline → scroll ok | fits | comfortable | fits | fits |
| **1536 / 1600 (large laptop)** | natural | natural/slightly tight | natural | natural | natural | natural | natural |
| **1920×1080 (normal desktop)** | natural full-width | natural | natural | natural | natural | natural | natural |
| **2560+ (external monitor)** | natural; no over-stretch of fixed cols | natural | natural | natural | natural | dialog stays centered (max = screen) | dialog stays centered |

**Per-size checks:** sticky header holds during vertical scroll; thin scrollbars render inside rounded corners; no mouse-wheel trap on the Reports operations grid; flexible (`*`) columns absorb width without pushing fixed money columns off-screen until h-scroll engages.

---

## 8. Print/export regression checklist

> Goal: confirm screen table changes do **not** alter any printed/exported output. All print layouts live in `Infrastructure/Printing/*` and build their own columns — **off-limits to edits**.

| Path | Service / source | Should screen changes affect it? | Verify |
|------|------------------|----------------------------------|--------|
| **Inventory print** | `InventoryPrintService` | **No** (own columns) | Print inventory before/after → identical sheet |
| **Invoice print** | `InvoicePrintService` | **No** | Print an invoice → identical |
| **Return print** | `PrintService.PrintReturnReceipt` | **No** | Print a return → identical |
| **Maintenance intake** | `PrintService.PrintRepairIntake` | **No** | Print intake → identical |
| **Maintenance invoice** | `PrintService.PrintRepairInvoice` | **No** | Print repair invoice → identical |
| **Supplier statement** | `SupplierStatementPrintService` | **No** | Print statement → identical (incl. line items) |
| **Reports print** | `ReportsViewModel.PrintReport` + `ReportPrintService` | **Yes (order coupled)** — values must stay identical | Per §6.4: order as expected, **values/totals identical** |
| **Reports CSV export** | `ReportsViewModel.ExportReport` | **Yes (order coupled)** — values must stay identical | Per §6.4: headers/order expected, **values identical** |
| **Daily/Monthly KPI print** | `PrintKpiSummary` | **No** (separate two-column) | Print summary → identical |
| **Supplier purchase import** | CSV/XLSX → lines (`ImportExcelCommand`) | **No** (import independent of screen column order) | Import a file → same lines parsed |

**Rule:** Only the two Reports outputs are order-coupled; everything else must be byte-identical. Any difference other than Reports **column order** is a regression.

---

## 9. Testing & verification checklist

**Build / launch**
- [ ] Solution builds with no new warnings/errors (tests target the Tests project with `--tl:off` per project convention).
- [ ] App launches; login → main shell loads.

**Per-table smoke (all 21 grids)**
- [ ] Each page/dialog opens with **no XAML runtime/binding errors** (check Output window for binding failures).
- [ ] Every **context menu** still appears and its items execute (Customers, Inventory, Suppliers, Invoices, Returns, Expenses, Employees, Users, POS products/cart, CustomerInvoices, ledger).
- [ ] Every **double-click** action still fires (Customers→details, Inventory→edit, Suppliers→transactions, Invoices→view, Maintenance→open, Returns→details, Employees→edit, Users→edit, POS products→add, ledger→details, CustomerInvoices→details).
- [ ] **Editable refund stepper** (#15) still adjusts return quantity + preview.

**Visual / alignment**
- [ ] Numeric/money/qty/balance/salary columns **right-aligned** and digits line up vertically on every opted-in table (§5 list).
- [ ] Colored/bold money cells keep color + weight (debt red, remaining orange, totals bold).
- [ ] Status badges centered and tightly sized (esp. #17 status no longer `*`).
- [ ] Codes/phones/invoice numbers remain **LTR**; Arabic text remains right-aligned; headers remain centered.
- [ ] Long-text columns truncate with **tooltip** where planned (address, description, issue, notes, report details).

**Adaptive / scroll**
- [ ] Horizontal scroll appears **only** on Invoices and Maintenance at narrow widths (and Reports ops, already) — not elsewhere.
- [ ] Compact rows apply **only** to Maintenance, ledger, Reports-ops.
- [ ] Sticky headers hold during scroll on all grids.

**States**
- [ ] Empty overlays appear on all newly wired dialog/POS grids (esp. SupplierPurchase lines, RepairOrder devices/payments, RepairParts, POS cart/products).
- [ ] **No-search-result** message appears on list pages when a query matches nothing; **no-data** message when truly empty; the two are distinct.
- [ ] Maintenance **loading overlay** shows during load.

**Safety**
- [ ] **Print/export values unchanged** across §8 (Reports order may change; values must not).
- [ ] No files under `Infrastructure/Printing/*`, `Application/*` services, `Domain/*`, `Infrastructure/*` data layers, or any command/calculation code were modified.
- [ ] No binding paths, converter logic, KPI/summary math, supplier balances, inventory quantities, salary, maintenance, or refund calculations changed.

---

## 10. Files likely to be edited

**Shared resources / utilities (Phase 1, 7)**
- `Presentation/Resources/DataGrid.xaml` — add `DataGridTextNumeric`, `DataGridTextTruncate` base, loading overlay style, optional `CompactDataGrid`, `CopyableCellTemplate`. (No existing key's behavior changed.)
- `Presentation/Converters/` — new presentation `IMultiValueConverter` for the no-search-result state (4.4). (Existing converters untouched.)

**Pages (Phases 2–5)**
- `CustomersPage.xaml`, `InventoryPage.xaml`, `SuppliersPage.xaml`, `InvoicesPage.xaml`, `MaintenancePage.xaml`, `ReturnsPage.xaml`, `ExpensesPage.xaml`, `EmployeesPage.xaml`, `UsersPage.xaml`, `SupplierTransactionsPage.xaml`, `SupplierTransactionDetailsPage.xaml`, `POSPage.xaml`.

**Dialogs (Phases 2–5)**
- `InvoiceViewDialog.xaml`, `ReturnDetailsDialog.xaml`, `CustomerInvoicesDialog.xaml`, `RepairPartsDialog.xaml`, `RepairOrderDialog.xaml`, `SupplierPurchaseDialog.xaml`.

**Reports (Phase 6)**
- `Presentation/Views/ReportsPage.xaml`, `Presentation/Views/ReportsPage.xaml.cs` (alignment/negative-color/empty overlay; optional copyable-template refactor).
- `Presentation/ViewModels/ReportsViewModel.cs` — **only** `_currentColumns` list **ordering** for display targets. **No** header/path/format/value/total change.

**Possibly touched for state wiring (Phase 4, presentation only)**
- `MaintenancePage.xaml` (loading overlay → existing `IsLoading`).
- Searchable page XAMLs (no-search overlays).

**Not edited (explicitly):** `Infrastructure/Printing/*`, any `Application`/`Domain`/`Infrastructure` service/data/command files, authentication/authorization.

---

## 11. Rollback plan

**Principle:** Each phase is a separate, self-contained commit so it can be reverted independently. Phase 1 is additive-only (safe to keep even if later phases revert).

- **By phase (preferred):** `git revert` the offending phase commit. Because Phases 2–5 only *reference* Phase-1 resource keys per column/table, reverting a later phase restores prior alignment/order/width/state without affecting other phases.
- **Phase 1 (shared resources):** If a new style misbehaves, the fix is to stop referencing it (revert the consuming phase); the unused key in `DataGrid.xaml` is harmless. If needed, delete the added keys last (only after no table references them).
- **Phase 2 (numeric alignment):** Per-column revert — change the column's `ElementStyle` back from `DataGridTextNumeric`/derived to its previous value. Safe to roll back a single table without touching others.
- **Phase 3 (order/width):** Per-file XAML revert; column reorders are declaration-only.
- **Phase 4 (states):** Remove the added overlay element(s) / converter reference; no data impact.
- **Phase 5 (adaptive):** Remove `HorizontalScrollBarVisibility=Auto` / `CompactDataGrid` opt-in on the specific table.
- **Phase 6 (Reports):** Highest-care revert. Restore the original `_currentColumns` list **order** in `ReportsViewModel` and the original `OnColumnsChanged` alignment block in `ReportsPage.xaml.cs`. **Immediately re-verify CSV + print** per §6.4 after any revert, since these are coupled.
- **Phase 7 (refactor):** Re-inline the copyable template / restore the `XamlReader.Parse` string; restore the unused Inventory row if desired (cosmetic).

**Regression trigger → action:**
- Visual-only regression on one table → revert that table's column edit (Phase 2/3/4/5) only.
- Any **print/export value** difference → revert Phase 6 immediately and re-verify §8 (this should never happen for non-Reports paths; if it does, the change exceeded scope).
- XAML runtime/binding error on launch → revert the most recent phase commit; binding errors almost always trace to an over-eager column edit referencing a wrong style key.

---

*End of implementation plan. No source files were edited. No code patches were generated.*
