import { useState } from 'react'
import type { ReactNode } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNotebookRates, useTariffGrades, keys } from '../../api/hooks'
import { deleteBlock, putAdmin, putGpd, putNonPedagogical, putPkr, putWorkload } from '../../api/endpoints'
import type { BlockKind } from '../../api/endpoints'
import { ErrorNote, Field } from '../../components/ui'
import { CABINET_TYPE_LABELS, CLASS_GRADE_GROUP_LABELS, GPD_GRADE_RANGE, PKR_GRADE_RANGE } from '../../api/types'
import type { CabinetType, ClassGradeGroup, EmployeePosition } from '../../api/types'
import { fmtNum, parseDec } from '../../lib/format'

// Спільна обгортка блока: якщо блока нема — кнопка додавання,
// якщо є (або щойно додається) — форма з кнопками «Прибрати» і збереження.
function BlockShell({ title, hint, exists, readOnly, children, onStart, started, employeeId, posId, kind, onRemoved }: {
  title: string
  hint: string
  exists: boolean
  readOnly: boolean
  started: boolean
  onStart: () => void
  employeeId: number
  posId: number
  kind: BlockKind
  onRemoved: () => void
  children: ReactNode
}) {
  const qc = useQueryClient()
  const [removeError, setRemoveError] = useState<unknown>(null)
  const remove = useMutation({
    mutationFn: () => deleteBlock(employeeId, posId, kind),
    onSuccess: () => {
      setRemoveError(null)
      onRemoved()
      qc.invalidateQueries({ queryKey: keys.employee(employeeId) })
    },
    onError: setRemoveError,
  })

  if (!exists && !started) {
    return (
      <div className="row mt8" style={{ borderTop: '1px solid var(--border)', paddingTop: 10 }}>
        <span style={{ fontWeight: 600 }}>{title}</span>
        <span className="hint">{hint}</span>
        <span className="spacer" />
        {!readOnly && (
          <button type="button" className="btn btn-sm" onClick={onStart}>+ Додати</button>
        )}
      </div>
    )
  }
  const confirmRemove = () => {
    if (window.confirm(`Прибрати блок «${title}» з цієї ставки? Введені в ньому дані буде втрачено.`))
      remove.mutate()
  }
  return (
    <div className="mt8" style={{ borderTop: '1px solid var(--border)', paddingTop: 10 }}>
      <div className="row mb16">
        <span style={{ fontWeight: 600 }}>{title}</span>
        <span className="hint">{hint}</span>
        <span className="spacer" />
        {!readOnly && (
          <button type="button" className="btn btn-sm btn-danger" onClick={confirmRemove} disabled={remove.isPending}>
            Прибрати
          </button>
        )}
      </div>
      <ErrorNote error={removeError} />
      {children}
    </div>
  )
}

function SaveRow({ saved, saving, disabled, onSave }: {
  saved: boolean
  saving: boolean
  disabled?: boolean
  onSave: () => void
}) {
  return (
    <div className="row mt8">
      {saved && <span className="badge badge-green">Збережено</span>}
      <span className="spacer" />
      <button type="button" className="btn btn-primary btn-sm" onClick={onSave} disabled={saving || disabled}>
        Зберегти блок
      </button>
    </div>
  )
}

/** Спільний стан збереження блока: мутація + бейдж «Збережено» + інвалідація картки. */
function useBlockSave(employeeId: number, fn: () => Promise<unknown>) {
  const qc = useQueryClient()
  const [error, setError] = useState<unknown>(null)
  const [saved, setSaved] = useState(false)
  const mutation = useMutation({
    mutationFn: fn,
    onSuccess: () => {
      setError(null)
      setSaved(true)
      setTimeout(() => setSaved(false), 2500)
      qc.invalidateQueries({ queryKey: keys.employee(employeeId) })
    },
    onError: setError,
  })
  return { mutation, error, saved }
}

const num = (raw: string): number => parseDec(raw) ?? 0

// ─── Навантаження (класи 1–2) ───

