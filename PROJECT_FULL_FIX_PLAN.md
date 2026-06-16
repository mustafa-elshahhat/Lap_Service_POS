# Project Full Fix Plan

**Repository:** AlJohary Service Hub (POS + repair/maintenance, WPF desktop)
**Path:** `D:\projects\Lap_Service_POS`
**Source report:** `PROJECT_FULL_AUDIT_REPORT.md` (HEAD `68807d3`, branch `main`, audited 2026-06-16)
**Plan date:** 2026-06-16
**Plan type:** Executable, phased remediation plan derived strictly from the audit report.

---

## 1. Plan Overview

### Purpose
Convert every finding in `PROJECT_FULL_AUDIT_REPORT.md` into a concrete, phase-by-phase work plan that another coding agent can execute without re-reading the audit. Each audit issue (High, Medium, Low, UI/UX, bug/edge case, financial/POS risk, refactor, simplification, dead-code candidate, clean-code, security, performance, and testing gap) is represented by at least one task and tracked in the traceability matrix (§8).

### Source report used
The single source of truth is `PROJECT_FULL_AUDIT_REPORT.md`. No issue is invented; no issue is dropped. Where the audit marks something "verify before removal", "optional", "latent", "documentation only", or "false positive", this plan preserves that status as a verification or documentation task rather than an automatic change.

### Overall strategy
The audit's verdict is **production-ready for single-operator deployment, with caveats**. There are **no confirmed money-loss or data-corruption bugs**. Work therefore proceeds in risk-ascending-confidence order:
1. Establish a safety baseline (clean tree, green build/tests, branch, DB backup, rollback).
2. Close the deployment blockers (security default credential, migration fail-fast, restore validation, CI test gating) and make the concurrency decision (H-1).
3. Harden financial correctness and reporting (latent invoice-discount gap, per-method KPI blind spot, recognition-timing documentation, `net_cash_flow` decision).
4. Harden schema/data-access/repository safety.
5. Refactor and simplify with **no behavior change**.
6. Polish UI/UX (reports date ranges, export, print, "other" bucket).
7. Remove dead code **only after** usage verification.
8. Add regression tests proving financial behavior is preserved.
9. Final verification and release readiness.

> Note: the audit's own roadmap (§18) groups refactor with Phase 2 and tests with Phase 4. This plan re-sequences into Phases 0–8 (per the required structure) so that **tests are added before refactors** where they protect financial behavior, and **dead-code removal happens last**, after build/test verification and usage search. No audit item is lost in the re-sequencing.

### Safety rules (apply to every task)
- Do **not** change financial behavior without an accompanying test (see §5).
- Do **not** remove any dead-code candidate without a usage-search verification step first (Grep across the whole tree, including XAML/reflection).
- Do **not** alter database/migration/restore logic without a fresh DB backup and a rollback note.
- Keep `PROJECT_FULL_AUDIT_REPORT.md` unchanged — it is the source of truth.
- Touch only files named in the relevant audit item; no opportunistic edits.
- Re-run build + full test suite after every phase (see §7).

### What is fixed first and why
**Phase 1** (after the Phase 0 baseline) handles the audit's "Biggest risks" and "Blockers for wider rollout":
- **H-2** default admin credential — small, high-leverage, security.
- **M-1** migrations fail-fast — prevents running against an inconsistent schema.
- **M-4** backup/restore validation — prevents silently replacing the live DB with a bad file.
- **M-2** CI runs tests — makes the strong 67-test suite actually gate changes (do this early so every later phase is gated).
- **H-1** concurrency decision — architectural; decide and document/assert before any multi-user rollout.

The audit's "Exact next action" is H-2 + M-2; both are in Phase 1.

### What must NOT be changed without verification
- **H-1 concurrency model:** do not silently swap SQLite or rip out the global connection. This is an architecture **decision** task; the default (low-risk) path is document + assert single-threaded access. A connection-per-unit-of-work rewrite is opt-in only.
- **All §13 dead-code candidates** (`GetOperationsReport`, `AddSupplierPurchase`, `GetUnpaidByCustomer`, `UpdateSaleFinancials`, `UpdateSaleItemFinancialsAfterReturn`, `UpdateItemPayment`, `net_cash_flow` output, `payment_details`): verify no reflection/XAML/test caller before any removal.
- **M-5 recognition timing** and **R-4 audit trail**: documentation/decision items by design — do not change the accrual model unless explicitly chosen, and only with tests.
- **L-7** (`BarcodeGenerator.cs:59`): confirmed **false positive** — no action; do not "fix" it.
- **`net_cash_flow` / `payment_details`** (M-6, L-1, L-2): decision tasks (surface accurately vs. remove) — pick one explicitly; do not remove output that tests still rely on without updating those tests.

---

## 2. Phase Summary

| Phase | Name | Goal | Risk | Expected impact | Effort | Dependencies | Completion criteria |
|---|---|---|---|---|---|---|---|
| 0 | Preparation & Safety Baseline | Make the repo safe to change | Low | No code change; safety net established | Small | None | Clean tree, green build/tests, branch created, DB backup taken, rollback documented |
| 1 | Must-Fix Before Wider Deployment | Close deployment blockers (security, migration, restore, CI, concurrency) | High | Removes the documented rollout blockers | Medium | Phase 0 | H-2, M-1, M-4, M-2 implemented; H-1 decision recorded; tests green; CI runs tests |
| 2 | Financial Correctness & Reporting Hardening | Close latent financial gaps & reporting blind spots | High | Per-method KPIs reconcile; invoice-discount guarded; recognition timing documented | Medium | Phase 1 | H-3, H-4 fixed; M-5, M-6 decided/documented; R-1..R-4 addressed; reconciliation holds |
| 3 | Schema, Data Access & Repository Hardening | Improve DB safety & repo reliability | Medium | Fail-loud ALTER; safer selects | Medium | Phase 1 | M-3 fixed; SELECT* scoped where chosen; raw-SQL readability improved |
| 4 | Refactor & Simplification | Reduce complexity, no behavior change | Medium | Smaller classes, DRY helpers | Large | Phase 7 tests recommended first for touched areas | All §11/§12 extractions done; tests unchanged-green |
| 5 | UI/UX & Reports Polish | Improve usability, no financial-logic risk | Low | Date ranges, export, print, other-bucket | Medium | Phase 2 (H-4 for other-bucket) | All §8 UI/UX items resolved or explicitly deferred |
| 6 | Dead Code, Dead Files & Cleanup | Remove obsolete code after verification | Low | Smaller surface | Small | Phases 1–3 build/test green; usage search | Every §13/§7 cleanup verified then removed/marked; tests green |
| 7 | Testing & Regression Coverage | Prove fixes didn't break financial behavior | Medium | Locks down H-3/H-4/M-5 and core money paths | Medium | Phases 1–2 | All §17.1–7 tests added and passing |
| 8 | Final Verification & Release Readiness | Confirm clean, stable, shippable | Low | Release sign-off | Small | All phases | Full build/test green, smoke + reconciliation pass, diff reviewed, checklist complete |

---

## 3. Detailed Task Breakdown

> Task ID convention: `P{phase}-T{nn}`. "Source audit item ID" preserves the report's IDs (H-1…, M-1…, L-1…, R-1…, B-1…, plus §11/§12/§13/§17 references). Line references are carried verbatim from the report.

---

### Phase 0 — Preparation and Safety Baseline

#### P0-T01
- **Title:** Confirm clean git status and pristine baseline
- **Source audit item ID:** Plan prerequisite (audit §3 "clean working tree")
- **Severity:** N/A (process)
- **Area:** Process / safety
- **Files affected:** none (working tree only; `PROJECT_FULL_FIX_PLAN.md` is the sole new file)
- **Current evidence from audit:** "Branch / HEAD: `main` @ `68807d3` (clean working tree)"
- **Problem summary:** Changes must start from a known-clean state with the audit report and this plan as the only untracked files.
- **Desired behavior:** `git status --short` shows only `PROJECT_FULL_AUDIT_REPORT.md` and `PROJECT_FULL_FIX_PLAN.md` (untracked) and nothing else modified.
- **Implementation notes:** Read-only; no source change.
- **Step-by-step work plan:** 1) Run `git status --short`. 2) Confirm no tracked file is modified. 3) Record HEAD `68807d3`.
- **Edge cases to handle:** Stale `bin/obj` — confirm `.gitignore` already excludes them (audit §13 confirms it does).
- **Tests to add/update:** none.
- **Manual verification steps:** Eyeball `git status --short` output.
- **Acceptance criteria:** Only the two markdown files appear as untracked; zero modified tracked files.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** none.
- **Rollback notes:** N/A (no change).

#### P0-T02
- **Title:** Confirm current build passes (0 warnings / 0 errors)
- **Source audit item ID:** audit §3 build row
- **Severity:** N/A
- **Area:** Build
- **Files affected:** none
- **Current evidence from audit:** "`dotnet build AlJohary.ServiceHub.sln -c Debug` ✅ passed — 0 Warning(s), 0 Error(s)"
- **Problem summary:** Establish the clean-build baseline so any later warning is attributable.
- **Desired behavior:** Build succeeds with 0 warnings / 0 errors.
- **Implementation notes:** none.
- **Step-by-step work plan:** Run `dotnet build AlJohary.ServiceHub.sln`.
- **Edge cases to handle:** none.
- **Tests to add/update:** none.
- **Manual verification steps:** Read build summary line.
- **Acceptance criteria:** 0 warnings, 0 errors.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** P0-T01.
- **Rollback notes:** N/A.

#### P0-T03
- **Title:** Confirm current tests pass (67/67)
- **Source audit item ID:** audit §3 test row
- **Severity:** N/A
- **Area:** Testing
- **Files affected:** none
- **Current evidence from audit:** "`dotnet test … --tl:off` ✅ Passed! Failed: 0, Passed: 67, Skipped: 0"
- **Problem summary:** Establish green-test baseline before changes.
- **Desired behavior:** All 67 tests pass.
- **Implementation notes:** Target the Tests project with `--tl:off` (per memory: targeting the .sln hides results).
- **Step-by-step work plan:** Run `dotnet test Tests\AlJohary.ServiceHub.Tests.csproj --tl:off`.
- **Edge cases to handle:** none.
- **Tests to add/update:** none.
- **Manual verification steps:** Confirm 67 passed / 0 failed.
- **Acceptance criteria:** 67/67 green.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** P0-T02.
- **Rollback notes:** N/A.

#### P0-T04
- **Title:** Create working branch for remediation
- **Source audit item ID:** Plan prerequisite
- **Severity:** N/A
- **Area:** Process
- **Files affected:** none (branch only)
- **Current evidence from audit:** Plan requirement; audit confirms linear history on `main`.
- **Problem summary:** Isolate remediation from `main`.
- **Desired behavior:** A dedicated branch (e.g. `remediation/audit-fixes`) is checked out.
- **Implementation notes:** Do not commit until the user requests it (harness rule). Branch creation only.
- **Step-by-step work plan:** 1) Create branch from `68807d3`. 2) Confirm checkout.
- **Edge cases to handle:** Ensure branch is not the default `main`.
- **Tests to add/update:** none.
- **Manual verification steps:** `git status` shows the new branch.
- **Acceptance criteria:** New branch active; `main` untouched.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** P0-T01.
- **Rollback notes:** Delete the branch; `main` is unaffected.

#### P0-T05
- **Title:** Review current database / migration state
- **Source audit item ID:** Prereq for M-1, M-3, M-4 (audit §6, §10)
- **Severity:** N/A
- **Area:** Database
- **Files affected (review only):** `Presentation/AppBootstrapper.cs:103-118`, `Infrastructure/Data/DatabaseManager.cs` (migrations/seed), Migration files (`Migration005…:21`, `Migration009…:33-48`)
- **Current evidence from audit:** "ten migrations run inside try/catch"; "Migration009 adds reporting indexes"; "Migration005's `catch { }` around DROP COLUMN is intentional and fine."
- **Problem summary:** Understand which migrations exist and the schema state before touching migration/restore logic.
- **Desired behavior:** A written note of the 10 migrations, their order, and the runtime `.db` location.
- **Implementation notes:** Read-only inspection; do not run migrations against production data.
- **Step-by-step work plan:** 1) List migration files. 2) Note Migration009 indexes and Migration005 intentional `catch`. 3) Identify runtime DB path (gitignored `.db`).
- **Edge cases to handle:** Distinguish intentional `catch {}` (Migration005 DROP COLUMN) from the swallow-all to be fixed in M-1/M-3.
- **Tests to add/update:** none (yet; see P7).
- **Manual verification steps:** Confirm migration list matches "ten migrations".
- **Acceptance criteria:** Written migration/schema state summary exists.
- **Estimated effort:** Small.
- **Risk level:** None.
- **Dependencies:** P0-T01.
- **Rollback notes:** N/A.

#### P0-T06
- **Title:** Preserve DB backup before any DB-related change
- **Source audit item ID:** Prereq for M-1, M-3, M-4
- **Severity:** N/A
- **Area:** Database / safety
- **Files affected:** none (copies the runtime `.db` to a safe location outside the repo)
- **Current evidence from audit:** M-4: "`Restore` does `File.Copy(...)` … silently replaces the live DB."
- **Problem summary:** DB-related changes risk the live database; a verified backup must exist first.
- **Desired behavior:** A timestamped copy of the runtime `.db` is stored outside the working tree.
- **Implementation notes:** Backup only; do not commit the `.db` (it is gitignored).
- **Step-by-step work plan:** 1) Locate runtime `.db` (from P0-T05). 2) Copy to a safe backup path. 3) Verify the copy opens as SQLite.
- **Edge cases to handle:** If no runtime DB exists yet (fresh checkout), note that and skip — the app seeds on first run.
- **Tests to add/update:** none.
- **Manual verification steps:** Backup file exists and opens.
- **Acceptance criteria:** Verified backup present (or documented absence).
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** P0-T05.
- **Rollback notes:** Restore from this backup if any DB change goes wrong.

#### P0-T07
- **Title:** Define rollback strategy & confirm no unrelated files change
- **Source audit item ID:** Plan prerequisite
- **Severity:** N/A
- **Area:** Process / safety
- **Files affected:** none
- **Current evidence from audit:** Plan safety rules.
- **Problem summary:** Each task must have a known rollback and must not touch unrelated files; the audit report must remain unchanged.
- **Desired behavior:** Documented rollback (per-phase `git restore`/branch reset + DB backup) and a rule that `git diff` is reviewed after each phase for scope creep.
- **Implementation notes:** Confirm `PROJECT_FULL_AUDIT_REPORT.md` is never edited.
- **Step-by-step work plan:** 1) Write rollback procedure (git + DB). 2) Establish per-phase `git status --short` review. 3) Add "audit report unchanged" check.
- **Edge cases to handle:** Generated files (`bin/obj`) must stay ignored.
- **Tests to add/update:** none.
- **Manual verification steps:** Rollback procedure documented; audit report hash unchanged.
- **Acceptance criteria:** Rollback strategy written; audit report confirmed read-only; scope-review cadence defined.
- **Estimated effort:** Small.
- **Risk level:** None.
- **Dependencies:** P0-T04, P0-T06.
- **Rollback notes:** This task defines them.

---

### Phase 1 — Must-Fix Before Wider Deployment

