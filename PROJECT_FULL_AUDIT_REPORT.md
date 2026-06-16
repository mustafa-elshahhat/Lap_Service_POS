# Project Full Audit Report

**Repository:** AlJohary Service Hub (POS + repair/maintenance, WPF desktop)
**Audited path:** `D:\projects\Lap_Service_POS`
**Branch / HEAD:** `main` @ `68807d3` (clean working tree)
**Date:** 2026-06-16
**Method:** Read-only evidence scan → safe build/test → manual confirmation of every cited file. No files in the repo were modified, deleted, renamed, or formatted. This report is the only file created.

---

## 1. Executive Summary

This is a **well-architected, carefully-reasoned financial desktop application**. It uses clean layered separation (Domain / Application / Infrastructure / Presentation / Core / Shared), enforces business invariants in the service layer (not just the UI), stores all money as `decimal`, wraps multi-statement money mutations in explicit transactions, and ships a genuinely good test suite that includes a raw-SQL reconciliation of the daily cash-flow figure. Build is **0 warnings / 0 errors**; tests are **67/67 passing**.

There are **no confirmed money-loss or data-corruption bugs**. Every cash inflow/outflow path records a transaction row (`sale_payments`, `repair_payments`, `supplier_transactions`, `employee_salary_transactions`, `expenses`, `returns.cash_refund`). The risks that remain are *latent* (a control gap that is unreachable through the current UI), *operational* (global singletons / process-wide transaction state), or *reporting blind spots* (per-method KPI buckets), plus the expected polish items (dead code, hardcoded report dates, stubbed export).

**Biggest risks (top 10 fixes):**

1. Process-global DB connection + `CurrentTransaction` singleton is not concurrency-safe (`Infrastructure/Data/DatabaseManager.cs:16,133`).
2. Default seeded admin password `admin123`, never force-changed (`Infrastructure/Data/DatabaseManager.cs:456`, `README.md:60`).
3. Invoice-level `discountAmount`/`markupAmount` bypass `PriceLimitValidator` and the below-cost floor — latent, not reachable today (`Application/Services/SaleService.cs:75`).
4. Per-method KPI cards silently drop money booked under any non-canonical/empty `payment_method` (`Infrastructure/Persistence/ReportRepository.cs:434`, `Presentation/ViewModels/ReportsViewModel.cs:201`).
5. Migrations run under a swallow-all catch — a partial migration is logged but the app continues (`Presentation/AppBootstrapper.cs:115`).
6. CI builds but never runs the test suite (`.github/workflows/build.yml:25`).
7. `DatabaseManager.Restore` overwrites the live DB with no validation of the backup file (`Infrastructure/Data/DatabaseManager.cs:73`).
8. Reports are hardwired to "today"/current month — no date-range selection (`Presentation/ViewModels/ReportsViewModel.cs:167,176`).
9. Dead/obsolete code to prune (verify first): `GetOperationsReport`, `AddSupplierPurchase`, `GetUnpaidByCustomer`, `UpdateSaleFinancials`, `net_cash_flow` output, `payment_details`.
10. `EnsureColumnExists` swallows ALTER failures silently (`Infrastructure/Data/DatabaseManager.cs:401`).

**Production-ready?** **Yes, with caveats** — this is shippable for its intended single-user / single-till desktop deployment. Before wider rollout, address items 1, 2, 5, and 7 above. None of these are blocking for a single-operator shop.

---

## 2. Stack Detected

| Layer | Technology |
|---|---|
| Language | C# (192 `.cs` files) |
| UI | WPF (45 `.xaml`), MVVM-style, RTL/Arabic |
| Runtime | .NET 10.0 (`net10.0-windows`, `win-x64`) |
| Database | SQLite via `Microsoft.Data.Sqlite` |
| Tests | xUnit (`Tests/AlJohary.ServiceHub.Tests.csproj`, 67 tests) |
| Build/CI | GitHub Actions, `.github/workflows/build.yml` (build only) |
| Architecture | Domain / Application / Core / Infrastructure / Presentation / Shared |