export function WorkloadEditor({ employeeId, position, readOnly }: {
  employeeId: number
  position: EmployeePosition
  readOnly: boolean
}) {
  const block = position.workload
  const rates = useNotebookRates()
  const [started, setStarted] = useState(false)
  const [form, setForm] = useState(() => ({
    hours1To4: fmtNum(block?.hours1To4 ?? 0),
    individualHours1To4: fmtNum(block?.individualHours1To4 ?? 0),
    hours5To9: fmtNum(block?.hours5To9 ?? 0),
    individualHours5To9: fmtNum(block?.individualHours5To9 ?? 0),
    hours10To11: fmtNum(block?.hours10To11 ?? 0),
    individualHours10To11: fmtNum(block?.individualHours10To11 ?? 0),
    notebookHours1To4: fmtNum(block?.notebookHours1To4 ?? 0),
    notebookHours5To9: fmtNum(block?.notebookHours5To9 ?? 0),
    notebookHours10To11: fmtNum(block?.notebookHours10To11 ?? 0),
    inclusiveHours1To4: fmtNum(block?.inclusiveHours1To4 ?? 0),
    inclusiveHours5To9: fmtNum(block?.inclusiveHours5To9 ?? 0),
    inclusiveHours10To11: fmtNum(block?.inclusiveHours10To11 ?? 0),
    notebookRateId: block?.notebookRateId ?? 0,
  }))

  const { mutation, error, saved } = useBlockSave(employeeId, () =>
    putWorkload(employeeId, position.id, {
      hours1To4: num(form.hours1To4),
      individualHours1To4: num(form.individualHours1To4),
      hours5To9: num(form.hours5To9),
      individualHours5To9: num(form.individualHours5To9),
      hours10To11: num(form.hours10To11),
      individualHours10To11: num(form.individualHours10To11),
      notebookHours1To4: num(form.notebookHours1To4),
      notebookHours5To9: num(form.notebookHours5To9),
      notebookHours10To11: num(form.notebookHours10To11),
      inclusiveHours1To4: num(form.inclusiveHours1To4),
      inclusiveHours5To9: num(form.inclusiveHours5To9),
      inclusiveHours10To11: num(form.inclusiveHours10To11),
      notebookRateId: form.notebookRateId || null,
    }),
  )

  type RowKey = 'hours' | 'individualHours' | 'notebookHours' | 'inclusiveHours'
  const rows: { key: RowKey; label: string }[] = [
    { key: 'hours', label: 'Тижневі години' },
    { key: 'individualHours', label: 'Індивідуальні' },
    { key: 'notebookHours', label: 'Перевірка зошитів' },
    { key: 'inclusiveHours', label: 'Інклюзивні' },
  ]
  const groups = ['1To4', '5To9', '10To11'] as const
  const fieldKey = (row: RowKey, g: (typeof groups)[number]) => `${row}${g}` as keyof typeof form

  const hasNotebookHours =
    num(form.notebookHours1To4) + num(form.notebookHours5To9) + num(form.notebookHours10To11) > 0

  return (
    <BlockShell
      title="Навантаження"
      hint="години на тиждень: уроки, індивідуальні, зошити, інклюзив"
      exists={block !== null}
      readOnly={readOnly}
      started={started}
      onStart={() => setStarted(true)}
      employeeId={employeeId}
      posId={position.id}
      kind="workload"
      onRemoved={() => setStarted(false)}
    >
      <table style={{ maxWidth: 560 }}>
        <thead>
          <tr>
            <th></th>
            <th className="num">1–4 кл</th>
            <th className="num">5–9 кл</th>
            <th className="num">10–11 кл</th>
          </tr>
        </thead>
        <tbody>
          {rows.map(r => (
            <tr key={r.key}>
              <td className="muted">{r.label}</td>
              {groups.map(g => {
                const fk = fieldKey(r.key, g)
                return (
                  <td key={g} className="num">
                    <input
                      type="text"
                      className="cell-input"
                      value={String(form[fk])}
                      onChange={e => setForm(f => ({ ...f, [fk]: e.target.value }))}
                      disabled={readOnly}
                    />
                  </td>
                )
              })}
            </tr>
          ))}
        </tbody>
      </table>
      {hasNotebookHours && (
        <div className="mt8" style={{ maxWidth: 360 }}>
          <Field label="Предмет для % зошитів" hint="Відсоток оплати залежить від предмета">
            <select
              value={form.notebookRateId}
              onChange={e => setForm(f => ({ ...f, notebookRateId: Number(e.target.value) }))}
              disabled={readOnly}
            >
              <option value={0}>Не вибрано</option>
              {(rates.data ?? []).map(r => (
                <option key={r.id} value={r.id}>{r.subjectKeyword} — {Math.round(r.pct * 100)}%</option>
              ))}
            </select>
          </Field>
          {hasNotebookHours && form.notebookRateId === 0 && (
            <div className="note note-warn mt8">
              Увага: години зошитів без предмета — надбавка за зошити буде 0 грн.
            </div>
          )}
        </div>
      )}
      <ErrorNote error={error} />
      {!readOnly && <SaveRow saved={saved} saving={mutation.isPending} onSave={() => mutation.mutate()} />}
    </BlockShell>
  )
}