#### P1-T01
- **Title:** Force admin password change on first login (remove default-credential risk)
- **Source audit item ID:** H-2 (§5, §15, §1 risk #2)
- **Severity:** High
- **Area:** Security
- **Files affected:** `Infrastructure/Data/DatabaseManager.cs:456`, `Presentation/Views/LoginWindow.xaml.cs`, `README.md:60`, `settings` table (flag), `Shared/Helpers/Utilities.cs:17-66` (hashing, reuse only)
- **Current evidence from audit:** "`string passwordHash = Security.HashPassword("admin123");` seeds the first admin with `admin`/`admin123`, and the README publishes it. Nothing forces a change on first login (`LoginWindow.xaml.cs` performs a plain `Login`)." Hashing is strong (PBKDF2-SHA256, 100k iters).
- **Problem summary:** A known default credential on a financial app, never force-changed.
- **Desired behavior:** First successful admin login forces a password change before any other action; or initial password is randomly generated and shown once at setup.
- **Implementation notes:** Add a `settings` flag (e.g. `force_password_change`) seeded `true` for the default admin; gate the main window behind a change-password dialog when the flag is set; clear the flag on success. Update `README.md:60` to stop publishing a usable default (describe the forced-change flow).
- **Step-by-step work plan:** 1) Add flag column/row to `settings` at seed. 2) On login, if flag set + user is the seeded admin, present mandatory change-password dialog. 3) Persist new hash via existing `Security.HashPassword`. 4) Clear flag. 5) Update README.
- **Edge cases to handle:** User cancels dialog (block access, do not proceed); new password equals old (reject); empty/weak password (enforce minimum per existing rules); non-admin users unaffected.
- **Tests to add/update:** Add test: seeded admin has `force_password_change=true`; after change, flag is false and old password fails. (Phase 7 alignment.)
- **Manual verification steps:** Fresh DB → login `admin`/`admin123` → forced change → re-login with new password works; `admin123` no longer works.
- **Acceptance criteria:** No login proceeds to the app with the default password still active; flag cleared only after a successful change; README no longer presents a working standing default.
- **Estimated effort:** Small.
- **Risk level:** Medium (auth path).
- **Dependencies:** Phase 0.
- **Rollback notes:** Revert the dialog gating and flag; restore DB from P0-T06 if seed altered.

#### P1-T02
- **Title:** Fail fast on migration error (stop swallowing partial migrations)
- **Source audit item ID:** M-1 (§6), B-3 (§9)
- **Severity:** Medium (deployment blocker per §18 Phase 1)
- **Area:** Database / startup
- **Files affected:** `Presentation/AppBootstrapper.cs:103-118` (esp. `:115`)
- **Current evidence from audit:** "ten migrations run inside `try { … } catch (Exception migrationEx) { Logger.LogException(...); }` — a failed/partial migration is logged but startup continues, potentially against an inconsistent schema." Migration005's intentional `catch {}` at `Migration005…:21` is fine.
- **Problem summary:** A partial migration is logged then the app runs against an inconsistent schema.
- **Desired behavior:** A migration exception blocks startup (fail fast) with a clear message; optionally record per-migration applied-flags and verify them.
- **Implementation notes:** Replace swallow-all at `:115` with rethrow/abort-startup. Do **not** touch Migration005's intentional inner `catch {}`.
- **Step-by-step work plan:** 1) On migration exception, log then halt startup (surface a fatal error dialog). 2) Optionally add applied-flag tracking + verification. 3) Leave Migration005 as-is.
- **Edge cases to handle:** Idempotent re-run (already-applied migrations must not be treated as failures — coordinate with P7 idempotency test); first-run empty DB.
- **Tests to add/update:** Migration idempotency/failure test (§17.6, P7-T06): run all migrations twice → no error; an injected failure aborts and is not silently swallowed.
- **Manual verification steps:** Start app against a healthy DB → normal start; simulate a broken migration → app refuses to start instead of continuing.
- **Acceptance criteria:** A genuine migration failure prevents app startup; healthy startup unchanged; Migration005 behavior preserved.
- **Estimated effort:** Small.
- **Risk level:** Medium (startup path).
- **Dependencies:** P0-T05, P0-T06.
- **Rollback notes:** Restore the prior catch block; DB from P0-T06.

#### P1-T03
- **Title:** Validate backup file before restore; keep pre-restore safety copy
- **Source audit item ID:** M-4 (§6), §1 risk #7
- **Severity:** Medium (deployment blocker per §18 Phase 1)
- **Area:** Database / backup-restore
- **Files affected:** `Infrastructure/Data/DatabaseManager.cs:73-81`
- **Current evidence from audit:** "`Restore` does `File.Copy(backupFilePath, _databasePath, true)` then `Initialize()`. A corrupt or wrong-schema file silently replaces the live DB and the app re-creates/extends tables on top of it."
- **Problem summary:** No validation before overwriting the live DB during restore.
- **Desired behavior:** Before overwriting, verify the backup opens as SQLite and carries expected `settings`/schema markers; take a pre-restore safety copy of the current DB; only then overwrite.
- **Implementation notes:** Open the candidate file read-only, check `PRAGMA`/`sqlite_master` for expected tables (e.g. `settings`); abort with a clear error on mismatch. Save current DB to a timestamped safety copy first.
- **Step-by-step work plan:** 1) Open backup read-only; validate it is SQLite. 2) Check expected schema markers (`settings` + key tables). 3) Copy live DB to safety path. 4) Overwrite. 5) `Initialize()`. 6) On any validation failure, do not overwrite.
- **Edge cases to handle:** Non-SQLite file; valid SQLite but wrong/old schema; locked live DB; partial copy interruption.
- **Tests to add/update:** Regression test for restore validation: valid backup restores; invalid/wrong-schema file is rejected and live DB untouched (§17 alignment, P7-T07).
- **Manual verification steps:** Restore a known-good backup → success; attempt restore of a random/corrupt file → rejected, live DB unchanged.
- **Acceptance criteria:** Invalid backups never replace the live DB; a pre-restore safety copy exists for every restore.
- **Estimated effort:** Small.
- **Risk level:** Medium (data-destructive path).
- **Dependencies:** P0-T06.
- **Rollback notes:** Restore from pre-restore safety copy or P0-T06 backup.

#### P1-T04
- **Title:** Run the test suite in CI
- **Source audit item ID:** M-2 (§6), §17.7, §1 risk #6
- **Severity:** Medium (high-leverage; audit "exact next action")
- **Area:** CI / build
- **Files affected:** `.github/workflows/build.yml:25-26`
- **Current evidence from audit:** "The workflow runs `dotnet build` only. The strong 67-test suite never gates pushes/PRs."
- **Problem summary:** CI does not execute tests, so regressions can merge.
- **Desired behavior:** CI runs `dotnet test Tests/AlJohary.ServiceHub.Tests.csproj` and fails the job on test failure.
- **Implementation notes:** Add a test step after build. Keep it gating PRs/pushes.
- **Step-by-step work plan:** 1) Add a `dotnet test Tests/AlJohary.ServiceHub.Tests.csproj` step to `build.yml`. 2) Ensure job fails on non-zero exit. 3) Confirm it runs on push/PR.
- **Edge cases to handle:** Windows-only runtime (`net10.0-windows`, `win-x64`) — ensure the runner OS supports the WPF/test targets.
- **Tests to add/update:** No new app tests; CI config only. (Later phases' new tests automatically gate once this lands.)
- **Manual verification steps:** Open a PR with a deliberately failing test locally → CI red; revert → CI green.
- **Acceptance criteria:** CI executes the full suite and blocks on failure.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** none (do early so subsequent phases are gated).
- **Rollback notes:** Revert `build.yml` to build-only.

#### P1-T05
- **Title:** Decide and enforce the DB concurrency model (global connection / `CurrentTransaction`)
- **Source audit item ID:** H-1 (§5), §16, §1 risk #1, §19 blocker
- **Severity:** High
- **Area:** Architecture / concurrency
- **Files affected:** `Infrastructure/Data/DatabaseManager.cs:16,124,133,135,177,222,255,287`, `Presentation/ViewModels/ReportsViewModel.cs:65`
- **Current evidence from audit:** "A single process-wide `SqliteConnection _connection` and a single `public SqliteTransaction CurrentTransaction { get; set; }` are shared by every repository … `BeginTransaction()` throws if one is already active (`:135`), and every `Execute/FetchOne/FetchAll` auto-enlists `CurrentTransaction` (`:177,222,255,287`)." "Correct only while all DB access happens serially on one thread."
- **Problem summary:** Process-global connection + singleton transaction is not concurrency-safe; overlapping flows block on `_lock` or run inside another operation's transaction.
- **Desired behavior:** A recorded decision. **Default (low-risk):** document single-threaded DB access as an invariant and add a guard/assert against re-entrant `BeginTransaction` from a different logical operation. **Opt-in (large):** connection-per-unit-of-work or transaction scoped to an explicitly passed connection.
- **Implementation notes:** This is a **decision task** — do not silently rewrite the data layer. If the low-risk path is chosen, add an assertion that detects re-entrant/cross-operation `BeginTransaction` and document the invariant near `:133`. The `ReportsViewModel.cs:65` Dispatcher marshal is evidence the invariant is currently relied upon.
- **Step-by-step work plan:** 1) Record the decision (document-and-assert vs. unit-of-work). 2) If document-and-assert: add re-entrancy guard + invariant comment + a developer note. 3) If unit-of-work: scope to a separate planned effort with its own tests (out of default scope). 
- **Edge cases to handle:** Background search/print/report-refresh touching the DB during an open sale/return transaction; `TypedMessenger`-triggered refresh.
- **Tests to add/update:** If guard added: a test asserting re-entrant `BeginTransaction` from a different logical operation is rejected/guarded.
- **Manual verification steps:** Trigger a report refresh during a checkout flow and confirm the guard/serialization behaves as documented.
- **Acceptance criteria:** A written concurrency decision exists; if document-and-assert chosen, a re-entrancy guard + invariant doc is in place and tests pass; no unintended behavior change for the single-till path.
- **Estimated effort:** Small (document + assert) or Large (unit-of-work) per appetite.
- **Risk level:** High if rewritten; Low if document-and-assert.
- **Dependencies:** Phase 0; ideally decided before any multi-user rollout.
- **Rollback notes:** Remove the guard/comment; no schema change in the default path.

---

### Phase 2 — Financial Correctness and Reporting Hardening

#### P2-T01
- **Title:** Validate invoice-level discount/markup (or remove unused parameters)
- **Source audit item ID:** H-3 (§5), B-1 (§9), R-1 (§10)
- **Severity:** High (latent)
- **Area:** Financial control
- **Files affected:** `Application/Services/SaleService.cs:75,84-93,264` (`CreateSaleInternal`), `Core/.../PriceLimitValidator.cs:41`, callers `Presentation/ViewModels/POSViewModel.cs:363`, `Tests/FinancialFlowTests.cs:84`
- **Current evidence from audit:** "`CalculateTotalWithDiscountAndMarkup(subtotal, discountAmount, markupAmount)` applies an invoice-level discount/markup that is never run through `PriceLimitValidator` (which only validates each item's `UnitFinalPrice` at `:264`). The below-cost floor is likewise per-item only." Production caller `CheckoutCash` passes `0,0` (`POSViewModel.cs:363`); multi-payment `CreateSale` overload invoked only by tests (`FinancialFlowTests.cs:84`).
- **Problem summary:** An invoice-level discount could push the total below total cost or exceed an employee's ceiling without tripping any validator. Not exploitable today; defense-in-depth gap.
- **Desired behavior:** Either (a) validate the post-discount invoice total against summed cost and the actor's discount ceiling inside `CreateSaleInternal`, or (b) remove the unused invoice-level discount/markup parameters until a guarded UI needs them.
- **Implementation notes:** Prefer option (a) so the multi-payment overload is safe if wired to UI later. Reuse `PriceLimitValidator` semantics (below-cost universal, ceiling per actor). Keep the cash-only invariant intact (`SaleService.cs:81,92`).
- **Step-by-step work plan:** 1) Decide (a) validate or (b) remove. 2a) Compute summed item cost; reject if post-discount total < total cost; reject if discount exceeds actor ceiling. 2b) If removing, drop the parameters and update the test-only overload. 3) Keep production `CheckoutCash` passing `0,0` behavior unchanged.
- **Edge cases to handle:** Discount exactly at cost floor; admin actor (still cannot go below cost); zero discount path unchanged; rounding at invoice vs. item level (preserve last-item-remainder allocation `:155-192`).
- **Tests to add/update:** P7-T05 invoice-level discount guard — assert an over-ceiling/below-cost invoice discount is rejected; assert distribution still sums (ties to §17.5 and §17.1).
- **Manual verification steps:** If UI later exposes it, attempt an over-discount and confirm rejection. Today, run the guard test.
- **Acceptance criteria:** No invoice-level discount can produce total < total cost or exceed actor ceiling; existing zero-discount production path behaves identically; tests pass.
- **Estimated effort:** Small.
- **Risk level:** Medium (financial logic — requires tests).
- **Dependencies:** Phase 0; pairs with P7-T05.
- **Rollback notes:** Revert validation/parameter change; restore test overload.

#### P2-T02
- **Title:** Coalesce non-canonical payment methods + add "other" bucket in payment breakdowns
- **Source audit item ID:** H-4 (§5), B-2 (§9), R-2 (§10), UI/UX per-method cards (§8)
- **Severity:** High
- **Area:** Reporting
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs:434-477` (`AddPaymentBreakdowns`), reference `:331,347,361,375,390,420` (`GetFinancialOperations` coalesce), `Presentation/ViewModels/ReportsViewModel.cs:201-206,222-232,389-393`, method constants (`PaymentMethods.Cash/InstaPay/EWallet`)
- **Current evidence from audit:** "`AddPaymentBreakdowns` groups by raw `payment_method` with **no** `COALESCE(...,'غير محدد')`, while `GetFinancialOperations` does coalesce NULL/empty to `'غير محدد'`. The KPI cards then sum only the three canonical methods via `GetMethodSum(...)`." Result: NULL/empty/non-canonical rows are invisible and the three per-method nets won't sum to `net_cash_flow`.
- **Problem summary:** Money under non-canonical/empty `payment_method` is dropped from per-method KPI cards; per-method nets don't reconcile with `net_cash_flow`.
- **Desired behavior:** Coalesce NULL/empty to `'غير محدد'` in `AddPaymentBreakdowns` exactly as `GetFinancialOperations` does, and surface an "other methods" bucket/card so no inflow/outflow is dropped; the three+other per-method nets sum to `net_cash_flow`.
- **Implementation notes:** Mirror the existing coalesce pattern from `GetFinancialOperations`. Add an "other" card in `ReportsViewModel` per-method display.
- **Step-by-step work plan:** 1) Add `COALESCE(payment_method,'')`/empty→`'غير محدد'` in the breakdown grouping. 2) Add an "other" bucket aggregating everything outside the three canonical methods. 3) Add the "other" KPI card in the VM/View. 4) Verify Σ(canonical + other inflow − outflow) == `net_cash_flow`.
- **Edge cases to handle:** NULL vs. empty string vs. whitespace; legacy `'كاش'` already folded by Migration009 (don't double-handle); supplier default `'نقدي'` (`SupplierRepository.cs:120`); zero "other" rows (card shows 0, not hidden incorrectly).
- **Tests to add/update:** P7-T03 per-method reconciliation including a NULL/empty `payment_method` row: assert `Σ(per-method inflow − outflow) == net_cash_flow` (§17.3).
- **Manual verification steps:** Insert/observe a row with non-canonical method; confirm it appears in the "other" card and totals reconcile.
- **Acceptance criteria:** No inflow/outflow is dropped from the per-method view; per-method nets (incl. other) reconcile to `net_cash_flow`; reconciliation test passes.
- **Estimated effort:** Small.
- **Risk level:** Medium (reporting correctness — requires test).
- **Dependencies:** Phase 1; pairs with P7-T03; precedes per-method reconciliation tests.
- **Rollback notes:** Revert coalesce + remove "other" card; reporting returns to prior (blind-spot) behavior.

#### P2-T03
- **Title:** Document (or change) cross-period profit-recognition timing
- **Source audit item ID:** M-5 (§6), R-3 (§10)
- **Severity:** Medium (by design)
- **Area:** Reporting / financial model
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs:58,104-112,131`, `Application/Services/ReturnService.cs` (paid/remaining update), KPI tooltip in `Presentation/ViewModels/ReportsViewModel.cs`
- **Current evidence from audit:** "`net_profit` = `SUM(sales.profit)` − date-ranged `lost_profit` − expenses − net salary. Returns never reduce the original sale's stored `profit`. Correct over time … but a single-day figure overstates profit if the matching return lands in a later period." "It's a deliberate accrual model and is internally consistent."
- **Problem summary:** Same-period netting is not done by design; a single-day profit can overstate if a return is in a later period.
- **Desired behavior:** Default: document the accrual model on the KPI tooltip so operators understand the figure. Optional (Medium): recognize profit on a cash/return-matched basis if same-period netting is desired — only with tests.
- **Implementation notes:** Default path is documentation only (tooltip text); do **not** change the model without an explicit decision and tests. Keep `lost_profit` computed by return date (`:104-112`) intact.
- **Step-by-step work plan:** 1) Add a KPI tooltip explaining accrual recognition (profit at sale, reversed at return by return date). 2) If model change chosen, scope separately with tests.
- **Edge cases to handle:** Return spanning month boundary; multiple partial returns (ties to P7-T02).
- **Tests to add/update:** None for the doc path. If model changed: same-period netting tests.
- **Manual verification steps:** Hover KPI → tooltip explains timing.
- **Acceptance criteria:** Tooltip documents the accrual model; no behavior change unless explicitly chosen with tests.
- **Estimated effort:** Small (doc) / Medium (model change).
- **Risk level:** Low (doc) / High (model change).
- **Dependencies:** Phase 1.
- **Rollback notes:** Remove tooltip text; model unchanged in default path.