**Build / test / lint scripts:** No lint config present. Build = `dotnet build AlJohary.ServiceHub.sln`. Test = `dotnet test Tests\AlJohary.ServiceHub.Tests.csproj`.

---

## 3. Commands Run

| Command | Result | Notes |
|---|---|---|
| `audit-scan.ps1 -Path … -Cap 8` | ✅ passed | 247 files, 245 text; 16 sections reviewed; capped categories re-checked via Grep |
| `git status` / `git log` | ✅ passed | Clean tree, linear history |
| `dotnet build AlJohary.ServiceHub.sln -c Debug` | ✅ passed | **0 Warning(s), 0 Error(s)** |
| `dotnet test Tests\AlJohary.ServiceHub.Tests.csproj --tl:off` | ✅ passed | **Passed! Failed: 0, Passed: 67, Skipped: 0** (1 s) |
| `npm` / `pip` / lint | ⏭ skipped | Not a Node/Python project; no lint tooling configured |

---

## 4. Critical Issues

**None confirmed.** No code path was found that loses money, corrupts data, or silently mis-states a report total under the current (UI-reachable) usage. The items below are graded High and down.

---

## 5. High Priority Issues

### H-1 · Architecture/Concurrency · `Infrastructure/Data/DatabaseManager.cs:16,124,133`
**Evidence:** A single process-wide `SqliteConnection _connection` and a single `public SqliteTransaction CurrentTransaction { get; set; }` are shared by every repository (all use `DatabaseManager.Instance`). `BeginTransaction()` throws if one is already active (`:135`), and every `Execute/FetchOne/FetchAll` auto-enlists `CurrentTransaction` (`:177,222,255,287`).
**Why it's a problem:** Correct only while all DB access happens serially on one thread. Any background work (a search, a print job, a `TypedMessenger`-triggered report refresh — e.g. `ReportsViewModel.cs:65` marshals to the Dispatcher precisely because of this) that touches the DB while a sale/return transaction is open will either block on `_lock` or silently run inside the other operation's transaction. For a single-user till this is acceptable; it becomes a real hazard the moment two flows overlap.
**Fix:** Prefer a connection-per-unit-of-work (or scope the transaction to the connection passed explicitly), or document and enforce single-threaded DB access. At minimum, guard against re-entrant `BeginTransaction` from a different logical operation.
**Effort:** Large (architectural) or Small (document + assert) depending on appetite.

### H-2 · Security · `Infrastructure/Data/DatabaseManager.cs:456`, `README.md:60`
**Evidence:** `string passwordHash = Security.HashPassword("admin123");` seeds the first admin with `admin`/`admin123`, and the README publishes it. Nothing forces a change on first login (`Presentation/Views/LoginWindow.xaml.cs` performs a plain `Login`).
**Why it's a problem:** A known default credential on a financial app. Hashing is strong (PBKDF2-SHA256, 100k iters, random salt, fixed-time compare — `Shared/Helpers/Utilities.cs:17-66`), so the risk is purely the *default*, not the storage.
**Fix:** Force a password change on first admin login (flag in `settings`), or generate a random initial password shown once at setup.
**Effort:** Small.