// ─── Педагогічні надбавки (клас 1) ───

export function AdminEditor({ employeeId, position, readOnly }: {
  employeeId: number
  position: EmployeePosition
  readOnly: boolean
}) {
  const block = position.admin
  const [started, setStarted] = useState(false)
  const [form, setForm] = useState(() => ({
    hasClassMgmt: block?.hasClassMgmt ?? false,
    classGradeGroup: (block?.classGradeGroup ?? 1) as ClassGradeGroup,
    hasCabinet: block?.hasCabinet ?? false,
    cabinetType: (block?.cabinetType ?? 0) as CabinetType,
    hasGym: block?.hasGym ?? false,
    hasShootingRange: block?.hasShootingRange ?? false,
    hasComputers: block?.hasComputers ?? false,
    hasExtracurricular: block?.hasExtracurricular ?? false,
    hasWebsite: block?.hasWebsite ?? false,
  }))

  const { mutation, error, saved } = useBlockSave(employeeId, () =>
    putAdmin(employeeId, position.id, {
      hasClassMgmt: form.hasClassMgmt,
      classGradeGroup: form.hasClassMgmt ? form.classGradeGroup : null,
      hasCabinet: form.hasCabinet,
      cabinetType: form.hasCabinet ? form.cabinetType : null,
      hasGym: form.hasGym,
      hasShootingRange: form.hasShootingRange,
      hasComputers: form.hasComputers,
      hasExtracurricular: form.hasExtracurricular,
      hasWebsite: form.hasWebsite,
    }),
  )

  const toggles: { key: 'hasGym' | 'hasShootingRange' | 'hasComputers' | 'hasExtracurricular' | 'hasWebsite'; label: string }[] = [
    { key: 'hasGym', label: 'Спортзал' },
    { key: 'hasShootingRange', label: 'Тир' },
    { key: 'hasComputers', label: 'Комп’ютерна техніка' },
    { key: 'hasExtracurricular', label: 'Позакласна робота' },
    { key: 'hasWebsite', label: 'Ведення вебсайту' },
  ]

  return (
    <BlockShell
      title="Педагогічні надбавки"
      hint="класне керівництво, кабінет, спортзал, сайт…"
      exists={block !== null}
      readOnly={readOnly}
      started={started}
      onStart={() => setStarted(true)}
      employeeId={employeeId}
      posId={position.id}
      kind="admin"
      onRemoved={() => setStarted(false)}
    >
      <div className="row">
        <label className="check">
          <input
            type="checkbox"
            checked={form.hasClassMgmt}
            onChange={e => setForm(f => ({ ...f, hasClassMgmt: e.target.checked }))}
            disabled={readOnly}
          />
          Класне керівництво
        </label>
        {form.hasClassMgmt && (
          <select
            style={{ width: 150 }}
            value={form.classGradeGroup}
            onChange={e => setForm(f => ({ ...f, classGradeGroup: Number(e.target.value) as ClassGradeGroup }))}
            disabled={readOnly}
          >
            {([0, 1] as const).map(g => <option key={g} value={g}>{CLASS_GRADE_GROUP_LABELS[g]}</option>)}
          </select>
        )}
      </div>
      <div className="row mt8">
        <label className="check">
          <input
            type="checkbox"
            checked={form.hasCabinet}
            onChange={e => setForm(f => ({ ...f, hasCabinet: e.target.checked }))}
            disabled={readOnly}
          />
          Завідування кабінетом
        </label>
        {form.hasCabinet && (
          <select
            style={{ width: 200 }}
            value={form.cabinetType}
            onChange={e => setForm(f => ({ ...f, cabinetType: Number(e.target.value) as CabinetType }))}
            disabled={readOnly}
          >
            {([0, 1, 2] as const).map(t => <option key={t} value={t}>{CABINET_TYPE_LABELS[t]}</option>)}
          </select>
        )}
      </div>
      <div className="row mt8">
        {toggles.map(t => (
          <label className="check" key={t.key}>
            <input
              type="checkbox"
              checked={form[t.key]}
              onChange={e => setForm(f => ({ ...f, [t.key]: e.target.checked }))}
              disabled={readOnly}
            />
            {t.label}
          </label>
        ))}
      </div>
      <ErrorNote error={error} />
      {!readOnly && <SaveRow saved={saved} saving={mutation.isPending} onSave={() => mutation.mutate()} />}
    </BlockShell>
  )
}