#### P2-T04
- **Title:** Decide `net_cash_flow` naming/display/removal (accurate label vs. drop)
- **Source audit item ID:** M-6 (§6), L-1 (§7), §13 dead-output, §6 fix
- **Severity:** Medium
- **Area:** Reporting
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs:137-142`, `Application/Services/ReportService.cs:52`, `Tests/FinancialFlowTests.cs:310-333`, Presentation (currently no reader)
- **Current evidence from audit:** "`net_cash_flow` sums **all** `sale_payments`/`repair_payments` regardless of method, i.e. it's really 'net money flow across all methods,' not physical cash. It is computed and unit-tested (`FinancialFlowTests.cs:310`) but **not displayed** anywhere in the UI." L-1: "Dead output kept alive only by tests."
- **Problem summary:** `net_cash_flow` is mislabeled (mixes methods) and never surfaced.
- **Desired behavior:** Either (a) surface it with an accurate label (e.g. "صافي التدفق النقدي لكل الطرق" / net money flow across all methods), or (b) drop it. Decision must be explicit; if dropped, update the dependent reconciliation test (`FinancialFlowTests.cs:310-333`) so coverage of the underlying six-source reconciliation is preserved.
- **Implementation notes:** Coordinate with P2-T02 (per-method nets must reconcile to this figure). If option (b), keep the raw-SQL reconciliation concept under a renamed assertion rather than deleting test coverage outright.
- **Step-by-step work plan:** 1) Decide surface-with-label vs. remove. 2a) If surface: add accurately-labeled display in VM/View. 2b) If remove: delete the computed output and update `ReportService.cs:52` and the test to reconcile via the per-method/sources sum instead. 3) Ensure reconciliation still holds.
- **Edge cases to handle:** Tests currently depend on `net_cash_flow` — do not break the six-source reconciliation; keep it under the new shape.
- **Tests to add/update:** Update `FinancialFlowTests.cs:310-333` to match the chosen shape; keep asserting six-source reconciliation. Pairs with P7-T03.
- **Manual verification steps:** If surfaced, confirm the label is accurate and value matches per-method sum.
- **Acceptance criteria:** `net_cash_flow` is either accurately labeled and displayed, or removed with reconciliation coverage preserved; no orphan mislabeled figure remains.
- **Estimated effort:** Small.
- **Risk level:** Medium (touches reporting + tests).
- **Dependencies:** P2-T02 (reconciliation), pairs with P7-T03.
- **Rollback notes:** Revert label/removal; restore original test.

#### P2-T05
- **Title:** Note that price overrides are not separately audited (audit-trail gap)
- **Source audit item ID:** R-4 (§10)
- **Severity:** Low/Medium (note)
- **Area:** Financial audit trail
- **Files affected:** `Application/Services/SaleService.cs:141` (`_saleRepo.LogActivity` on success), `:278-289` (persisted item discount/markup amounts)
- **Current evidence from audit:** "plain sales are logged via `_saleRepo.LogActivity` only on success — fine; just note that price overrides themselves aren't separately audited (who discounted, by how much) beyond the persisted item discount/markup amounts."
- **Problem summary:** Who applied a price override and by how much is not separately recorded in the activity log (only the resulting persisted amounts exist).
- **Desired behavior:** Decision recorded: either (a) accept as-is (persisted amounts suffice for a single-till shop) and document, or (b) add an activity-log entry capturing actor + override magnitude when a discount/markup is applied.
- **Implementation notes:** This is a "note" in the audit, not a confirmed bug. Default: document acceptance; option (b) is an enhancement with a test if chosen.
- **Step-by-step work plan:** 1) Record decision. 2) If (b): log actor + discount/markup on override at `SaleService.cs:278-289`. 3) Keep success-only `LogActivity` semantics.
- **Edge cases to handle:** Zero override (no extra log noise); admin vs. capped employee.
- **Tests to add/update:** If (b): assert an override writes an activity-log row with actor and amount.
- **Manual verification steps:** If (b): apply a discount, inspect `activity_log`.
- **Acceptance criteria:** Decision documented; if enhanced, override actor + amount are auditable and tested.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** Phase 1.
- **Rollback notes:** Remove the extra log entry if added.

---

### Phase 3 — Schema, Data Access, and Repository Hardening

#### P3-T01
- **Title:** Stop swallowing real ALTER failures in `EnsureColumnExists`
- **Source audit item ID:** M-3 (§6), B-3 (§9), §14 swallow-all catch
- **Severity:** Medium
- **Area:** Database / schema
- **Files affected:** `Infrastructure/Data/DatabaseManager.cs:381-405` (esp. `:398,401`)
- **Current evidence from audit:** "`EnsureColumnExists` wraps the `ALTER TABLE` in `try/catch(Exception)` that only logs. A genuinely failed column add proceeds silently and later queries referencing that column will throw far from the cause." The `$"ALTER TABLE {table}…"` uses hard-coded identifiers (`:398`).
- **Problem summary:** A real ALTER failure is logged and ignored; downstream queries fail far from the cause.
- **Desired behavior:** Distinguish "column already exists" (benign, swallow) from real failures (rethrow/abort).
- **Implementation notes:** Inspect the exception/message to detect "duplicate column" and treat only that as benign; rethrow others. Coordinate with M-1 fail-fast (P1-T02) so a rethrow aborts startup cleanly.
- **Step-by-step work plan:** 1) Pre-check column existence (PRAGMA table_info) before ALTER. 2) If exists, no-op. 3) If ALTER throws for any other reason, rethrow. 4) Keep hard-coded identifier usage (no user input).
- **Edge cases to handle:** Concurrent add; already-exists race; SQLite-specific error messages; ensure idempotency for re-run migrations.
- **Tests to add/update:** Add to migration idempotency/failure test (P7-T06): adding an existing column is a no-op; a forced ALTER failure rethrows.
- **Manual verification steps:** Run against a DB missing a column → added; run twice → no error; force a failure → surfaced.
- **Acceptance criteria:** Benign "already exists" is silent; any real ALTER failure propagates (and, with M-1, aborts startup).
- **Estimated effort:** Small.
- **Risk level:** Medium (startup/schema path).
- **Dependencies:** P1-T02, P0-T06.
- **Rollback notes:** Restore the log-only catch; DB from backup.

#### P3-T02
- **Title:** Replace `SELECT *` over-fetch with explicit column lists (where chosen)
- **Source audit item ID:** L-3 (§7), §16 performance note
- **Severity:** Low
- **Area:** Data access / robustness
- **Files affected:** 26 occurrences across repositories, e.g. `Infrastructure/.../CustomerRepository.cs:77`, `Infrastructure/.../ProductRepository.cs:43`, `Infrastructure/Persistence/SaleRepository.cs:191`
- **Current evidence from audit:** "`SELECT *` over-fetch across repositories (26 occurrences). Harmless on local SQLite but a brittleness/clarity nit; column-list selects are safer against schema drift."
- **Problem summary:** `SELECT *` is brittle against schema drift and obscures intent.
- **Desired behavior:** Explicit column lists where it improves safety/clarity, matched to each mapper's expected columns. No behavior change to result mapping.
- **Implementation notes:** This is a robustness nit, not a bug. Convert incrementally; ensure each column list matches the reader/mapper exactly. Optional/low priority — may be deferred but must be tracked.
- **Step-by-step work plan:** 1) Enumerate the 26 `SELECT *` sites. 2) For each, list the columns the mapper reads. 3) Replace `*` with that list. 4) Build + test after each repository.
- **Edge cases to handle:** Mappers reading by ordinal vs. name; columns added by migrations; views.
- **Tests to add/update:** Rely on existing repo/financial tests to catch mapping regressions; add targeted assertions if a mapper is ambiguous.
- **Manual verification steps:** Smoke-test each affected screen (customers, products, sales).
- **Acceptance criteria:** Converted selects return identical data; build/tests green; no `SELECT *` remains in the chosen scope (or remaining ones are explicitly documented as deferred).
- **Estimated effort:** Medium (26 sites).
- **Risk level:** Low.
- **Dependencies:** Phase 1; ideally after P7 financial tests for the sales path.
- **Rollback notes:** Revert per-repository; mapping unchanged.

#### P3-T03
- **Title:** Improve raw-SQL readability/safety where recommended (move SQL to constants)
- **Source audit item ID:** §11 ReportRepository note ("move SQL to constants"), §14 parameterization confirmation
- **Severity:** Low
- **Area:** Data access / readability
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs` (large UNION-ALL queries), related repos with inline SQL
- **Current evidence from audit:** §11: "two large UNION-ALL report queries … move SQL to constants." §14: parameterized SQL everywhere; only `$"ALTER TABLE {table}…"` uses hard-coded identifiers — no injection.
- **Problem summary:** Large inline SQL blocks hurt readability; moving to named constants aids clarity and review (no security issue exists today).
- **Desired behavior:** Extract large SQL strings to clearly named `const`/`static readonly` fields; keep parameterization intact.
- **Implementation notes:** Readability-only; do not alter query semantics. Pairs naturally with the ReportRepository split (P4-T01).
- **Step-by-step work plan:** 1) Identify the large UNION-ALL queries. 2) Move to named constants. 3) Confirm identical SQL text/behavior.
- **Edge cases to handle:** Whitespace/formatting changes must not alter parameter order.
- **Tests to add/update:** Existing reporting/reconciliation tests must remain green.
- **Manual verification steps:** Reports render identical figures.
- **Acceptance criteria:** SQL moved to constants with no behavior change; tests green.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** Can fold into P4-T01.
- **Rollback notes:** Inline the SQL again.

---

### Phase 4 — Refactor and Simplification

> All Phase 4 tasks are **behavior-preserving**. For financially-sensitive areas, land the relevant Phase 7 tests first (see Dependency Map §4).

#### P4-T01
- **Title:** Split `ReportRepository` into focused query classes
- **Source audit item ID:** §11 (refactor), §14 KISS/SOLID
- **Severity:** Refactor
- **Area:** Architecture / data access
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs` (447 lines)
- **Current evidence from audit:** "`BuildSummaryRange` + two large UNION-ALL report queries + breakdowns in one class → Split into `SummaryQueries`, `OperationsLogQueries`, `PaymentBreakdownQueries`; move SQL to constants."
- **Problem summary:** One class mixes summary, operations-log, and breakdown queries.
- **Desired behavior:** Three focused query classes (`SummaryQueries`, `OperationsLogQueries`, `PaymentBreakdownQueries`) with SQL in constants; `IReportRepository` surface unchanged.
- **Implementation notes:** Pure extraction; keep public behavior identical. Fold P3-T03 here. Apply H-4 coalesce (P2-T02) before/at split so the new `PaymentBreakdownQueries` carries the fix.
- **Step-by-step work plan:** 1) Carve out the three concerns. 2) Move SQL to constants. 3) Re-wire `ReportRepository` to delegate (or replace) keeping the interface. 4) Build + full test run.
- **Edge cases to handle:** Shared helpers (`BuildSummaryRange`, `GetMethodSum`); ensure reconciliation queries unchanged.
- **Tests to add/update:** All existing reporting tests + new P7-T03 reconciliation must pass unchanged.
- **Manual verification steps:** Reports produce identical numbers pre/post split.
- **Acceptance criteria:** Behavior identical; classes split; tests green; `net_cash_flow`/per-method reconciliation preserved.
- **Estimated effort:** Medium.
- **Risk level:** Medium (reporting — gated by tests).
- **Dependencies:** P2-T02, P7-T03 recommended first.
- **Rollback notes:** Revert to single class.

#### P4-T02
- **Title:** Extract `ReturnRepository` from `SaleRepository`
- **Source audit item ID:** §11 (refactor)
- **Severity:** Refactor
- **Area:** Architecture / data access
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs` (480 lines)
- **Current evidence from audit:** "Sales + sale items + returns + report queries in one repo → Extract `ReturnRepository` (returns/return_items already have their own interface boundary)."
- **Problem summary:** Returns logic lives inside `SaleRepository`.
- **Desired behavior:** A dedicated `ReturnRepository` owning returns/return_items; `SaleRepository` slimmed; interfaces preserved.
- **Implementation notes:** returns/return_items already have an interface boundary — move implementations behind it. Behavior-preserving.
- **Step-by-step work plan:** 1) Move returns/return_items methods to `ReturnRepository`. 2) Wire DI. 3) Build + test.
- **Edge cases to handle:** Shared transaction enlistment via `DatabaseManager` (respect H-1 model); return math guards (`RefundValidator.cs:13`, `ReturnService.cs:63,110,132-135`).
- **Tests to add/update:** Existing returns tests + P7-T02 partial/repeated returns must pass.
- **Manual verification steps:** Process a return; verify stock/paid/remaining unchanged behavior.
- **Acceptance criteria:** Returns behavior identical; repository split; tests green.
- **Estimated effort:** Medium.
- **Risk level:** Medium (financial path — gated by tests).
- **Dependencies:** P7-T02 recommended first.
- **Rollback notes:** Re-merge into `SaleRepository`.

#### P4-T03
- **Title:** Extract `CartModel` and `PriceEditPolicy` from `POSViewModel`
- **Source audit item ID:** §11 (refactor)
- **Severity:** Refactor
- **Area:** Presentation / MVVM
- **Files affected:** `Presentation/ViewModels/POSViewModel.cs` (468 lines; pricing mirror at `:292,317`; checkout `:363`; confirm `:229`)
- **Current evidence from audit:** "Cart, search, pricing-policy, checkout, printing in one VM → Extract a `CartModel` and a `PriceEditPolicy` (UI mirror of `PriceLimitValidator`)."
- **Problem summary:** The POS VM concentrates too many responsibilities.
- **Desired behavior:** A `CartModel` (cart state/items) and a `PriceEditPolicy` (UI mirror of `PriceLimitValidator`) extracted; VM orchestrates.
- **Implementation notes:** `PriceEditPolicy` must stay a faithful mirror of server-side `PriceLimitValidator` (below-cost universal, ceilings). Behavior-preserving.
- **Step-by-step work plan:** 1) Extract `CartModel`. 2) Extract `PriceEditPolicy` mirroring validator rules at `:292,317`. 3) Re-point VM. 4) Build + test + manual POS smoke.
- **Edge cases to handle:** Below-cost block (admin included); ceiling for capped employees; confirmation dialog (`:229`) preserved.
- **Tests to add/update:** Existing POS/financial tests; add VM-policy unit tests mirroring validator cases if feasible.
- **Manual verification steps:** Add items, edit price below cost (blocked), over-ceiling (blocked), checkout cash works.
- **Acceptance criteria:** POS behavior identical; classes extracted; tests green.
- **Estimated effort:** Medium.
- **Risk level:** Medium (UI pricing mirror — gated by manual + tests).
- **Dependencies:** P2-T01 (invoice-discount decision), P7 financial tests recommended.
- **Rollback notes:** Re-inline into the VM.

