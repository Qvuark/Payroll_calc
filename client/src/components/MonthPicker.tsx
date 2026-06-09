import { monthName } from '../lib/format'

export interface Period {
  year: number
  month: number
}

/** Поточний місяць — стартове значення для табеля/розрахунку. */
export function currentPeriod(): Period {
  const d = new Date()
  return { year: d.getFullYear(), month: d.getMonth() + 1 }
}

/**
 * Перемикач місяця «← Березень 2026 →». Стрілки переходять через межі року.
 */
export function MonthPicker({ value, onChange }: { value: Period; onChange: (p: Period) => void }) {
  const shift = (delta: number) => {
    const idx = value.year * 12 + (value.month - 1) + delta
    onChange({ year: Math.floor(idx / 12), month: (idx % 12) + 1 })
  }
  return (
    <div className="month-nav">
      <button type="button" className="btn btn-sm" onClick={() => shift(-1)} aria-label="Попередній місяць">←</button>
      <span className="label">{monthName(value.month)} {value.year}</span>
      <button type="button" className="btn btn-sm" onClick={() => shift(1)} aria-label="Наступний місяць">→</button>
    </div>
  )
}
