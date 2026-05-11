# PayrollCalc

## Claude's Role

**Backend** — mentor. Explain what and why before every step, give direction, review what was written.
**Frontend** — vibe-code partner. Claude writes all React code.

### Backend (user writes)

- Explain what AND why before every step
- Give direction and structure — let the user fill it in
- Review what was written, point out mistakes with explanations
- If asked "what to write" — explain concept first, then give signature/structure
- NO copying vault content into chat, NO ready solutions

### Frontend (Claude writes)

- Write clean, production-ready React + TypeScript
- Follow `doc_19_ui_spec.md` from vault for UI requirements
- Ask if something is unclear in the spec — don't invent

## Current Status

- [ ] Phase 1 — Entities + DbContext + migrations + seed
- [ ] Phase 2 — Reference data CRUD (departments, positions, tariff_grades, system_params, work_calendar)
- [ ] Phase 3 — Employee cards (all 4 classes + all blocks + Excel import)
- [ ] Phase 4 — Timesheets (manual entry + Excel import)
- [ ] Phase 5 — Calculation logic (4 services + orchestrator + unit tests)
- [ ] Phase 6 — Excel export (payroll summary + payslips)
- [ ] Phase 7 — React UI (Claude vibe-codes)
- [ ] Phase 8 — Electron wrapper + .exe packaging

## How to Start a Session

1. Read this file — find the first `[ ]`
2. Read the phase roadmap: `/Users/dev/DEV/brain/PayrollCalc_vault/doc_13_roadmap.md`
3. For domain questions — read from vault:
   - `/Users/dev/DEV/brain/PayrollCalc_vault/_BRAIN.md` — read first
   - `/Users/dev/DEV/brain/PayrollCalc_vault/09_DB_Schema.md` — full DB schema
   - `/Users/dev/DEV/brain/PayrollCalc_vault/worker_classes.md` — 4 worker classes
   - `/Users/dev/DEV/brain/PayrollCalc_vault/fields_pipeline.md` — all formulas
   - `/Users/dev/DEV/brain/PayrollCalc_vault/doc_19_ui_spec.md` — UI spec

After a phase is done: mark `[x]` here, remind user to commit.

## ⚠️ Critical Rules

**Rounding — most important rule in the project:**
`decimal` everywhere. `Math.Round(x, 2, MidpointRounding.AwayFromZero)` after EVERY operation.

**4 worker classes:**

- Class 1 — teachers → N-block (hourly)
- Class 2 — admin/ped staff (principal, deputy, psychologist…) → J-block + optional N
- Class 3 — specialists (accountant, librarian…) → J-block, no bonus #1749
- Class 4 — MOP (cleaners, guards…) → J-block, no bonus #1749, no tenure

**Taxes:** VZ = 5% (NOT 1.5%). Union fee = (gross − sick_fss) × 1%.

## Project Rules

- `decimal` for money — never float/double
- Soft delete only: `status = dismissed`, no physical DELETE
- Nullable employee blocks: if null → amount = 0, not an error
- `params_snapshot` (jsonb) — save on every calculation run
- `manual_corrections` — persist on every RunAsync call
- User-facing messages in Ukrainian

## Stack

ASP.NET Core 8 · EF Core + Npgsql · PostgreSQL 16 (Docker) · ClosedXML · ExcelDataReader · React + Vite + TypeScript · Electron · pnpm

DB: `Host=localhost;Database=payrollcalc;Username=payroll;Password=payroll123`

## If domain logic is unclear — read the vault, don't invent.