#### P4-T04
- **Title:** Extract `ReceiptDocumentBuilder` from `ReceiptPrintService`
- **Source audit item ID:** §11 (refactor)
- **Severity:** Refactor
- **Area:** Printing
- **Files affected:** `Infrastructure/Printing/ReceiptPrintService.cs` (471 lines)
- **Current evidence from audit:** "Document-building + pagination + printing → Extract a `ReceiptDocumentBuilder`."
- **Problem summary:** Document construction is entangled with printing/pagination.
- **Desired behavior:** A `ReceiptDocumentBuilder` produces the document; the service handles pagination/printing.
- **Implementation notes:** Behavior-preserving. The benign empty `catch {}` fallbacks (`ReceiptPrintService.cs:125,165,325`) are best-effort (logo/page-media) — keep them; optionally add a one-line comment (see P6-T11).
- **Step-by-step work plan:** 1) Extract builder. 2) Keep pagination/printing in service. 3) Build + print smoke test.
- **Edge cases to handle:** Logo-load failure path; page media size fallback; RTL/Arabic layout.
- **Tests to add/update:** No financial tests; rely on manual print verification.
- **Manual verification steps:** Print a sample receipt; compare layout to baseline.
- **Acceptance criteria:** Printed output identical; builder extracted; build green.
- **Estimated effort:** Medium.
- **Risk level:** Low.
- **Dependencies:** none (independent).
- **Rollback notes:** Re-merge builder into service.

#### P4-T05
- **Title:** Split `DatabaseManager` (connection/transaction core vs. seeding/number-generation)
- **Source audit item ID:** §11 (refactor), §14 KISS/SOLID god-object
- **Severity:** Refactor
- **Area:** Infrastructure / data
- **Files affected:** `Infrastructure/Data/DatabaseManager.cs` (452 lines)
- **Current evidence from audit:** "Connection + transactions + query helpers + schema/seed/migrate-branding + number generators → Split connection/transaction core from seeding/number-generation." §14: "god-object."
- **Problem summary:** `DatabaseManager` does too much.
- **Desired behavior:** Connection/transaction/query core separated from schema-seed/branding and number generation; behavior preserved.
- **Implementation notes:** Coordinate with H-1 decision (P1-T05) — the connection/transaction core is exactly the concurrency surface. Land the H-1 decision first so the split reflects it. Keep `DatabaseManager.Instance` semantics unless H-1 chose unit-of-work.
- **Step-by-step work plan:** 1) Carve number-generation + seeding/branding out. 2) Keep connection/tx/query core (with H-1 guard if chosen). 3) Re-wire callers. 4) Build + full test.
- **Edge cases to handle:** All repositories depend on `DatabaseManager.Instance`; preserve transaction enlistment; preserve M-1/M-3 fail-fast added earlier.
- **Tests to add/update:** Full suite must remain green; migration idempotency (P7-T06).
- **Manual verification steps:** App starts, seeds, generates numbers (invoice/repair numbers) correctly.
- **Acceptance criteria:** Behavior identical; responsibilities separated; tests green.
- **Estimated effort:** Large.
- **Risk level:** Medium-High (central component — gated by tests + H-1 decision).
- **Dependencies:** P1-T02, P1-T05, P3-T01, P7-T06.
- **Rollback notes:** Re-merge; DB from backup if seed touched.

#### P4-T06
- **Title:** Promote a single `WithTransaction` / unit-of-work helper and delete duplicated boilerplate
- **Source audit item ID:** §12 (simplification), §11 MaintenanceService note
- **Severity:** Simplification
- **Area:** Application services
- **Files affected:** `Application/Services/MaintenanceService.cs` (×7), `Application/Services/SaleService.cs`, `Application/Services/ExpenseService.cs`, `Application/Services/SupplierService.cs`, `Application/Services/EmployeeService.cs` (`:134` `RunSalaryWrite` exemplar), `Application/Services/AuthService.cs`; new `IDbTransactionManager.Execute(Action)` / `Execute<T>(Func<T>)`
- **Current evidence from audit:** "The `BeginTransaction → try → Commit → catch → Rollback → throw` block is copy-pasted ~15×. `EmployeeService.RunSalaryWrite` (`:134`) already shows the pattern — promote a single `IDbTransactionManager.Execute(...)` helper and delete the duplication."
- **Problem summary:** Transaction boilerplate is duplicated ~15×.
- **Desired behavior:** One reusable transaction helper; all 15 call sites use it; behavior identical (commit on success, rollback + rethrow on error).
- **Implementation notes:** Model after `RunSalaryWrite`. Respect H-1 concurrency model (re-entrancy guard). Behavior-preserving — same commit/rollback semantics.
- **Step-by-step work plan:** 1) Add `Execute(Action)`/`Execute<T>(Func<T>)`. 2) Replace each of the ~15 blocks. 3) Build + full test after each service.
- **Edge cases to handle:** Nested/re-entrant transactions (H-1 guard); exceptions translated to `Success=false` vs. rethrow — preserve each call site's existing contract.
- **Tests to add/update:** Existing service tests (sales, returns, maintenance, supplier, salary, expense) must remain green.
- **Manual verification steps:** Run a sale, return, maintenance op, salary write, expense — all commit/rollback correctly.
- **Acceptance criteria:** Boilerplate removed at all ~15 sites; behavior identical; tests green.
- **Estimated effort:** Medium.
- **Risk level:** Medium (touches every money mutation — gated by tests).
- **Dependencies:** P1-T05 (H-1), P7 financial tests recommended first.
- **Rollback notes:** Revert per-service to inline blocks.

#### P4-T07
- **Title:** Consolidate duplicate percent helpers
- **Source audit item ID:** §12 (simplification), §14 DRY
- **Severity:** Simplification
- **Area:** Core / shared helpers
- **Files affected:** `Core/Accounting/FinancialCalculator.cs:22-32` (`CalculateDiscountPercent/CalculateMarkupPercent`), `Shared/Helpers/Utilities.cs:327-337` (`Calculations.CalculateDiscountPercent/…`)
- **Current evidence from audit:** "`FinancialCalculator.CalculateDiscountPercent/CalculateMarkupPercent` duplicate `Calculations.CalculateDiscountPercent/…`. Consolidate."
- **Problem summary:** Two identical percent-helper implementations.
- **Desired behavior:** One canonical implementation; the other delegates or is removed; all callers updated.
- **Implementation notes:** Percentages are `double` (not money) per §10 — keep that. Choose one home (likely `Calculations` in Shared) and delegate.
- **Step-by-step work plan:** 1) Pick canonical. 2) Update callers of the duplicate. 3) Remove/redirect the duplicate. 4) Build + test.
- **Edge cases to handle:** Divide-by-zero in percent calc; rounding parity between the two implementations (verify identical before consolidating).
- **Tests to add/update:** Add a small test asserting both former entry points yield identical results (or that callers still compute correct percentages).
- **Manual verification steps:** Discount/markup percentages display unchanged in POS.
- **Acceptance criteria:** Single implementation; identical results; tests green.
- **Estimated effort:** Small.
- **Risk level:** Low-Medium (financial-adjacent — verify parity).
- **Dependencies:** Phase 1.
- **Rollback notes:** Restore the second implementation.

#### P4-T08
- **Title:** Extract duplicated customer get-or-create logic to `CustomerService`
- **Source audit item ID:** §12 (simplification), §14 DRY
- **Severity:** Simplification
- **Area:** Application services
- **Files affected:** `Application/Services/SaleService.cs:219` (`HandleCustomer`), `Application/Services/MaintenanceService.cs:33` (`ResolveCustomer`), `CustomerService`
- **Current evidence from audit:** "`SaleService.HandleCustomer` (`:219`) and `MaintenanceService.ResolveCustomer` (`:33`) are near-identical get-or-create-by-phone; extract to `CustomerService`."
- **Problem summary:** Near-identical get-or-create-by-phone logic in two services.
- **Desired behavior:** A single `CustomerService` method used by both; behavior identical.
- **Implementation notes:** Preserve the exact get-or-create-by-phone semantics; behavior-preserving.
- **Step-by-step work plan:** 1) Add/centralize method on `CustomerService`. 2) Point both services at it. 3) Build + test.
- **Edge cases to handle:** Missing/duplicate phone; new vs. existing customer; transaction enlistment within sale/maintenance flows.
- **Tests to add/update:** Existing sale/maintenance tests; add a get-or-create test if not covered.
- **Manual verification steps:** Create a sale and a maintenance order with a new and an existing phone.
- **Acceptance criteria:** Both flows use one method; behavior identical; tests green.
- **Estimated effort:** Small.
- **Risk level:** Low-Medium.
- **Dependencies:** Phase 1.
- **Rollback notes:** Restore per-service methods.

#### P4-T09
- **Title:** Review `GetDailySummary`/`GetMonthlySummary` on `SaleRepository` (overlap with `ReportRepository`)
- **Source audit item ID:** §12 (simplification) — overlaps §13 dead-code (verify)
- **Severity:** Simplification / dead-code candidate
- **Area:** Data access
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:358,372`
- **Current evidence from audit:** "`GetDailySummary`/`GetMonthlySummary` on `SaleRepository` (`:358,372`) overlap the canonical `ReportRepository` summary and appear unused by the reports UI — candidates to remove (verify)."
- **Problem summary:** Duplicate summary methods that appear unused.
- **Desired behavior:** Verified decision: if no caller (incl. tests/XAML/reflection), schedule removal in Phase 6; otherwise consolidate to the `ReportRepository` summary.
- **Implementation notes:** This is a **verify-before-removal** task; do not delete here. Hand the removal to Phase 6 (P6) if confirmed unused.
- **Step-by-step work plan:** 1) Grep all callers across tree. 2) If none, mark for Phase 6 removal. 3) If some, plan consolidation.
- **Edge cases to handle:** Reflection/XAML binding usage; test-only usage.
- **Tests to add/update:** none here (removal handled in Phase 6 with verification).
- **Manual verification steps:** Confirm reports UI uses `ReportRepository` summaries, not these.
- **Acceptance criteria:** Usage status documented; removal/consolidation routed appropriately.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** Build/test green (Phases 1–3).
- **Rollback notes:** N/A (no change here).

---

### Phase 5 — UI/UX and Reports Polish

#### P5-T01
- **Title:** Add date-range picker for daily/monthly reports
- **Source audit item ID:** §8 Reports row, §1 risk #8
- **Severity:** UI/UX
- **Area:** Presentation / reports
- **Files affected:** `Presentation/ViewModels/ReportsViewModel.cs:167,176,253,267`, corresponding Reports View(s), `GetDailySummary`/`GetPeriodSummary` consumers
- **Current evidence from audit:** "Daily/monthly reports hardwire `DateTime.Today`/current month; no date picker → Add start/end date inputs feeding `GetDailySummary`/`GetPeriodSummary`."
- **Problem summary:** Cannot review an arbitrary past day/month.
- **Desired behavior:** Start/end (or single-day/month) date inputs drive the summaries.
- **Implementation notes:** Feed selected dates into existing `GetDailySummary`/`GetPeriodSummary`; do not change the financial math.
- **Step-by-step work plan:** 1) Add date inputs to the Reports view. 2) Bind to VM properties. 3) Pass selected range to summary calls. 4) Default to today/current month for parity.
- **Edge cases to handle:** Start > end; future dates; empty range; locale/RTL date display.
- **Tests to add/update:** VM test: selecting a past range calls summaries with those dates.
- **Manual verification steps:** Pick a past day/month; verify figures match that period.
- **Acceptance criteria:** Reports reflect the chosen range; default behavior unchanged; build/tests green.
- **Estimated effort:** Medium.
- **Risk level:** Low (no financial-math change).
- **Dependencies:** Phase 2.
- **Rollback notes:** Revert VM/View changes; reports return to today/current-month.

#### P5-T02
- **Title:** Add user-selectable date range to the Returns report
- **Source audit item ID:** §8 Returns report row
- **Severity:** UI/UX
- **Area:** Presentation / reports
- **Files affected:** `Presentation/ViewModels/ReportsViewModel.cs:304`
- **Current evidence from audit:** "Fixed last-30-days window, not user-selectable → Add date range."
- **Problem summary:** Returns report is locked to last 30 days.
- **Desired behavior:** User-selectable date range for the returns report.
- **Implementation notes:** Reuse the date-range control from P5-T01.
- **Step-by-step work plan:** 1) Add range inputs for returns. 2) Pass to the returns query. 3) Default last-30-days for parity.
- **Edge cases to handle:** Start > end; empty range; no returns in range (empty state).
- **Tests to add/update:** VM test: returns report honors selected range.
- **Manual verification steps:** Select a past range; verify returns listed match it.
- **Acceptance criteria:** Returns report honors selected range; default unchanged; tests green.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P5-T01 (shared control).
- **Rollback notes:** Revert to fixed window.

#### P5-T03
- **Title:** Implement report export (CSV/XLSX) or hide the export button
- **Source audit item ID:** §8 Reports export row, §1 risk (stubbed export)
- **Severity:** UI/UX
- **Area:** Presentation / reports
- **Files affected:** `Presentation/ViewModels/ReportsViewModel.cs:395-398` (`ExportReport` stub: "ميزة التصدير قيد التطوير")
- **Current evidence from audit:** "`ExportReport` is a stub … Advertised export does nothing → Implement CSV/XLSX export or hide the button."
- **Problem summary:** Export button does nothing.
- **Desired behavior:** Either a working CSV/XLSX export of the current report, or the button is hidden until implemented.
- **Implementation notes:** Decision task. If implementing, reuse any existing CSV/XLSX patterns in the codebase (the project already has CSV/XLSX purchase import per git history). Default low-risk path: hide the button.
- **Step-by-step work plan:** 1) Decide implement vs. hide. 2a) If implement: export current report rows to CSV/XLSX with a save dialog. 2b) If hide: remove/collapse the button until ready. 
- **Edge cases to handle:** Empty report; file-in-use; RTL/Arabic encoding (UTF-8 BOM for Excel); large reports.
- **Tests to add/update:** If implemented: export-content test (header + rows match the report).
- **Manual verification steps:** Export a report → file opens in Excel with correct data; or button is no longer visible.
- **Acceptance criteria:** No "coming soon" stub remains reachable; either export works or the control is hidden.
- **Estimated effort:** Medium (implement) / Trivial (hide).
- **Risk level:** Low.
- **Dependencies:** Phase 2.
- **Rollback notes:** Revert export code or restore the button.

#### P5-T04
- **Title:** Optional richer card-style KPI print layout (resolve TODO)
- **Source audit item ID:** §8 Reports print row, L-6 (§7), §13 open TODO
- **Severity:** UI/UX (optional)
- **Area:** Presentation / printing
- **Files affected:** `Presentation/ViewModels/ReportsViewModel.cs:456` (TODO)
- **Current evidence from audit:** "KPI print is a plain two-column table (TODO acknowledges a richer layout) … Optional card-style print layout."
- **Problem summary:** KPI printout is functional but plain; a TODO acknowledges a richer layout.
- **Desired behavior:** Either implement a card-style print layout, or resolve the TODO explicitly (document as intentionally deferred and remove/annotate the stale TODO).
- **Implementation notes:** Optional polish. If deferring, the TODO at `:456` should be turned into a tracked note rather than left dangling (ties to L-6 cleanup, P6-T12).
- **Step-by-step work plan:** 1) Decide implement vs. defer. 2a) Implement card-style print. 2b) Defer: convert TODO to a documented decision.
- **Edge cases to handle:** Page breaks; RTL; many KPI cards.
- **Tests to add/update:** none (manual print verification).
- **Manual verification steps:** Print KPIs; confirm layout.
- **Acceptance criteria:** Print layout improved or TODO explicitly resolved/annotated.
- **Estimated effort:** Medium (implement) / Trivial (defer).
- **Risk level:** Low.
- **Dependencies:** none.
- **Rollback notes:** Revert layout; restore prior TODO if needed.

#### P5-T05
- **Title:** Display the per-method "other/unknown" bucket card
- **Source audit item ID:** §8 Per-method KPI cards row (display side of H-4)
- **Severity:** UI/UX
- **Area:** Presentation / reports
- **Files affected:** `Presentation/ViewModels/ReportsViewModel.cs:222-232`
- **Current evidence from audit:** "Money under non-canonical methods is not shown (see H-4) … Add 'other' bucket + coalesce."
- **Problem summary:** The per-method KPI cards have no place to show non-canonical/unknown money.
- **Desired behavior:** An "other/unknown" (`غير محدد`) KPI card displays the bucket produced by P2-T02 so the per-method view is complete.
- **Implementation notes:** This is the **display** half; the data half is P2-T02. Implement after P2-T02.
- **Step-by-step work plan:** 1) Add the "other" card to the per-method KPI section. 2) Bind to the bucket from P2-T02. 3) Confirm cards sum to `net_cash_flow`.
- **Edge cases to handle:** Zero "other" (show 0, not hidden); label clarity (`غير محدد`).
- **Tests to add/update:** Covered by P7-T03 reconciliation (data); VM binding sanity check.
- **Manual verification steps:** With a non-canonical row present, the "other" card shows it and totals reconcile on screen.
- **Acceptance criteria:** Per-method cards (incl. other) reconcile to `net_cash_flow` in the UI.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P2-T02.
- **Rollback notes:** Remove the card.

---

### Phase 6 — Dead Code, Dead Files, and Cleanup

> **Every removal task below MUST begin with a usage-search verification step** (Grep across the whole tree for callers, including XAML bindings and reflection). Only remove after confirming zero usage AND after build/test are green (Phases 1–3). Items the audit calls "false positive" or "not dead" are explicitly preserved.

#### P6-T01
- **Title:** Verify then remove `GetOperationsReport` (and its interface/service decls)
- **Source audit item ID:** §13 dead code, §1 risk #9
- **Severity:** Dead code (verify before removal)
- **Area:** Reporting
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs:223` (`GetOperationsReport`), `IReportService.GetOperationsReport`, `IReportRepository.GetOperationsReport`, `ReportService.GetOperationsReport`
- **Current evidence from audit:** "the legacy delivery-date operations log, **superseded** by `GetFinancialOperations`; no UI caller. **Verify before removal.**"
- **Problem summary:** Legacy operations-log method superseded by `GetFinancialOperations`, no UI caller.
- **Desired behavior:** After verifying zero callers (Presentation/tests/reflection/XAML), remove the method and its interface/service declarations.
- **Implementation notes:** Removal only after Grep confirms no caller and tests are green.
- **Step-by-step work plan:** 1) Grep whole tree for `GetOperationsReport`. 2) Confirm only declarations + no caller. 3) Remove method + interface + service decls. 4) Build + full test.
- **Edge cases to handle:** Reflection/DI registration referencing the method; test references.
- **Tests to add/update:** Ensure no test references it; full suite green post-removal.
- **Manual verification steps:** App builds and reports work without it.
- **Acceptance criteria:** Method + decls removed; zero callers existed; build/tests green.
- **Estimated effort:** Small.
- **Risk level:** Low (after verification).
- **Dependencies:** Phases 1–3 green; usage search.
- **Rollback notes:** Restore the method/decls.

