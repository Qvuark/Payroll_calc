// Тонка обгортка над fetch: JSON, обробка помилок ProblemDetails/валідації,
// FormData для імпорту файлів. Всі шляхи відносні — працює через Vite-проксі.

const BASE = '/api'

export class ApiError extends Error {
  readonly status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/**
 * Витягує людський текст помилки з відповіді бекенда.
 * Бекенд повертає або голий рядок, або ProblemDetails {title, detail},
 * або ASP.NET validation problem {errors: {поле: [повідомлення]}}.
 */
async function toApiError(res: Response): Promise<ApiError> {
  let message = `Помилка сервера (${res.status})`
  try {
    const text = await res.text()
    if (text) {
      try {
        const data = JSON.parse(text)
        if (typeof data === 'string') message = data
        else if (data.errors && typeof data.errors === 'object')
          message = Object.values(data.errors as Record<string, string[]>).flat().join(' ')
        else if (data.detail) message = data.detail
        else if (data.title) message = data.title
        else message = text
      } catch {
        message = text
      }
    }
  } catch {
    // тіло не прочиталось — лишаємо дефолтний текст
  }
  return new ApiError(res.status, message)
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type'))
    headers.set('Content-Type', 'application/json')
  let res: Response
  try {
    res = await fetch(BASE + path, { ...init, headers })
  } catch {
    throw new ApiError(0, 'Немає зв’язку з сервером. Перевірте, чи запущена програма розрахунку.')
  }
  if (!res.ok) throw await toApiError(res)
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

export function apiUrl(path: string): string {
  return BASE + path
}

/**
 * Завантажує файл (xlsx) і зберігає через тимчасовий <a download>.
 * Ім'я файлу бере з Content-Disposition, fallback — передане.
 */
export async function downloadFile(path: string, fallbackName: string): Promise<void> {
  let res: Response
  try {
    res = await fetch(BASE + path)
  } catch {
    throw new ApiError(0, 'Немає зв’язку з сервером.')
  }
  if (!res.ok) throw await toApiError(res)
  const blob = await res.blob()
  const disposition = res.headers.get('Content-Disposition') ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)
  const name = match ? decodeURIComponent(match[1]) : fallbackName
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  a.click()
  URL.revokeObjectURL(url)
}