// ─── ГПД / ПКР (класи 1–2) ───

function HoursWithGradeEditor({ employeeId, position, readOnly, title, hint, hoursLabel, range, block, kind, save }: {
  employeeId: number
  position: EmployeePosition
  readOnly: boolean
  title: string
  hint: string
  hoursLabel: string
  range: [number, number]
  block: { tariffGradeId: number; hours: number } | null
  kind: BlockKind
  save: (employeeId: number, posId: number, hours: number, tariffGradeId: number) => Promise<unknown>
}) {
  const grades = useTariffGrades()
  const [started, setStarted] = useState(false)
  const [hours, setHours] = useState(fmtNum(block?.hours ?? 0))
  const [gradeId, setGradeId] = useState(block?.tariffGradeId ?? 0)

  const { mutation, error, saved } = useBlockSave(employeeId, () =>
    save(employeeId, position.id, num(hours), gradeId),
  )

  const allowed = (grades.data ?? []).filter(g => g.grade >= range[0] && g.grade <= range[1])

  return (
    <BlockShell
      title={title}
      hint={hint}
      exists={block !== null}
      readOnly={readOnly}
      started={started}
      onStart={() => setStarted(true)}
      employeeId={employeeId}
      posId={position.id}
      kind={kind}
      onRemoved={() => setStarted(false)}
    >
      <div className="row">
        <Field label={hoursLabel}>
          <input type="text" style={{ width: 110 }} value={hours} onChange={e => setHours(e.target.value)} disabled={readOnly} />
        </Field>
        <Field label="Тарифний розряд" hint={`дозволено ${range[0]}–${range[1]}`}>
          <select value={gradeId} onChange={e => setGradeId(Number(e.target.value))} disabled={readOnly} style={{ width: 220 }}>
            <option value={0} disabled>Оберіть…</option>
            {allowed.map(g => (
              <option key={g.id} value={g.id}>{g.grade} — {g.monthlyRate.toLocaleString('uk-UA')} грн</option>
            ))}
          </select>
        </Field>
      </div>
      <ErrorNote error={error} />
      {!readOnly && (
        <SaveRow saved={saved} saving={mutation.isPending} disabled={gradeId === 0} onSave={() => mutation.mutate()} />
      )}
    </BlockShell>
  )
}

export function GpdEditor(props: { employeeId: number; position: EmployeePosition; readOnly: boolean }) {
  const gpd = props.position.gpd
  return (
    <HoursWithGradeEditor
      {...props}
      title="ГПД"
      hint="група продовженого дня — окрема оплата з власним розрядом"
      hoursLabel="Годин на тиждень"
      range={GPD_GRADE_RANGE}
      block={gpd ? { tariffGradeId: gpd.tariffGradeId, hours: gpd.gpdHours } : null}
      kind="gpd"
      save={(empId, posId, hours, tariffGradeId) => putGpd(empId, posId, { gpdHours: hours, tariffGradeId })}
    />
  )
}

export function PkrEditor(props: { employeeId: number; position: EmployeePosition; readOnly: boolean }) {
  const pkr = props.position.pkr
  return (
    <HoursWithGradeEditor
      {...props}
      title="ПКР"
      hint="гуртки та секції — окрема оплата з власним розрядом"
      hoursLabel="Годин на тиждень"
      range={PKR_GRADE_RANGE}
      block={pkr ? { tariffGradeId: pkr.tariffGradeId, hours: pkr.pkrHours } : null}
      kind="pkr"
      save={(empId, posId, hours, tariffGradeId) => putPkr(empId, posId, { pkrHours: hours, tariffGradeId })}
    />
  )
}