#### P6-T02
- **Title:** Verify then remove `AddSupplierPurchase` (`[Obsolete]`) + interface decls
- **Source audit item ID:** §13 dead code, §1 risk #9
- **Severity:** Dead code (verify before removal)
- **Area:** Supplier
- **Files affected:** `Application/Services/SupplierService.cs:108` (`AddSupplierPurchase`), `SupplierRepository.AddSupplierPurchase`, interface decls
- **Current evidence from audit:** "`[Obsolete]`, no caller. **Verify before removal.**"
- **Problem summary:** Obsolete supplier-purchase method with no caller.
- **Desired behavior:** Remove after verifying zero callers.
- **Implementation notes:** Removal only after Grep + green tests.
- **Step-by-step work plan:** 1) Grep `AddSupplierPurchase`. 2) Confirm `[Obsolete]` + no caller. 3) Remove method + repo impl + interface decls. 4) Build + test.
- **Edge cases to handle:** Tests referencing it; supplier import flow (confirm it uses a different path).
- **Tests to add/update:** Full suite green post-removal.
- **Manual verification steps:** Supplier purchase/import flows still work.
- **Acceptance criteria:** Removed; zero callers existed; build/tests green.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green; usage search.
- **Rollback notes:** Restore the method/decls.

#### P6-T03
- **Title:** Verify then remove `SaleRepository.GetUnpaidByCustomer` (`[Obsolete]`, credit-era)
- **Source audit item ID:** §13 dead code
- **Severity:** Dead code (verify before removal)
- **Area:** Sales
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:228`
- **Current evidence from audit:** "`[Obsolete]`, credit-era, no caller."
- **Problem summary:** Credit-era method, unused (credit was de-scoped).
- **Desired behavior:** Remove after verifying zero callers.
- **Implementation notes:** Removal only after Grep + green tests.
- **Step-by-step work plan:** 1) Grep `GetUnpaidByCustomer`. 2) Confirm no caller. 3) Remove. 4) Build + test.
- **Edge cases to handle:** Any reporting referencing unpaid balances (should not, since cash-only invariant).
- **Tests to add/update:** Full suite green.
- **Manual verification steps:** Build/tests pass.
- **Acceptance criteria:** Removed; zero callers; green.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green; usage search.
- **Rollback notes:** Restore method.

#### P6-T04
- **Title:** Verify then remove `SaleRepository.UpdateSaleFinancials`
- **Source audit item ID:** §13 dead code, §1 risk #9
- **Severity:** Dead code (verify before removal)
- **Area:** Sales
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:144`
- **Current evidence from audit:** "no callers found."
- **Problem summary:** Unused sale-financials updater.
- **Desired behavior:** Remove after verifying zero callers.
- **Implementation notes:** Removal only after Grep + green tests.
- **Step-by-step work plan:** 1) Grep `UpdateSaleFinancials`. 2) Confirm no caller. 3) Remove. 4) Build + test.
- **Edge cases to handle:** Confused with `UpdateSaleItemFinancialsAfterReturn` (separate, P6-T05) and `updatePaymentStatus` (P6-T13).
- **Tests to add/update:** Full suite green.
- **Manual verification steps:** Build/tests pass.
- **Acceptance criteria:** Removed; zero callers; green.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green; usage search.
- **Rollback notes:** Restore method.

#### P6-T05
- **Title:** Verify then remove `SaleRepository.UpdateSaleItemFinancialsAfterReturn` (`[Obsolete]`)
- **Source audit item ID:** §13 dead code
- **Severity:** Dead code (verify before removal)
- **Area:** Sales / returns
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:265`
- **Current evidence from audit:** "`[Obsolete]`, … no callers found."
- **Problem summary:** Obsolete post-return item-financials updater, unused.
- **Desired behavior:** Remove after verifying zero callers.
- **Implementation notes:** Confirm current returns flow (`ReturnService.cs:132-135`) does not depend on it.
- **Step-by-step work plan:** 1) Grep `UpdateSaleItemFinancialsAfterReturn`. 2) Confirm no caller. 3) Remove. 4) Build + test (esp. returns tests + P7-T02).
- **Edge cases to handle:** Returns math must remain correct after removal.
- **Tests to add/update:** P7-T02 partial/repeated returns green.
- **Manual verification steps:** Process a return; paid/remaining/stock correct.
- **Acceptance criteria:** Removed; zero callers; returns tests green.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green; P7-T02; usage search.
- **Rollback notes:** Restore method.

#### P6-T06
- **Title:** Verify then remove `SaleRepository.UpdateItemPayment`
- **Source audit item ID:** §13 dead code
- **Severity:** Dead code (verify before removal)
- **Area:** Sales
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:313`
- **Current evidence from audit:** "no callers found."
- **Problem summary:** Unused item-payment updater.
- **Desired behavior:** Remove after verifying zero callers.
- **Implementation notes:** Removal only after Grep + green tests.
- **Step-by-step work plan:** 1) Grep `UpdateItemPayment`. 2) Confirm no caller. 3) Remove. 4) Build + test.
- **Edge cases to handle:** Per-item payment tracking in returns/sales (confirm handled elsewhere).
- **Tests to add/update:** Full suite green.
- **Manual verification steps:** Build/tests pass.
- **Acceptance criteria:** Removed; zero callers; green.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green; usage search.
- **Rollback notes:** Restore method.

#### P6-T07
- **Title:** Execute the `net_cash_flow` output decision from P2-T04 (remove or keep-with-label)
- **Source audit item ID:** L-1 (§7), M-6 (§6), §13 dead-output
- **Severity:** Dead code / decision
- **Area:** Reporting
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs:137`, `Application/Services/ReportService.cs:52`, `Tests/FinancialFlowTests.cs:310-333`
- **Current evidence from audit:** L-1: "no ViewModel/View reads it. Dead output kept alive only by tests."
- **Problem summary:** `net_cash_flow` is computed but never displayed.
- **Desired behavior:** Apply the P2-T04 decision. If "remove": delete the output and update the reconciliation test to assert the six-source sum directly (do not lose coverage). If "keep-with-label": this becomes a no-op here (handled in P5/P2).
- **Implementation notes:** Do **not** delete output that tests rely on without migrating the test's reconciliation assertion. This task only executes the already-made decision.
- **Step-by-step work plan:** 1) Confirm P2-T04 decision. 2) If remove: drop computation + `ReportService.cs:52` forward; migrate `FinancialFlowTests.cs:310-333` to reconcile via sources/per-method. 3) Build + test.
- **Edge cases to handle:** Reconciliation coverage must survive removal.
- **Tests to add/update:** Updated reconciliation test (P7-T03 alignment).
- **Manual verification steps:** Reports unaffected; reconciliation test green.
- **Acceptance criteria:** Decision executed; no mislabeled/dead output remains; reconciliation coverage intact.
- **Estimated effort:** Small.
- **Risk level:** Low-Medium (test-coupled).
- **Dependencies:** P2-T04, P7-T03.
- **Rollback notes:** Restore output + original test.

#### P6-T08
- **Title:** Decide `payment_details` summary key (wire it forward or remove)
- **Source audit item ID:** L-2 (§7), §13 dead-output
- **Severity:** Dead code / decision
- **Area:** Reporting
- **Files affected:** `Infrastructure/Persistence/ReportRepository.cs:477` (set), `Application/Services/ReportService.cs` (`GetPeriodSummary` does not copy it forward)
- **Current evidence from audit:** "`payment_details` summary key set but never read — `ReportService.GetPeriodSummary` does not copy it forward."
- **Problem summary:** `payment_details` is set but never consumed.
- **Desired behavior:** Either copy it forward in `GetPeriodSummary` and surface it (useful with the H-4 per-method work), or remove the unused key.
- **Implementation notes:** Given P2-T02/P5-T05 add per-method/other display, consider wiring `payment_details` forward; otherwise remove.
- **Step-by-step work plan:** 1) Decide wire-forward vs. remove. 2a) Wire: copy in `GetPeriodSummary` and consume in VM. 2b) Remove: drop the key set at `:477`. 3) Build + test.
- **Edge cases to handle:** Consumers expecting the key; reconciliation parity.
- **Tests to add/update:** If wired: a test asserting `payment_details` reaches the summary.
- **Manual verification steps:** Reports unaffected or new detail visible.
- **Acceptance criteria:** Key is either consumed or removed; no dead key remains.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P2-T02 (related), Phases 1–3 green.
- **Rollback notes:** Restore/remove the key set.

#### P6-T09
- **Title:** Execute the `GetDailySummary`/`GetMonthlySummary` removal/consolidation decision (from P4-T09)
- **Source audit item ID:** §12 / §13 (verify) — SaleRepository summaries
- **Severity:** Dead code (verify before removal)
- **Area:** Data access
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:358,372`
- **Current evidence from audit:** "overlap the canonical `ReportRepository` summary and appear unused by the reports UI — candidates to remove (verify)."
- **Problem summary:** Duplicate, apparently-unused summary methods.
- **Desired behavior:** If P4-T09 verification found zero callers, remove both; else consolidate.
- **Implementation notes:** Removal only after P4-T09 confirmed no caller and tests green.
- **Step-by-step work plan:** 1) Use P4-T09 findings. 2) If unused: remove both. 3) Build + test.
- **Edge cases to handle:** Reflection/XAML/test callers.
- **Tests to add/update:** Full suite green.
- **Manual verification steps:** Reports use `ReportRepository` summaries only.
- **Acceptance criteria:** Removed/consolidated per verification; build/tests green.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P4-T09; Phases 1–3 green.
- **Rollback notes:** Restore methods.

#### P6-T10
- **Title:** Remove the debug leftover in `LanguageService`
- **Source audit item ID:** L-4 (§7), §1 (debug leftover)
- **Severity:** Low cleanup
- **Area:** Application services
- **Files affected:** `Application/Services/LanguageService.cs:111` (`System.Diagnostics.Debug.WriteLine(...)`)
- **Current evidence from audit:** "Debug leftover (`LanguageService.cs:111`)."
- **Problem summary:** Stray debug-write statement.
- **Desired behavior:** Remove the `Debug.WriteLine` line.
- **Implementation notes:** Trivial, no behavior change in release.
- **Step-by-step work plan:** 1) Confirm the line at `:111`. 2) Remove it. 3) Build.
- **Edge cases to handle:** Ensure no logic depends on its side effect (it doesn't).
- **Tests to add/update:** none.
- **Manual verification steps:** Build green; language switching still works.
- **Acceptance criteria:** Debug line gone; build green.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** Phases 1–3 green.
- **Rollback notes:** Re-add the line.

#### P6-T11
- **Title:** Annotate benign empty `catch {}` print/branding fallbacks (optional, no behavior change)
- **Source audit item ID:** §14 (empty catch blocks, "benign … leave as-is or add a one-line comment")
- **Severity:** Low cleanup (optional)
- **Area:** Printing
- **Files affected:** `Infrastructure/Printing/A4PrintBase.cs:362,376`, `Infrastructure/Printing/ReceiptPrintService.cs:125,165,325`, `Infrastructure/Printing/ReportPrintService.cs:57,68`
- **Current evidence from audit:** "benign best-effort fallbacks (logo load / page-media-size) confirmed by reading them — leave as-is or add a one-line comment."
- **Problem summary:** Empty catches read as suspicious without a comment.
- **Desired behavior:** Add a one-line explanatory comment to each (best-effort logo/page-media fallback) OR leave as-is by explicit decision. **No code/behavior change.**
- **Implementation notes:** Optional; documentation-only. Do not convert these to throwing.
- **Step-by-step work plan:** 1) Add a one-line comment at each site, or record the decision to leave as-is.
- **Edge cases to handle:** Keep the swallow behavior — these are intentional fallbacks.
- **Tests to add/update:** none.
- **Manual verification steps:** Printing still degrades gracefully (logo missing → still prints).
- **Acceptance criteria:** Each empty catch is either commented or explicitly recorded as intentional; behavior unchanged.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** none.
- **Rollback notes:** Remove comments.

