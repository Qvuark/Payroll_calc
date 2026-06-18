import { useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useTimesheets, keys } from '../../api/hooks'
import { importTimesheet, upsertTimesheet } from '../../api/endpoints'
import { Loading, LoadError } from '../../components/ui'
import { ImportDialog } from '../../components/ImportDialog'
import { MonthPicker } from '../../components/MonthPicker'
import { currentPeriod, type Period } from '../../lib/period'
import type { Timesheet, TimesheetRequest } from '../../api/types'
import { fmtNum, parseDec } from '../../lib/format'

// Числові колонки табеля: перша група — час, далі ручні суми.
const COLS = [
  { key: 'workedDays', label: 'Дні' },
  { key: 'workedHours', label: 'Год.' },
  { key: 'nightHours', label: 'Нічні год.' },
  { key: 'replacementHours', label: 'Заміни год.' },
  { key: 'bonus', label: 'Премія' },
  { key: 'sickEmployer', label: 'Лікарн. (роб.)' },
  { key: 'sickFss', label: 'Лікарн. (ФСС)' },
  { key: 'vacation', label: 'Відпускні' },
  { key: 'holidayAmount', label: 'Святкові' },
  { key: 'annualBonus', label: 'Щоріч. винаг.' },
  { key: 'recalculation', label: 'Перерахунок' },
  { key: 'physEducation', label: 'Фізвих.' },
  { key: 'vacationCompensation', label: 'Компенс. відп.' },
  { key: 'downtime', label: 'Простій' },
  { key: 'courses', label: 'Курси' },
  { key: 'indexation', label: 'Індексація' },
  { key: 'advance', label: 'Аванс' },
  { key: 'enforcementOrders', label: 'Викон. листи' },
] as const

type NumKey = (typeof COLS)[number]['key']

type Edits = Record<number, Partial<Record<NumKey, string>>>

export function TimesheetPage() {
  const [period, setPeriod] = useState<Period>(currentPeriod)
  const { data, isPending, error } = useTimesheets(period.year, period.month)
  const qc = useQueryClient()
  const [edits, setEdits] = useState<Edits>({})
  const [rowErrors, setRowErrors] = useState<Record<number, string>>({})
  const [saving, setSaving] = useState(false)
  const [importing, setImporting] = useState(false)

  const dirtyIds = useMemo(() => Object.keys(edits).map(Number), [edits])

  const changePeriod = (p: Period) => {
    if (dirtyIds.length > 0 && !window.confirm('Є незбережені зміни. Перейти без збереження?')) return
    setEdits({})
    setRowErrors({})
    setPeriod(p)
  }

  const setCell = (employeeId: number, key: NumKey, value: string) => {
    setEdits(prev => ({
      ...prev,
      [employeeId]: { ...prev[employeeId], [key]: value },
    }))
  }

  const buildRequest = (row: Timesheet): TimesheetRequest => {
    const rowEdits = edits[row.employeeId] ?? {}
    const val = (key: NumKey): number => {
      const raw = rowEdits[key]
      if (raw === undefined) return row[key]
      return parseDec(raw) ?? 0
    }
    return {
      employeeId: row.employeeId,
      year: period.year,
      month: period.month,
      workedDays: val('workedDays'),
      workedHours: val('workedHours'),
      nightHours: val('nightHours'),
      replacementHours: val('replacementHours'),
      holidayAmount: val('holidayAmount'),
      advance: val('advance'),
      enforcementOrders: val('enforcementOrders'),
      annualBonus: val('annualBonus'),
      bonus: val('bonus'),
      sickEmployer: val('sickEmployer'),
      sickFss: val('sickFss'),
      vacation: val('vacation'),
      recalculation: val('recalculation'),
      physEducation: val('physEducation'),
      vacationCompensation: val('vacationCompensation'),
      downtime: val('downtime'),
      courses: val('courses'),
      indexation: val('indexation'),
    }
  }

  const saveAll = async () => {
    if (!data) return
    setSaving(true)
    const nextErrors: Record<number, string> = {}
    for (const employeeId of dirtyIds) {
      const row = data.find(r => r.employeeId === employeeId)
      if (!row) continue
      try {
        await upsertTimesheet(buildRequest(row))
        setEdits(prev => {
          const next = { ...prev }
          delete next[employeeId]
          return next
        })
      } catch (e) {
        nextErrors[employeeId] = e instanceof Error ? e.message : 'Помилка збереження'
      }
    }
    setRowErrors(nextErrors)
    setSaving(false)
    qc.invalidateQueries({ queryKey: keys.timesheets(period.year, period.month) })
  }

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Табель</h1>
          <p>Відпрацьований час і ручні суми за місяць. Зміни підсвічуються — не забудьте зберегти.</p>
        </div>
        <div className="row">
          <button type="button" className="btn" onClick={() => setImporting(true)}>⬆ Імпорт з Excel</button>
        </div>
      </div>

      <div className="row mb16">
        <MonthPicker value={period} onChange={changePeriod} />
        <span className="spacer" />
        <span className="muted">{data.length} працівників</span>
      </div>

      <div className="table-wrap" style={{ maxHeight: 'calc(100vh - 290px)' }}>
        <table>
          <thead>
            <tr>
              <th className="sticky-col">ПІБ</th>
              {COLS.map(c => <th key={c.key} className="num">{c.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {data.map(row => {
              const rowEdits = edits[row.employeeId]
              const rowError = rowErrors[row.employeeId]
              return (
                <tr key={row.employeeId}>
                  <td className="sticky-col">
                    <strong>{row.fullName}</strong>
                    <div className="hint">№ {row.tabNumber}</div>
                    {rowError && <div className="hint" style={{ color: 'var(--danger)' }}>{rowError}</div>}
                  </td>
                  {COLS.map(c => {
                    const edited = rowEdits?.[c.key]
                    return (
                      <td key={c.key} className="num">
                        <input
                          type="text"
                          className={edited !== undefined ? 'cell-input dirty' : 'cell-input'}
                          value={edited ?? fmtNum(row[c.key])}
                          onChange={e => setCell(row.employeeId, c.key, e.target.value)}
                        />
                      </td>
                    )
                  })}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {dirtyIds.length > 0 && (
        <div className="savebar">
          <span>Змінено: {dirtyIds.length} {dirtyIds.length === 1 ? 'працівник' : 'працівників'}</span>
          <span className="spacer" />
          <button type="button" className="btn" onClick={() => { setEdits({}); setRowErrors({}) }} disabled={saving}>
            Скасувати зміни
          </button>
          <button type="button" className="btn btn-primary" onClick={saveAll} disabled={saving}>
            {saving ? 'Зберігаю…' : 'Зберегти все'}
          </button>
        </div>
      )}

      {importing && (
        <ImportDialog
          title={`Імпорт табеля: ${period.month.toString().padStart(2, '0')}.${period.year}`}
          description="Оновлює дні, заміни та нічні години з файлу. Грошові колонки (аванс, премії) імпорт не чіпає — вони вводяться тут вручну."
          templatePath={`/import/templates/timesheet?year=${period.year}&month=${period.month}`}
          templateName={`timesheet_${period.year}_${period.month}.xlsx`}
          onImport={file => importTimesheet(file, period.year, period.month)}
          invalidateKeys={[keys.timesheets(period.year, period.month)]}
          onClose={() => setImporting(false)}
        />
      )}
    </>
  )
}
