import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useSystemParams, keys } from '../../api/hooks'
import { updateSystemParam } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote } from '../../components/ui'
import { parseDec } from '../../lib/format'

// Людські назви параметрів (ключ у БД → підпис для бухгалтера).
const PARAM_LABELS: Record<string, string> = {
  pdfo_rate: 'ПДФО (податок на доходи)',
  vz_rate: 'Військовий збір',
  esv_rate: 'ЄСВ (нараховує школа)',
  union_rate: 'Профспілковий внесок',
  bonus_1749: 'Підвищення №1749 (педагогічним)',
  prestige_rate: 'Престижність (загальна)',
  prestige_rate_director: 'Престижність (директорська гілка)',
  mzp: 'Мінімальна зарплата, грн',
  unfavorable_base: 'Несприятливі умови — база, грн',
  cabinet_standard: 'Завідування кабінетом (звичайний)',
  cabinet_music_it: 'Завідування кабінетом (музика / ІТ)',
  workshop: 'Завідування майстернею',
  gym: 'Завідування спортзалом',
  shooting_range: 'Завідування тиром',
  computers: 'Обслуговування комп’ютерної техніки',
  extracurricular: 'Позакласна робота',
  website: 'Ведення вебсайту',
  inclusive_rate: 'Інклюзивне навчання',
  class_mgmt_1_4: 'Класне керівництво (1–4 класи)',
  class_mgmt_5_11: 'Класне керівництво (5–11 класи)',
  military_accounting: 'Військовий облік',
  notebook_foreign_lang: 'Перевірка зошитів: іноземна мова',
  notebook_default: 'Перевірка зошитів: стандарт',
  notebook_lang_lit: 'Перевірка зошитів: мова та література',
  disinfectants_rate: 'Дезінфікуючі засоби',
  night_shifts_rate: 'Нічні зміни',
}

export function SystemParamsTab() {
  const { data, isPending, error } = useSystemParams()
  const qc = useQueryClient()
  const [drafts, setDrafts] = useState<Record<string, string>>({})
  const [saveError, setSaveError] = useState<unknown>(null)

  const save = useMutation({
    mutationFn: ({ key, value }: { key: string; value: number }) => updateSystemParam(key, value),
    onSuccess: (_, { key }) => {
      setDrafts(d => {
        const next = { ...d }
        delete next[key]
        return next
      })
      qc.invalidateQueries({ queryKey: keys.systemParams })
    },
    onError: setSaveError,
  })

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  const commit = (key: string) => {
    const raw = drafts[key]
    if (raw === undefined) return
    const value = parseDec(raw)
    if (value === null || value < 0) {
      setSaveError(new Error('Введіть невід’ємне число.'))
      return
    }
    setSaveError(null)
    save.mutate({ key, value })
  }

  return (
    <>
      <div className="note note-info mb16">
        Частки записуються десятковим числом: 0,18 = 18%. Зміна параметра діє на наступні розрахунки.
      </div>
      <ErrorNote error={saveError} />
      <div className="table-wrap mt8">
        <table>
          <thead>
            <tr>
              <th style={{ width: '45%' }}>Параметр</th>
              <th style={{ width: 160 }}>Значення</th>
              <th>Підказка</th>
              <th style={{ width: 110 }}></th>
            </tr>
          </thead>
          <tbody>
            {data.map(p => {
              const draft = drafts[p.key]
              const dirty = draft !== undefined
              return (
                <tr key={p.key}>
                  <td>
                    {PARAM_LABELS[p.key] ?? p.key}
                    <div className="hint mono">{p.key}</div>
                  </td>
                  <td>
                    <input
                      type="text"
                      className={dirty ? 'cell-input dirty' : 'cell-input'}
                      style={{ width: 120 }}
                      value={draft ?? String(p.value).replace('.', ',')}
                      onChange={e => setDrafts(d => ({ ...d, [p.key]: e.target.value }))}
                      onKeyDown={e => { if (e.key === 'Enter') commit(p.key) }}
                    />
                  </td>
                  <td className="muted">
                    {p.value < 1 ? `= ${(p.value * 100).toLocaleString('uk-UA')}%` : 'сума в грн'}
                  </td>
                  <td>
                    {dirty && (
                      <button type="button" className="btn btn-sm btn-primary" onClick={() => commit(p.key)} disabled={save.isPending}>
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
