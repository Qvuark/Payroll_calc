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
  - [x] Phase 3.5a — Domain Inventory: 4 Departments, 21 Positions, `Position.ExcelAliases` jsonb, drop `Employee.WorkerClass` (commit 1a3abe0).
  - [x] **Phase 3.6 — Multi-position refactor + payslip-критичні поля** ✅ ЗАКРИТО 2026-05-21 (commit 170d331)
    - Multi-position: один Employee → N EmployeePosition. Workload/Admin/Gpd/Pkr/NonPedagogical перенесені на EmployeePosition.
    - 3 нові поля: `Employee.TaxId`, `Employee.SocialBenefitPct`, `EmployeePosition.HasMilitaryRecord`.
  - [x] **Phase 3.6.5 — MomReview refactor** ✅ ЗАКРИТО 2026-05-23
    - Причина: 14 відповідей мами 2026-05-22 → 7 модельних змін. Деталі — [[mom_answers_review_2026-05-22]].
    - `Employee`: TaxId mandatory+unique, TabNumber drop unique (сумісник кейс), +GeneralExperienceYears, drop HasComplexityBonus.
    - `EmployeePosition`: +ComplexityBonusPct decimal? (5-50%, всі класи), +PrestigeBonusPct decimal? (5-20%, Class 1).
    - `EmployeeAdmin`: drop DirectorPct/AdminRateCount/PedRateCount (legacy, дублювало RateCount + WorkerClass).
    - `EmployeeWorkload`: +InclusiveHours10To11.
    - `TitleType`: +WorkerClass scope (Старший вчитель/C1/10%, Методист/C1/15%, Психолог-методист/C2/10%, Педагог-організатор/C2/10%).
    - Migration `Phase3_6_5_MomReview` (incremental, не reset). Smoke test через curl: 8/8 пройшли (POST/GET/PUT, dup TaxId, сумісник, Class validation).
    - Документація: дезінфектанти 10% (не 15%), нічна формула (`/norm_hours × night_hours × 40%`), ГПД 10-14, ПКР 10-12.
  - [ ] **Phase 3.7 — Excel парсери (2 шт)** — Strategy B (split clean) зафіксована 2026-05-23
    - Головні доки: [[parsers_implementation]] (план реалізації), [[template_schemas_draft]] (схема), [[parsers_design_options]] (варіанти з рекомендаціями).
    - Пакети: `ExcelDataReader` + `ExcelDataReader.DataSet` (уже в csproj).
    - **Стратегічне рішення 2026-05-21:** дроп парсера тарифікації (97 cols, парні рядки, крихкий). Власні шаблони (програма диктує входи).
    - **Strategy B 2026-05-23:** педагогічна частина (хоч і у адмін-вчителя) лежить у teachers.xlsx, не-педагогічна у staff.xlsx.
    - [ ] **Парсер #1: teachers.xlsx Class 1** — `POST /api/employees/import/teachers [FromForm IFormFile]`. 34 колонки: persona + Position=Вчитель + Subject + Workload (hours/notebooks/inclusive) + педагогічні надбавки (ClassMgmt/Cabinet/Gym/Shooting/Computers/Extracurricular/Website).
    - [ ] **Парсер #2: staff.xlsx Class 2-4** — `POST /api/employees/import/staff [FromForm IFormFile]`. 24 колонки: persona + Position + GPD/PKR + NonPedagogical + Disinfectants/NightShifts. (DirectorPct/AdminRateCount/PedRateCount drop у Phase 3.6.5.)
    - [ ] **Preflight endpoint** — `POST /api/employees/import/preflight (files[])` — dry-run cross-file перевірка IsPrimary.
    - **Архітектурні рішення A1-A6 ✅ 2026-05-23:** MVP upsert + `EffectiveTo` поле в міграції / PUT persona / error при missing IsPrimary / group by TabNumber / `PositionStartDate` колонка / окремі endpoints per file.
    - Спільна інфра `src/PayrollCalc.Documents/Import/Common/`: `ExcelReaderBase`, `BoolParser`, `DateParser`, `DecimalParser` ✅, `HeaderValidator` ✅, `ParserResult` ✅, `ParserError` ✅.
    - **Міграції перед стартом:** `Phase3_7_AddEffectiveTo`, `Phase3_7_AddPositionStartDate`, `Phase3_7_AddIsHonored`, `Phase3_7_TitleTypeAliases`.
    - Returns: `{ imported, updated, skipped, errors[], warnings[] }`. Одна EF транзакція на файл (all-or-nothing).
  - Лишилось питати маму: Q15 (% звання соц.педагог), Class 3/4 діапазони розрядів (мама обіцяла таблицю).
  - Відкладено: тип шкідливості (HasUnfavorable bool → enum/%) — Phase 5.
- [ ] Phase 4 — Timesheets (manual entry + Excel import)
- [ ] Phase 5 — Calculation logic (4 services + orchestrator + unit tests)
  - ⚠️ Before starting: verify ALL seed data with accountant, clear DB, re-run seeder
    - TariffGrades: all 25 grades correct? (seeder updated 2026-05-12, based on real table)
    - SystemParams: all 24 keys correct? (vz_rate=0.05, bonus_1749=0.40, mzp=8647...)
    - WorkCalendar 2026: 249 days total, verify with official holiday list
    - Run: `DELETE FROM "TariffGrades"; DELETE FROM "SystemParams";` then restart app
