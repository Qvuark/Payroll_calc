import { Fragment, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { calculateAll } from '../../api/endpoints'
import { ErrorNote } from '../../components/ui'
import { MonthPicker, currentPeriod } from '../../components/MonthPicker'
import type { Period } from '../../components/MonthPicker'
import type { CalcComponent, CalcResult } from '../../api/types'
import { fmtMoney, monthName } from '../../lib/format'

export function CalculationsPage() {
  const [period, setPeriod] = useState<Period>(currentPeriod)
  const [results, setResults] = useState<CalcResult[] | null>(null)
  const [openId, setOpenId] = useState<number | null>(null)

  const run = useMutation({
    mutationFn: () => calculateAll(period.year, period.month),
    onSuccess: data => {
      setResults(data)
      setOpenId(null)
    },
  })

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
                  </tr>
                  {openId === r.employeeId && (
                    <tr className="calc-detail">
                      <td colSpan={5}>
                        <ComponentsBreakdown earnings={r.earnings} deductions={r.deductions} />
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

function ComponentsBreakdown({ earnings, deductions }: {
  earnings: CalcComponent[]
  deductions: CalcComponent[]
}) {
  return (
    <div className="row" style={{ alignItems: 'flex-start', gap: 32 }}>
      <ComponentsTable title="Нарахування" components={earnings} />
      <ComponentsTable title="Утримання" components={deductions} />
    </div>
  )
}

function ComponentsTable({ title, components }: { title: string; components: CalcComponent[] }) {
  return (
    <div style={{ flex: 1, minWidth: 320 }}>
      <h3 className="mb16">{title}</h3>
      {components.length === 0 ? (
        <p className="muted">Немає</p>
      ) : (
        <table>
          <tbody>
            {components.map((c, i) => (
              <tr key={i}>
                <td>{c.name}</td>
                <td className="num" style={{ whiteSpace: 'nowrap' }}>{fmtMoney(c.amount)}</td>
                <td className="mono muted">{c.formula}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