#### P6-T12
- **Title:** Resolve the open TODO in `ReportsViewModel`
- **Source audit item ID:** L-6 (§7), §13 open TODO, §8 print row
- **Severity:** Low cleanup
- **Area:** Presentation / reports
- **Files affected:** `Presentation/ViewModels/ReportsViewModel.cs:456`
- **Current evidence from audit:** "Open TODO (`ReportsViewModel.cs:456`)."
- **Problem summary:** A dangling TODO (the KPI print layout).
- **Desired behavior:** Resolve it — either by implementing P5-T04 or by converting the TODO into a documented, tracked decision (not a bare TODO).
- **Implementation notes:** Tightly linked to P5-T04. If P5-T04 implements the layout, delete the TODO; if deferred, annotate clearly.
- **Step-by-step work plan:** 1) If P5-T04 done → remove TODO. 2) Else → replace with a documented note.
- **Edge cases to handle:** Don't leave a bare `TODO` with no context.
- **Tests to add/update:** none.
- **Manual verification steps:** No bare TODO remains at `:456`.
- **Acceptance criteria:** TODO resolved or converted to a documented decision.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** P5-T04.
- **Rollback notes:** Restore TODO text.

#### P6-T13
- **Title:** Rename `SaleRepository.updatePaymentStatus` to PascalCase (naming nit)
- **Source audit item ID:** §14 naming nit
- **Severity:** Low cleanup
- **Area:** Data access / style
- **Files affected:** `Infrastructure/Persistence/SaleRepository.cs:138`
- **Current evidence from audit:** "`SaleRepository.updatePaymentStatus` (`:138`) is camelCase, inconsistent with the rest of the C# API surface."
- **Problem summary:** A camelCase method name inconsistent with C# conventions.
- **Desired behavior:** Rename to `UpdatePaymentStatus` and update all callers.
- **Implementation notes:** The hard rules forbid file renames, not symbol renames; this is a method-symbol rename within a file. Update every caller in the same change. Build must stay green.
- **Step-by-step work plan:** 1) Grep `updatePaymentStatus`. 2) Rename method + all callers to `UpdatePaymentStatus`. 3) Build + test.
- **Edge cases to handle:** Reflection/XAML by-name usage (unlikely for a repo method); interface declaration if any.
- **Tests to add/update:** Full suite green; update any test referencing the old name.
- **Manual verification steps:** Build green; payment-status updates still work.
- **Acceptance criteria:** Method is PascalCase; all callers updated; build/tests green.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green.
- **Rollback notes:** Rename back.

#### P6-T14
- **Title:** Address `ExpenseService.SearchExpenses` culture-sensitive `ToLower()` (latent nit)
- **Source audit item ID:** L-5 (§7)
- **Severity:** Low cleanup
- **Area:** Application services
- **Files affected:** `Application/Services/ExpenseService.cs:50`
- **Current evidence from audit:** "uses `ToLower()` without InvariantCulture. Arabic is unaffected, but culture-sensitive casing is a latent nit."
- **Problem summary:** Culture-sensitive lowercasing in search.
- **Desired behavior:** Use `ToLowerInvariant()` (or `ToLower(CultureInfo.InvariantCulture)`) for the search comparison.
- **Implementation notes:** Behavior unchanged for Arabic; removes the latent culture bug. Pure nit fix.
- **Step-by-step work plan:** 1) Change `ToLower()` → `ToLowerInvariant()` at `:50`. 2) Build + test.
- **Edge cases to handle:** Mixed Latin/Arabic search terms; ensure search results unchanged for existing data.
- **Tests to add/update:** Optional: a search test with mixed-case Latin term.
- **Manual verification steps:** Search expenses by name; results as expected.
- **Acceptance criteria:** Invariant casing used; search behavior unchanged for Arabic; build/tests green.
- **Estimated effort:** Trivial.
- **Risk level:** Low.
- **Dependencies:** Phases 1–3 green.
- **Rollback notes:** Revert to `ToLower()`.

#### P6-T15
- **Title:** Record `BarcodeGenerator.cs:59` as a confirmed false positive (no action)
- **Source audit item ID:** L-7 (§7)
- **Severity:** None (false positive)
- **Area:** Core / barcode
- **Files affected:** `Core/.../BarcodeGenerator.cs:59` (review only)
- **Current evidence from audit:** "flagged by scan as float-equality but it's a `string` compare (`elements[i] == "w"`); **false positive**, no action."
- **Problem summary:** A scanner false positive (string compare, not float equality).
- **Desired behavior:** No change. Record explicitly that this is intentional/correct so it is not "fixed" by mistake.
- **Implementation notes:** Documentation-only entry; do not modify the line.
- **Step-by-step work plan:** 1) Note in the traceability matrix as "No action — false positive."
- **Edge cases to handle:** none.
- **Tests to add/update:** none.
- **Manual verification steps:** Confirm `:59` is a string comparison.
- **Acceptance criteria:** Logged as no-action false positive; file unchanged.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** none.
- **Rollback notes:** N/A.

---

### Phase 7 — Testing and Regression Coverage

> Each test task targets `Tests/AlJohary.ServiceHub.Tests.csproj`; run with `--tl:off`. Land tests that protect a fix **before** the corresponding refactor where indicated.

#### P7-T01
- **Title:** Multi-item invoice distribution test (≥3 items + invoice-level discount + last-item remainder)
- **Source audit item ID:** §17.1
- **Severity:** Testing gap
- **Area:** Sales financial distribution
- **Files affected (under test):** `Application/Services/SaleService.cs:155-192`; new test in `Tests/`
- **Current evidence from audit:** "assert `Σ item.TotalPrice == sale.TotalAmount` and `Σ item.Profit == sale.Profit` with an invoice-level discount and ≥3 items including the last-item remainder."
- **Problem summary:** No test pins the last-item-remainder allocation with an invoice-level discount.
- **Desired behavior:** A test creating a sale with ≥3 items and an invoice-level discount asserting exact sums.
- **Implementation notes:** Coordinate with P2-T01 (invoice discount may be validated/removed). If discount param removed, test the per-item path; if validated, include a within-limits discount.
- **Step-by-step work plan:** 1) Build a ≥3-item sale. 2) Apply an in-bounds invoice discount. 3) Assert `Σ item.TotalPrice == sale.TotalAmount` and `Σ item.Profit == sale.Profit`.
- **Edge cases to handle:** Rounding remainder on the last item; zero-profit item; cost==price item.
- **Tests to add/update:** New test (added).
- **Manual verification steps:** Test passes; sums exact.
- **Acceptance criteria:** Distribution sums assert exactly; no rounding drift.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P2-T01.
- **Rollback notes:** Remove the test.

#### P7-T02
- **Title:** Partial & repeated returns test (two `CreateReturn` calls)
- **Source audit item ID:** §17.2
- **Severity:** Testing gap
- **Area:** Returns
- **Files affected (under test):** `Application/Services/ReturnService.cs` (`:63,110,130,132-135`); new test
- **Current evidence from audit:** "return some units, then more; assert paid/remaining/stock and `lost_profit` across two `CreateReturn` calls."
- **Problem summary:** No test exercises repeated/partial returns end-to-end.
- **Desired behavior:** Return some units, then more; assert paid, remaining, stock, and `lost_profit` after each call.
- **Implementation notes:** Pre-merge of duplicate return lines (`:31-44`) and `Math.Min`/`Math.Max` guards should hold.
- **Step-by-step work plan:** 1) Create a multi-unit sale. 2) Return some units; assert. 3) Return more; assert cumulative paid/remaining/stock/lost_profit. 4) Attempt over-return; assert rejected.
- **Edge cases to handle:** Over-return guard (`RefundValidator.cs:13`); refund cap `Math.Min(sale.PaidAmount, totalCashRefund)`; stock restore.
- **Tests to add/update:** New test (added).
- **Manual verification steps:** Test passes.
- **Acceptance criteria:** Paid/remaining/stock/lost_profit correct across two returns; over-return rejected.
- **Estimated effort:** Medium.
- **Risk level:** Low.
- **Dependencies:** none (precedes P4-T02 return-repo split).
- **Rollback notes:** Remove the test.

#### P7-T03
- **Title:** Per-method reconciliation test incl. NULL/empty `payment_method`
- **Source audit item ID:** §17.3, locks down H-4
- **Severity:** Testing gap (high value)
- **Area:** Reporting reconciliation
- **Files affected (under test):** `Infrastructure/Persistence/ReportRepository.cs:434-477`, `:137-142`; new test
- **Current evidence from audit:** "assert `Σ(per-method inflow − outflow) == net_cash_flow`, including a row with NULL/empty `payment_method`, to lock down **H-4**."
- **Problem summary:** No test guards the per-method/total reconciliation across non-canonical methods.
- **Desired behavior:** Insert inflows/outflows including a NULL/empty-method row; assert per-method (incl. "other") nets sum to the total (`net_cash_flow` or its replacement from P2-T04).
- **Implementation notes:** Must pass after P2-T02 coalesce + other-bucket. If P2-T04 removed `net_cash_flow`, assert against the six-source sum instead.
- **Step-by-step work plan:** 1) Seed payments across canonical + a NULL/empty method. 2) Compute per-method nets + other. 3) Assert sum equals total. 4) Assert the "other" row is not dropped.
- **Edge cases to handle:** NULL vs. empty vs. whitespace; folded legacy `'كاش'`; supplier default `'نقدي'`.
- **Tests to add/update:** New test (added); aligns with P2-T02/P2-T04/P6-T07.
- **Manual verification steps:** Test passes; reconciliation exact.
- **Acceptance criteria:** Per-method (incl. other) nets reconcile to the total even with NULL/empty methods.
- **Estimated effort:** Medium.
- **Risk level:** Low.
- **Dependencies:** P2-T02 (and P2-T04 if `net_cash_flow` changed).
- **Rollback notes:** Remove the test.

#### P7-T04
- **Title:** Maintenance profit across multiple payments test (recognized once, capped)
- **Source audit item ID:** §17.4
- **Severity:** Testing gap
- **Area:** Maintenance reporting
- **Files affected (under test):** `Infrastructure/Persistence/ReportRepository.cs:149-217` (esp. `:201-208`); new test
- **Current evidence from audit:** "partial then final payment; assert profit recognized once and capped."
- **Problem summary:** No test pins payment-proportional maintenance-profit recognition across multiple payments.
- **Desired behavior:** Partial then final payment; assert profit recognized proportionally, once, and capped against legacy overpayment.
- **Implementation notes:** Labor = 100% margin; parts = `total_cost − purchase_cost*qty`.
- **Step-by-step work plan:** 1) Create a repair with labor + parts. 2) Make a partial payment; assert partial profit. 3) Make final payment; assert total profit recognized once and capped. 
- **Edge cases to handle:** Legacy overpayment cap; multiple payments double-count avoidance; parts vs. labor split.
- **Tests to add/update:** New test (added).
- **Manual verification steps:** Test passes.
- **Acceptance criteria:** Profit recognized proportionally, exactly once, capped.
- **Estimated effort:** Medium.
- **Risk level:** Low.
- **Dependencies:** none.
- **Rollback notes:** Remove the test.

#### P7-T05
- **Title:** Invoice-level discount guard test (post H-3 fix)
- **Source audit item ID:** §17.5, ties to H-3/R-1/B-1
- **Severity:** Testing gap
- **Area:** Sales financial control
- **Files affected (under test):** `Application/Services/SaleService.cs:75,84-93` (`CreateSaleInternal`); new test
- **Current evidence from audit:** "once **H-3** is fixed, assert an over-ceiling/below-cost invoice discount is rejected."
- **Problem summary:** No test guards the invoice-level discount control gap.
- **Desired behavior:** With validation in place (P2-T01 option a), assert an invoice discount that pushes total below cost or exceeds the actor's ceiling is rejected.
- **Implementation notes:** Only applicable if P2-T01 chose "validate". If P2-T01 chose "remove parameters", replace with a test asserting the parameters are gone / not callable with a discount (document the chosen shape).
- **Step-by-step work plan:** 1) Build a sale with an invoice discount below cost → assert rejected. 2) Build one exceeding a capped actor's ceiling → assert rejected. 3) Build an in-bounds discount → assert accepted with correct distribution.
- **Edge cases to handle:** Admin still cannot go below cost; exactly-at-floor; rounding.
- **Tests to add/update:** New test (added).
- **Manual verification steps:** Test passes.
- **Acceptance criteria:** Over-ceiling/below-cost invoice discounts rejected; valid ones accepted.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P2-T01.
- **Rollback notes:** Remove the test.

#### P7-T06
- **Title:** Migration idempotency / failure test (run all migrations twice; injected failure aborts)
- **Source audit item ID:** §17.6, ties to M-1/M-3/B-3
- **Severity:** Testing gap
- **Area:** Database / migrations
- **Files affected (under test):** `Presentation/AppBootstrapper.cs:103-118`, `Infrastructure/Data/DatabaseManager.cs:381-405`; new test
- **Current evidence from audit:** "run all migrations twice; assert no error and stable state."
- **Problem summary:** No test proves migrations are idempotent or that failures surface.
- **Desired behavior:** Running all migrations twice yields no error and stable schema; an injected migration/ALTER failure aborts (does not get swallowed).
- **Implementation notes:** Pairs with P1-T02 (fail-fast) and P3-T01 (ALTER rethrow). Use a temp DB.
- **Step-by-step work plan:** 1) Apply migrations to a temp DB. 2) Apply again; assert no error + identical schema. 3) Inject a failure; assert it propagates/aborts. 4) Assert adding an existing column is a no-op (P3-T01).
- **Edge cases to handle:** Migration005 intentional `catch {}` must remain green; idempotent column adds.
- **Tests to add/update:** New test (added).
- **Manual verification steps:** Test passes.
- **Acceptance criteria:** Double-run is clean and stable; real failures surface; benign already-exists is a no-op.
- **Estimated effort:** Medium.
- **Risk level:** Low.
- **Dependencies:** P1-T02, P3-T01.
- **Rollback notes:** Remove the test.

#### P7-T07
- **Title:** Restore-validation regression test (valid restores; invalid rejected, live DB untouched)
- **Source audit item ID:** §17 (restored-backup validation), ties to M-4
- **Severity:** Testing gap
- **Area:** Database / backup-restore
- **Files affected (under test):** `Infrastructure/Data/DatabaseManager.cs:73-81`; new test
- **Current evidence from audit:** §17 "Regression tests for restored backup validation if applicable"; M-4 fix.
- **Problem summary:** No test guards the new restore validation.
- **Desired behavior:** A valid backup restores; an invalid/wrong-schema file is rejected and the live DB is unchanged; a pre-restore safety copy is created.
- **Implementation notes:** Depends on P1-T03 implementation.
- **Step-by-step work plan:** 1) Restore a valid backup → success. 2) Attempt restore of a corrupt/non-SQLite file → rejected. 3) Assert live DB unchanged + safety copy exists.
- **Edge cases to handle:** Non-SQLite; valid SQLite/wrong schema; locked DB.
- **Tests to add/update:** New test (added).
- **Manual verification steps:** Test passes.
- **Acceptance criteria:** Invalid backups never replace the live DB; valid ones do; safety copy present.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P1-T03.
- **Rollback notes:** Remove the test.

