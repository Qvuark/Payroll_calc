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

- [x] Phase 1 — Entities + DbContext + migrations + seed
- [x] Phase 2 — Reference data CRUD (departments, positions, tariff_grades, system_params, work_calendar)
- [ ] Phase 3 — Employee cards (all 4 classes + all blocks + Excel import)
  - [x] CRUD: GET/POST/PUT/DELETE для працівників + 7 блоків (Base, Workload, Admin, Allowances, Gpd, Pkr, NonPedagogical)
  - [x] Audit 2026-05-15 — закритий 2026-05-16 (PUT-семантика з Upsert helpers, unique constraints, decimal precision, jsonb для ParamsSnapshot, OnDelete.Restrict, WorkerClass consistency, NotebookRateId nullable, фільтр WorkCalendar по року, дефолти entity)
  - [x] Smoke test пройдено (curl): happy path, дублі, class mismatch, перемикання блоків через PUT, soft delete, captable Department.
  - [ ] **Excel парсери (2 шт)** — стратегія "много мелких парсеров" зафіксована 2026-05-17
    - Головний док: `/Users/dev/DEV/brain/PayrollCalc_vault/parsers_strategy.md`
    - Пакети: `ExcelDataReader` + `ExcelDataReader.DataSet` (уже в csproj)
    - [ ] **Парсер #1: тарифікація Class 1+2** — `POST /api/employees/import/tarification [FromForm IFormFile]`
      - Структура файлу (97 cols, парні рядки): `excel_tarification_structure.md` у vault
      - Маппінг колонок → блоки повністю описано
    - [ ] **Парсер #2: спецперсонал Class 3+4** — `POST /api/employees/import/specstaff [FromForm IFormFile]`
      - НАШ шаблон 15-18 cols: `excel_specstaff_structure.md` у vault
      - Створити `docs/templates/specstaff_template.xlsx` (мати у репо для бухгалтера)
    - Спільна інфраструктура `PayrollCalc.Infrastructure/Excel/`: `ExcelReaderBase`, `ParserResult`, `DecimalParser`, `HeaderValidator`
    - Returns: `{ imported, updated, skipped, errors[] }`. Одна EF транзакція на пачку.
  - Відкладено (потрібен бухгалтер): тип шкідливості (HasUnfavorable bool → enum/%), % дир-залежних окладів (chief_accountant_pct, vice_principal_pct тощо), структура нічних і дезінфектантів
- [ ] Phase 4 — Timesheets (manual entry + Excel import)
- [ ] Phase 5 — Calculation logic (4 services + orchestrator + unit tests)
  - ⚠️ Before starting: verify ALL seed data with accountant, clear DB, re-run seeder
    - TariffGrades: all 25 grades correct? (seeder updated 2026-05-12, based on real table)
    - SystemParams: all 24 keys correct? (vz_rate=0.05, bonus_1749=0.40, mzp=8647...)
    - WorkCalendar 2026: 249 days total, verify with official holiday list
    - Run: `DELETE FROM "TariffGrades"; DELETE FROM "SystemParams";` then restart app
- [ ] Phase 6 — Excel export (payroll summary + payslips)
- [ ] Phase 7 — React UI (Claude vibe-codes)
- [ ] Phase 8 — Electron wrapper + .exe packaging
- [ ] Phase 9 — Бекапи БД (автоматичні + ручні)
  - Деталі: `/Users/dev/DEV/brain/PayrollCalc_vault/doc_20_backups.md`
  - Розглянути зв'язок з власним пет-проектом DbBackuper (universal tool) у personal_vault

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

## Working Style

- Never ask Roman to paste or show code — read files directly with the Read tool
- Always explain what a concept is and why it's needed BEFORE giving an implementation task

## Code Comments (Roman is learning production style)

Roman is building the habit of writing comments. Prompt and review.

**Comment types:**

| Type | When | Example |
|---|---|---|
| `///` XML doc | All `public` classes/methods/non-obvious properties | Method summary + params + returns |
| `//` why-comment | Non-obvious decision, business rule, workaround | `// VZ = 5%, not 1.5% — постанова КМУ 1163` |
| `// TODO:` | Open question + link/note | `// TODO: clarify with accountant — see questions_for_accountant.md` |

**Rules:**
- NO comments that restate code (`// increment counter` over `counter++`)
- YES comments for WHY when non-obvious (>1 sec to grok)
- Public API in `Common/`, `Documents/`, `Core/`, `Infrastructure/` → XML docs required
- Private methods → no XML, just `//` for tricky parts
- Self-explanatory DTO properties (`Imported`, `Updated`) → no XML
- Domain magic numbers (VZ=0.05, MZP=8647) → comment with source (постанова/закон)

**Mentor behavior:**
- After Roman writes a class/method, point out where comments belong
- Show example XML doc inline if needed
- Don't add comments for him — let him write, review

**XML doc syntax:**
```csharp
/// <summary>One-two sentences, capital + period.</summary>
/// <param name="value">Description.</param>
/// <returns>What it returns.</returns>
```
