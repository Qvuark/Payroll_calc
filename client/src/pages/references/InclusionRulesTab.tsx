import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useInclusionRules, keys } from '../../api/hooks'
import { updateInclusionRule } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote } from '../../components/ui'
import type { AvgSalaryInclusionRule } from '../../api/types'

type FlagKey = 'includeSick' | 'includeVacation' | 'includeTraining' | 'includeCompensation'

const COLUMNS: { key: FlagKey; label: string }[] = [
  { key: 'includeSick', label: 'Лікарняні' },
  { key: 'includeVacation', label: 'Відпускні' },
  { key: 'includeTraining', label: 'Курси' },
  { key: 'includeCompensation', label: 'Компенсація' },
]

export function InclusionRulesTab() {
  const { data, isPending, error } = useInclusionRules()
  const qc = useQueryClient()

  // Перемикач зберігається одразу: шлемо всі 4 галочки з перевернутою. savingId disable-ить рядок під час запиту.
  const save = useMutation({
    mutationFn: ({ rule, key }: { rule: AvgSalaryInclusionRule; key: FlagKey }) =>
      updateInclusionRule(rule.id, {
        includeSick: rule.includeSick,
        includeVacation: rule.includeVacation,
        includeTraining: rule.includeTraining,
        includeCompensation: rule.includeCompensation,
        [key]: !rule[key],
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.inclusionRules }),
  })

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  const savingId = save.isPending ? save.variables?.rule.id : undefined

  return (
    <>
      <div className="note note-info mb16">
        Що входить у базу середньоденної. Для кожної виплати познач, чи рахувати її в середню при лікарняних,
        відпускних, навчальних курсах і компенсації за невикористану відпустку. Самі події (лікарняні, відпускні)
        у власну базу не входять. Дефолти вже виставлені — змінюй лише за потреби.
      </div>
      <ErrorNote error={save.error} />
      <div className="table-wrap mt8" style={{ maxWidth: 720 }}>
        <table>
          <thead>
            <tr>
              <th>Виплата</th>
              {COLUMNS.map(c => (
                <th key={c.key} className="num" style={{ width: 110 }}>{c.label}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map(rule => (
              <tr key={rule.id}>
                <td>{rule.label}</td>
                {COLUMNS.map(c => (
                  <td key={c.key} className="num">
                    <input
                      type="checkbox"
                      checked={rule[c.key]}
                      disabled={savingId === rule.id}
                      onChange={() => save.mutate({ rule, key: c.key })}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}
