import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useWorkCalendar, keys } from '../../api/hooks'
import { createCalendarYear, updateCalendarMonth } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote } from '../../components/ui'
import { monthName, parseDec } from '../../lib/format'

export function CalendarTab() {
  const [year, setYear] = useState(new Date().getFullYear())
  const { data, isPending, error } = useWorkCalendar(year)
  const qc = useQueryClient()
  const [drafts, setDrafts] = useState<Record<number, string>>({})
  const [actionError, setActionError] = useState<unknown>(null)

  const invalidate = () => qc.invalidateQueries({ queryKey: keys.workCalendar(year) })

  const createYear = useMutation({
    mutationFn: () => createCalendarYear(year),
    onSuccess: () => {
      setActionError(null)
      invalidate()
    },
    onError: setActionError,
  })

  const saveMonth = useMutation({
    mutationFn: ({ month, workDays }: { month: number; workDays: number }) =>
      updateCalendarMonth(year, month, workDays),
    onSuccess: (_, { month }) => {
      setActionError(null)
      setDrafts(d => {
        const next = { ...d }
        delete next[month]
        return next
      })
      invalidate()
    },
    onError: setActionError,
  })

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  const byMonth = new Map(data.map(m => [m.month, m.workDays]))
  const total = data.reduce((sum, m) => sum + m.workDays, 0)

  const commit = (month: number) => {
    const raw = drafts[month]
    if (raw === undefined) return
    const days = parseDec(raw)
    if (days === null || days < 0 || days > 31) {
      setActionError(new Error('Норма днів має бути від 0 до 31.'))
      return
    }
    saveMonth.mutate({ month, workDays: days })
  }

  const switchYear = (delta: number) => {
    setDrafts({})
    setActionError(null)
    setYear(y => y + delta)
  }

  return (
    <>
      <div className="row mb16">
        <button type="button" className="btn btn-sm" onClick={() => switchYear(-1)}>←</button>
        <strong style={{ fontSize: 16 }}>{year} рік</strong>
        <button type="button" className="btn btn-sm" onClick={() => switchYear(1)}>→</button>
        <span className="spacer" />
        {data.length > 0 && <span className="muted">Разом: {total} робочих днів</span>}
      </div>
      <ErrorNote error={actionError} />
      {data.length === 0 ? (
        <div className="empty">
          Календар на {year} рік ще не заповнено.
          <div className="mt16">
            <button type="button" className="btn btn-primary" onClick={() => createYear.mutate()} disabled={createYear.isPending}>
              Створити календар на {year} рік
            </button>
          </div>
          <div className="hint mt8">Створяться 12 місяців з нулями — далі проставте норму днів у кожному.</div>
        </div>
      ) : (
        <>
          <div className="calendar-grid mt8">
            {Array.from({ length: 12 }, (_, i) => i + 1).map(m => {
              const draft = drafts[m]
              const dirty = draft !== undefined
              return (
                <div key={m} className="calendar-cell">
                  <span>{monthName(m)}</span>
                  <span className="row">
                    <input
                      type="text"
                      className={dirty ? 'cell-input dirty' : 'cell-input'}
                      style={{ width: 64 }}
                      value={draft ?? String(byMonth.get(m) ?? '')}
                      onChange={e => setDrafts(d => ({ ...d, [m]: e.target.value }))}
                      onKeyDown={e => { if (e.key === 'Enter') commit(m) }}
                    />
                    {dirty && (
                      <button type="button" className="btn btn-sm btn-primary" onClick={() => commit(m)} disabled={saveMonth.isPending}>
                        ✓
                      </button>
                    )}
                  </span>
                </div>
              )
            })}
          </div>
          <p className="hint mt16">
            Норма робочих днів — знаменник пропорції «відпрацьовано / норма» у розрахунку.
            Місяць, за який уже збережено розрахунки, закритий для правок — програма поверне помилку.
          </p>
        </>
      )}
    </>
  )
}
