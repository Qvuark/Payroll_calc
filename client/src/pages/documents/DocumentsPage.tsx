import { useState } from 'react'
import { downloadFile } from '../../api/client'
import { ErrorNote } from '../../components/ui'
import { MonthPicker } from '../../components/MonthPicker'
import { currentPeriod, type Period } from '../../lib/period'

export function DocumentsPage() {
  const [period, setPeriod] = useState<Period>(currentPeriod)
  const [busy, setBusy] = useState<'vedomost' | 'payslips' | null>(null)
  const [error, setError] = useState<unknown>(null)

  const download = async (kind: 'vedomost' | 'payslips') => {
    setBusy(kind)
    setError(null)
    try {
      const mm = String(period.month).padStart(2, '0')
      await downloadFile(
        `/calculations/${kind}?year=${period.year}&month=${period.month}`,
        `${kind}_${period.year}_${mm}.xlsx`,
      )
    } catch (e) {
      setError(e)
    } finally {
      setBusy(null)
    }
  }

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Документи</h1>
          <p>Готові файли Excel за місяць. Програма сама перерахує всіх перед формуванням.</p>
        </div>
      </div>

      <div className="row mb16">
        <MonthPicker value={period} onChange={setPeriod} />
      </div>

      <ErrorNote error={error} />

      <div className="row" style={{ alignItems: 'stretch', gap: 16 }}>
        <div className="card doc-card">
          <h2>Відомість нарахування</h2>
          <p className="muted">
            Повна відомість по всіх працівниках у звичному форматі: колонки нарахувань,
            утримань, підсумки. У клітинках — живі формули: видно, звідки кожне число.
          </p>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => download('vedomost')}
            disabled={busy !== null}
          >
            {busy === 'vedomost' ? 'Формую…' : '⬇ Скачати відомість'}
          </button>
        </div>
        <div className="card doc-card">
          <h2>Розрахункові листи</h2>
          <p className="muted">
            Платіжки для видачі працівникам — по дві на аркуші. Кожен рядок нарахувань
            і утримань з формулою розрахунку.
          </p>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => download('payslips')}
            disabled={busy !== null}
          >
            {busy === 'payslips' ? 'Формую…' : '⬇ Скачати розрахункові листи'}
          </button>
        </div>
      </div>
    </>
  )
}