#### P7-T08
- **Title:** Wire `dotnet test` into CI so the suite gates changes
- **Source audit item ID:** §17.7, M-2
- **Severity:** Testing gap
- **Area:** CI
- **Files affected:** `.github/workflows/build.yml:25-26`
- **Current evidence from audit:** "wire `dotnet test` into `build.yml` so these actually run (**M-2**)."
- **Problem summary:** Same as M-2; restated as a testing-gap closure to ensure the new tests run in CI.
- **Desired behavior:** CI executes the full suite (including all new P7 tests) and fails on any failure.
- **Implementation notes:** This is the same change as P1-T04; listed here for traceability of §17.7. Implement once (P1-T04); verify here that the new tests are included.
- **Step-by-step work plan:** 1) Confirm P1-T04 added the test step. 2) Confirm new P7 tests run in CI. 
- **Edge cases to handle:** Windows runner for WPF target.
- **Tests to add/update:** CI config only.
- **Manual verification steps:** CI run shows the full test count including new tests.
- **Acceptance criteria:** CI runs and gates on the complete suite.
- **Estimated effort:** Trivial (already done in P1-T04).
- **Risk level:** Low.
- **Dependencies:** P1-T04; all P7 tests added.
- **Rollback notes:** Revert `build.yml`.

---

### Phase 8 — Final Verification and Release Readiness

#### P8-T01
- **Title:** Full clean build (0 warnings / 0 errors)
- **Source audit item ID:** §3 build baseline, §19
- **Severity:** N/A (gate)
- **Area:** Build
- **Files affected:** none
- **Current evidence from audit:** Baseline was 0/0; must remain so.
- **Problem summary:** No new warnings/errors may be introduced.
- **Desired behavior:** `dotnet build AlJohary.ServiceHub.sln` → 0 warnings, 0 errors.
- **Implementation notes:** none.
- **Step-by-step work plan:** Run the build; compare to baseline.
- **Edge cases to handle:** New analyzer warnings from added code.
- **Tests to add/update:** none.
- **Manual verification steps:** Read build summary.
- **Acceptance criteria:** 0 warnings, 0 errors.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** All phases.
- **Rollback notes:** Address regressions before sign-off.

#### P8-T02
- **Title:** Full test suite green (≥67 + new tests)
- **Source audit item ID:** §3 test baseline, §17
- **Severity:** N/A (gate)
- **Area:** Testing
- **Files affected:** none
- **Current evidence from audit:** Baseline 67/67; new P7 tests added.
- **Problem summary:** All tests, old and new, must pass.
- **Desired behavior:** `dotnet test Tests\AlJohary.ServiceHub.Tests.csproj --tl:off` → 0 failed.
- **Implementation notes:** Count should exceed 67 by the number of new P7 tests.
- **Step-by-step work plan:** Run the suite; confirm 0 failures and increased count.
- **Edge cases to handle:** Flaky DB temp-file tests.
- **Tests to add/update:** none (verification).
- **Manual verification steps:** Read test summary.
- **Acceptance criteria:** 0 failed; total ≥ 67 + new tests.
- **Estimated effort:** Trivial.
- **Risk level:** None.
- **Dependencies:** Phase 7.
- **Rollback notes:** Fix failures before sign-off.

#### P8-T03
- **Title:** Manual financial smoke test
- **Source audit item ID:** §10 confirmed-correct paths, §19
- **Severity:** N/A (gate)
- **Area:** Financial / POS
- **Files affected:** none (runtime)
- **Current evidence from audit:** §10 lists the invariants to preserve (cash-only rejection, price floor/ceilings, distribution, maintenance profit, returns).
- **Problem summary:** Confirm core money paths still behave after all changes.
- **Desired behavior:** A cash sale, a return, a maintenance order with payments, a salary write/reversal, and an expense each behave per §10.
- **Implementation notes:** Use a disposable DB (from P0 backup workflow), not production.
- **Step-by-step work plan:** 1) Cash sale (under-paid/non-cash rejected). 2) Price below cost blocked. 3) Multi-item distribution sums. 4) Return adjusts stock/paid/remaining. 5) Maintenance profit across payments. 6) Salary reversal nets to zero. 7) Expense soft-delete excluded from totals.
- **Edge cases to handle:** Per §9/§10 guards.
- **Tests to add/update:** none (manual).
- **Manual verification steps:** As above; record outcomes.
- **Acceptance criteria:** All §10 invariants hold in the running app.
- **Estimated effort:** Medium.
- **Risk level:** Low.
- **Dependencies:** Phases 1–7.
- **Rollback notes:** N/A (read/observe).

#### P8-T04
- **Title:** Reports reconciliation check
- **Source audit item ID:** §10 (`net_cash_flow` reconciles), H-4/§17.3
- **Severity:** N/A (gate)
- **Area:** Reporting
- **Files affected:** none (runtime)
- **Current evidence from audit:** "`net_cash_flow` reconciles with raw SQL over all six in/out sources."
- **Problem summary:** Confirm per-method (incl. other) and total reconcile after H-4 work.
- **Desired behavior:** Per-method nets (cash/instapay/ewallet/other) sum to the total across the six sources; KPI cards match.
- **Implementation notes:** Mirrors P7-T03 but in the running app with realistic data.
- **Step-by-step work plan:** 1) Generate mixed-method inflows/outflows incl. a non-canonical one. 2) Compare per-method cards to the total. 3) Confirm no money dropped.
- **Edge cases to handle:** Non-canonical method appears in "other".
- **Tests to add/update:** none (manual; backed by P7-T03).
- **Manual verification steps:** Cards reconcile on screen.
- **Acceptance criteria:** Per-method + other nets reconcile to the total; nothing dropped.
- **Estimated effort:** Small.
- **Risk level:** Low.
- **Dependencies:** P2-T02, P5-T05, P7-T03.
- **Rollback notes:** N/A.

#### P8-T05
- **Title:** Git diff review — no unrelated changes, no secrets, audit report unchanged
- **Source audit item ID:** §15 secrets, plan safety rules
- **Severity:** N/A (gate)
- **Area:** Process / security
- **Files affected:** all changed files (review only)
- **Current evidence from audit:** §15 "no real keys/tokens committed"; plan rule "audit report is source of truth".
- **Problem summary:** Final scope and secret review before release.
- **Desired behavior:** `git diff` shows only intended changes; no secrets/keys added; `PROJECT_FULL_AUDIT_REPORT.md` unchanged; no committed build artifacts.
- **Implementation notes:** Confirm `.gitignore` still excludes `bin/obj/publish/.db`.
- **Step-by-step work plan:** 1) `git status --short` + `git diff`. 2) Scan for secrets. 3) Confirm audit report untouched. 4) Confirm no `bin/obj/.db` staged.
- **Edge cases to handle:** Accidental DB/backup files; accidental TODO leftovers.
- **Tests to add/update:** none.
- **Manual verification steps:** Read the diff end-to-end.
- **Acceptance criteria:** Only intended files changed; no secrets; audit report unchanged; no artifacts.
- **Estimated effort:** Small.
- **Risk level:** None.
- **Dependencies:** All phases.
- **Rollback notes:** Revert stray changes.

#### P8-T06
- **Title:** Final release checklist sign-off
- **Source audit item ID:** §18 roadmap, §19 verdict
- **Severity:** N/A (gate)
- **Area:** Release
- **Files affected:** none
- **Current evidence from audit:** §19 "Blockers for wider rollout: H-1, H-2."
- **Problem summary:** Confirm every audit item is resolved or explicitly deferred with rationale.
- **Desired behavior:** The §10 Final Checklist (below) is fully ticked; rollout blockers H-1/H-2 are resolved or consciously accepted.
- **Implementation notes:** Cross-check the traceability matrix (§8) — every row is Done or Deferred-with-reason.
- **Step-by-step work plan:** 1) Walk the traceability matrix. 2) Confirm each item's status. 3) Tick the final checklist.
- **Edge cases to handle:** Any "deferred" item must have a recorded reason.
- **Tests to add/update:** none.
- **Manual verification steps:** Checklist complete.
- **Acceptance criteria:** No audit issue is unaccounted for; checklist complete.
- **Estimated effort:** Small.
- **Risk level:** None.
- **Dependencies:** All phases.
- **Rollback notes:** N/A.

---

## 4. Dependency Map

**Hard ordering (must precede):**
- **P0-T01 → all code changes.** Nothing changes before a clean baseline + branch + DB backup (P0-T01..T07).
- **P1-T04 (CI runs tests) early** → so every later phase is gated by the suite.
- **P1-T02 (migration fail-fast)** before **P3-T01 (ALTER rethrow)** and **P7-T06 (migration test)** — they share the fail-fast behavior.
- **P1-T03 (restore validation)** before **P7-T07 (restore test)**.
- **P1-T05 (H-1 concurrency decision)** before **P4-T05 (DatabaseManager split)** and **P4-T06 (WithTransaction helper)** — both touch the transaction core.
- **P2-T01 (invoice-discount fix)** before **P7-T05 (invoice-discount guard test)** and informs **P7-T01**.
- **P2-T02 (coalesce + other bucket)** before **P5-T05 (other card display)**, **P7-T03 (reconciliation test)**, and **P8-T04 (reconciliation check)**.
- **P2-T04 (`net_cash_flow` decision)** before **P6-T07 (execute removal/keep)** and constrains **P7-T03**.
- **P4-T09 (verify summaries unused)** before **P6-T09 (remove/consolidate summaries)**.
- **P5-T04 (KPI print)** ↔ **P6-T12 (resolve TODO at `:456`)** — resolve together.

**Test-before-refactor (recommended, financial safety):**
- **P7-T02 (returns test)** before **P4-T02 (extract ReturnRepository)**.
- **P7-T01/P7-T05 (distribution/discount tests)** before **P4-T03 (POSViewModel split)** and **P4-T06 (WithTransaction)**.
- **P7-T03 (reconciliation)** before **P4-T01 (ReportRepository split)**.
- **P7-T06 (migration test)** before **P4-T05 (DatabaseManager split)**.

**Dead-code rule:** All Phase 6 removals depend on (a) Phases 1–3 build/test green AND (b) a usage-search verification step in the task itself. **H-2 (P1-T01)** and **M-2 (P1-T04)** can be done before any refactor. **Dead-code removal happens after build/test verification and usage search — last among code changes, before final verification.**

---

## 5. Risk Control Rules

**Financial logic changes (P2-T01, P2-T02, P2-T03, P2-T04, P4-T06, P4-T07, P4-T08):**
- Never change behavior without a paired test (P7) proving the invariant.
- Preserve the cash-only invariant (reject, not coerce), the universal below-cost floor, per-actor ceilings, and last-item-remainder distribution.
- Money stays `decimal`; percentages stay `double` and are never money.

**Database / migration changes (P1-T02, P3-T01, P4-T05, P7-T06):**
- Take a DB backup (P0-T06) first; document rollback.
- Fail fast on real errors; keep Migration005's intentional `catch {}` untouched.
- Migrations must stay idempotent (verified by P7-T06).

**Dead-code removal (all Phase 6 removals):**
- Mandatory usage search (Grep whole tree incl. XAML/reflection/tests) before deletion.
- Only after Phases 1–3 build/test are green.
- Preserve items the audit calls "not dead" (`TypedMessenger`/`FocusHelper`/`WindowResizer`) and "false positive" (`BarcodeGenerator.cs:59`).
- Removing output coupled to tests (`net_cash_flow`) requires migrating the test's coverage, not deleting it.

**UI-only changes (Phase 5):**
- No change to financial math — only inputs (date ranges) and display (cards, export, print).
- Default values preserve current behavior (today/current month/last-30-days).

**Test changes (Phase 7):**
- Add tests; do not weaken existing assertions to make changes pass.
- New tests must run in CI (P1-T04 / P7-T08).

**Refactor changes (Phase 4):**
- Behavior-preserving only; full suite must stay green before and after.
- Land protective tests first for financially-sensitive extractions (see §4).
- Interface surfaces unchanged unless explicitly part of the task.

**Backup/restore changes (P1-T03, P7-T07):**
- Validate the backup is SQLite + carries expected schema markers before overwriting.
- Always create a pre-restore safety copy.
- Never overwrite the live DB on validation failure.

**Concurrency-related changes (P1-T05, P4-T05, P4-T06):**
- H-1 is a **decision** first. Default to document-and-assert (low risk); a unit-of-work rewrite is opt-in with its own tests.
- Do not swap the DB engine as a side effect.
- Preserve the `ReportsViewModel.cs:65` Dispatcher marshaling that relies on single-threaded access until H-1 is resolved otherwise.

---

## 6. Implementation Order

Exact recommended task-by-task order (risk-minimizing):

**1. Safety baseline**
1. P0-T01 (clean status) → 2. P0-T02 (build) → 3. P0-T03 (tests) → 4. P0-T04 (branch) → 5. P0-T05 (DB/migration review) → 6. P0-T06 (DB backup) → 7. P0-T07 (rollback strategy).

**2. CI/tests gate**
8. P1-T04 (CI runs tests).

**3. Security & migration fail-fast**
9. P1-T01 (H-2 force password change) → 10. P1-T02 (M-1 migration fail-fast) → 11. P3-T01 (M-3 ALTER rethrow).

**4. Backup/restore validation**
12. P1-T03 (M-4 restore validation).

**5. Concurrency decision**
13. P1-T05 (H-1 decision + guard/doc).

**6. Financial correctness**
14. P2-T01 (H-3 invoice discount) → 15. P2-T03 (M-5 recognition timing doc) → 16. P2-T05 (R-4 audit-trail note).

**7. Reporting reconciliation**
17. P2-T02 (H-4 coalesce + other bucket) → 18. P2-T04 (M-6/L-1 `net_cash_flow` decision) → 19. P6-T08 (L-2 `payment_details` decision).

**8. Tests (lock in the fixes)**
20. P7-T01 → 21. P7-T02 → 22. P7-T03 → 23. P7-T04 → 24. P7-T05 → 25. P7-T06 → 26. P7-T07 → 27. P7-T08 (verify CI includes new tests).

**9. Refactors (behavior-preserving, tests now protect them)**
28. P3-T02 (SELECT* scoping) → 29. P3-T03 (SQL→constants) → 30. P4-T07 (percent helpers) → 31. P4-T08 (customer resolve) → 32. P4-T06 (WithTransaction) → 33. P4-T01 (ReportRepository split) → 34. P4-T02 (ReturnRepository) → 35. P4-T03 (CartModel/PriceEditPolicy) → 36. P4-T04 (ReceiptDocumentBuilder) → 37. P4-T05 (DatabaseManager split) → 38. P4-T09 (verify summaries unused).

**10. UX**
39. P5-T01 (daily/monthly date range) → 40. P5-T02 (returns date range) → 41. P5-T05 (other card) → 42. P5-T03 (export) → 43. P5-T04 (KPI print) → 44. P6-T12 (resolve TODO).

**11. Dead code cleanup (after build/test green + usage search)**
45. P6-T01 (GetOperationsReport) → 46. P6-T02 (AddSupplierPurchase) → 47. P6-T03 (GetUnpaidByCustomer) → 48. P6-T04 (UpdateSaleFinancials) → 49. P6-T05 (UpdateSaleItemFinancialsAfterReturn) → 50. P6-T06 (UpdateItemPayment) → 51. P6-T07 (net_cash_flow output) → 52. P6-T09 (SaleRepository summaries) → 53. P6-T10 (debug leftover) → 54. P6-T11 (annotate benign catches) → 55. P6-T13 (rename updatePaymentStatus) → 56. P6-T14 (InvariantCulture) → 57. P6-T15 (record false positive).

**12. Final verification**
58. P8-T01 (build) → 59. P8-T02 (tests) → 60. P8-T03 (financial smoke) → 61. P8-T04 (reports reconciliation) → 62. P8-T05 (diff/secrets/report-unchanged) → 63. P8-T06 (release checklist).

---

## 7. Verification Commands

Run after every phase (and the relevant subset after each task):

