import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTariffGrades, keys } from '../../api/hooks'
import { updateTariffGrade } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote } from '../../components/ui'
import { fmtDate, fmtMoney, parseDec } from '../../lib/format'

export function TariffGradesTab() {
  const { data, isPending, error } = useTariffGrades()
  const qc = useQueryClient()
  const [drafts, setDrafts] = useState<Record<number, string>>({})
  const [saveError, setSaveError] = useState<unknown>(null)

  const save = useMutation({
    mutationFn: ({ id, rate }: { id: number; rate: number }) => updateTariffGrade(id, rate),
    onSuccess: (_, { id }) => {
      setDrafts(d => {
        const next = { ...d }
        delete next[id]
        return next
      })
      qc.invalidateQueries({ queryKey: keys.tariffGrades })
    },
    onError: setSaveError,
  })

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  const commit = (id: number) => {
    const raw = drafts[id]
    if (raw === undefined) return
    const rate = parseDec(raw)
    if (rate === null || rate <= 0) {
      setSaveError(new Error('Ставка розряду має бути більшою за нуль.'))
      return
    }
    setSaveError(null)
    save.mutate({ id, rate })
  }

  return (
    <>
      <div className="note note-info mb16">
        Тарифна сітка — місячна ставка кожного розряду. Оновлюється при зміні постанови КМУ.
      </div>
      <ErrorNote error={saveError} />
      <div className="table-wrap mt8" style={{ maxWidth: 640 }}>
        <table>
          <thead>
            <tr>
              <th style={{ width: 100 }}>Розряд</th>
              <th style={{ width: 200 }}>Ставка, грн</th>
              <th>Діє з</th>
              <th style={{ width: 110 }}></th>
            </tr>
          </thead>
          <tbody>
            {data.map(g => {
              const draft = drafts[g.id]
              const dirty = draft !== undefined
              return (
                <tr key={g.id}>
                  <td><strong>{g.grade}</strong></td>
                  <td>
                    <input
                      type="text"
                      className={dirty ? 'cell-input dirty' : 'cell-input'}
                      style={{ width: 140 }}
                      value={draft ?? fmtMoney(g.monthlyRate).replace(/\s/g, ' ')}
                      onChange={e => setDrafts(d => ({ ...d, [g.id]: e.target.value }))}
                      onKeyDown={e => { if (e.key === 'Enter') commit(g.id) }}
                    />
                  </td>
                  <td className="muted">{fmtDate(g.effectiveDate)}</td>
                  <td>
                    {dirty && (
                      <button type="button" className="btn btn-sm btn-primary" onClick={() => commit(g.id)} disabled={save.isPending}>
                        Зберегти
                      </button>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}
