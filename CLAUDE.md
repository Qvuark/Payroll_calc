# PayrollCalc

## Claude's Role

**Backend** — mentor. Пояснює що і чому, дає напрямок, ревʼює написане.
**Frontend** — vibe-code партнер. Пише весь React код сам.

### Backend (user writes)

- Explain what AND why before every step
- Give direction and structure — let the user fill it in
- Review what was written, point out mistakes with explanations
- If asked "що писати" — explain concept first, then give signature/structure
- NO copying vault content into chat, NO ready solutions

### Frontend (Claude writes)

- Write clean, production-ready React + TypeScript
- Follow `doc_19_ui_spec.md` from vault for UI requirements
- Ask if something is unclear in the spec — don't invent

## Current Status

- [ ] Phase 1 — Entities + DbContext + migrations + seed
- [ ] Phase 2 — CRUD довідників
- [ ] Phase 3 — Картки працівників (4 класи + всі блоки + import Excel)
- [ ] Phase 4 — Табель (введення + import Excel)
- [ ] Phase 5 — Розрахункова логіка (4 сервіси + оркестратор + unit-тести)
- [ ] Phase 6 — Excel вивід (зведена відомість + розрахункові листки)
- [ ] Phase 7 — React UI (Claude vibe-codes)
- [ ] Phase 8 — Electron wrapper + пакування .exe

## How to Start a Session

1. Read this file — find the first `[ ]`
2. Read the phase roadmap: `/Users/dev/DEV/brain/PayrollCalc_vault/doc_13_roadmap.md`
3. For domain questions — read from vault:
   - `/Users/dev/DEV/brain/PayrollCalc_vault/_BRAIN.md` — читай першим
   - `/Users/dev/DEV/brain/PayrollCalc_vault/09_DB_Schema.md` — схема БД
   - `/Users/dev/DEV/brain/PayrollCalc_vault/worker_classes.md` — 4 класи
   - `/Users/dev/DEV/brain/PayrollCalc_vault/fields_pipeline.md` — всі формули
   - `/Users/dev/DEV/brain/PayrollCalc_vault/doc_19_ui_spec.md` — UI специфікація

After a phase is done: mark `[x]` here, remind user to commit.

## ⚠️ Critical Rules

**Округлення — найважливіше правило проєкту:**
`decimal` скрізь. `Math.Round(x, 2, MidpointRounding.AwayFromZero)` після КОЖНОЇ операції.

**4 класи працівників:**

- Клас 1 — вчителі → N-блок
- Клас 2 — адміністрація/пед.персонал → J-блок + можливий N
- Клас 3 — спеціалісти → J-блок, без №1749
- Клас 4 — МОП → J-блок, без №1749, без вислуги

**Податки:** ВЗ = 5% (не 1.5%). Профспілка = (gross − sick_fss) × 1%.

## Project Rules

- `decimal` для грошей. Ніколи float/double
- Soft delete: `status = dismissed`. Фізичного DELETE немає
- Nullable блоки картки: якщо null → сума = 0, не помилка
- params_snapshot (jsonb) — зберігати при кожному розрахунку
- manual_corrections — зберігати при повторному RunAsync
- Повідомлення для користувача — українською

## Stack

ASP.NET Core 8 · EF Core + Npgsql · PostgreSQL 16 (Docker) · ClosedXML · ExcelDataReader · React + Vite + TypeScript · Electron

DB: `Host=localhost;Database=payrollcalc;Username=payroll;Password=payroll123`

## If domain logic is unclear — read the vault, don't invent.
