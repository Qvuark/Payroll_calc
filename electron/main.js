// Десктоп-обгортка: піднімає БД (docker) і API (dotnet), коли вони ще не запущені,
// чекає готовності і відкриває вікно на localhost. Уся програма живе в API —
// тут лише оркестрація процесів і вікно.
const { app, BrowserWindow, dialog } = require('electron')
const { spawn } = require('child_process')
const path = require('path')

const API_URL = 'http://localhost:5196'
const REPO_ROOT = path.join(__dirname, '..')
const API_PROJECT = path.join(REPO_ROOT, 'src', 'PayrollCalc.API')

/** Процес API, якщо запускали ми — вбиваємо при виході. Чужий не чіпаємо. */
let apiProcess = null

function apiAlive() {
  return fetch(`${API_URL}/api/departments`, { signal: AbortSignal.timeout(1500) })
    .then(r => r.ok)
    .catch(() => false)
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms))
}

/** docker compose up -d — ідемпотентно: вже запущений контейнер просто лишиться. */
function ensureDb() {
  return new Promise(resolve => {
    const proc = spawn('docker', ['compose', 'up', '-d'], { cwd: REPO_ROOT })
    proc.on('close', () => resolve())
    proc.on('error', () => resolve()) // нема docker у PATH — хай API сам скаже про БД
  })
}

async function ensureApi() {
  if (await apiAlive()) return true
  await ensureDb()
  apiProcess = spawn('dotnet', ['run', '--project', API_PROJECT], {
    cwd: REPO_ROOT,
    stdio: 'ignore',
  })
  apiProcess.on('error', () => { apiProcess = null })
  // dotnet run збирає проект перед стартом — на холодну це десятки секунд.
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
      'Не вдалося запустити сервер розрахунку.\nПеревірте, що встановлені Docker і .NET, і спробуйте ще раз.',
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
  await win.loadURL(API_URL)
  splash.destroy()
}

app.whenReady().then(() => {
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
})
