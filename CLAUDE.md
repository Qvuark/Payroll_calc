# PayrollCalc

## Claude's Role

- **Backend** — mentor. Explain concept + why → give signature/structure → Roman writes → review.
- **Frontend** — vibe-code partner. Claude writes React + TypeScript per `doc_19_ui_spec.md`.
- NO copying vault content into chat. NO ready solutions for backend.
- Read files yourself — never ask Roman to paste.

Стиль/коментарі/тести — в Claude memory (`feedback_*.md`). Деталі по сесії — у vault `_what_to_do_now.md`.

## Current Status

- [x] Phase 1 — Entities + DbContext + migrations + seed
- [x] Phase 2 — Reference CRUD (departments, positions, tariff_grades, system_params, work_calendar)
- [x] Phase 3 — Employee CRUD + 7 блоків (single-position model)
- [x] Phase 3.5a — Domain Inventory (4 Departments, 21 Positions, `Position.ExcelAliases` jsonb)
- [x] Phase 3.6 — Multi-position refactor (1 Employee → N EmployeePosition) + TaxId/SocialBenefitPct/HasMilitaryRecord
- [x] Phase 3.6.5 — MomReview (TaxId mandatory, ComplexityBonusPct/PrestigeBonusPct, TitleType WorkerClass scope, EmployeeAdmin cleanup, InclusiveHours10To11). 7 модельних змін, smoke 8/8.
- [x] Phase 3.7 prep — IsHonored, EffectiveTo, PositionStartDate, TitleType aliases (commit c03393b)
- [x] **Phase 3.7 Staff lane** — парсер + Importer + ImportController + DI extension + smoke (commit `d99f06f`)
- [ ] **Phase 3.7 Teachers lane ← ЗАРАЗ** — TeachersParser готовий (commit `8867fbc`), треба Importer + Controller endpoint
  - 📚 Теорія integration-тестів (Testcontainers, fixtures, EF tracker) — переглянути після Phase 5: `[[integration_tests_walkthrough]]`.
- [ ] Phase 4 — Timesheets (manual + Excel import)
- [ ] Phase 5 — Calculation logic (4 services + orchestrator)
  - ⚠️ Перед стартом: clear DB, re-run seeder, verify TariffGrades / SystemParams / WorkCalendar з бухгалтером.
- [ ] Phase 6 — Excel export (відомість + розрахункові листи)
- [ ] Phase 7 — React UI (Claude vibe-codes)
  - Валідація розряду: disable розрядів які не дозволені для WorkerClass посади (`tariff_grade_ranges.md` + `ValidateGradeForClass`).
- [ ] Phase 8 — Electron wrapper + .exe
- [ ] Phase 9 — Бекапи БД (`doc_20_backups.md`)

## Session Start

1. Read `/Users/dev/DEV/brain/PayrollCalc_vault/_what_to_do_now.md` — checklist.
2. Read `00_OVERVIEW.md` if context bare.
3. For deep domain → vault links from `00_OVERVIEW.md`.
4. Open questions → `questions_for_accountant.md`.

After a phase done: `[x]` here, update `_what_to_do_now.md`, remind Roman to commit.

## ⚠️ Critical Domain Rules

- **Rounding:** `decimal` everywhere. `Math.Round(x, 2, MidpointRounding.AwayFromZero)` after EVERY operation.
- **4 worker classes:**
  - Class 1 — teachers → N-block (hourly)
  - Class 2 — admin/ped (director, deputy, psychologist) → J-block + optional N
  - Class 3 — specialists (accountant, librarian) → J-block, no #1749
  - Class 4 — MOP (cleaners, guards) → J-block, no #1749, no tenure
- **Taxes:** VZ = 5% (NOT 1.5%, постанова КМУ 1163). Union fee = `(gross − sick_fss) × 1%`.
- **Soft delete only:** `status = dismissed`. No physical DELETE.
- **Nullable employee blocks:** null → amount = 0, not error.
- **`params_snapshot` (jsonb)** — save on every calculation run.
- **`manual_corrections`** — persist on every RunAsync call.
- **User-facing messages** — Ukrainian.

## Stack

ASP.NET Core 9 · EF Core 9 + Npgsql · PostgreSQL 16 (Docker) · ClosedXML · ExcelDataReader · React + Vite + TypeScript · Electron · pnpm

DB: `Host=localhost;Database=payrollcalc;Username=payroll;Password=payroll123`

If domain logic unclear — read vault, don't invent.