### H-3 · Financial control (latent) · `Application/Services/SaleService.cs:75,84-93`
**Evidence:** `CalculateTotalWithDiscountAndMarkup(subtotal, discountAmount, markupAmount)` applies an **invoice-level** discount/markup that is never run through `PriceLimitValidator` (which only validates each item's `UnitFinalPrice` at `:264`). The below-cost floor is likewise per-item only.
**Why it's a problem:** A large invoice-level `discountAmount` could push the invoice total below total cost (negative profit) and would let a capped employee exceed their discount ceiling — *without* tripping any validator. **Not exploitable today:** the only production caller, `CheckoutCash`, passes `0, 0` (`Presentation/ViewModels/POSViewModel.cs:363`); the multi-payment `CreateSale` overload is invoked only by tests (`Tests/FinancialFlowTests.cs:84`). It is a defense-in-depth gap that becomes a live bug the day an invoice-level discount field is wired into the UI.
**Fix:** Validate the post-discount invoice total against summed cost (and the actor's ceiling) inside `CreateSaleInternal`, or remove the unused invoice-level discount/markup parameters until a guarded UI needs them.
**Effort:** Small.

### H-4 · Reporting blind spot · `Infrastructure/Persistence/ReportRepository.cs:434-477` + `Presentation/ViewModels/ReportsViewModel.cs:201-206,389-393`
**Evidence:** `AddPaymentBreakdowns` groups `payment_inflows`/`payment_outflows` by raw `payment_method` with **no** `COALESCE(... ,'غير محدد')`, while the sibling `GetFinancialOperations` (`:331,347,361,375,390,420`) *does* coalesce NULL/empty to `'غير محدد'`. The KPI cards then sum only the three canonical methods via `GetMethodSum(..., PaymentMethods.Cash/InstaPay/EWallet)`.
**Why it's a problem:** Any row whose `payment_method` is NULL/empty or a non-canonical string is bucketed under `""`/that string and is **invisible** in the per-method KPI cards (صافي النقدية / إنستا باي / محفظة). The three per-method nets then won't sum to the computed `net_cash_flow`. The in-code claim that the breakdowns "reconcile by construction" with the operations log (`:312-320`) is therefore not strictly true. **Low likelihood today** because the UI constrains methods to the three canonical values (`CashSaleViewModel.cs:54` → `PaymentMethods.GetAll()`), and repos default to `'نقدي'` (`SupplierRepository.cs:120`) — but supplier/expense paths can still pass through whatever string they're given.
**Fix:** Coalesce NULL/empty to `'غير محدد'` in `AddPaymentBreakdowns` exactly as `GetFinancialOperations` does, and add an "other methods" bucket/card so no inflow/outflow is dropped from the per-method view.
**Effort:** Small.

---

## 6. Medium Priority Issues

### M-1 · Migrations swallow failures · `Presentation/AppBootstrapper.cs:103-118`
The ten migrations run inside `try { … } catch (Exception migrationEx) { Logger.LogException(...); }` — a failed/partial migration is logged but startup continues, potentially against an inconsistent schema. (Migration005's `catch { }` around `DROP COLUMN` at `Migration005…:21` is intentional and fine.)
**Fix:** Fail fast (block startup) on migration error, or record per-migration applied-flags and verify them. **Effort:** Small.

### M-2 · CI does not run tests · `.github/workflows/build.yml:25-26`
The workflow runs `dotnet build` only. The strong 67-test suite never gates pushes/PRs.
**Fix:** Add a `dotnet test Tests/AlJohary.ServiceHub.Tests.csproj` step. **Effort:** Trivial.

### M-3 · Silent schema-ALTER failure · `Infrastructure/Data/DatabaseManager.cs:381-405`
`EnsureColumnExists` wraps the `ALTER TABLE` in `try/catch(Exception)` that only logs. A genuinely failed column add proceeds silently and later queries referencing that column will throw far from the cause.
**Fix:** Distinguish "already exists" from real failures; rethrow the latter. **Effort:** Small.

### M-4 · Backup/restore has no validation · `Infrastructure/Data/DatabaseManager.cs:73-81`
`Restore` does `File.Copy(backupFilePath, _databasePath, true)` then `Initialize()`. A corrupt or wrong-schema file silently replaces the live DB and the app re-creates/extends tables on top of it.
**Fix:** Validate the file opens as SQLite and carries the expected `settings`/schema markers before overwriting; keep a pre-restore safety copy. **Effort:** Small.

### M-5 · Profit-recognition timing across periods · `Infrastructure/Persistence/ReportRepository.cs:58,104-112,131`
`net_profit` = `SUM(sales.profit)` − date-ranged `lost_profit` − expenses − net salary. Returns never reduce the original sale's stored `profit` (`ReturnService.cs` updates paid/remaining only). This is correct *over time* (profit accrues at sale, reverses at return) but a single-day figure overstates profit if the matching return lands in a later period.
**Why acceptable:** It's a deliberate accrual model and is internally consistent. **Fix (optional):** Document it on the KPI tooltip, or recognize profit on a cash/return-matched basis if same-period netting is desired. **Effort:** Small (doc) / Medium (model change).

### M-6 · `net_cash_flow` mixes payment methods under a "cash" name · `Infrastructure/Persistence/ReportRepository.cs:137-142`
`net_cash_flow` sums **all** `sale_payments`/`repair_payments` regardless of method, i.e. it's really "net money flow across all methods," not physical cash. It is computed and unit-tested (`FinancialFlowTests.cs:310`) but **not displayed** anywhere in the UI (no Presentation reference) — see L-1.
**Fix:** Either surface it with an accurate label or drop it. **Effort:** Small.

---

## 7. Low Priority Issues

- **L-1 · `net_cash_flow` is computed but never surfaced** (`ReportRepository.cs:137`, `ReportService.cs:52`) — no ViewModel/View reads it. Dead output kept alive only by tests.
- **L-2 · `payment_details` summary key set but never read** (`ReportRepository.cs:477`) — `ReportService.GetPeriodSummary` does not copy it forward.
- **L-3 · `SELECT *` over-fetch** across repositories (26 occurrences, e.g. `CustomerRepository.cs:77`, `ProductRepository.cs:43`, `SaleRepository.cs:191`). Harmless on local SQLite but a brittleness/clarity nit; column-list selects are safer against schema drift.
- **L-4 · Debug leftover** (`Application/Services/LanguageService.cs:111` `System.Diagnostics.Debug.WriteLine(...)`).
- **L-5 · `ExpenseService.SearchExpenses` uses `ToLower()` without InvariantCulture** (`Application/Services/ExpenseService.cs:50`). Arabic is unaffected, but culture-sensitive casing is a latent nit.
- **L-6 · Open TODO** (`Presentation/ViewModels/ReportsViewModel.cs:456`).
- **L-7 · `BarcodeGenerator.cs:59` string `==` comparison** — flagged by scan as float-equality but it's a `string` compare (`elements[i] == "w"`); **false positive**, no action.

---

## 8. UI/UX Issues

| Page / Component | Problem | User impact | Fix |
|---|---|---|---|
| Reports (`ReportsViewModel.cs:167,176,253,267`) | Daily/monthly reports hardwire `DateTime.Today`/current month; no date picker | Cannot review an arbitrary past day/month | Add start/end date inputs feeding `GetDailySummary`/`GetPeriodSummary` |
| Reports export (`ReportsViewModel.cs:395-398`) | `ExportReport` is a stub: "ميزة التصدير قيد التطوير" | Advertised export does nothing | Implement CSV/XLSX export or hide the button |
| Reports print (`ReportsViewModel.cs:456`) | KPI print is a plain two-column table (TODO acknowledges a richer layout) | Functional but plain printout | Optional card-style print layout |
| Per-method KPI cards (`ReportsViewModel.cs:222-232`) | Money under non-canonical methods is not shown (see H-4) | Operator could under-count a method's net | Add "other" bucket + coalesce |
| Returns report (`ReportsViewModel.cs:304`) | Fixed last-30-days window, not user-selectable | Limited historical view | Add date range |

The MVVM layering, RTL handling, role-gated admin actions, and confirmation dialogs (e.g. `POSViewModel.cs:229`) are otherwise solid.

---

## 9. Bugs and Edge Cases

- **B-1 (latent, not a live bug):** Invoice-level discount below total cost / over ceiling — see **H-3**. Unreachable via current UI.
- **B-2 (reporting):** Non-canonical/empty `payment_method` disappears from per-method KPI cards — see **H-4**.
- **B-3 (operational):** Migration or `EnsureColumnExists` failure is swallowed; later queries fail far from the cause — see **M-1/M-3**.
- **No issue found** in the returns math: quantity validation guards over-return (`RefundValidator.cs:13`, `ReturnService.cs:63`), refund is capped at `Math.Min(sale.PaidAmount, totalCashRefund)` (`ReturnService.cs:110`), per-item paid/remaining are decremented with `Math.Max(0, …)` guards (`:132-135`), and stock is restored (`:130`). Duplicate return lines are pre-merged (`:31-44`).
- **No issue found** in maintenance: overpayment (`MaintenanceService.cs:335`) and payment-on-cancelled (`:332`) are rejected; inventory is restored on part removal / device removal / order cancel (`:303,385,455`).
- **No issue found** in supplier flow: debt reconciles `Σpurchases − Σpayments` and paid-at-purchase is counted once (proven by `FinancialFlowTests.cs:217`); overpayment leaves no orphan row (`:251`).

---

## 10. Financial / POS Logic Risks

**Overall: strong.** Money is `decimal` end-to-end; percentages (`max_discount_percent`) are the only `double`s and are not money. Every money movement is journaled.

**Confirmed-correct paths:**
- **Cash-only invariant is *rejected*, not coerced.** Non-cash or under-paid sales throw rather than silently zeroing a balance (`SaleService.cs:81,92`), so `remaining_amount` is provably 0. Verified by `CreditSale_Rejected`.
- **Price floor & ceilings** enforced server-side, below-cost universal (admin included) (`PriceLimitValidator.cs:41`, mirrored in UI `POSViewModel.cs:292,317`). Verified by `AdminBypassesCeiling_ButNeverBelowCost`.
- **Per-item financial distribution** uses last-item-remainder allocation so item totals sum exactly to the invoice and `Σ item.profit == sale.profit` with no rounding drift (`SaleService.cs:155-192`).
- **Maintenance profit** is recognized payment-proportionally against revenue, capping legacy overpayments and avoiding double-count across multiple payments (`ReportRepository.cs:149-217`). Labor = 100% margin, parts = `total_cost − purchase_cost*qty` — a deliberate model.
- **Returns lost-profit** is computed independently by return date (`ReportRepository.cs:104-112`) and reconciles with how item profit was booked.
- **Payment-method normalization** folds legacy `"كاش" → "نقدي"` only on payment-method columns, never on salary deductions (kept NULL) or sale-type labels (`Migration009…:33-40`). Verified by `Migration009_FoldsLegacyKashToCanonicalCash`.
- **Soft-deleted expenses** are excluded from every total via `COALESCE(is_deleted,0)=0` and retain an audit row (`ExpenseRepository.cs:87`, reports `:90,260,368`). Verified by `Expense_SoftDelete_…`.
- **Protected categories** (salaries / supplier payments) cannot be double-booked as generic expenses (`ExpenseService.cs:67-80`). Verified by `Expense_ProtectedCategory_Rejected`.
- **Salary reversal** posts a compensating negated row (no destructive delete), netting period totals to 0 (`EmployeeService.cs:112-132`). Verified by `SalaryReversal_NetsToZero_OriginalRetained`.
- **`net_cash_flow` reconciles with raw SQL** over all six in/out sources (`FinancialFlowTests.cs:310-333`).
- **No untracked money:** every inflow/outflow writes a row (`sale_payments`, `repair_payments`, `supplier_transactions`, `employee_salary_transactions`, `expenses`, `returns.cash_refund`). Supplier `'purchase'` rows are correctly excluded from cash-out (`ReportRepository.cs:382`).

**Risks to act on:**
- **R-1 (latent):** invoice-level discount/markup is unvalidated (**H-3**).
- **R-2 (reporting):** per-method KPI buckets drop non-canonical methods (**H-4**); the three per-method nets may not sum to `net_cash_flow`.
- **R-3 (recognition timing):** cross-period sale/return profit timing (**M-5**) — by design; worth documenting.
- **R-4 (audit trail):** expense/supplier/salary writes are journaled to `activity_log`, but **plain sales** are logged via `_saleRepo.LogActivity` only on success (`SaleService.cs:141`) — fine; just note that price overrides themselves aren't separately audited (who discounted, by how much) beyond the persisted item discount/markup amounts (`SaleService.cs:278-289`).

---

## 11. Refactor Needed

| File | Lines | Why | Suggested extraction |
|---|---|---|---|
| `Infrastructure/Persistence/ReportRepository.cs` | 447 | `BuildSummaryRange` + two large UNION-ALL report queries + breakdowns in one class | Split into `SummaryQueries`, `OperationsLogQueries`, `PaymentBreakdownQueries`; move SQL to constants |
| `Infrastructure/Persistence/SaleRepository.cs` | 480 | Sales + sale items + returns + report queries in one repo | Extract `ReturnRepository` (returns/return_items already have their own interface boundary) |
| `Presentation/ViewModels/POSViewModel.cs` | 468 | Cart, search, pricing-policy, checkout, printing in one VM | Extract a `CartModel` and a `PriceEditPolicy` (UI mirror of `PriceLimitValidator`) |
| `Infrastructure/Printing/ReceiptPrintService.cs` | 471 | Document-building + pagination + printing | Extract a `ReceiptDocumentBuilder` |
| `Infrastructure/Data/DatabaseManager.cs` | 452 | Connection + transactions + query helpers + schema/seed/migrate-branding + number generators | Split connection/transaction core from seeding/number-generation |
| `Application/Services/MaintenanceService.cs` | 424 | Many transactional operations; lots of repeated begin/commit/rollback | Extract a `WithTransaction(Action)` helper (see §12) |

---

## 12. Simplification Opportunities

- **Repeated transaction boilerplate.** The `BeginTransaction → try → Commit → catch → Rollback → throw` block is copy-pasted ~15× (`MaintenanceService.cs` ×7, `SaleService.cs`, `ExpenseService.cs`, `SupplierService.cs`, `EmployeeService.cs`, `AuthService.cs`). `EmployeeService.RunSalaryWrite` (`:134`) already shows the pattern — promote a single `IDbTransactionManager.Execute(Action)` / `Execute<T>(Func<T>)` helper and delete the duplication.
- **Duplicate percent helpers.** `FinancialCalculator.CalculateDiscountPercent/CalculateMarkupPercent` (`Core/Accounting/FinancialCalculator.cs:22-32`) duplicate `Calculations.CalculateDiscountPercent/…` (`Shared/Helpers/Utilities.cs:327-337`). Consolidate.
- **Duplicate customer-resolve logic.** `SaleService.HandleCustomer` (`:219`) and `MaintenanceService.ResolveCustomer` (`:33`) are near-identical get-or-create-by-phone; extract to `CustomerService`.
- **`GetDailySummary`/`GetMonthlySummary` on `SaleRepository`** (`:358,372`) overlap the canonical `ReportRepository` summary and appear unused by the reports UI — candidates to remove (verify).

---

## 13. Dead Code / Dead Files / Unused Items (verify before removal)

All confirmed by Grep across the whole tree; none are referenced from `Presentation/`:

- `ReportRepository.GetOperationsReport` + `IReportService.GetOperationsReport` + `IReportRepository.GetOperationsReport` + `ReportService.GetOperationsReport` (`ReportRepository.cs:223`) — the legacy delivery-date operations log, **superseded** by `GetFinancialOperations`; no UI caller. **Verify before removal.**
- `SupplierService.AddSupplierPurchase` + `SupplierRepository.AddSupplierPurchase` + interface decls (`SupplierService.cs:108`) — `[Obsolete]`, no caller. **Verify before removal.**
- `SaleRepository.GetUnpaidByCustomer` (`:228`) — `[Obsolete]`, credit-era, no caller.
- `SaleRepository.UpdateSaleFinancials` (`:144`) and `UpdateSaleItemFinancialsAfterReturn` (`:265`, `[Obsolete]`) and `UpdateItemPayment` (`:313`) — no callers found.
- `net_cash_flow` (computed/surfaced but never displayed — L-1) and `payment_details` (set, never read — L-2).
- `Shared/Helpers/TypedMessenger.cs` / `FocusHelper` / `WindowResizer` — referenced; **not** dead (kept for completeness).

> Repository hygiene is otherwise excellent: scan section 2 reported **no committed build artifacts/junk** and `.gitignore` correctly excludes `bin/obj/publish` and the runtime `.db`.

---

## 14. Clean Code / Best-Practice Review

**Strengths:** consistent naming, small focused methods, interfaces for every repository/service, DI wired explicitly in one bootstrapper (`AppBootstrapper.cs:44`), `decimal` for money, parameterized SQL everywhere (no string-concatenated or interpolated user SQL — scan section 10 found zero concatenation hits; the only `$"ALTER TABLE {table}…"` uses hard-coded identifiers, `DatabaseManager.cs:398`), and genuinely explanatory comments that document *why* (e.g. the cash-only invariant, de-scoped credit columns, maintenance profit recognition).

**Gaps:**
- **DRY:** transaction boilerplate and percent/customer helpers duplicated (§12).
- **Broad `catch (Exception)`** appears 94× (scan §7). Confirmed mostly legitimate (service methods translating to `Success=false`/rethrow). The ones worth a second look are the **swallow-all** catches that only log and continue: migrations (`AppBootstrapper.cs:115`, **M-1**) and `EnsureColumnExists` (`DatabaseManager.cs:401`, **M-3**). The empty `catch { }` blocks in printing (`A4PrintBase.cs:362,376`, `ReceiptPrintService.cs:125,165,325`, `ReportPrintService.cs:57,68`) are **benign** best-effort fallbacks (logo load / page-media-size) confirmed by reading them — leave as-is or add a one-line comment.
- **KISS/SOLID:** `DatabaseManager` is a god-object (connection + tx + query + schema + seeding + number generation) — see §11.
- **Naming nit:** `SaleRepository.updatePaymentStatus` (`:138`) is camelCase, inconsistent with the rest of the C# API surface.

---

## 15. Security Review

- **Password storage:** strong — PBKDF2/SHA256, 100k iterations, 16-byte salt, 32-byte key, constant-time compare, with a 10k-iteration legacy fallback (`Shared/Helpers/Utilities.cs:17-66`). ✅
- **Default credential:** `admin/admin123`, documented, never force-changed — **H-2**.
- **SQL injection:** not found — all queries parameterized via `AddWithValue`; dynamic SQL fragments (`UserRepository.Update` `:91`, identifier-only `ALTER`) use no user-controlled strings. ✅
- **AuthZ:** admin-gated operations consistently check `IsAdmin`/throw `UnauthorizedAccessException` (`AuthService.cs:78,100,141,147,159,168`; `EmployeeService.EnsureAdmin:185`). Last-admin-deletion and self-deletion are guarded (`AuthService.cs:148,151`). ✅
- **Secrets:** no real keys/tokens committed (scan §9 "likely real keys" = none). The flagged lines are WPF `PasswordBox.Password` plumbing, not leaks. ✅
- **Trust boundary:** single-user desktop app with a local DB; the main exposure is physical/console access plus the default credential. The mutable global `AuthService.Instance` (`AuthService.cs:27`) is a design smell but not a remote risk.

---

## 16. Performance Review

Fine for the intended scale (single shop, local SQLite).
- Migration009 adds the reporting indexes the financial queries need (`Migration009…:43-48`). ✅
- Report list queries cap rows (`LIMIT 100/200`) (`SaleRepository.cs:442`, `SupplierRepository.cs:250`). ✅
- **Watch items:** `CalculateMaintenanceProfitForPayments` (`ReportRepository.cs:149`) runs correlated subqueries per repair payment — fine at shop volume, could be tuned with joins/CTEs if repair history grows large. `SELECT *` over-fetch (L-3) is negligible locally. The global `_lock` on every query serializes all DB access (acceptable single-user; see H-1).

---

## 17. Testing Gaps

The suite (67 tests) is strong and covers the core money paths. Add tests for:

1. **Multi-item invoice distribution** — assert `Σ item.TotalPrice == sale.TotalAmount` and `Σ item.Profit == sale.Profit` with an invoice-level discount and ≥3 items including the last-item remainder (`SaleService.cs:155`).
2. **Partial & repeated returns** — return some units, then more; assert paid/remaining/stock and `lost_profit` across two `CreateReturn` calls (`ReturnService.cs`).
3. **Per-method reconciliation** — assert `Σ(per-method inflow − outflow) == net_cash_flow`, including a row with NULL/empty `payment_method`, to lock down **H-4**.
4. **Maintenance profit across multiple payments** — partial then final payment; assert profit recognized once and capped (`ReportRepository.cs:201-208`).
5. **Invoice-level discount guard** — once **H-3** is fixed, assert an over-ceiling/below-cost invoice discount is rejected.
6. **Migration idempotency / failure** — run all migrations twice; assert no error and stable state.
7. **CI:** wire `dotnet test` into `build.yml` so these actually run (**M-2**).

---

## 18. Recommended Fix Roadmap

**Phase 1 — Must-fix before wider deployment**
- H-2 force admin password change; M-1 fail-fast on migration error; M-4 validate backup before restore; M-2 run tests in CI.
- H-1 decide the concurrency model (document single-thread access *or* move to connection-per-unit-of-work).

**Phase 2 — Correctness hardening & refactor**
- H-3 validate invoice-level discount (or remove the unused parameters); H-4 coalesce + "other" bucket in payment breakdowns; M-3 stop swallowing ALTER failures.
- Extract the `WithTransaction` helper (§12); split `ReportRepository`/`DatabaseManager` (§11).

**Phase 3 — UX**
- Date-range pickers for daily/monthly/returns reports; implement or hide Export; optional richer KPI print.

**Phase 4 — Testing & cleanup**
- Add tests §17.1–6; remove verified dead code §13 after confirming no reflection/XAML usage.

---

## 19. Final Verdict

**Production-ready for its intended single-operator deployment: YES, with the Phase-1 caveats.** This is one of the cleaner financial WPF codebases you'll audit — invariants are enforced in the service layer, money is `decimal`, every cash movement is journaled, and the test suite even reconciles cash flow against raw SQL. The build is clean and all 67 tests pass.

**Blockers for wider/multi-user rollout:** the process-global connection/transaction model (H-1) and the default admin credential (H-2). Neither blocks a single-till shop today.

**Clean vs. major cleanup:** **Clean, not a major cleanup.** The outstanding work is a short list of hardening fixes (force password change, fail-fast migrations, validate restore, run tests in CI), one latent control gap to close (invoice-level discount), one reporting blind spot (per-method buckets), and routine dead-code pruning.

**Exact next action:** Implement **H-2** (force first-login admin password change) and **M-2** (add `dotnet test` to `.github/workflows/build.yml`) — both small, both high-leverage — then schedule **H-1** for a deliberate decision before any multi-user/networked deployment.

---

*Audit method note: every finding above was confirmed by opening the cited file at the cited line and tracing usage with Grep. Capped scan categories (exception handling, SQL, money, auth) were re-checked beyond the cap. Zero files in the target repository were modified, deleted, renamed, formatted, or committed; this report is the only file created.*
