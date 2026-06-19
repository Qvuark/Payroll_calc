import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTariffGrades, useTitleTypes, keys } from '../../api/hooks'
import { dismissEmployeePosition, updateEmployeePosition } from '../../api/endpoints'
import { ErrorNote, Field, Modal } from '../../components/ui'
import { GRADE_RANGES, WORKER_CLASS_LABELS } from '../../api/types'
import type { EmployeePosition } from '../../api/types'
import { fmtDate, fmtMoney, fmtNum, parseDec } from '../../lib/format'
import { AdminEditor, GpdEditor, NonPedagogicalEditor, PkrEditor, WorkloadEditor } from './blockEditors'

// Відсотки у формах показуємо людськими числами (20), у БД — частка (0.20).
const pctToInput = (v: number | null): string =>
  v === null ? '' : String(Math.round(v * 10000) / 100).replace('.', ',')
const inputToPct = (raw: string): number | null => {
  const n = parseDec(raw)
  return n === null ? null : n / 100
}

/**
 * Одна ставка працівника: базові поля + блоки надбавок.
 * Набір блоків залежить від класу посади (дзеркало EmployeeValidator.ValidateBlocks):
 * Клас 1 — навантаження, педнадбавки, ГПД, ПКР; Клас 2 — без педнадбавок;
 * Класи 3/4 — лише непедагогічний блок.
 */
