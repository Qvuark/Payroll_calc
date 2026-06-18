import { Fragment, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { calculateAll, calculateOne } from '../../api/endpoints'
import { ErrorNote } from '../../components/ui'
import { CalcBreakdown } from '../../components/CalcBreakdown'
import { MonthPicker } from '../../components/MonthPicker'
import { currentPeriod, type Period } from '../../lib/period'
import type { CalcResult } from '../../api/types'
import { fmtMoney, monthName } from '../../lib/format'

export function CalculationsPage() {
  const [period, setPeriod] = useState<Period>(currentPeriod)
  const [results, setResults] = useState<CalcResult[] | null>(null)
  const [openId, setOpenId] = useState<number | null>(null)
  const [recalcingId, setRecalcingId] = useState<number | null>(null)
  const [rowError, setRowError] = useState<string | null>(null)

  const run = useMutation({
    mutationFn: () => calculateAll(period.year, period.month),
    onSuccess: data => {
      setResults(data)
      setOpenId(null)
      setRowError(null)
    },
  })

  // Перерахунок однієї людини без прогону всіх: підміняє лише її рядок у списку.
  const recalcOne = async (employeeId: number) => {
    setRecalcingId(employeeId)
    setRowError(null)
    try {
      const updated = await calculateOne(employeeId, period.year, period.month)
      setResults(prev => (prev ? prev.map(r => (r.employeeId === employeeId ? updated : r)) : prev))
    } catch (e) {
      setRowError(e instanceof Error ? e.message : 'Помилка перерахунку')
    } finally {
      setRecalcingId(null)
    }
  }

  const totals = results?.reduce(
    (acc, r) => ({
      gross: acc.gross + r.gross,
      withheld: acc.withheld + r.totalWithheld,
      net: acc.net + r.netPay,
    }),
    { gross: 0, withheld: 0, net: 0 },
  )

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Розрахунок</h1>
          <p>Рахує зарплату всім активним працівникам. Натисніть на рядок — побачите кожну надбавку з формулою.</p>
        </div>
      </div>

      <div className="row mb16">
        <MonthPicker value={period} onChange={p => { setPeriod(p); setResults(null) }} />
        <button type="button" className="btn btn-primary" onClick={() => run.mutate()} disabled={run.isPending}>
          {run.isPending ? 'Рахую…' : `Розрахувати за ${monthName(period.month).toLowerCase()}`}
        </button>
        {results && <span className="muted">Розраховано: {results.length} працівників</span>}
      </div>

      <ErrorNote error={run.error} />
      {rowError && <div className="note note-error mb16">{rowError}</div>}

      {results && results.length === 0 && (
        <div className="card empty">Немає активних працівників для розрахунку.</div>
      )}

      {results && results.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>ПІБ</th>
                <th className="num">Днів (факт / норма)</th>
                <th className="num">Нараховано</th>
                <th className="num">Утримано</th>
                <th className="num">До виплати</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {results.map(r => (
                <Fragment key={r.employeeId}>
                  <tr className="clickable" onClick={() => setOpenId(openId === r.employeeId ? null : r.employeeId)}>
                    <td><strong>{r.fullName}</strong></td>
                    <td className="num">{r.workedDays} / {r.normDays}</td>
                    <td className="num">{fmtMoney(r.gross)}</td>
                    <td className="num">{fmtMoney(r.totalWithheld)}</td>
                    <td className="num"><strong>{fmtMoney(r.netPay)}</strong></td>
                    <td className="num">
                      <button
                        type="button"
                        className="btn btn-sm btn-ghost"
                        title="Перерахувати цього працівника"
                        disabled={recalcingId === r.employeeId}
                        onClick={e => { e.stopPropagation(); recalcOne(r.employeeId) }}
                      >
                        {recalcingId === r.employeeId ? '…' : '↻'}
                      </button>
                    </td>
                  </tr>
                  {openId === r.employeeId && (
                    <tr className="calc-detail">
                      <td colSpan={6}>
                        <CalcBreakdown earnings={r.earnings} deductions={r.deductions} />
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
            {totals && (
              <tfoot>
                <tr>
                  <td colSpan={2}>Разом</td>
                  <td className="num">{fmtMoney(totals.gross)}</td>
                  <td className="num">{fmtMoney(totals.withheld)}</td>
                  <td className="num">{fmtMoney(totals.net)}</td>
                  <td></td>
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}

      {!results && !run.isPending && (
        <div className="card empty">
          Оберіть місяць і натисніть «Розрахувати».
          <div className="hint mt8">Перед розрахунком переконайтесь, що табель за місяць заповнено.</div>
        </div>
      )}
    </>
  )
}
