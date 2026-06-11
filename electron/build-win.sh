#!/usr/bin/env bash
# Збірка Windows-інсталятора з нуля: publish API → embedded PostgreSQL → NSIS.
# Результат: electron/release/PayrollCalc Setup <version>.exe
set -euo pipefail
cd "$(dirname "$0")"

PG_VERSION="16.6-1"
PG_ZIP="/tmp/pg${PG_VERSION}-win.zip"

echo "── 1/4 Фронт → wwwroot"
(cd ../client && pnpm install --silent && pnpm build)

echo "── 2/4 API publish win-x64 (self-contained)"
rm -rf api-win
dotnet publish ../src/PayrollCalc.API -c Release -r win-x64 --self-contained true -o api-win

echo "── 3/4 Embedded PostgreSQL ${PG_VERSION}"
if [ ! -d pgsql-win ]; then
  [ -f "$PG_ZIP" ] || curl -L -o "$PG_ZIP" \
    "https://get.enterprisedb.com/postgresql/postgresql-${PG_VERSION}-windows-x64-binaries.zip"
  rm -rf /tmp/pgex && mkdir /tmp/pgex
  unzip -q "$PG_ZIP" -d /tmp/pgex
  # Інсталятору потрібні лише bin/lib/share — решта (pgAdmin, доки, симболи) важить сотні МБ.
  (cd /tmp/pgex/pgsql && rm -rf "pgAdmin 4" StackBuilder doc include symbols && find . -name "*.pdb" -delete)
  mv /tmp/pgex/pgsql pgsql-win
else
  echo "pgsql-win вже є — пропускаю (rm -rf pgsql-win щоб перекачати)"
fi

echo "── 4/4 electron-builder NSIS"
pnpm install --silent
pnpm dist:win

echo "✅ Готово:"
ls -lh release/*.exe
