// Десктоп-обгортка. Два режими:
//  - dev (pnpm start у репо): піднімає docker-БД і `dotnet run`, як і раніше;
//  - packaged (встановлений exe): несе ВСЕ з собою — embedded PostgreSQL (resources/pgsql)
//    з даними у %APPDATA%/PayrollCalc/pgdata + self-contained API (resources/api).
// Уся програма живе в API — тут лише оркестрація процесів і вікно.
const { app, BrowserWindow, dialog } = require('electron')
const { spawn, spawnSync } = require('child_process')
const path = require('path')
const fs = require('fs')

const API_PORT = 5196
const API_URL = `http://localhost:${API_PORT}`
// Окремий порт embedded-БД, щоб не битися з чиїмось локальним постгресом на 5432.
const PG_PORT = 5433
const REPO_ROOT = path.join(__dirname, '..')
const API_PROJECT = path.join(REPO_ROOT, 'src', 'PayrollCalc.API')

/** Процеси, які запускали ми — вбиваємо при виході. Чужі не чіпаємо. */
let apiProcess = null
let pgStarted = false

/** Лог запуску — щоб дебажити перший старт на чужому ПК по фото екрана. */
const logFile = path.join(app.getPath('userData'), 'startup.log')
function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`
  console.log(line)
  try { fs.appendFileSync(logFile, line + '\n') } catch { /* лог не критичний */ }
}

function apiAlive() {
  return fetch(`${API_URL}/api/departments`, { signal: AbortSignal.timeout(1500) })
    .then(r => r.ok)
    .catch(() => false)
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

// ─── dev-гілка: docker compose + dotnet run ───

/** docker compose up -d — ідемпотентно: вже запущений контейнер просто лишиться. */
function ensureDockerDb() {
  return new Promise(resolve => {
    const proc = spawn('docker', ['compose', 'up', '-d'], { cwd: REPO_ROOT })
    proc.on('close', () => resolve())
    proc.on('error', () => resolve()) // нема docker у PATH — хай API сам скаже про БД
  })
}

function spawnDevApi() {
  apiProcess = spawn('dotnet', ['run', '--project', API_PROJECT], {
    cwd: REPO_ROOT,
    stdio: 'ignore',
  })
  apiProcess.on('error', () => { apiProcess = null })
}

// ─── packaged-гілка: embedded postgres + published API ───

const RES = process.resourcesPath
const PG_BIN = path.join(RES, 'pgsql', 'bin')
const PG_DATA = path.join(app.getPath('userData'), 'pgdata')

/** Шлях до утиліти постгреса з ресурсів (initdb/pg_ctl/createdb). */
function pgTool(name) {
  return path.join(PG_BIN, process.platform === 'win32' ? `${name}.exe` : name)
}

function run(cmd, args) {
  log(`run: ${path.basename(cmd)} ${args.join(' ')}`)
  const r = spawnSync(cmd, args, { encoding: 'utf8', windowsHide: true })
  if (r.status !== 0)
    log(`  exit=${r.status} stderr=${(r.stderr || '').slice(0, 500)}`)
  return r.status === 0
}

/**
 * Перший старт: initdb у %APPDATA%/PayrollCalc/pgdata (trust-auth — БД слухає лише localhost,
 * це локальна однокористувацька прога). Далі pg_ctl start + createdb (ідемпотентно).
 */
function ensureEmbeddedDb() {
  if (!fs.existsSync(path.join(PG_DATA, 'PG_VERSION'))) {
    log('initdb: перший запуск, створюю кластер')
    const ok = run(pgTool('initdb'), [
      '-D', PG_DATA, '-U', 'payroll', '--auth=trust', '-E', 'UTF8',
      '--locale-provider=icu', '--icu-locale=uk-UA', '--locale=C',
    ])
    if (!ok) return false
  }
  // pg_ctl start ідемпотентний у наш бік: «вже запущений» вважаємо успіхом.
  const started = run(pgTool('pg_ctl'), [
    '-D', PG_DATA, '-w', '-t', '60',
    '-o', `-p ${PG_PORT}`, '-l', path.join(app.getPath('userData'), 'postgres.log'),
    'start',
  ])
  pgStarted = started
  if (!started && !run(pgTool('pg_isready'), ['-p', String(PG_PORT)]))
    return false
  // createdb падає якщо база вже є — це норм, глотаємо.
  run(pgTool('createdb'), ['-p', String(PG_PORT), '-U', 'payroll', 'payrollcalc'])
  return true
}

function spawnPackagedApi() {
  const apiExe = path.join(RES, 'api', 'PayrollCalc.API.exe')
  log(`api: ${apiExe}`)
  apiProcess = spawn(apiExe, [], {
    cwd: path.join(RES, 'api'),
    stdio: 'ignore',
    windowsHide: true,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: API_URL,
      ConnectionStrings__DefaultConnection:
        `Host=localhost;Port=${PG_PORT};Database=payrollcalc;Username=payroll`,
    },
  })
  apiProcess.on('error', e => { log(`api spawn error: ${e.message}`); apiProcess = null })
}

// ─── спільний старт ───

async function ensureApi() {
  if (await apiAlive()) return true

  if (app.isPackaged) {
    if (!ensureEmbeddedDb()) return false
    spawnPackagedApi()
  } else {
    await ensureDockerDb()
    spawnDevApi()
  }
  // Холодний старт: міграції+сід на першому запуску — десятки секунд.
  for (let attempt = 0; attempt < 90; attempt++) {
    await sleep(1000)
    if (await apiAlive()) return true
  }
  return false
}

async function start() {
  const splash = new BrowserWindow({
    width: 420,
    height: 180,
    frame: false,
    resizable: false,
  })
  splash.loadURL('data:text/html;charset=utf-8,' + encodeURIComponent(`
    <body style="font-family:system-ui;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;background:#f6f7f9;color:#1f2430">
      <div style="text-align:center">
        <div style="font-size:18px;font-weight:700">PayrollCalc</div>
        <div style="color:#687083;margin-top:8px">Запускаю базу і сервер розрахунку…</div>
      </div>
    </body>`))

  const ok = await ensureApi()
  if (!ok) {
    splash.destroy()
    dialog.showErrorBox(
      'PayrollCalc',
      'Не вдалося запустити сервер розрахунку.\n' +
      (app.isPackaged
        ? `Надішліть розробнику файл логу:\n${logFile}`
        : 'Перевірте, що встановлені Docker і .NET, і спробуйте ще раз.'),
    )
    app.quit()
    return
  }

  const win = new BrowserWindow({
    width: 1440,
    height: 900,
    title: 'PayrollCalc',
    webPreferences: { contextIsolation: true },
  })
  win.removeMenu?.()
  // Чистимо кеш перед завантаженням: інакше Chromium може віддати стару збірку SPA
  // після оновлення wwwroot (і у мами після апдейту програми — щоб бачила новий UI).
  await win.webContents.session.clearCache()
  await win.loadURL(API_URL)
  splash.destroy()
}

app.whenReady().then(() => {
  log(`start: packaged=${app.isPackaged} platform=${process.platform}`)
  // Іконка в доці mac працює і без упаковки; для Windows-збірки іконку
  // підхопить пакувальник з цього ж файлу.
  if (process.platform === 'darwin')
    app.dock.setIcon(path.join(__dirname, 'icon.png'))
  return start()
})

app.on('window-all-closed', () => {
  // Утиліта, не mac-додаток з доком: закрили вікно — програма завершилась.
  app.quit()
})

app.on('quit', () => {
  if (apiProcess && !apiProcess.killed) apiProcess.kill()
  // Зупиняємо лише власний embedded postgres; чужий (docker/системний) не чіпаємо.
  if (app.isPackaged && pgStarted)
    run(pgTool('pg_ctl'), ['-D', PG_DATA, '-m', 'fast', 'stop'])
})
