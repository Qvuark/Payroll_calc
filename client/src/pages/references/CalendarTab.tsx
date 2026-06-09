import { useState } from 'react'
import { useWorkCalendar } from '../../api/hooks'
import { Loading, LoadError } from '../../components/ui'
import { monthName } from '../../lib/format'

export function CalendarTab() {
  const [year, setYear] = useState(new Date().getFullYear())
  const { data, isPending, error } = useWorkCalendar(year)

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  const byMonth = new Map(data.map(m => [m.month, m.workDays]))
  const total = data.reduce((sum, m) => sum + m.workDays, 0)

  return (
    <>
      <div className="row mb16">
        <button type="button" className="btn btn-sm" onClick={() => setYear(y => y - 1)}>←</button>
        <strong style={{ fontSize: 16 }}>{year} рік</strong>
        <button type="button" className="btn btn-sm" onClick={() => setYear(y => y + 1)}>→</button>
        <span className="spacer" />
        {data.length > 0 && <span className="muted">Разом: {total} робочих днів</span>}
      </div>
      {data.length === 0 ? (
        <div className="empty">
          Календар на {year} рік ще не заповнено.
          <div className="hint mt8">Додавання нового року — у наступній версії (поки що додається при оновленні програми).</div>
        </div>
      ) : (
        <>
          <div className="calendar-grid">
            {Array.from({ length: 12 }, (_, i) => i + 1).map(m => (
              <div key={m} className="calendar-cell">
                <span>{monthName(m)}</span>
                <span className="days">{byMonth.get(m) ?? '—'}</span>
              </div>
            ))}
          </div>
          <p className="hint mt16">
            Норма робочих днів — знаменник пропорції «відпрацьовано / норма» у розрахунку.
            Редагування календаря з програми — у наступній версії.
          </p>
        </>
      )}
    </>
  )
}