```bash
# Scope & cleanliness
git status --short

# Build (must stay 0 warnings / 0 errors)
dotnet build AlJohary.ServiceHub.sln

# Tests (target the Tests project with --tl:off; targeting the .sln hides results)
dotnet test Tests\AlJohary.ServiceHub.Tests.csproj --tl:off
```

Additional safe commands (use as useful, non-destructive):

```bash
# Confirm the audit report is unchanged (source of truth)
git diff --stat -- PROJECT_FULL_AUDIT_REPORT.md      # expect: no output

# Usage search before any Phase 6 removal (repeat per symbol)
#   e.g. GetOperationsReport, AddSupplierPurchase, GetUnpaidByCustomer,
#        UpdateSaleFinancials, UpdateSaleItemFinancialsAfterReturn,
#        UpdateItemPayment, GetDailySummary, GetMonthlySummary,
#        net_cash_flow, payment_details, updatePaymentStatus
#   (use the Grep tool across the whole tree, including .xaml)

# CI sanity (after P1-T04): confirm a test step exists in the workflow
git diff -- .github/workflows/build.yml

# Branch confirmation
git branch --show-current

# Confirm no build artifacts/db are staged
git status --short | grep -Ei '(bin/|obj/|publish/|\.db$)'   # expect: no output
```

Per-phase gate: after each phase, run the three core commands; do not advance while the build has new warnings or any test fails.

---

## 8. Traceability Matrix

Status legend: ☐ Planned · (all items start Planned). "Fix type": Code / Decision / Doc / Test / Config / CI / No-action.

| Audit item | Report section | Severity | File path(s) | Planned task ID(s) | Fix type | Phase | Status |
|---|---|---|---|---|---|---|---|
| Critical (none confirmed) | §4 | — | — | (none — confirmed clear; tracked here for completeness) | No-action | — | ☐ |
| H-1 concurrency / global connection | §5,§16,§19 | High | `DatabaseManager.cs:16,124,133,135,177,222,255,287`; `ReportsViewModel.cs:65` | P1-T05 (→P4-T05,P4-T06) | Decision/Code | 1 | ☐ |
| H-2 default admin password | §5,§15 | High | `DatabaseManager.cs:456`, `README.md:60`, `LoginWindow.xaml.cs` | P1-T01 | Code | 1 | ☐ |
| H-3 invoice-level discount unvalidated | §5 | High (latent) | `SaleService.cs:75,84-93,264`; `POSViewModel.cs:363`; `FinancialFlowTests.cs:84` | P2-T01, P7-T05 | Code/Test | 2/7 | ☐ |
| H-4 per-method KPI blind spot | §5 | High | `ReportRepository.cs:434-477`; `ReportsViewModel.cs:201-206,222-232,389-393` | P2-T02, P5-T05, P7-T03 | Code/Test | 2/5/7 | ☐ |
| M-1 migrations swallow failures | §6 | Medium | `AppBootstrapper.cs:103-118` | P1-T02, P7-T06 | Code/Test | 1/7 | ☐ |
| M-2 CI does not run tests | §6 | Medium | `.github/workflows/build.yml:25-26` | P1-T04, P7-T08 | CI | 1/7 | ☐ |
| M-3 silent ALTER failure | §6 | Medium | `DatabaseManager.cs:381-405` | P3-T01, P7-T06 | Code/Test | 3/7 | ☐ |
| M-4 backup/restore no validation | §6 | Medium | `DatabaseManager.cs:73-81` | P1-T03, P7-T07 | Code/Test | 1/7 | ☐ |
| M-5 profit-recognition timing | §6 | Medium (by design) | `ReportRepository.cs:58,104-112,131`; `ReturnService.cs` | P2-T03 | Doc/Decision | 2 | ☐ |
| M-6 `net_cash_flow` mislabeled | §6 | Medium | `ReportRepository.cs:137-142` | P2-T04, P6-T07 | Decision/Code | 2/6 | ☐ |
| L-1 `net_cash_flow` never surfaced | §7 | Low | `ReportRepository.cs:137`; `ReportService.cs:52` | P2-T04, P6-T07 | Decision/Code | 2/6 | ☐ |
| L-2 `payment_details` never read | §7 | Low | `ReportRepository.cs:477`; `ReportService.GetPeriodSummary` | P6-T08 | Decision/Code | 6 | ☐ |
| L-3 `SELECT *` over-fetch (26×) | §7,§16 | Low | `CustomerRepository.cs:77`, `ProductRepository.cs:43`, `SaleRepository.cs:191`, +23 | P3-T02 | Code | 3 | ☐ |
| L-4 debug leftover | §7 | Low | `LanguageService.cs:111` | P6-T10 | Code | 6 | ☐ |
| L-5 `ToLower()` not invariant | §7 | Low | `ExpenseService.cs:50` | P6-T14 | Code | 6 | ☐ |
| L-6 open TODO | §7 | Low | `ReportsViewModel.cs:456` | P6-T12 (→P5-T04) | Code/Doc | 6/5 | ☐ |
| L-7 barcode `==` (false positive) | §7 | None | `BarcodeGenerator.cs:59` | P6-T15 | No-action | 6 | ☐ |
| UI: reports date picker | §8 | UI/UX | `ReportsViewModel.cs:167,176,253,267` | P5-T01 | Code | 5 | ☐ |
| UI: reports export stub | §8 | UI/UX | `ReportsViewModel.cs:395-398` | P5-T03 | Code/Decision | 5 | ☐ |
| UI: KPI print layout | §8 | UI/UX | `ReportsViewModel.cs:456` | P5-T04, P6-T12 | Code/Doc | 5/6 | ☐ |
| UI: per-method "other" card | §8 | UI/UX | `ReportsViewModel.cs:222-232` | P5-T05 (data: P2-T02) | Code | 5 | ☐ |
| UI: returns report date range | §8 | UI/UX | `ReportsViewModel.cs:304` | P5-T02 | Code | 5 | ☐ |
| B-1 invoice discount below cost/ceiling | §9 | Latent | `SaleService.cs:75` (=H-3) | P2-T01, P7-T05 | Code/Test | 2/7 | ☐ |
| B-2 non-canonical method dropped | §9 | Reporting | `ReportRepository.cs:434` (=H-4) | P2-T02, P7-T03 | Code/Test | 2/7 | ☐ |
| B-3 migration/ALTER swallowed | §9 | Operational | `AppBootstrapper.cs:115`, `DatabaseManager.cs:401` (=M-1/M-3) | P1-T02, P3-T01, P7-T06 | Code/Test | 1/3/7 | ☐ |
| R-1 invoice discount unvalidated | §10 | Latent | `SaleService.cs:75` (=H-3) | P2-T01, P7-T05 | Code/Test | 2/7 | ☐ |
| R-2 per-method buckets drop money | §10 | Reporting | `ReportRepository.cs:434` (=H-4) | P2-T02, P7-T03 | Code/Test | 2/7 | ☐ |
| R-3 recognition timing | §10 | By design | `ReportRepository.cs:104-112` (=M-5) | P2-T03 | Doc | 2 | ☐ |
| R-4 price overrides not separately audited | §10 | Note | `SaleService.cs:141,278-289` | P2-T05 | Decision/Code | 2 | ☐ |
| Refactor: split ReportRepository | §11 | Refactor | `ReportRepository.cs` (447) | P4-T01 (+P3-T03) | Code | 4 | ☐ |
| Refactor: extract ReturnRepository | §11 | Refactor | `SaleRepository.cs` (480) | P4-T02 | Code | 4 | ☐ |
| Refactor: CartModel/PriceEditPolicy | §11 | Refactor | `POSViewModel.cs` (468) | P4-T03 | Code | 4 | ☐ |
| Refactor: ReceiptDocumentBuilder | §11 | Refactor | `ReceiptPrintService.cs` (471) | P4-T04 | Code | 4 | ☐ |
| Refactor: split DatabaseManager | §11 | Refactor | `DatabaseManager.cs` (452) | P4-T05 | Code | 4 | ☐ |
| Refactor: WithTransaction helper | §11,§12 | Refactor | `MaintenanceService.cs` +6 services | P4-T06 | Code | 4 | ☐ |
| Simplify: transaction boilerplate (~15×) | §12 | Simplification | `MaintenanceService.cs`×7, `SaleService.cs`, `ExpenseService.cs`, `SupplierService.cs`, `EmployeeService.cs:134`, `AuthService.cs` | P4-T06 | Code | 4 | ☐ |
| Simplify: duplicate percent helpers | §12 | Simplification | `FinancialCalculator.cs:22-32`; `Utilities.cs:327-337` | P4-T07 | Code | 4 | ☐ |
| Simplify: duplicate customer resolve | §12 | Simplification | `SaleService.cs:219`; `MaintenanceService.cs:33` | P4-T08 | Code | 4 | ☐ |
| Simplify: SaleRepo daily/monthly summaries | §12 | Simplification/Dead | `SaleRepository.cs:358,372` | P4-T09 (verify), P6-T09 (remove) | Code | 4/6 | ☐ |
| Dead: `GetOperationsReport` | §13 | Dead (verify) | `ReportRepository.cs:223` + I/Service decls | P6-T01 | Code | 6 | ☐ |
| Dead: `AddSupplierPurchase` | §13 | Dead (verify) | `SupplierService.cs:108`; `SupplierRepository`; interface | P6-T02 | Code | 6 | ☐ |
| Dead: `GetUnpaidByCustomer` | §13 | Dead (verify) | `SaleRepository.cs:228` | P6-T03 | Code | 6 | ☐ |
| Dead: `UpdateSaleFinancials` | §13 | Dead (verify) | `SaleRepository.cs:144` | P6-T04 | Code | 6 | ☐ |
| Dead: `UpdateSaleItemFinancialsAfterReturn` | §13 | Dead (verify) | `SaleRepository.cs:265` | P6-T05 | Code | 6 | ☐ |
| Dead: `UpdateItemPayment` | §13 | Dead (verify) | `SaleRepository.cs:313` | P6-T06 | Code | 6 | ☐ |
| Dead: `net_cash_flow` output | §13,§7 | Dead-output | `ReportRepository.cs:137`; `ReportService.cs:52` | P2-T04, P6-T07 | Decision/Code | 2/6 | ☐ |
| Dead: `payment_details` | §13,§7 | Dead-output | `ReportRepository.cs:477` | P6-T08 | Decision/Code | 6 | ☐ |
| Not-dead: TypedMessenger/FocusHelper/WindowResizer | §13 | Keep | `Shared/Helpers/*` | (no removal — preserve) | No-action | — | ☐ |
| Clean: broad `catch(Exception)` (swallow-all subset) | §14 | Clean code | `AppBootstrapper.cs:115`; `DatabaseManager.cs:401` | P1-T02, P3-T01 | Code | 1/3 | ☐ |
| Clean: benign empty `catch{}` (printing) | §14 | Clean code | `A4PrintBase.cs:362,376`; `ReceiptPrintService.cs:125,165,325`; `ReportPrintService.cs:57,68` | P6-T11 | Doc | 6 | ☐ |
| Clean: DatabaseManager god-object | §14,§11 | Clean code | `DatabaseManager.cs` | P4-T05 | Code | 4 | ☐ |
| Clean: `updatePaymentStatus` naming | §14 | Clean code | `SaleRepository.cs:138` | P6-T13 | Code | 6 | ☐ |
| Security: password storage (strong) | §15 | Info | `Utilities.cs:17-66` | (no action — confirmed strong) | No-action | — | ☐ |
| Security: default credential | §15 | High | `DatabaseManager.cs:456` (=H-2) | P1-T01 | Code | 1 | ☐ |
| Security: SQL injection (none) | §15 | Info | parameterized everywhere | (no action) | No-action | — | ☐ |
| Security: AuthZ guards (solid) | §15 | Info | `AuthService.cs:*` | (no action) | No-action | — | ☐ |
| Security: secrets (none) | §15 | Info | — | P8-T05 (final scan) | No-action | 8 | ☐ |
| Perf: maintenance correlated subqueries | §16 | Perf (watch) | `ReportRepository.cs:149` | (watch; covered by P4-T01 review + P7-T04) | Doc/Code | 4/7 | ☐ |
| Perf: `SELECT *` over-fetch | §16 | Perf (negligible) | (=L-3) | P3-T02 | Code | 3 | ☐ |
| Perf: global `_lock` serializes DB | §16 | Perf (acceptable) | `DatabaseManager.cs` (=H-1) | P1-T05 | Decision | 1 | ☐ |
| Test gap: multi-item distribution | §17.1 | Testing | `SaleService.cs:155` | P7-T01 | Test | 7 | ☐ |
| Test gap: partial/repeated returns | §17.2 | Testing | `ReturnService.cs` | P7-T02 | Test | 7 | ☐ |
| Test gap: per-method reconciliation | §17.3 | Testing | `ReportRepository.cs:434` | P7-T03 | Test | 7 | ☐ |
| Test gap: maintenance profit multi-payment | §17.4 | Testing | `ReportRepository.cs:201-208` | P7-T04 | Test | 7 | ☐ |
| Test gap: invoice-discount guard | §17.5 | Testing | `SaleService.cs` | P7-T05 | Test | 7 | ☐ |
| Test gap: migration idempotency/failure | §17.6 | Testing | `AppBootstrapper.cs`; `DatabaseManager.cs:381` | P7-T06 | Test | 7 | ☐ |
| Test gap: CI runs tests | §17.7 | Testing | `build.yml` | P1-T04, P7-T08 | CI | 1/7 | ☐ |
| Test gap: restore validation regression | §17 | Testing | `DatabaseManager.cs:73-81` | P7-T07 | Test | 7 | ☐ |

---

## 9. Non-Goals

This plan will **not**:
- Add features beyond what the audit identifies (no new modules, no scope expansion outside §5–§17 findings).
- Redesign the UI except for the specific UI/UX findings in §8 (date ranges, export, KPI print, "other" bucket).
- Replace the database engine. SQLite stays unless a deliberate **H-1** architecture decision (P1-T05) explicitly chooses a connection-per-unit-of-work model — and even then, engine replacement is out of scope.
- Remove any uncertain/"verify before removal" dead code without a usage-search verification step (all Phase 6 removals are gated).
- Change any financial behavior without an accompanying test (Phase 7).
- Modify, reformat, or re-author `PROJECT_FULL_AUDIT_REPORT.md` (it is the source of truth).
- "Fix" the L-7 `BarcodeGenerator.cs:59` false positive or convert benign best-effort `catch {}` print fallbacks into throwing code.
- Alter the deliberate accrual profit-recognition model (M-5) unless explicitly chosen, with tests.
- Commit, push, or open PRs unless the user explicitly asks.

---

## 10. Final Checklist

- [ ] Every audit issue is mapped to at least one task (verify against §8 — Critical/High/Medium/Low/UI-UX/bugs/financial risks/refactor/simplification/dead-code/clean-code/security/performance/testing all present).
- [ ] Every High and Medium risk has a concrete task (H-1..H-4, M-1..M-6).
- [ ] Every dead-code candidate has a usage-search verification step before removal (P6-T01..T09).
- [ ] Every financial behavior change has a paired test (P2 ↔ P7).
- [ ] Build passes with 0 warnings / 0 errors (P8-T01).
- [ ] Full test suite passes, count ≥ 67 + new tests (P8-T02).
- [ ] CI runs the test suite and gates changes (P1-T04 / P7-T08).
- [ ] No unrelated files changed; no secrets; `PROJECT_FULL_AUDIT_REPORT.md` unchanged (P8-T05).
- [ ] Final `git status` reviewed and clean of artifacts/DB files (P8-T05).
- [ ] H-1 and H-2 rollout blockers resolved or consciously accepted with rationale (P8-T06).
- [ ] Manual financial smoke test and reports reconciliation pass (P8-T03, P8-T04).