export function PositionCard({ employeeId, position, hasActivePrimary }: {
  employeeId: number
  position: EmployeePosition
  hasActivePrimary: boolean
}) {
  const qc = useQueryClient()
  const grades = useTariffGrades()
  const titles = useTitleTypes()
  const isDismissed = position.dismissalDate !== null
  const [expanded, setExpanded] = useState(!isDismissed)
  const [closing, setClosing] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [saved, setSaved] = useState(false)

  const [form, setForm] = useState({
    tariffGradeId: position.tariffGradeId,
    rateCount: fmtNum(position.rateCount),
    isPrimary: position.isPrimary,
    maintainsMilitaryRecords: position.maintainsMilitaryRecords,
    hasUnfavorable: position.hasUnfavorable,
    positionStartDate: position.positionStartDate ?? '',
    titleTypeId: position.titleTypeId ?? 0,
    complexityBonusPct: pctToInput(position.complexityBonusPct),
    prestigeBonusPct: pctToInput(position.prestigeBonusPct),
  })

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: keys.employee(employeeId) })
    qc.invalidateQueries({ queryKey: keys.employees })
  }

  const save = useMutation({
    mutationFn: () =>
      updateEmployeePosition(employeeId, position.id, {
        tariffGradeId: form.tariffGradeId,
        rateCount: parseDec(form.rateCount) ?? position.rateCount,
        dismissalDate: position.dismissalDate,
        isPrimary: form.isPrimary,
        maintainsMilitaryRecords: form.maintainsMilitaryRecords,
        hasUnfavorable: form.hasUnfavorable,
        positionStartDate: form.positionStartDate || null,
        titleTypeId: form.titleTypeId || null,
        complexityBonusPct: inputToPct(form.complexityBonusPct),
        prestigeBonusPct: inputToPct(form.prestigeBonusPct),
      }),
    onSuccess: () => {
      setError(null)
      setSaved(true)
      setTimeout(() => setSaved(false), 2500)
      invalidate()
    },
    onError: setError,
  })

  const close = useMutation({
    mutationFn: () => dismissEmployeePosition(employeeId, position.id),
    onSuccess: () => {
      setClosing(false)
      invalidate()
    },
    onError: setError,
  })

  // Відновлення помилково закритої ставки: знімаємо дату звільнення. Якщо у працівника
  // нема активної головної — ця стає головною (інакше лишилися б активні ставки без головної).
  const revive = useMutation({
    mutationFn: () =>
      updateEmployeePosition(employeeId, position.id, {
        tariffGradeId: position.tariffGradeId,
        rateCount: position.rateCount,
        dismissalDate: null,
        isPrimary: !hasActivePrimary,
        maintainsMilitaryRecords: position.maintainsMilitaryRecords,
        hasUnfavorable: position.hasUnfavorable,
        positionStartDate: position.positionStartDate,
        titleTypeId: position.titleTypeId,
        complexityBonusPct: position.complexityBonusPct,
        prestigeBonusPct: position.prestigeBonusPct,
      }),
    onSuccess: () => {
      setError(null)
      invalidate()
    },
    // Розгортаємо картку, щоб показати помилку (напр. 409: посаду вже пере-прийняли активною).
    onError: e => { setExpanded(true); setError(e) },
  })

  const gradeRange = GRADE_RANGES[position.workerClass]
  const allowedGrades = (grades.data ?? []).filter(g => g.grade >= gradeRange[0] && g.grade <= gradeRange[1])
  const allowedTitles = (titles.data ?? []).filter(t => t.workerClass === position.workerClass)
  const showPrestige = position.workerClass === 1 || position.workerClass === 2

  return (
    <div className="card" style={isDismissed ? { opacity: 0.65 } : undefined}>
      <div className="card-title" style={{ cursor: 'pointer' }} onClick={() => setExpanded(v => !v)}>
        <div className="row">
          <h2>{position.positionName}</h2>
          {position.isPrimary && !isDismissed && <span className="badge badge-blue">головна</span>}
          {isDismissed && <span className="badge badge-gray">закрита {fmtDate(position.dismissalDate)}</span>}
          <span className="muted">
            {position.departmentName} · клас {position.workerClass} · розряд {position.tariffGrade} ·{' '}
            {fmtNum(position.rateCount)} ст. · {fmtMoney(position.tariffMonthlyRate)} грн
          </span>
        </div>
        <div className="row">
          {isDismissed && (
            <button
              type="button"
              className="btn btn-sm"
              onClick={e => { e.stopPropagation(); revive.mutate() }}
              disabled={revive.isPending}
            >
              Відновити ставку
            </button>
          )}
          <button type="button" className="btn btn-sm btn-ghost">{expanded ? 'Згорнути ▲' : 'Розгорнути ▼'}</button>
        </div>
      </div>

      {expanded && (
        <>
          <div className="form-grid">
            <Field label="Тарифний розряд" hint={`Дозволено ${gradeRange[0]}–${gradeRange[1]} (${WORKER_CLASS_LABELS[position.workerClass]})`}>
              <select
                value={form.tariffGradeId}
                onChange={e => setForm(f => ({ ...f, tariffGradeId: Number(e.target.value) }))}
                disabled={isDismissed}
              >
                {allowedGrades.map(g => (
                  <option key={g.id} value={g.id}>{g.grade} — {g.monthlyRate.toLocaleString('uk-UA')} грн</option>
                ))}
              </select>
            </Field>
            <Field label="Кількість ставок">
              <input
                type="text"
                value={form.rateCount}
                onChange={e => setForm(f => ({ ...f, rateCount: e.target.value }))}
                disabled={isDismissed}
              />
            </Field>
            <Field label="На посаді з" hint="порожньо = дата прийняття у школу">
              <input
                type="date"
                value={form.positionStartDate}
                onChange={e => setForm(f => ({ ...f, positionStartDate: e.target.value }))}
                disabled={isDismissed}
              />
            </Field>
            {(allowedTitles.length > 0 || position.titleTypeId !== null) && (
              <Field label="Звання">
                {allowedTitles.length > 0 ? (
                  <select
                    value={form.titleTypeId}
                    onChange={e => setForm(f => ({ ...f, titleTypeId: Number(e.target.value) }))}
                    disabled={isDismissed}
                  >
                    <option value={0}>Без звання</option>
                    {allowedTitles.map(t => (
                      <option key={t.id} value={t.id}>{t.name} (+{Math.round(t.pct * 100)}%)</option>
                    ))}
                  </select>
                ) : (
                  <input type="text" value={position.titleTypeName ?? ''} disabled />
                )}
              </Field>
            )}
            <Field label="Складність і напруженість, %" hint="5–50, порожньо = немає">
              <input
                type="text"
                value={form.complexityBonusPct}
                onChange={e => setForm(f => ({ ...f, complexityBonusPct: e.target.value }))}
                disabled={isDismissed}
              />
            </Field>
            {showPrestige && (
              <Field label="Престижність, %" hint="вчителі 20, директорська гілка 25">
                <input
                  type="text"
                  value={form.prestigeBonusPct}
                  onChange={e => setForm(f => ({ ...f, prestigeBonusPct: e.target.value }))}
                  disabled={isDismissed}
                />
              </Field>
            )}
          </div>

          <div className="row mt16">
            <label className="check">
              <input
                type="checkbox"
                checked={form.isPrimary}
                onChange={e => setForm(f => ({ ...f, isPrimary: e.target.checked }))}
                disabled={isDismissed}
              />
              Головна ставка
            </label>
            <label className="check">
              <input
                type="checkbox"
                checked={form.maintainsMilitaryRecords}
                onChange={e => setForm(f => ({ ...f, maintainsMilitaryRecords: e.target.checked }))}
                disabled={isDismissed}
              />
              Військовий облік (+5%)
            </label>
            <label className="check">
              <input
                type="checkbox"
                checked={form.hasUnfavorable}
                onChange={e => setForm(f => ({ ...f, hasUnfavorable: e.target.checked }))}
                disabled={isDismissed}
              />
              Несприятливі умови (доплата 2600)
            </label>
          </div>

          <ErrorNote error={error} />

          {!isDismissed && (
            <div className="row mt16">
              {saved && <span className="badge badge-green">Збережено</span>}
              <span className="spacer" />
              <button type="button" className="btn btn-danger btn-sm" onClick={() => setClosing(true)}>
                Закрити ставку
              </button>
              <button type="button" className="btn btn-primary btn-sm" onClick={() => save.mutate()} disabled={save.isPending}>
                Зберегти ставку
              </button>
            </div>
          )}

          <BlocksSection employeeId={employeeId} position={position} readOnly={isDismissed} />
        </>
      )}

      {closing && (
        <Modal title="Закрити ставку?">
          <p>
            Ставку «{position.positionName}» буде закрито сьогоднішньою датою.
            Якщо це головна ставка — головною стане інша активна.
          </p>
          <ErrorNote error={error} />
          <div className="modal-actions">
            <button type="button" className="btn" onClick={() => setClosing(false)}>Скасувати</button>
            <button type="button" className="btn btn-danger" onClick={() => close.mutate()} disabled={close.isPending}>
              Закрити ставку
            </button>
          </div>
        </Modal>
      )}
    </div>
  )
}

/**
 * Блоки надбавок ставки. Матриця дозволеного per клас —
 * дзеркало бекендового EmployeeValidator.ValidateBlocks.
 */
function BlocksSection({ employeeId, position, readOnly }: {
  employeeId: number
  position: EmployeePosition
  readOnly: boolean
}) {
  const wc = position.workerClass
  const allowWorkload = wc === 1 || wc === 2
  const allowAdmin = wc === 1
  const allowGpdPkr = wc === 1 || wc === 2
  const allowNonPed = wc === 3 || wc === 4

  return (
    <div className="mt16">
      {allowWorkload && (
        <WorkloadEditor employeeId={employeeId} position={position} readOnly={readOnly} />
      )}
      {allowAdmin && (
        <AdminEditor employeeId={employeeId} position={position} readOnly={readOnly} />
      )}
      {allowGpdPkr && (
        <>
          <GpdEditor employeeId={employeeId} position={position} readOnly={readOnly} />
          <PkrEditor employeeId={employeeId} position={position} readOnly={readOnly} />
        </>
      )}
      {allowNonPed && (
        <NonPedagogicalEditor employeeId={employeeId} position={position} readOnly={readOnly} />
      )}
    </div>
  )
}