// ─── Непедагогічні надбавки (класи 3–4) ───

export function NonPedagogicalEditor({ employeeId, position, readOnly }: {
  employeeId: number
  position: EmployeePosition
  readOnly: boolean
}) {
  const block = position.nonPedagogical
  const [started, setStarted] = useState(false)
  const [form, setForm] = useState(() => ({
    hasDisinfectants: block?.hasDisinfectants ?? false,
    hasNightShifts: block?.hasNightShifts ?? false,
    hasMentor: block?.hasMentor ?? false,
    mentorAmount: fmtNum(block?.mentorAmount ?? 0),
    hasLibraryMgmt: block?.hasLibraryMgmt ?? false,
    libraryMgmtAmount: fmtNum(block?.libraryMgmtAmount ?? 0),
    hasTextbooks: block?.hasTextbooks ?? false,
    textbooksAmount: fmtNum(block?.textbooksAmount ?? 0),
  }))

  const { mutation, error, saved } = useBlockSave(employeeId, () =>
    putNonPedagogical(employeeId, position.id, {
      hasDisinfectants: form.hasDisinfectants,
      hasNightShifts: form.hasNightShifts,
      hasMentor: form.hasMentor,
      mentorAmount: form.hasMentor ? num(form.mentorAmount) : 0,
      hasLibraryMgmt: form.hasLibraryMgmt,
      libraryMgmtAmount: form.hasLibraryMgmt ? num(form.libraryMgmtAmount) : 0,
      hasTextbooks: form.hasTextbooks,
      textbooksAmount: form.hasTextbooks ? num(form.textbooksAmount) : 0,
    }),
  )

  const amountRow = (
    checkKey: 'hasMentor' | 'hasLibraryMgmt' | 'hasTextbooks',
    amountKey: 'mentorAmount' | 'libraryMgmtAmount' | 'textbooksAmount',
    label: string,
  ) => (
    <div className="row mt8">
      <label className="check" style={{ minWidth: 260 }}>
        <input
          type="checkbox"
          checked={form[checkKey]}
          onChange={e => setForm(f => ({ ...f, [checkKey]: e.target.checked }))}
          disabled={readOnly}
        />
        {label}
      </label>
      {form[checkKey] && (
        <input
          type="text"
          style={{ width: 140 }}
          placeholder="Сума, грн"
          value={form[amountKey]}
          onChange={e => setForm(f => ({ ...f, [amountKey]: e.target.value }))}
          disabled={readOnly}
        />
      )}
    </div>
  )

  return (
    <BlockShell
      title="Непедагогічні надбавки"
      hint="нічні, дезінфекція, наставництво, бібліотека, підручники"
      exists={block !== null}
      readOnly={readOnly}
      started={started}
      onStart={() => setStarted(true)}
      employeeId={employeeId}
      posId={position.id}
      kind="nonpedagogical"
      onRemoved={() => setStarted(false)}
    >
      <div className="row">
        <label className="check">
          <input
            type="checkbox"
            checked={form.hasDisinfectants}
            onChange={e => setForm(f => ({ ...f, hasDisinfectants: e.target.checked }))}
            disabled={readOnly}
          />
          Дезінфікуючі засоби (+10%)
        </label>
        <label className="check">
          <input
            type="checkbox"
            checked={form.hasNightShifts}
            onChange={e => setForm(f => ({ ...f, hasNightShifts: e.target.checked }))}
            disabled={readOnly}
          />
          Нічні зміни (+40% за нічні години)
        </label>
      </div>
      {amountRow('hasMentor', 'mentorAmount', 'Наставництво')}
      {amountRow('hasLibraryMgmt', 'libraryMgmtAmount', 'Завідування бібліотекою')}
      {amountRow('hasTextbooks', 'textbooksAmount', 'Облік підручників')}
      <ErrorNote error={error} />
      {!readOnly && <SaveRow saved={saved} saving={mutation.isPending} onSave={() => mutation.mutate()} />}
    </BlockShell>
  )
}
