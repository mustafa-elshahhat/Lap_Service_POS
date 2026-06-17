# Tables & Data-Grid UX Audit Report

**Project:** AlJohary ServiceHub (Lap_Service POS) — WPF desktop application
**Scope:** Every table / `DataGrid` / list-table / report table / ledger table / modal table in the application
**Type:** Desktop-only. RTL Arabic primary UI. **Audit only — no code was changed.**
**Date:** 2026-06-17

---

## 0. How to read this report

- This is a **WPF** application. "Tables" are almost all `System.Windows.Controls.DataGrid` instances. There are no HTML/web grids, no `ListView` report tables, and no third-party grid library.
- **RTL note:** Every page/dialog sets `FlowDirection="RightToLeft"`. In RTL, the **first column declared in XAML renders on the right** and reading proceeds right→left. Throughout this report, "current column order" is listed **in XAML declaration order = on-screen reading order (right→left)** unless stated otherwise.
- **Desktop sizes used for adaptive analysis** (no mobile/tablet considered):
  - **Min safe window:** `MainWindow` enforces `MinWidth=1280, MinHeight=720`. The left nav rail consumes 220 px, so a page gets **~1040 px** usable width at the minimum. This is the binding constraint for "smallest desktop."
  - **Small laptop:** ~1366×768 (the app's default `Width=1366 Height=768`).
  - **Large laptop:** ~1536–1600 wide.
  - **Normal desktop:** ~1920×1080.
  - **Large external monitor:** ≥2560 wide.

---

## 1. Executive summary

The grid layer is **unusually consistent** for a WPF app: almost every table uses one shared style (`MainDataGrid`, or `DialogDataGrid` derived from it) defined in `Presentation/Resources/DataGrid.xaml`. That style already does several things well:

- **Sticky header by default.** The `MainDataGrid` control template puts the column-header presenter in a non-scrolling row above the scroll content, so headers stay fixed during vertical scroll on *every* grid. No per-table work needed.
- **Both scrollbars are templated in** (custom thin `ModernScrollBar`), so horizontal scroll *is* available when columns overflow — but only some grids opt into `HorizontalScrollBarVisibility="Auto"`.
- A **shared empty-state overlay** style (`DataGridEmptyState`) + `InverseBooleanToVisibilityConverter` bound to `HasItems` is used on most full-page grids.
- Sensible RTL alignment defaults: **headers centered**, **text cells right-aligned** (Arabic reading edge), with opt-in helper styles (`DataGridTextCenter`, `DataGridCodeStyle` for LTR codes, `DataGridTextRight/Left`).

The main systemic weaknesses are:

1. **Numeric/amount columns are centered, not right-aligned.** `DataGridTextCenter` is applied to virtually all money/quantity columns across the app. Centered numbers are hard to compare down a column (digits don't line up). This hurts the financial tables most: Invoices, Maintenance, Supplier ledger, Reports/Operations.
2. **Inconsistent alignment inside dialog grids.** Several modal grids (`RepairPartsDialog`, `RepairOrderDialog` devices/payments) define **no element style at all**, so numeric columns fall back to the default *right* alignment — i.e. money is centered in page grids but right-aligned in dialog grids. There is no single rule being followed.
3. **Empty/loading/error states are applied unevenly.** Full-page grids mostly have an empty overlay; **modal grids generally have none**. There is no loading state surfaced anywhere (even though `MaintenanceViewModel` has an `IsLoading` flag), and no error/"no search results" state distinct from "no data".
4. **Horizontal-overflow behavior is opt-in and ad-hoc.** Only `InventoryPage`, `POS`, and `Reports` set `HorizontalScrollBarVisibility="Auto"`. Wide grids (Maintenance = 10 columns, Operations report = 10 columns) can squeeze columns below readability at the 1040 px minimum width without a controlled scroll fallback.
5. **A duplicated "copyable cell" template** (number + hover copy button) is hand-inlined in ≥5 places and also generated as a XAML string in `ReportsPage.xaml.cs`. This is a shared-component opportunity.
6. **Reports column order is reversed in code-behind** and the *same* column list drives both screen and print/CSV — a subtle print/export coupling that must be respected.

No table needs pagination today (data volumes are店-scale and grids virtualize), but the **Invoices**, **Maintenance**, and **Operations report** grids are the ones that will degrade first as row counts grow, and they currently have no row-count footer (except Maintenance, which does).

---

## 2. Complete list of tables found

| # | Table | File | Style | Kind |
|---|-------|------|-------|------|
| 1 | Customers list | `Presentation/Views/CustomersPage.xaml` | MainDataGrid | List page |
| 2 | Products / Inventory list | `Presentation/Views/InventoryPage.xaml` | MainDataGrid | List page |
| 3 | Suppliers list | `Presentation/Views/SuppliersPage.xaml` | MainDataGrid | List page |
| 4 | Invoices list | `Presentation/Views/InvoicesPage.xaml` | MainDataGrid | List page |
| 5 | Maintenance orders list | `Presentation/Views/MaintenancePage.xaml` | MainDataGrid | List page |
| 6 | Returns list | `Presentation/Views/ReturnsPage.xaml` | MainDataGrid | List page |
| 7 | Expenses list | `Presentation/Views/ExpensesPage.xaml` | MainDataGrid | List page |
| 8 | Employees list | `Presentation/Views/EmployeesPage.xaml` | MainDataGrid | List page |
| 9 | Users list | `Presentation/Views/UsersPage.xaml` | MainDataGrid | List page |
| 10 | Supplier transactions (ledger) | `Presentation/Views/SupplierTransactionsPage.xaml` | MainDataGrid | Ledger page |
| 11 | Supplier transaction items | `Presentation/Views/SupplierTransactionDetailsPage.xaml` | MainDataGrid | Detail page |
| 12 | Reports grid (dynamic; 5 variants) | `Presentation/Views/ReportsPage.xaml` + `ReportsPage.xaml.cs` | MainDataGrid | Report page |
| 13 | POS product search | `Presentation/Views/POSPage.xaml` | DialogDataGrid | Embedded grid |
| 14 | POS cart | `Presentation/Views/POSPage.xaml` | DialogDataGrid | Embedded grid |
| 15 | Invoice view / refund lines | `Presentation/Views/InvoiceViewDialog.xaml` | DialogDataGrid | Modal (editable) |
| 16 | Return details items | `Presentation/Views/ReturnDetailsDialog.xaml` | DialogDataGrid | Modal |
| 17 | Customer invoices | `Presentation/Views/CustomerInvoicesDialog.xaml` | DialogDataGrid | Modal |
| 18 | Repair parts | `Presentation/Views/RepairPartsDialog.xaml` | MainDataGrid | Modal |
| 19 | Repair order — Devices tab | `Presentation/Views/RepairOrderDialog.xaml` | MainDataGrid | Modal (tab) |
| 20 | Repair order — Payments tab | `Presentation/Views/RepairOrderDialog.xaml` | MainDataGrid | Modal (tab) |
| 21 | Supplier purchase lines | `Presentation/Views/SupplierPurchaseDialog.xaml` | DialogDataGrid | Modal |

**The Reports grid (#12) is really 5 logical tables** generated at runtime: Daily/Monthly Operations log, Returns report, Inventory low-stock, Suppliers-debt (Daily/Monthly summary reports show **KPI cards only — no table**).

**Not tables (checked and excluded):**
- `SettingsPage.xaml` — only an `ItemsControl` of shop phone numbers (a simple repeated row list, not a tabular grid).
- `CashSaleDialog.xaml` — no grid.
- `ReportsPage` KPI area — `ItemsControl`/`WrapPanel` of KPI cards, not a table.
- `MainWindow.xaml` — navigation chrome only.
- No **nested tables** (a grid inside a grid cell) exist anywhere. Repair flow opens *separate* dialogs, it does not nest grids.

---

## 3. High-priority issues

> Each item names the exact table(s) it applies to. None require business-logic, calculation, command, DB, totals, export-math, or print-math changes.

- **H1 — Money columns are centered everywhere, defeating column scanning.** Tables #3,#4,#5,#6,#7,#8,#10,#11,#12(operations/returns/suppliers),#13,#14,#15,#16. `DataGridTextCenter` is applied to amounts, totals, paid/remaining, debt, balance. Recommend **right-aligned** numeric columns (with consistent decimals) so figures line up. This is the single biggest readability win and is purely an `ElementStyle` change per amount column.
- **H2 — Dialog grids define no cell alignment and silently fall back to right-align, contradicting page grids.** Tables #18 (RepairParts), #19/#20 (RepairOrder devices/payments). Their numeric columns (`الكمية`, `سعر الوحدة`, `الإجمالي`, `أجر العمل`, `مبلغ التحصيل`) inherit the default *right* alignment, while the same concepts are *centered* in page grids. Pick one rule (recommended: right for numbers) and apply it; today the app is internally inconsistent.
- **H3 — Modal grids have no empty state.** Tables #15,#16,#18,#19,#20,#21 and the POS grids #13,#14 show a blank white area when there are no rows. `RepairOrderDialog` Devices/Payments and `SupplierPurchaseDialog` lines are the worst (a new order/purchase always starts empty). Reuse the existing `DataGridEmptyState` overlay pattern already used on the page grids.
- **H4 — No "no search results" distinction.** Tables #1,#2,#3,#4,#5,#6,#7,#8 filter the same collection on search; when a search matches nothing, the generic "لا يوجد… للعرض" overlay appears, indistinguishable from "there is no data at all." A search-aware message ("لا توجد نتائج مطابقة للبحث") would prevent users thinking the dataset is empty.
- **H5 — Widest grids can crush columns at the 1040 px minimum width with no controlled fallback.** Table #5 Maintenance (10 columns, ~950 px of fixed widths + a `*` customer column with `MinWidth=120`) and Table #12 Operations report (10 columns) approach/exceed the usable width at `MinWidth=1280`. Maintenance does **not** set `HorizontalScrollBarVisibility`, so the `*` column and `MinColumnWidth=50` absorb the squeeze and the customer name truncates hard. Recommend enabling controlled horizontal scroll (it's already templated in) once total min-widths exceed the viewport, rather than letting the flexible column collapse.

---

## 4. Medium-priority issues

- **M1 — Status badges centered but the column is still `*`/wide in places.** Table #17 (CustomerInvoices) gives the status badge `Width="*"`, so the badge floats in a large empty cell on wide windows. A fixed/`Auto` width keeps the badge tight. (Maintenance #5 and Employees #8 badges are already fixed-width — good.)
- **M2 — `CustomerInvoicesDialog` (#17) amounts use neither center nor right helper** — they inherit default right align, while the *same* invoice list on the full `InvoicesPage` (#4) centers them. Two views of the same data disagree. Align them (recommended: right for both).
- **M3 — Action presentation is inconsistent across tables.** Most tables expose actions via **right-click context menu** (#1,#2,#3,#4,#6,#7,#8,#9,#13,#14,#17) and/or double-click; but **#10 Supplier ledger** puts an inline "عرض" button in a dedicated `التفاصيل` column, and **#12 Reports** generates copy buttons inline. There is no single convention. For desktop this is acceptable, but the lone inline-button column in #10 wastes ~100 px and duplicates the existing double-click handler (`TransactionsGrid_MouseDoubleClick`).
- **M4 — Date formats differ between tables.** `yyyy-MM-dd hh:mm tt` (#4 Invoices, #6 Returns), `yyyy/MM/dd` (#17), raw DB string (#5 Maintenance `IntakeDate`/`ExpectedDelivery`, #7 Expenses `expense_date`, #10 ledger `transaction_date`, #20 `PaymentDate`). Mixed separators and 12-h vs none. Recommend one display convention per data type (date vs date-time). **Do not change stored values or report/print formatting — display only.**
- **M5 — Long-text columns lack consistent truncation+tooltip.** `العميل`/`الفني` in #5 and `ملاحظات` in #8 correctly use `TextTrimming=CharacterEllipsis` + `ToolTip`. But `العنوان` (#3 Suppliers), `الوصف` (#7 Expenses), `المشكلة` (#19 RepairOrder), `التفاصيل`/`السبب` (#12 reports) can overflow/clip with no tooltip. Apply the same trim+tooltip pattern to those specific long-text columns.
- **M6 — Inventory has a stray empty 4th grid row.** Table #2 `InventoryPage` declares 4 `RowDefinition`s (`Auto,Auto,*,Auto`) but only fills 3; the trailing `Auto` row is unused. Harmless, but it's dead layout and a candidate for a row-count footer (which Inventory currently lacks).
- **M7 — Row count / footer is present on only one grid.** Only #5 Maintenance shows "عدد الطلبات". Invoices (#4), Returns (#6), Expenses (#7), Inventory (#2), Suppliers (#3), Customers (#1) have no count footer, so users can't tell at a glance how many records matched a search.

---

## 5. Low-priority issues

- **L1 — Duplicated copyable-cell template.** Identical "value + hover copy button" `DataGridTemplateColumn` is inlined in #1 (phone), #4 (invoice #), #6 (return #, invoice #), #17 (invoice #), and string-generated in `ReportsPage.xaml.cs`. Extract one shared template/`DataTemplate` resource. Pure refactor; no behavior change.
- **L2 — `ColumnWidth="*"` default never effectively used.** `MainDataGrid` sets `ColumnWidth="*"`, but virtually every column declares an explicit width, so the default is moot. Not a bug, just noise.
- **L3 — `MinColumnWidth=50` (page) / `80` (dialog) can still allow a centered number column to clip a 4–5 digit total** at extreme squeeze. Raise min-widths on the specific money columns rather than globally.
- **L4 — Header height (52) + row height (48) is generous.** Fine on large monitors; at small-laptop heights (768 → ~600 px of grid viewport) this shows ~11 rows. A compact row-height variant for dense tables (Operations report, ledger) would show more data, but this is optional polish, per-table.
- **L5 — `NumberSubstitution=European` is set on `MainDataGrid` and the currency converter but not on every ad-hoc `TextBlock`.** Numbers in grids render as Western digits consistently; just confirm any future hand-placed numeric `TextBlock`s (e.g., footer totals) keep the same substitution so digits don't mix Arabic-Indic and Western.

---

## 6. Per-table detailed audit

> Format per table: **Location/Purpose · Current columns (right→left) · Recommended order · Alignment · Width · Desktop adaptive · Readability · Container · Actions · States · Footer/pagination · Print/export**.

### 6.1 — Customers (`CustomersPage.xaml`, grid `CustomersGrid`)
- **Purpose:** Customer directory; double-click → details, context menu → details.
- **Current columns:** `م` (Id, Auto, center) → `اسم العميل` (Name, `*`) → `الهاتف` (Phone, Auto, template w/ LTR phone + hover copy).
- **Recommended order:** Keep `م → اسم العميل → الهاتف`. Identifier first, then name (primary scan target), then contact. Good as-is for an RTL directory.
- **Alignment:** Header center / Name right (correct for Arabic) / Id center (correct) / Phone center+LTR (correct — phone numbers must be LTR). ✔ No change.
- **Width:** `م` Auto is fine; `الهاتف` Auto can jump as numbers vary — pin to a fixed ~150 px so the column edge is stable. `اسم العميل` `*` is correct.
- **Desktop adaptive:** Natural full-width; 3 columns never overflow even at 1040 px. No horizontal scroll needed. Sticky header inherited.
- **Readability:** Phone LTR ✔. Name right ✔. Add `TextTrimming`+tooltip on Name for very long names.
- **Container:** Card fills page; grid `Margin=24,0,24,24`. Fine.
- **Actions:** Context menu + double-click only. No action column → no wasted width. ✔
- **States:** Empty overlay present ("لا يوجد عملاء للعرض"). No loading/error/no-search-result state.
- **Footer/pagination:** None. Add a customer-count footer (M7); pagination not needed.
- **Print/export:** None for this grid.

### 6.2 — Inventory / Products (`InventoryPage.xaml`, grid `ProductsGrid`)
- **Purpose:** Product catalogue & stock; double-click → edit; context → edit/adjust qty/delete; page also prints inventory via a separate print service.
- **Current columns:** `م` (Id,60,center) → `اسم المنتج` (Name,`*`,min160) → `الفئة` (Category,120,center) → `الكمية` (Quantity,90,center) → `سعر البيع` (SellingPrice,120,center).
- **Recommended order:** `م → اسم المنتج → الفئة → الكمية → سعر البيع`. Logical (id, what, classification, how many, price). **Keep.** Optionally move `الكمية` to the far reading-end so low-stock scanning aligns with the warning workflow — but current order is defensible.
- **Alignment:** **`الكمية` and `سعر البيع` should be right-aligned** (H1), not centered, so stock levels and prices compare down the column. `الفئة` center is fine (short tags). Name right ✔.
- **Width:** Good. `اسم المنتج` `*`/min160 ✔. Consider `min` on `سعر البيع` for large prices.
- **Desktop adaptive:** Already sets `HorizontalScrollBarVisibility=Auto` ✔ and `VerticalScrollBarVisibility=Auto`. 5 columns fit comfortably at 1040 px → natural full-width; scroll only on extreme narrowing.
- **Readability:** Numbers Western via grid substitution ✔. Category center fine. Empty cells render clean.
- **Container:** Note the **unused 4th grid row** (M6) — a natural slot for a count/low-stock footer.
- **Actions:** Context menu only; no action column. ✔
- **States:** Empty overlay ✔. No loading/error/no-search state.
- **Footer/pagination:** None (M7). Largest realistic catalogue still virtualizes fine; pagination not required.
- **Print/export:** "🖨️ طباعة الجرد" calls `InventoryPrintService` which builds its **own** columns — **screen column changes do NOT affect the printed inventory sheet.** Safe to restyle screen freely.

### 6.3 — Suppliers (`SuppliersPage.xaml`, grid `SuppliersGrid`)
- **Purpose:** Supplier directory with outstanding debt; double-click → transactions; context → edit/pay/transactions/delete.
- **Current columns:** `م` (Id,60,center) → `اسم المورد` (Name,`2*`) → `الهاتف` (Phone,150,center) → `العنوان` (Address,`*`) → `المديونية` (TotalDebt,150,center,**red bold**).
- **Recommended order:** `م → اسم المورد → الهاتف → المديونية → العنوان`. **Move `المديونية` left of `العنوان`** — debt is the key actionable figure and should sit at the consistent money position (reading-end), while the free-text address (least scannable) goes last. Address as the final `*` column also wraps/truncates more gracefully at the edge.
- **Alignment:** `المديونية` should be **right-aligned** (H1), keep red+bold. Phone center fine. Name/Address right ✔.
- **Width:** `اسم المورد` `2*` vs `العنوان` `*` is a reasonable 2:1 split. Phone fixed 150 ✔. `المديونية` 150 fixed ✔.
- **Desktop adaptive:** 5 columns, two flexible — natural full-width; never overflows. No horizontal scroll needed.
- **Readability:** **`العنوان` needs `TextTrimming`+tooltip** (M5) — addresses are long and currently clip silently. Phone LTR not set here (it is centered but not forced LTR) — addresses/phone OK because Arabic context, but consider LTR for phone consistency with #1.
- **Container/Actions/States:** Card fine; context+double-click, no action column ✔; empty overlay ✔.
- **Footer/pagination:** None (M7). Not needed.
- **Print/export:** None from this grid (supplier statement printing is from the transactions page).

### 6.4 — Invoices (`InvoicesPage.xaml`, grid `InvoicesGrid`)
- **Purpose:** Sales invoice register; double-click → view; context → view/print.
- **Current columns:** `م` (Id,60,min50,center) → `رقم الفاتورة` (InvoiceNumber,150,min120, template LTR + copy) → `التاريخ` (SaleDate `yyyy-MM-dd hh:mm tt`,160,center) → `العميل` (CustomerName,`*`,min160) → `المستخدم` (UserName,120,center) → `الإجمالي` (110,center) → `المدفوع` (110,center) → `المتبقي` (110,center,**orange bold**).
- **Recommended order:** `م → رقم الفاتورة → التاريخ → العميل → الإجمالي → المدفوع → المتبقي → المستخدم`. **Move `المستخدم` to the end** and group the three money columns together immediately after the customer — the financial tri(total/paid/remaining) reads as a unit and `المتبقي` (the action trigger for collections) lands at the reading-end. The operator (`المستخدم`) is audit metadata, least scanned, so it goes last.
- **Alignment:** Make `الإجمالي/المدفوع/المتبقي` **right-aligned** (H1) for column comparison; keep `المتبقي` orange-bold. `م`, `التاريخ`, `المستخدم` center fine. Invoice number LTR center ✔. Customer right ✔.
- **Width:** 8 columns; fixed widths sum ≈ 60+150+160+120+110+110+110 = 820 + customer `*`(min160). At 1040 px → ~220 px for customer. Tight but OK. Money columns 110 fit 6–7 digit totals.
- **Desktop adaptive:** No `HorizontalScrollBarVisibility` set. At 1040 px it just fits; below would clip customer to its 160 min. **Recommend enabling `HorizontalScrollBarVisibility=Auto`** so growth (longer dates, more digits) degrades into scroll, not crush. Sticky header inherited ✔.
- **Readability:** Date-time is long (`hh:mm tt`); fine at 160 px. Customer right + add trim+tooltip. `المتبقي` orange clearly flags balances ✔.
- **Container:** Card fills page; grid `Margin=24,0,24,24`. Fine.
- **Actions:** Context menu (view/print) + double-click. No action column. ✔
- **States:** Empty overlay ✔. No loading/error/no-search state (H4 applies — search filters this grid).
- **Footer/pagination:** None (M7). Will be the **first grid to feel large** as invoices accumulate — recommend a count footer and, longer-term, server-side/date-range paging if registers exceed a few thousand rows (not required now; do not change query logic in this audit).
- **Print/export:** Per-invoice print uses `InvoicePrintService` (own columns). **Screen changes don't affect printed invoices.** Safe.

### 6.5 — Maintenance orders (`MaintenancePage.xaml`, grid `MaintenanceGrid`)
- **Purpose:** Repair-order queue; double-click → open order; toolbar deliver/print/cancel; status filter combo.
- **Current columns (10):** `رقم الطلب` (OrderNumber,100,code LTR center) → `العميل` (`*`,min120, right + tooltip phone + ellipsis) → `الفني` (100, center + tooltip + ellipsis) → `الأجهزة` (DeviceCount,60,center) → `الإجمالي` (90,center) → `المدفوع` (90,center) → `المتبقي` (90,center) → `الحالة` (status badge,100,center) → `تاريخ الاستلام` (IntakeDate,100,center) → `التسليم المتوقع` (ExpectedDelivery,100,center).
- **Recommended order:** `رقم الطلب → الحالة → العميل → الفني → الأجهزة → الإجمالي → المدفوع → المتبقي → تاريخ الاستلام → التسليم المتوقع`. **Move `الحالة` up next to the order number** — status is the primary triage signal in a work queue and currently it's buried between money and dates. Keep money columns grouped; keep the two dates together at the end.
- **Alignment:** Money trio → **right-align** (H1). `الأجهزة` count center fine. Status badge centered ✔. Dates center fine. Order number LTR code ✔. Customer right + tooltip ✔ (good model for other tables).
- **Width:** This is the **widest page grid.** Fixed widths ≈ 100+100+60+90+90+90+100+100+100 = 830 + customer `*`(min120) → ~950 px floor. At 1040 px only ~210 px remain for customer.
- **Desktop adaptive (H5):** **No horizontal scrollbar configured.** At/near minimum width the customer column hits its 120 min and the layout has almost no slack; any extra (longer technician names, 12-h dates) forces hard truncation. **Recommend `HorizontalScrollBarVisibility=Auto`** and consider a **compact row height** at small-laptop widths. On large monitors it's comfortably natural-width. Sticky header inherited ✔.
- **Readability:** `IntakeDate`/`ExpectedDelivery` bind **raw DB strings** (no `StringFormat`) — verify they're already `yyyy-MM-dd`; otherwise normalize *display* (M4). Status badge color-mapped (Success/Danger/Warning/Primary) ✔.
- **Container:** Card; grid `Margin=24,0,24,0` with a real footer row below.
- **Actions:** Toolbar + double-click; no action column → no wasted width ✔.
- **States:** Empty overlay ✔. **`IsLoading` exists in the ViewModel but is not surfaced** in the grid — add a loading overlay (the one place in the app where a loading state is already modeled).
- **Footer/pagination:** **Has** a count + hint footer ✔ (the only grid that does). Good template for M7 elsewhere.
- **Print/export:** Intake/invoice printing via `PrintService.PrintRepairIntake/Invoice` (own columns). Screen-independent. Safe.

### 6.6 — Returns (`ReturnsPage.xaml`, grid `ReturnsGrid`)
- **Purpose:** Returns register; double-click → details; context → details/print.
- **Current columns:** `م` (60,center) → `رقم المرتجع` (150, template LTR + copy) → `التاريخ` (ReturnDate `yyyy-MM-dd hh:mm tt`,180,center) → `رقم الفاتورة` (150, template LTR + copy) → `العميل` (`*`) → `المستخدم` (150,center) → `الإجمالي` (120,center,**red bold**).
- **Recommended order:** `م → رقم المرتجع → رقم الفاتورة → التاريخ → العميل → الإجمالي → المستخدم`. **Put the two reference numbers adjacent** (return # then its source invoice #) — they're cross-referenced together — then date, customer, amount; operator last.
- **Alignment:** `الإجمالي` → **right-align** (H1), keep red bold. Reference numbers LTR center ✔. Date center fine (180 is wide for it — could drop to 160). Customer right ✔.
- **Width:** `التاريخ` 180 is wider than needed; trim to ~150 and give the freed space to `العميل`. Two 150-px reference columns are fine.
- **Desktop adaptive:** 7 columns, one flexible — fits at 1040 px. No scroll needed; natural full-width.
- **Readability:** Red total clearly signals refunds ✔. Empty cells clean.
- **Container/Actions/States:** Card fine; context+double-click; empty overlay ✔; no loading/error/no-search state.
- **Footer/pagination:** None (M7).
- **Print/export:** Return receipt via `PrintService.PrintReturnReceipt` (own columns). Screen-independent. Safe.

### 6.7 — Expenses (`ExpensesPage.xaml`, grid `ExpensesGrid`)
- **Purpose:** Expense log; context → delete. **No double-click handler.**
- **Current columns:** `م` (`[id]`,60,min50,center) → `الوصف` (`[description]`,`2*`) → `الفئة` (`[category]`,120,center) → `المبلغ` (`[amount]`,100,center,**orange bold**) → `طريقة الدفع` (`[payment_method]`,100,center) → `التاريخ` (`[expense_date]`,120,center) → `المستخدم` (`[user_name]`,120,center).
- **Recommended order:** `م → التاريخ → الوصف → الفئة → المبلغ → طريقة الدفع → المستخدم`. **Move `التاريخ` near the front** — expenses are scanned chronologically; description then stays the big flexible column, amount sits with payment method. Operator last.
- **Alignment:** `المبلغ` → **right-align** (H1), keep orange bold. `الفئة`/`طريقة الدفع` center fine (short). Description right ✔.
- **Width:** `الوصف` `2*` dominates ✔. `التاريخ` binds raw `[expense_date]` string — confirm/normalize display (M4). **`الوصف` should get trim+tooltip** (M5) — descriptions are free text and currently clip.
- **Desktop adaptive:** 7 columns, one flexible — fits 1040 px. Natural full-width; no scroll needed.
- **Readability:** Dictionary-indexer bindings (`[amount]` etc.) — confirm `amount` flows through `FlexibleNumberConverter` ✔ (it does). Empty cells clean.
- **Container/Actions/States:** Card fine; **delete-only context menu** (no edit/double-click — intentional?); empty overlay ✔; no other states.
- **Footer/pagination:** None (M7). A "total expenses for filter" footer would be valuable here (display-only sum of the visible column — but **do not** introduce any figure that could be mistaken for a report total; if added, label clearly and keep out of print/export).
- **Print/export:** None from this grid.

### 6.8 — Employees (`EmployeesPage.xaml`, grid `EmployeesGrid`)
- **Purpose:** Employee directory + salary/deduction actions; double-click → edit.
- **Current columns:** `م` (60,center) → `الاسم الكامل` (FullName,`2*`) → `الهاتف` (Phone,140,center) → `المسمى الوظيفي` (JobTitle,150,center) → `الراتب الأساسي` (BaseSalary,130,center) → `الحالة` (IsActive badge,100,center) → `ملاحظات` (Notes,`*`, right + tooltip + ellipsis).
- **Recommended order:** `م → الاسم الكامل → المسمى الوظيفي → الهاتف → الراتب الأساسي → الحالة → ملاحظات`. **Move `المسمى الوظيفي` next to name** (identity block: who + role), phone after, then salary, status, and free-text notes last (already last ✔).
- **Alignment:** `الراتب الأساسي` → **right-align** (H1). Phone center (consider LTR like #1). Job title center fine. Status badge centered ✔. Notes right + tooltip ✔ (good model).
- **Width:** `الاسم الكامل` `2*` vs `ملاحظات` `*` reasonable. Status fixed 100 ✔ (badge stays tight — contrast with #17).
- **Desktop adaptive:** 7 columns, two flexible — fits 1040 px. Natural full-width.
- **Readability:** Active/inactive badge with text+color ✔. Notes trim+tooltip ✔.
- **Container/Actions/States:** Card; toolbar + context + double-click; empty overlay ✔; no loading/error/no-search.
- **Footer/pagination:** None (M7).
- **Print/export:** None from this grid.

### 6.9 — Users (`UsersPage.xaml`, grid `UsersGrid`)
- **Purpose:** System users & permissions; double-click → edit; context → edit/change-password/delete.
- **Current columns:** `م` (`[id]`,60,center) → `اسم المستخدم` (`[username]`,150,center) → `الاسم الكامل` (`[full_name]`,`2*`) → `الموظف` (`[employee_name]`,180,center) → `الصلاحية` (`[role]`,120,center) → `حد الخصم (%)` (`[max_discount_percent]`,120,center).
- **Recommended order:** `م → اسم المستخدم → الاسم الكامل → الموظف → الصلاحية → حد الخصم (%)`. **Keep** — identity (login → real name → linked employee) then authority (role → discount cap). Logical.
- **Alignment:** `حد الخصم (%)` is numeric → **right-align** (H1). `اسم المستخدم` could be LTR (logins are often Latin) — verify; if logins are Arabic keep center. `الصلاحية` center fine. Full name right ✔.
- **Width:** `الاسم الكامل` `2*` dominates ✔; `الموظف` 180 generous — could be 150.
- **Desktop adaptive:** 6 columns, one flexible — fits easily. Natural full-width.
- **Readability:** Percentage column: confirm it shows a `%`/clear unit; header already says "(%)". Empty cells clean.
- **Container/Actions/States:** Header has **no search box** (Users page has only 2 rows: header + grid). Empty overlay ✔. No loading/error.
- **Footer/pagination:** None; user counts are tiny — not needed.
- **Print/export:** None.

### 6.10 — Supplier transactions / ledger (`SupplierTransactionsPage.xaml`, grid `TransactionsGrid`)
- **Purpose:** Per-supplier movement ledger (purchases/payments) with running balance; double-click → details; inline "عرض" button; prints a statement; debt banner above.
- **Current columns (8):** `#` (`[id]`,60,center) → `التاريخ` (`[transaction_date]`,140,center) → `النوع` (`[transaction_type_ar]`,120, right) → `المبلغ` (`[amount]`,120,center) → `المدفوع` (`[paid_amount]`,120,center) → `الرصيد بعد` (`[balance_after]`,120,center) → `بواسطة` (`[created_by]`,`*`) → `التفاصيل` (action button "عرض",100).
- **Recommended order:** `# → التاريخ → النوع → المبلغ → المدفوع → الرصيد بعد → بواسطة` (+ drop the inline action column, see Actions). This is a ledger, so **date → type → debit/credit → running balance** is the canonical read; keep that. `الرصيد بعد` (running balance) is the most important number and should be the last numeric, right-aligned, ideally emphasized.
- **Alignment (critical for a ledger):** `المبلغ / المدفوع / الرصيد بعد` → **right-align** (H1) — a ledger is unreadable with centered figures because the running balance must line up vertically. Consider **negative balance in red** for `الرصيد بعد` (display-only color; **do not change the value or sign logic**). `النوع` right is fine (Arabic label). Date center fine.
- **Width:** Three 120-px money columns + 140 date + 60 id + 100 action + `بواسطة *`. Fits at 1040 px. If the action column is removed, give the space to `الرصيد بعد` or `بواسطة`.
- **Desktop adaptive:** 8 columns — fits. Natural full-width; sticky header lets the user scroll a long statement while keeping headers. Consider compact rows for long statements.
- **Readability:** `transaction_date` is a raw string — confirm/normalize display (M4). Running balance is the scan target — right-align + emphasis materially improves it.
- **Container:** Debt banner (InfoLight) above the grid is a good summary; grid `Margin=24,0,24,24`; footer hint below.
- **Actions (M3):** The `التفاصيل` "عرض" button column duplicates the existing `MouseDoubleClick` → details. On desktop the double-click + a context menu would free ~100 px. If kept, make the button column `Auto`/narrower and right-aligned consistently.
- **States:** Empty overlay ✔ ("لا توجد حركات للعرض"). No loading/error.
- **Footer/pagination:** Hint footer present; no count. Statements are bounded per supplier — pagination not needed.
- **Print/export:** "طباعة كشف حساب" → `SupplierStatementPrintService` builds its **own** statement columns + pulls purchase items separately. **Screen column changes do NOT affect the printed statement.** Safe to restyle/reorder the screen ledger.

### 6.11 — Supplier transaction items (`SupplierTransactionDetailsPage.xaml`, grid `ItemsGrid`)
- **Purpose:** Line items of one purchase transaction; summary tiles above (date/amount/paid/balance/type).
- **Current columns:** `اسم المنتج` (ProductName,`*`) → `الكمية` (Quantity,120,center) → `سعر الشراء` (UnitPurchasePrice,160,center) → `الإجمالي` (LineTotal,160,center).
- **Recommended order:** **Keep** `اسم المنتج → الكمية → سعر الشراء → الإجمالي` (item → qty → unit price → line total is the natural invoice-line reading). 
- **Alignment:** `الكمية / سعر الشراء / الإجمالي` → **right-align** (H1). Product name right ✔.
- **Width:** `سعر الشراء` and `الإجمالي` at 160 are wider than needed for typical prices; 120 each would suffice and give the product column more room. `الكمية` 120 is generous for an integer — 80 is enough.
- **Desktop adaptive:** Only 4 columns — always natural full-width on every desktop size. No scroll concerns.
- **Readability:** Clean. Empty cells fine.
- **Container:** 5-tile `UniformGrid` summary above ✔. Grid `Margin=24,0,24,18`.
- **Actions:** None (read-only detail). ✔
- **States:** Has a **dedicated empty text** (`EmptyItemsText`, toggled in code-behind) — but it is `Grid.Row=3` *below* a `*`-height grid, so the empty message appears under an empty grid rather than centered inside it. Recommend the standard overlay-on-grid pattern for visual consistency with page grids.
- **Footer/pagination:** None needed (few lines).
- **Print/export:** None from this grid (the parent statement print covers it).

### 6.12 — Reports grid (`ReportsPage.xaml` `ReportDataGrid` + `ReportsPage.xaml.cs`)
- **Purpose:** Multi-report surface. Columns are **built at runtime in code-behind** (`OnColumnsChanged`) from `ReportColumn` lists in `ReportsViewModel`. **The code-behind iterates the list in reverse** (`for i = Count-1 … 0`) to lay columns out for RTL, so **on-screen order = reverse of the ViewModel list.** Daily/Monthly *summary* reports show **KPI cards only** (no grid).
- **Variants & current on-screen order (right→left):**
  - **Operations log (Daily/Monthly):** `الموظف → التأثير الصافي → خصم/تسوية → صادر → وارد → طريقة الدفع → التفاصيل → رقم المرجع → نوع العملية → التاريخ`.
  - **Returns report:** `رقم المرتجع → تاريخ → رقم الفاتورة → العميل → القيمة → السبب/المرتجع → الموظف`.
  - **Inventory low-stock:** `كود → المنتج → الكمية → الحد الأدنى → المورد`.
  - **Suppliers debt:** `المورد → الهاتف → المديونية → العنوان`.
- **Recommended order (display):**
  - **Operations:** `التاريخ → نوع العملية → رقم المرجع → التفاصيل → طريقة الدفع → وارد → صادر → خصم/تسوية → التأثير الصافي → الموظف`. A financial log reads date-first; the money columns (in/out/adjustment/net) belong together at the end with `التأثير الصافي` as the emphasized final figure. (Achieve by reordering the ViewModel list — see Print/export caveat.)
  - **Returns:** `رقم المرتجع → رقم الفاتورة → تاريخ → العميل → القيمة → السبب/المرتجع → الموظف` (references adjacent, then date, customer, value, reason, operator).
  - **Inventory low-stock:** `كود → المنتج → الكمية → الحد الأدنى → المورد` — keep, but place `الكمية` and `الحد الأدنى` adjacent (they are — good) for at-a-glance shortfall.
  - **Suppliers debt:** `المورد → الهاتف → المديونية → العنوان` — keep; `المديونية` is right-aligned money.
- **Alignment:** Code already centers "numeric" headers (those containing القيمة/المبلغ/الكمية/إجمالي/صافي/عدد/وارد/صادر/خصم) and right-aligns the rest, with the `العملية` column color-coded by type. Per H1, **switch the numeric set to right-align** for column comparison (especially `وارد/صادر/التأثير الصافي`). **`التأثير الصافي` negative values are not color-flagged** — recommend red for negative net (display-only; do not alter the computed value). Copyable refs (`رقم المرجع` etc.) get the hover-copy template ✔.
- **Width:** Long-text headers (العميل/المورد/البيان/التفاصيل/السبب/اسم/ملاحظات/الوصف/المنتج/العنوان) are given `*`; everything else is `Auto` with `MinWidth=60`. Operations (10 cols) is the widest; copyable columns are fixed 140.
- **Desktop adaptive (H5):** The grid sets `ScrollViewer.HorizontalScrollBarVisibility=Auto` ✔ and `DataGridScrollBehavior.ForwardMouseWheelToParent=True` so wheel scrolling hands off to the page when the grid hits its end — good behavior for the operations log inside the bordered card. The grid is given a `Row="*"` viewport so it **virtualizes and owns its own scroll** (per the in-code comment), which is the right call for the longest table in the app. Natural width on large monitors; horizontal scroll engages gracefully at small-laptop/minimum widths.
- **Readability:** Date-time `yyyy-MM-dd HH:mm` (24-h) here vs 12-h on Invoices/Returns (M4). `Auto` width on numeric columns means money columns size to content — verify large values don't make the column jump between report reloads.
- **Container:** Card with drop shadow; KPI region and operations region are mutually exclusive via `KpiVisibility`/`OperationsVisibility`. Clean separation.
- **Actions:** None inside the grid (report is read-only); export/print are page-level buttons.
- **States:** **No empty-state overlay on the grid.** Inventory report explicitly clears columns when there's no low-stock (good), but the operations/returns/suppliers grids show a blank grid when empty. Recommend an overlay. No loading state during report fetch.
- **Footer/pagination:** None. Operations over a long month could be large; the self-owned virtualized scroll handles it, but a row-count/"showing N operations" line would help.
- **Print/export (IMPORTANT coupling):** Both **CSV export** (`ExportReport`) and **print** (`PrintReport`) read the **same `_currentColumns` list, in forward order** (not reversed), via header/binding-path arrays. So:
  - **Reordering a report's columns on screen means reordering `_currentColumns`, which also reorders the CSV and the printout.** Screen order (reversed) and file/print order (forward) are derived from one source.
  - **Recommendation:** If you change display order, **either** accept that print/CSV order changes too (re-verify both outputs), **or** introduce a separate display-order vs export/print-order so the two can diverge intentionally. **Do not change column headers, binding paths, formats, or the `FlexibleNumber` formatting**, as export/print values and totals depend on them.

### 6.13 — POS product search (`POSPage.xaml`, grid `ProductsGrid`, DialogDataGrid)
- **Purpose:** Live product search to add to cart; double-click → add to cart; context → add.
- **Current columns:** `#` (Id,60,min50,center) → `اسم المنتج` (Name,`*`,min140) → `السعر` (SellingPrice,100,min90,center) → `المخزون` (Quantity,90,min80,center).
- **Recommended order:** **Keep** `# → اسم المنتج → السعر → المخزون`. For POS speed, the name is the scan target and price/stock at the end inform the add decision.
- **Alignment:** `السعر` and `المخزون` → **right-align** (H1) for quick scanning; keep name right. (POS is fast-paced — aligned numbers reduce mis-reads.)
- **Width:** Good. Name `*`/min140 keeps it usable in the narrow left pane.
- **Desktop adaptive:** Lives in the left pane of a 2-column POS layout where the cart pane is 320 px (min 280) and the products pane is `*` (min 480). At minimum window the products pane ≈ 1040−320−16 ≈ 700 px → 4 columns fit. `VerticalScrollBarVisibility=Auto` ✔. No horizontal scroll needed.
- **Readability:** Stock count center→right; price aligned. Clean.
- **Container:** Inside a rounded, opacity-masked border with a "نتائج البحث" label. Good.
- **Actions:** Double-click + context. No action column ✔.
- **States:** **No empty state** (H3) — before searching, the grid is blank. A "ابحث لإظهار المنتجات" hint would help.
- **Footer/pagination:** None needed.
- **Print/export:** None.

### 6.14 — POS cart (`POSPage.xaml`, grid `CartGrid`, DialogDataGrid)
- **Purpose:** Current sale lines; double-click → edit qty; context → edit qty/price/remove.
- **Current columns:** `المنتج` (ProductName,`*`) → `العدد` (Quantity,Auto,center) → `إجمالي` (Total,Auto,center,**bold**).
- **Recommended order:** **Keep** `المنتج → العدد → إجمالي`.
- **Alignment:** `إجمالي` → **right-align** (H1), keep bold; `العدد` → right-align (it's small but consistency matters in a money pane). Product right ✔.
- **Width:** `العدد`/`إجمالي` `Auto` can shift the layout as values change; in a fixed 320-px pane consider small fixed widths (e.g. 50 / 90) so the product column edge is stable.
- **Desktop adaptive:** Fixed-width right pane (320, min 280). The cart never needs horizontal scroll; vertical scroll `Auto` ✔. Pane width adapts via `PosRootGrid_SizeChanged` (code-behind) — confirm cart columns stay readable at the 280 min.
- **Readability:** Big total label below the grid is the headline figure ✔. Line totals bold ✔.
- **Container:** Cart border with header (clear button), grid, totals box, action buttons. Cohesive.
- **Actions:** Double-click + context **and** a 3-button action bar below — slightly redundant but fine for a touch-friendly POS on desktop.
- **States:** **No empty-cart state inside the grid** (H3). An "السلة فارغة" hint would be clearer than a blank grid (note `CartModel.IsEmpty` already exists to bind to).
- **Footer/pagination:** Total box acts as footer ✔. No pagination needed.
- **Print/export:** Checkout prints a receipt via `ReceiptPrintService` (own layout). Screen-independent.

### 6.15 — Invoice view / refund lines (`InvoiceViewDialog.xaml`, editable grid)
- **Purpose:** Show invoice lines and **enter return quantities** (interactive; `IsReadOnly=False`); summary cards (original/returns/net/remaining), payment chips, refund preview, print/refund buttons.
- **Current columns (6):** `#` (Index,50,center,LTR) → `المنتج` (template: ProductName + "تم استرجاع: n" tag,`*`, right, ellipsis) → `الكمية` (Quantity,100,center,LTR) → `سعر الوحدة` (UnitPrice,120,center,LTR) → `الإجمالي` (TotalPrice,120,center,LTR,bold) → `المرتجع` (template: −/qty/+ stepper,160,center).
- **Recommended order:** **Keep** `# → المنتج → الكمية → سعر الوحدة → الإجمالي → المرتجع`. The interactive `المرتجع` stepper rightly sits at the reading-end as the action column.
- **Alignment:** `الكمية / سعر الوحدة / الإجمالي` → **right-align** (H1); these are LTR already (good for digits) — just switch horizontal alignment to right. Stepper centered ✔. Product right + ellipsis ✔.
- **Width:** Stepper column 160 is appropriate for −/box/+. Money columns 120 fine.
- **Desktop adaptive:** Dialog is fixed-chrome (`ResizeMode=NoResize`, 982×732, min 832×600). Within that, 6 columns + `*` product fit comfortably; vertical scroll `Auto` ✔. No horizontal scroll needed at any allowed dialog size.
- **Readability:** "تم استرجاع: n" inline tag on already-returned items is a nice touch ✔. Bold total ✔.
- **Container:** Grid is `Row=3` `*` between summary cards and action row — correct vertical ownership.
- **Actions:** Per-row stepper (inc/dec return qty) + dialog-level print/refund. Clear.
- **States:** **No empty state** for the lines grid (an invoice always has lines, so low risk).
- **Footer/pagination:** Summary cards + refund preview act as footer. No pagination.
- **Print/export:** "طباعة الفاتورة" via print service (own layout). Screen-independent.

### 6.16 — Return details items (`ReturnDetailsDialog.xaml`, grid `ItemsGrid`)
- **Purpose:** Read-only returned items for one return; refund total banner below; print/close.
- **Current columns:** `المنتج` (`[product_name]`,`*`) → `الكمية` (`[quantity]`,80,center,LTR) → `السعر` (`[unit_price]`,100,center,LTR) → `الإجمالي` (`[total_price]`,100,center,LTR,bold).
- **Recommended order:** **Keep** (item → qty → price → total).
- **Alignment:** Numeric trio → **right-align** (H1), already LTR. Product right ✔.
- **Width:** Good for the 682-px dialog. 4 columns + `*`.
- **Desktop adaptive:** Fixed dialog (682×532, `NoResize`). Always fits; no scroll concerns. Sticky header inherited.
- **Readability:** Bold total ✔; refund total banner below is the headline ✔.
- **Container:** Grid `Row=1` `*` with banner `Row=2 Auto` — correct.
- **Actions:** None in-grid; print/close buttons.
- **States:** **No empty state** (low risk — a return always has items).
- **Footer/pagination:** Banner footer ✔.
- **Print/export:** Return print via service (own layout). Screen-independent.

### 6.17 — Customer invoices (`CustomerInvoicesDialog.xaml`, grid `InvoicesGrid`)
- **Purpose:** All invoices for one customer; search + type filter; double-click → details; context → view/print; print-statement button.
- **Current columns (8):** `#` (Id,50,center) → `رقم الفاتورة` (140, template LTR + copy) → `التاريخ` (SaleDate `yyyy/MM/dd`,120) → `النوع` (SaleType,100) → `الإجمالي` (TotalAmount,110,**bold**) → `المدفوع` (PaidAmount,110, green) → `المتبقي` (RemainingAmount,110, orange, semibold) → `الحالة` (status badge,`*`,center).
- **Recommended order:** `# → رقم الفاتورة → التاريخ → النوع → الإجمالي → المدفوع → المتبقي → الحالة`. **Keep, but fix the status width** — `الحالة` as `*` puts a small badge in a huge cell. Make it `Auto`/fixed (≈100) so the money columns and status stay tight (M1).
- **Alignment (M2):** `الإجمالي/المدفوع/المتبقي` here have **no center style → default right alignment.** That's actually the *desired* H1 outcome — but it **disagrees with the full Invoices page (#4) which centers them.** Make the two consistent (recommended: right everywhere). `التاريخ`/`النوع` also fall to default right — fine. Invoice number LTR center ✔. Status badge centered ✔.
- **Width:** Money columns 110 ✔. Status `*` → change (M1). Date `yyyy/MM/dd` at 120 ✔ (note format differs from #4 — M4).
- **Desktop adaptive:** Dialog 932×632 (min 780×520). 8 columns fit; with status fixed, extra width is dead space on a wide dialog — acceptable, or let the invoice-number/customer area breathe.
- **Readability:** Green paid / orange remaining color cues ✔. Status badge color-mapped via converters ✔.
- **Container:** Header band + search/filter row + grid + footer band. Clean.
- **Actions:** Context (view/print) + double-click + footer statement print.
- **States:** **Has an empty state** via `IsEmpty` → `EmptyStateText` ✔ (good — a dialog grid that does it right). No loading/error.
- **Footer/pagination:** Header shows a sales-count text; footer has print-statement. No pagination needed.
- **Print/export:** "طباعة كشف حساب" via service (own layout). Screen-independent.

### 6.18 — Repair parts (`RepairPartsDialog.xaml`, parts grid)
- **Purpose:** Parts used on a repair device; add-part form above; delete-selected + running total footer.
- **Current columns:** `القطعة` (PartName,`*`) → `الكمية` (Quantity,65) → `سعر الوحدة` (UnitCost,110) → `الإجمالي` (TotalCost,110) → `المصدر` (SourceLabel,90).
- **Recommended order:** **Keep** `القطعة → الكمية → سعر الوحدة → الإجمالي → المصدر`.
- **Alignment (H2):** **No element styles at all** → every column (including the three numeric ones) uses the default **right** alignment. So numbers here are right-aligned while the same numbers are centered in page grids — internally inconsistent. Standardize: numbers right (recommended), short labels (`المصدر`) optionally center. Decide one rule app-wide.
- **Width:** `الكمية` 65 is tight but ok for small counts; money 110 ✔; `المصدر` 90 ✔. `القطعة` `*` ✔.
- **Desktop adaptive:** Resizable dialog (912×712, **CanResize**, min 792×592). 5 columns always fit; grows the `القطعة` column. No horizontal scroll needed. Sticky header inherited.
- **Readability:** Footer "إجمالي القطع" with bold value + "ج.م" ✔. Clean.
- **Container:** Grid in a bordered card with a footer bar (delete button + total). Good vertical ownership.
- **Actions:** Delete via footer button (uses `SelectedPart`); add via form. No per-row action column — fine.
- **States:** **No empty state** (H3). New devices start with zero parts → blank grid. Add an overlay ("لا توجد قطع مضافة").
- **Footer/pagination:** Total footer ✔.
- **Print/export:** Parts appear on the repair invoice via print service (own layout). Screen-independent.

### 6.19 — Repair order · Devices tab (`RepairOrderDialog.xaml`)
- **Purpose:** Devices on a repair order; toolbar add/parts/edit/remove; double-click not bound.
- **Current columns:** `النوع` (DeviceType,80) → `الجهاز` (DisplayName,`*`) → `المشكلة` (ReportedIssue,`*`) → `الحالة` (DeviceStatusAr,100) → `أجر العمل` (LaborCost,90).
- **Recommended order:** `النوع → الجهاز → المشكلة → الحالة → أجر العمل`. **Keep**, but consider `الحالة` right after `الجهاز` (status is a triage signal) — optional.
- **Alignment (H2):** **No element styles** → all default right, including `أجر العمل` (money, right — fine) and `الحالة` (right — but a status reads better centered, and other tables center status badges). `المشكلة` is long free text → **add trim+tooltip** (M5).
- **Width:** Two `*` columns (`الجهاز`, `المشكلة`) split the flexible space 1:1; on a wide dialog the issue text gets room ✔. `النوع` 80 tight for an Arabic device type — verify it doesn't clip.
- **Desktop adaptive:** Resizable dialog (892×732, min 832×632). 5 columns fit; both `*` columns absorb width. No horizontal scroll needed.
- **Readability:** `الحالة` is plain text (not a colored badge) here, unlike Maintenance page (#5) and Employees (#8). For consistency, consider the same status-badge treatment (display-only).
- **Container:** Grid `Row=1` `*` between toolbar and hint text. Correct.
- **Actions:** Toolbar (add/parts/edit/remove) operating on `SelectedDevice`. No action column ✔.
- **States:** **No empty state** (H3) — a new order's Devices tab is empty until saved (there's a hint *below* the grid, but not an in-grid empty overlay). Add overlay.
- **Footer/pagination:** Hint line; no count. Not needed.
- **Print/export:** Devices print on intake/invoice via service (own layout). Screen-independent.

### 6.20 — Repair order · Payments tab (`RepairOrderDialog.xaml`)
- **Purpose:** Payments recorded against a repair order; add-payment form above; totals (total/paid/remaining) below.
- **Current columns:** `مبلغ التحصيل` (Amount,120) → `طريقة الدفع` (PaymentMethod,120) → `التاريخ` (PaymentDate,140) → `ملاحظات` (Notes,`*`).
- **Recommended order:** `التاريخ → مبلغ التحصيل → طريقة الدفع → ملاحظات`. **Move date first** — payments are chronological; then amount, method, notes. (Minor.)
- **Alignment (H2):** **No element styles** → all default right. `مبلغ التحصيل` (money) right — fine and actually the H1 target. `التاريخ` right — acceptable. `ملاحظات` long text right ✔ (add trim+tooltip, M5).
- **Width:** `ملاحظات` `*` ✔. Date 140 fine (verify display format, M4).
- **Desktop adaptive:** Same resizable dialog. 4 columns always fit; notes absorbs width. No scroll concerns.
- **Readability:** Totals box (total/paid/remaining, color-coded) below acts as a clear footer ✔.
- **Container:** Grid `Row=1` `*` between add-form and totals. Correct.
- **Actions:** Add via form; no per-row delete (intentional — payments are records). No action column.
- **States:** **No empty state** (H3) — new orders have no payments. Add overlay.
- **Footer/pagination:** Totals footer ✔.
- **Print/export:** Payments print on the repair invoice via service. Screen-independent.

### 6.21 — Supplier purchase lines (`SupplierPurchaseDialog.xaml`, grid `Lines`)
- **Purpose:** Build a purchase's line items before saving; add-line form above; totals + delete/import/AI-prompt actions below.
- **Current columns (all `DataGridTemplateColumn` with `DataGridTextCenter`):** `اسم المنتج` (ProductName,`2*`, center) → `الكمية` (Quantity,90, center) → `سعر الشراء` (UnitPurchasePrice,120, center) → `الإجمالي` (LineTotal,120, center).
- **Recommended order:** **Keep** (item → qty → unit price → total).
- **Alignment:** All four are explicitly centered via `DataGridTextCenter` in templates. Per H1, **right-align the three numeric columns** (`الكمية/سعر الشراء/الإجمالي`); product name is centered here (unusual — most name columns are right) — consider right for the name to match other item grids.
- **Width:** `اسم المنتج` `2*` dominates ✔; money 120 / qty 90 fine.
- **Desktop adaptive:** Fixed dialog (1012×812, `NoResize`, min 820×560). 4 columns always fit; `2*` name absorbs width. No scroll concerns. Sticky header inherited.
- **Readability:** Numbers via `FlexibleNumberConverter` ✔. Clean.
- **Container:** Grid `Row=3` `*` between the add-line card and the totals row — correct vertical ownership.
- **Actions:** Delete-line via footer button (uses `SelectedLine`); no per-row action column. Import CSV/XLSX and AI-prompt are footer actions.
- **States:** **No empty state** (H3) — a new purchase always starts empty; the grid is blank until the first line is added. **Highest-value empty-state addition in the app** (the very first thing the user sees). Add overlay ("أضف أسطر المشتريات").
- **Footer/pagination:** Totals row (purchase value / paid / method + invoice total) ✔.
- **Print/export:** Has an **import** path (CSV/XLSX → lines) but **no export of this grid**; saving persists via service. Import does not depend on screen column order. Safe.

---

## 7. Recommended per-table column order (consolidated)

> Listed in on-screen RTL reading order (right→left). "Keep" = current order is already optimal.

| Table | Recommended order (right→left) | Rationale |
|-------|-------------------------------|-----------|
| Customers (#1) | Keep: `م · الاسم · الهاتف` | id → name → contact |
| Inventory (#2) | Keep: `م · الاسم · الفئة · الكمية · السعر` | id → what → class → qty → price |
| Suppliers (#3) | `م · الاسم · الهاتف · المديونية · العنوان` | move debt before address; address (free text) last |
| Invoices (#4) | `م · رقم · التاريخ · العميل · الإجمالي · المدفوع · المتبقي · المستخدم` | group money trio; operator last |
| Maintenance (#5) | `رقم · الحالة · العميل · الفني · الأجهزة · الإجمالي · المدفوع · المتبقي · الاستلام · التسليم` | status up for triage; money & dates grouped |
| Returns (#6) | `م · رقم المرتجع · رقم الفاتورة · التاريخ · العميل · الإجمالي · المستخدم` | adjacent reference numbers |
| Expenses (#7) | `م · التاريخ · الوصف · الفئة · المبلغ · طريقة الدفع · المستخدم` | date forward; desc flexible |
| Employees (#8) | `م · الاسم · المسمى · الهاتف · الراتب · الحالة · ملاحظات` | identity block (name+role) together |
| Users (#9) | Keep: `م · اسم المستخدم · الاسم · الموظف · الصلاحية · حد الخصم` | identity → authority |
| Supplier ledger (#10) | `# · التاريخ · النوع · المبلغ · المدفوع · الرصيد بعد · بواسطة` (+drop action col) | canonical ledger read |
| Txn items (#11) | Keep: `المنتج · الكمية · سعر الشراء · الإجمالي` | invoice-line read |
| Reports/Operations (#12) | `التاريخ · نوع · مرجع · تفاصيل · طريقة · وارد · صادر · خصم · صافي · الموظف` | date-first log; money grouped, net last |
| Reports/Returns (#12) | `رقم المرتجع · رقم الفاتورة · تاريخ · العميل · القيمة · السبب · الموظف` | references adjacent |
| Reports/Inventory (#12) | Keep: `كود · المنتج · الكمية · الحد الأدنى · المورد` | shortfall pair adjacent |
| Reports/Suppliers (#12) | Keep: `المورد · الهاتف · المديونية · العنوان` | debt is the figure |
| POS products (#13) | Keep: `# · الاسم · السعر · المخزون` | name-first for speed |
| POS cart (#14) | Keep: `المنتج · العدد · إجمالي` | line read |
| Refund lines (#15) | Keep: `# · المنتج · الكمية · سعر · الإجمالي · المرتجع` | action stepper at end |
| Return items (#16) | Keep: `المنتج · الكمية · السعر · الإجمالي` | line read |
| Customer invoices (#17) | Keep order; fix `الحالة` width | status badge tight |
| Repair parts (#18) | Keep: `القطعة · الكمية · سعر · الإجمالي · المصدر` | part-line read |
| Repair devices (#19) | Keep (or status after device) | device-first |
| Repair payments (#20) | `التاريخ · مبلغ · طريقة · ملاحظات` | date forward |
| Purchase lines (#21) | Keep: `المنتج · الكمية · سعر الشراء · الإجمالي` | line read |

> **Reports caveat:** changing #12 display order means editing `_currentColumns` in `ReportsViewModel`, which also re-orders CSV/print. See §6.12.

---

## 8. Recommended per-table width strategy

| Table | Strategy |
|-------|----------|
| Customers (#1) | `م` Auto→fixed 60; Name `*` (wrap-off, trim+tooltip); **Phone fixed 150** (stop Auto jitter). |
| Inventory (#2) | Keep fixed numerics; Name `*`/min160; add `min` to السعر; use the empty row for a footer. |
| Suppliers (#3) | Name `2*` / Address `*` (trim+tooltip); Phone 150; **المديونية 150 right-align**. |
| Invoices (#4) | Money cols fixed 110 (right-align, min ~90); Customer `*`/min160 trim+tooltip; **enable horizontal scroll**. |
| Maintenance (#5) | Tighten by right-aligning money (90 each); Customer `*`/min120 trim+tooltip; **enable horizontal scroll + compact rows at small widths**. |
| Returns (#6) | Trim `التاريخ` 180→150; reference cols 150; Customer `*`; Total 120 right-align. |
| Expenses (#7) | Desc `2*` trim+tooltip; numerics fixed; Amount 100 right-align. |
| Employees (#8) | Name `2*` / Notes `*` (trim+tooltip ✔); Salary 130 right-align; Status fixed 100. |
| Users (#9) | Name `2*`; الموظف 180→150; حد الخصم right-align. |
| Supplier ledger (#10) | Three money cols fixed 120 **right-aligned**; `الرصيد بعد` emphasized; `بواسطة` `*`; drop/narrow action col. |
| Txn items (#11) | Shrink سعر/الإجمالي 160→120, الكمية 120→80; give slack to المنتج `*`. |
| Reports (#12) | Keep `Auto`+`*` scheme; numerics right-aligned; copyable cols fixed 140; rely on horizontal scroll (already on). |
| POS products (#13) | Keep; name `*`/min140; price/stock right-align fixed. |
| POS cart (#14) | Convert العدد/إجمالي `Auto`→fixed (≈50/90) for a stable edge; product `*`. |
| Refund lines (#15) | Keep; stepper 160; money 120 right-align. |
| Return items (#16) | Keep; numerics right-align. |
| Customer invoices (#17) | **Status `*`→Auto/100**; money 110 right-align. |
| Repair parts (#18) | Add numeric right-align styles; الكمية 65 (verify), money 110, المصدر 90, القطعة `*`. |
| Repair devices (#19) | Two `*` cols ok; المشكلة trim+tooltip; verify النوع 80 doesn't clip. |
| Repair payments (#20) | ملاحظات `*` trim+tooltip; money/method 120, date 140. |
| Purchase lines (#21) | Name `2*`; numerics 90/120/120 **right-align**. |

**Global width principles applied above:** never widen beyond content need; never force equal widths; one flexible (`*`) column per table for free text; fixed widths for ids/dates/money/status; `min` widths only on money columns that can hold 5–6 digits.

---

## 9. Recommended desktop adaptive strategy (per table)

> No mobile/tablet/card layouts. "Adapt" = behave correctly from the **1040 px page minimum** up to large external monitors.

| Table | Large monitor | Normal desktop | Large laptop | Small laptop | Min window (1040px) |
|-------|---------------|----------------|--------------|--------------|---------------------|
| #1,#2,#3,#7,#8,#9,#11,#16 (≤7 cols, ≤1 flex) | Natural full-width | Natural | Natural | Natural | Fits; sticky header; no h-scroll |
| #4 Invoices (8 cols) | Natural | Natural | Natural | Tight | **Enable h-scroll**; customer trims |
| #5 Maintenance (10 cols) | Natural | Natural | Slightly tight | h-scroll likely | **h-scroll + compact rows**; status pinned-ish |
| #6 Returns (7 cols) | Natural | Natural | Natural | Natural | Fits |
| #10 Ledger (8 cols) | Natural | Natural | Natural | Tight | Fits; right-align balance critical; compact rows for long statements |
| #12 Operations (10 cols) | Natural | Natural | Tight | h-scroll | **h-scroll already on** + self-owned virtualized scroll ✔ |
| #12 other report variants | Natural | Natural | Natural | Natural | Fits |
| #13,#14 POS grids | Natural in panes | Natural | Natural | Panes shrink (cart min 280, products min 480) | Fits; v-scroll only |
| #15,#16,#17,#18,#19,#20,#21 dialog grids | Fixed/resizable dialog chrome bounds them | — | — | Dialogs have own min sizes | Always fit; v-scroll only |

**Cross-cutting adaptive recommendations:**
- **Sticky header:** already inherent in `MainDataGrid`/`DialogDataGrid` for *all* grids — keep; no work needed.
- **Sticky action column:** only #10 has an inline action column; if kept, it could use `FrozenColumnCount` so "عرض" stays visible during h-scroll. Otherwise N/A.
- **Compact spacing at small widths:** add an optional compact variant (row 48→36, header 52→40) for the dense grids (#5, #10, #12-operations) to show more rows on 768-px-tall laptops. Per-table opt-in, **not** global.
- **Text truncation + tooltip:** apply specifically to long-text columns (§M5 list).
- **Horizontal scroll only when needed:** enable `HorizontalScrollBarVisibility=Auto` on #4 and #5 (the two page grids that can overflow); leave it off where columns always fit.

---

## 10. Shared / global improvements that are safe

These touch shared resources/components and benefit every table without per-table layout decisions:

1. **Add a shared numeric element style** (e.g. `DataGridTextNumeric`: right-align, vertical center, fixed padding, European digits) in `DataGrid.xaml`, then swap money/quantity columns from `DataGridTextCenter` to it **per column**. Defining it once is global; applying it is per-column (so it stays a deliberate choice). Resolves H1/H2 consistently.
2. **Promote the empty-state overlay into a reusable attached behavior or `UserControl`** so dialog grids (#15,#16,#18,#19,#20,#21,#13,#14) can opt in with one line, exactly like the page grids already do. Resolves H3.
3. **Add a shared loading-overlay style** bound to an `IsLoading`/`IsBusy` flag; wire it first to `MaintenanceViewModel.IsLoading` (already exists). Standardizes the missing loading state.
4. **Extract the copyable-cell template** (value + hover copy button) into one `DataTemplate` resource and reuse it in #1,#4,#6,#17 and `ReportsPage.xaml.cs` (replacing the string-built XAML). Pure refactor (L1).
5. **Standardize display date formatting** via a couple of shared converters/`StringFormat` constants (date vs date-time) and apply per column. **Display only — never touch stored values or print/export formatting.** Addresses M4.
6. **Keep the sticky-header template and `ModernScrollBar` as-is** — they already work app-wide; just ensure new grids use `MainDataGrid`/`DialogDataGrid`.
7. **Confirm `HorizontalScrollBarVisibility=Auto` is the default expectation** for any grid whose summed min-widths can exceed 1040 px (currently only #4, #5 need it added).

---

## 11. Table-specific improvements that must NOT be globalized

- **Column order** — every table's order is purpose-driven (ledger vs directory vs work-queue vs invoice-lines). Do not apply one ordering rule app-wide (§7).
- **Which column is flexible (`*`)** and which wrap/truncate — depends on each table's free-text field (address vs notes vs issue vs details).
- **Status badge styling & color mapping** — Maintenance (4-state), Employees (active/inactive), Customer invoices (paid/partial/unpaid) each have distinct semantics. Keep per-table.
- **Negative-value coloring** — meaningful for ledger `الرصيد بعد` (#10) and Operations `التأثير الصافي` (#12); not for, say, quantities. Apply per column, display-only.
- **Inline action column** (#10 "عرض") and **per-row stepper** (#15 المرتجع) — bespoke interactions; do not standardize away or replicate.
- **`الحالة` width fix** (#17) and **POS cart fixed numeric widths** (#14) — local layout tuning.
- **Reports column list** (#12) — order changes are coupled to CSV/print; treat as a special, isolated change (not part of any global column pass).
- **Compact row height** — opt-in only for dense tables (#5, #10, #12), not the directories.

---

## 12. Risks & files likely to be affected (for the future implementation phase)

**Shared/global (high blast radius — change carefully, re-test every grid):**
- `Presentation/Resources/DataGrid.xaml` — `MainDataGrid`, `DialogDataGrid`, `DataGridHeaderStyle`, `DataGridCellStyle`, element styles, control template. Any change here touches **all 21 grids**.
- `Presentation/Converters/FlexibleNumberConverter.cs` / `InverseBooleanToVisibilityConverter.cs` — used widely; do not alter conversion math, only consume.
- `Presentation/Behaviors/DataGridScrollBehavior.cs` — only used by Reports today; safe to extend.

**Per-view (low blast radius):**
- Each page/dialog `.xaml` listed in §2 — column `Width`, `ElementStyle`, `HorizontalScrollBarVisibility`, empty/loading overlays.
- `Presentation/Views/ReportsPage.xaml.cs` + `Presentation/ViewModels/ReportsViewModel.cs` — **highest-risk file pair**: column order, alignment, *and* CSV/print column identity all live here and are coupled. Touching `_currentColumns` affects screen + export + print simultaneously.

**Explicitly out of scope / must not change (per task constraints):**
- `Infrastructure/Printing/*` (InvoicePrintService, ReceiptDocumentBuilder, InventoryPrintService, SupplierStatementPrintService, ReportPrintService, A4PrintBase) — printed layouts are **independent** of screen grids (except the report header/column arrays passed from `ReportsViewModel`). Do not change print column math/totals.
- Any business logic, command, DB query, report totals, KPI calculations, export value formatting.

**Behavioral risks to watch when implementing:**
- Switching amount columns center→right may expose previously-hidden differences in decimal counts; verify `FlexibleNumberConverter` decimals are consistent per column (don't change the converter).
- Enabling horizontal scroll on #4/#5 changes the visual when the window is dragged narrow — verify the sticky header + thin scrollbar still render inside the rounded-corner clip (`OpacityMask`/`CornerRadius` in the template).
- Adding empty/loading overlays inside grids that sit in a `*` row: ensure the overlay is a sibling in the same parent `Grid` (the established pattern), not inside the `DataGrid`, so it centers correctly.
- Reordering Reports columns: **re-verify both the CSV file and the printout** column order/headers after any change.

---

## 13. Verification checklist (for the later implementation phase)

**Per-table layout**
- [ ] Each grid's columns appear in the recommended RTL order; no column unintentionally clipped at 1040 px page width.
- [ ] Money/quantity/balance columns are right-aligned and digits line up vertically (#3,#4,#5,#6,#7,#8,#10,#11,#12,#13,#14,#15,#16,#17,#18,#21).
- [ ] One flexible (`*`) free-text column per table; long-text columns (§M5) show ellipsis + tooltip and never break row height.
- [ ] Status badges are fixed/`Auto` width and centered (esp. #17 status no longer `*`).
- [ ] POS cart numeric columns use fixed widths (stable edge); ledger running balance emphasized.

**States**
- [ ] Every grid shows a clear empty state, including all dialog grids (#13,#14,#15,#16,#18,#19,#20,#21) — highest priority: #21 purchase lines, #19/#20 repair tabs, #18 parts.
- [ ] "No search results" message is distinct from "no data" on searchable list pages (#1–#8).
- [ ] Loading overlay appears during data fetch where modeled (start with #5 Maintenance `IsLoading`).
- [ ] No grid shows a raw blank white area in any normal flow.

**Desktop adaptive**
- [ ] Window resized from 2560 px down to 1280 px: headers stay sticky; no grid traps the mouse wheel (parent scroll handoff works on the Reports operations grid).
- [ ] #4 Invoices and #5 Maintenance engage horizontal scroll (not column crush) when narrowed; #12 Operations already scrolls.
- [ ] Dialog grids remain fully usable at each dialog's `MinWidth/MinHeight`.
- [ ] Optional compact row height (if added) only affects #5/#10/#12 and is legible at 768-px tall laptops.

**Dates / numbers / RTL**
- [ ] Date display is consistent per data type (date vs date-time); raw DB-string date columns (#5,#7,#10,#20) render cleanly; **stored values unchanged**.
- [ ] Western digits everywhere in grids (no Arabic-Indic/Western mixing).
- [ ] Phone/invoice/reference/code columns remain LTR; Arabic text columns remain right-aligned.
- [ ] Negative balances/net effects are color-flagged where meaningful (#10 `الرصيد بعد`, #12 `التأثير الصافي`) **without changing the value or sign**.

**Print / export integrity (must remain unchanged)**
- [ ] Inventory print, invoice/receipt print, return print, supplier statement print, repair intake/invoice print produce **identical** output to before (they're screen-independent).
- [ ] Reports CSV export column order/headers verified after any Reports screen reorder.
- [ ] Reports print column order/headers verified after any Reports screen reorder.
- [ ] No report totals, KPI values, or exported figures changed.

**Shared components**
- [ ] Numeric element style defined once and applied per money column (not blanket-applied to text).
- [ ] Copyable-cell template extracted and reused (no duplicated inline copies / no string-built XAML in code-behind).
- [ ] Empty/loading overlay is a reusable pattern consumed identically across page and dialog grids.
- [ ] All grids still resolve `MainDataGrid`/`DialogDataGrid` (no accidental style breakage from `DataGrid.xaml` edits).

---

*End of audit. No source files were modified.*