- [ ] Phase 6 — Excel export (payroll summary + payslips)
- [ ] Phase 7 — React UI (Claude vibe-codes)
  - **Валідація розряду в UI:** при додаванні/редагуванні ставки disable розрядів які не дозволені для WorkerClass посади. Source: [[tariff_grade_ranges]]. Не дати двірнику поставити розряд 17. Активне використання `ValidateGradeForClass` з бекенду + клієнтський guard.
- [ ] Phase 8 — Electron wrapper + .exe packaging
- [ ] Phase 9 — Бекапи БД (автоматичні + ручні)
  - Деталі: `/Users/dev/DEV/brain/PayrollCalc_vault/doc_20_backups.md`
  - Розглянути зв'язок з власним пет-проектом DbBackuper (universal tool) у personal_vault

## How to Start a Session

1. Read **`/Users/dev/DEV/brain/PayrollCalc_vault/_what_to_do_now.md`** — короткий чеклист, точка входу
2. Read this file — find the first `[ ]`
3. For domain questions — read from vault:
   - `/Users/dev/DEV/brain/PayrollCalc_vault/_BRAIN.md` — повний контекст
   - `/Users/dev/DEV/brain/PayrollCalc_vault/doc_13_roadmap.md` — phase roadmap
   - `/Users/dev/DEV/brain/PayrollCalc_vault/09_DB_Schema.md` — full DB schema
   - `/Users/dev/DEV/brain/PayrollCalc_vault/worker_classes.md` — 4 worker classes
   - `/Users/dev/DEV/brain/PayrollCalc_vault/fields_pipeline.md` — all formulas
   - `/Users/dev/DEV/brain/PayrollCalc_vault/payslip_and_summary_structure.md` — output structure
   - `/Users/dev/DEV/brain/PayrollCalc_vault/doc_19_ui_spec.md` — UI spec
4. Open questions → `/Users/dev/DEV/brain/PayrollCalc_vault/questions_for_accountant.md`

After a phase is done: mark `[x]` here, update `_what_to_do_now.md`, remind user to commit.

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
- **ВАЖЛИВО:** Roman забуває коментувати — нагадувати після КОЖНОГО написаного класу/методу. Якщо Roman явно просить "сам докоментуй" — додавати XML docs самостійно за встановленим стилем (3-line format, на public API).

**XML doc syntax:**

Roman предпочитает 3-строчный формат — открывающий тег, текст, закрывающий тег на разных строках. НЕ inline. Это правило для всех XML docs в проекте.

```csharp
/// <summary>
/// One-two sentences, capital + period.
/// </summary>
/// <param name="value">Description.</param>
/// <returns>What it returns.</returns>
```

Inline-форма `/// <summary>...</summary>` — НЕ использовать, даже если текст короткий.

**Code spacing — НЕ добавлять пустые строки между полями entity / properties класса.**

ИИ-генерация любит ставить пустые строки между группами свойств (FK+nav парами, скаляры, навигации) для "визуальной группировки". Roman это не любит — палится как AI-код. Пиши плотно, одной стеной полей. Пустые строки только:
- между using-блоком и namespace
- между namespace и class
- между class summary и class declaration
- между методами

ПРИМЕР НЕПРАВИЛЬНО:
```csharp
public int Id { get; set; }

public int EmployeeId { get; set; }
public Employee? Employee { get; set; }

public int PositionId { get; set; }
public Position? Position { get; set; }
```

ПРИМЕР ПРАВИЛЬНО:
```csharp
public int Id { get; set; }
public int EmployeeId { get; set; }
public Employee? Employee { get; set; }
public int PositionId { get; set; }
public Position? Position { get; set; }
```

## Testing (Roma is learning from scratch)

Full reference: `/Users/dev/DEV/brain/PayrollCalc_vault/testing_principles.md` (F.I.R.S.T, AAA, Test Pyramid, mocking rules, stack, plan by phase).

**Stack**: xUnit + Moq + FluentAssertions + WebApplicationFactory + Testcontainers.PostgreSQL (when integration).

**Approach**: test-after (test in same commit-session as code), NOT TDD yet.

**Mentor behavior**:
- After pure function (validator, formula, parser) → suggest unit test with edge cases.
- After controller endpoint → suggest integration test via `WebApplicationFactory`.
- After bug discovery → write regression test FIRST, then fix.
- Don't write tests for Roma — give skeleton, he writes, review.
- Exception: if Roma asks "напиши за меня" or it's mechanical refactor of existing tests.

**Phase plan**:
- Phase 3.7 parsers → unit tests on `TeachersParser`, `StaffParser`, `DecimalParser`, `BoolParser`, `DateParser`, `HeaderValidator` with xlsx fixtures. Integration tests on `TeachersImporter`, `StaffImporter` via Testcontainers.PostgreSQL.
- Phase 5 calculation → 5-10 unit tests per formula (VZ, PDFO, ESV, надбавки) + snapshot tests.
- Phase 6 export → 1 integration test, open Excel, check key cells.
- CI later: GitHub Actions `dotnet test` on each push.
