import type { CalcComponent } from '../api/types'
import { fmtMoney } from '../lib/format'

/**
 * Покомпонентний розклад розрахунку: дві колонки — нарахування й утримання,
 * у кожному рядку сума + жива формула. Спільний для сторінки розрахунку
 * (усі працівники) і картки працівника (превʼю однієї людини).
 */
export function CalcBreakdown({ earnings, deductions }: {
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
