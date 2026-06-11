# PayrollCalc

Розрахунок зарплати працівників школи: довідники, картки працівників, табель, рушій нарахувань (~20 надбавок, податки, доплата до МЗП) і готові Excel-документи — відомість та розрахункові листи з **живими формулами** в клітинках (видно, звідки кожне число).

Працює як звичайна десктоп-програма (Electron, усе в комплекті — нічого ставити не треба) або як веб-аплікація на localhost.

## Можливості

- **Довідники** — підрозділи, посади (4 класи працівників), тарифна сітка, системні параметри (МЗП, ставки податків, №1749), робочий календар.
- **Працівники** — картка з кількома ставками (директор-вчитель тощо), блоки надбавок per клас: навантаження/зошити/інклюзив, класне керівництво/кабінет, ГПД/ПКР з власними розрядами, непедагогічні доплати.
- **Масовий імпорт з Excel** — вчителі, інший персонал, табель; програма сама генерує шаблони, звіт по кожному рядку.
- **Табель** — сітка на місяць: дні/години/заміни/нічні + ручні суми (лікарняні, відпускні, премії, індексація…).
- **Рушій розрахунку** — повний decimal без проміжних округлень, формули-літерали для кожного компонента, знімок параметрів на кожен розрахунок.
- **Документи** — розрахунково-платіжна відомість (1:1 з робочим бланком бухгалтера, колонки A..BK) і розрахункові листи (по два на аркуш).

## Стек

ASP.NET Core 9 · EF Core 9 + Npgsql · PostgreSQL 16 · ClosedXML + ExcelDataReader · React 19 + Vite + TypeScript · Electron

## Структура репозиторію

```
src/
  PayrollCalc.API/            ASP.NET Core API + роздача SPA з wwwroot
  PayrollCalc.Core/           Domain: entities, DTO, валідатори
  PayrollCalc.Calculation/    Рушій розрахунку (чистий, без БД/Excel)
  PayrollCalc.Documents/      Excel: імпорт (парсери) та експорт (відомість, листи, шаблони)
  PayrollCalc.Infrastructure/ EF Core: DbContext, міграції, сід
tests/PayrollCalc.Tests/      Unit + integration (Testcontainers) — у т.ч. diff-тести з еталонною відомістю
client/                       React SPA (білд лягає в API/wwwroot)
electron/                     Десктоп-обгортка + пакування інсталятора
```

## Запуск для розробки

Потрібні: .NET 9 SDK · Docker · Node 20+ і pnpm.

```bash
docker compose up -d                      # PostgreSQL 16
dotnet run --project src/PayrollCalc.API  # міграції+сід на старті; http://localhost:5196
cd client && pnpm install && pnpm dev     # фронт із hot-reload на :5173 (проксі /api → 5196)
```

Без hot-reload можна простіше: `cd client && pnpm build` (бандл ляже в `wwwroot`) — і вся програма доступна на http://localhost:5196.

Десктоп-вікно в dev-режимі (саме підніме docker і API, якщо вони не запущені):

```bash
cd electron && pnpm install && pnpm start
```

### Тести

```bash
dotnet test   # unit рушія + integration (Testcontainers підніме свій PostgreSQL)
```

## Збірка Windows-інсталятора

Інсталятор несе все з собою: self-contained API (.NET runtime всередині), embedded PostgreSQL і вікно Electron. Користувачу нічого ставити не треба — один exe.

```bash
cd electron && ./build-win.sh   # → electron/release/PayrollCalc Setup 0.1.0.exe
```

Скрипт сам: публікує API під win-x64 → качає portable PostgreSQL 16 (EDB binaries) і урізає зайве → збирає NSIS-інсталятор. Дані БД користувача живуть у `%APPDATA%/PayrollCalc/pgdata`, лог запуску — `%APPDATA%/PayrollCalc/startup.log`.

Готові збірки — у [Releases](../../releases).
