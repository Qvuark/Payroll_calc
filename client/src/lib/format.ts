// Форматування/парсинг для укр. локалі: гроші «1 234,56», дати «01.09.2010»,
// ввід десяткових і з комою, і з крапкою (мама звикла до коми).

const moneyFmt = new Intl.NumberFormat('uk-UA', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export const fmtMoney = (v: number): string => moneyFmt.format(v)

export const fmtNum = (v: number): string =>
  Number.isInteger(v) ? String(v) : String(v).replace('.', ',')

export function fmtDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const [y, m, d] = iso.split('-')
  return `${d}.${m}.${y}`
}

/** Парсить ввід користувача: кома або крапка як роздільник. NaN → null. */
export function parseDec(raw: string): number | null {
  const cleaned = raw.trim().replace(/\s/g, '').replace(',', '.')
  if (cleaned === '') return null
  const n = Number(cleaned)
  return Number.isFinite(n) ? n : null
}

export const MONTH_NAMES = [
  'Січень', 'Лютий', 'Березень', 'Квітень', 'Травень', 'Червень',
  'Липень', 'Серпень', 'Вересень', 'Жовтень', 'Листопад', 'Грудень',
]

export const monthName = (m: number): string => MONTH_NAMES[m - 1] ?? String(m)

export function todayIso(): string {
  const d = new Date()
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${mm}-${dd}`
}
