import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useSickLeaves, useVacations, useTrainingLeaves, keys } from '../../api/hooks'
import {
  createSickLeave, updateSickLeave, deleteSickLeave,
  createVacation, updateVacation, deleteVacation,
  createTrainingLeave, updateTrainingLeave, deleteTrainingLeave,
} from '../../api/endpoints'
import { Modal, Field, ErrorNote, Loading } from '../../components/ui'
import { VACATION_TYPE_LABELS, UNPAID_VACATION_TYPES } from '../../api/types'
import type {
  SickLeave, Vacation, TrainingLeave, VacationType,
  CreateSickLeaveRequest, CreateVacationRequest, CreateTrainingLeaveRequest,
} from '../../api/types'
import { fmtDate, fmtMoney, fmtNum, parseDec, todayIso } from '../../lib/format'

// Три типи подій середньоденної в одному списку. Сума рахується на бекенді одразу при
// збереженні (eager) — рядок показує її без окремого прогону розрахунку місяця.

type Kind = 'sick' | 'vacation' | 'training'
const ICON: Record<Kind, string> = { sick: '🤒', vacation: '🏖', training: '📚' }
const KIND_LABEL: Record<Kind, string> = { sick: 'Лікарняний', vacation: 'Відпустка', training: 'Курси' }

type EditTarget =
  | { kind: 'sick'; entity: SickLeave }
  | { kind: 'vacation'; entity: Vacation }
  | { kind: 'training'; entity: TrainingLeave }

interface Row {
  kind: Kind
  id: number
  start: string
  end: string
  total: number | null
  sub: string
  edit: EditTarget
}

function keyFor(kind: Kind, employeeId: number) {
  if (kind === 'sick') return keys.sickLeaves(employeeId)
  if (kind === 'vacation') return keys.vacations(employeeId)
  return keys.trainingLeaves(employeeId)
}

export function AbsencesSection({ employeeId }: { employeeId: number }) {
  const sick = useSickLeaves(employeeId)
  const vacations = useVacations(employeeId)
  const training = useTrainingLeaves(employeeId)
  const [adding, setAdding] = useState(false)
  const [editing, setEditing] = useState<EditTarget | null>(null)
  const [deleting, setDeleting] = useState<Row | null>(null)

  const rows: Row[] = [
    ...(sick.data ?? []).map((s): Row => ({
      kind: 'sick', id: s.id, start: s.startDate, end: s.endDate, total: s.totalAmount,
      sub: `${s.daysTotal} дн · ${Math.round(s.paymentPct * 100)}%`, edit: { kind: 'sick', entity: s },
    })),
    ...(vacations.data ?? []).map((v): Row => ({
      kind: 'vacation', id: v.id, start: v.startDate, end: v.endDate, total: v.totalAmount,
      sub: VACATION_TYPE_LABELS[v.vacationType], edit: { kind: 'vacation', entity: v },
    })),
    ...(training.data ?? []).map((t): Row => ({
      kind: 'training', id: t.id, start: t.startDate, end: t.endDate, total: t.totalAmount,
      sub: t.institutionName ?? '', edit: { kind: 'training', entity: t },
    })),
  ].sort((a, b) => b.start.localeCompare(a.start))

  const loading = sick.isPending || vacations.isPending || training.isPending

  return (
    <>
      <div className="row mt16 mb16">
        <h2>Відсутності</h2>
        <span className="spacer" />
        <button type="button" className="btn btn-primary" onClick={() => setAdding(true)}>
          + Додати відсутність
        </button>
      </div>

      <div className="card">
        {loading && <Loading />}
        {!loading && rows.length === 0 && (
          <div className="empty">Лікарняних, відпусток і курсів ще немає.</div>
        )}
        {rows.map(r => (
          <div key={`${r.kind}-${r.id}`} className="row" style={{ padding: '10px 0', borderBottom: '1px solid #eee' }}>
            <span style={{ fontSize: 20 }}>{ICON[r.kind]}</span>
            <div style={{ flex: 1 }}>
              <div>
                {KIND_LABEL[r.kind]}
                {r.sub && <span className="muted"> · {r.sub}</span>}
              </div>
              <div className="hint">{fmtDate(r.start)} – {fmtDate(r.end)}</div>
            </div>
            <div style={{ fontWeight: 600, minWidth: 130, textAlign: 'right' }}>
              {r.total == null ? <span className="muted">без виплати</span> : `${fmtMoney(r.total)} ₴`}
            </div>
            <button type="button" className="btn btn-sm" onClick={() => setEditing(r.edit)}>Змінити</button>
            <button type="button" className="btn btn-sm btn-danger" onClick={() => setDeleting(r)}>Видалити</button>
          </div>
        ))}
      </div>

      {(adding || editing) && (
        <AbsenceDialog
          employeeId={employeeId}
          editing={editing}
          onClose={() => { setAdding(false); setEditing(null) }}
        />
      )}
      {deleting && (
        <ConfirmDelete employeeId={employeeId} row={deleting} onClose={() => setDeleting(null)} />
      )}
    </>
  )
}

// ─── Форма додавання / редагування ───

interface FormState {
  startDate: string; endDate: string; notes: string
  daysTotal: string; baseAmount: string; baseExcludedDays: string; insuranceSeniorityYrs: string; paymentPct: string; efssNumber: string
  vacationType: string; calendarDays: string; workingDaysAbsent: string; baseDays: string; orderNumber: string
  baseWorkingDays: string; institutionName: string
}

const EMPTY: FormState = {
  startDate: todayIso(), endDate: todayIso(), notes: '',
  daysTotal: '', baseAmount: '', baseExcludedDays: '0', insuranceSeniorityYrs: '', paymentPct: '', efssNumber: '',
  vacationType: '0', calendarDays: '', workingDaysAbsent: '', baseDays: '', orderNumber: '',
  baseWorkingDays: '', institutionName: '',
}

function initForm(t: EditTarget | null): FormState {
  if (!t) return { ...EMPTY }
  if (t.kind === 'sick') {
    const s = t.entity
    return {
      ...EMPTY, startDate: s.startDate, endDate: s.endDate, notes: s.notes ?? '',
      daysTotal: String(s.daysTotal), baseAmount: fmtNum(s.baseAmount), baseExcludedDays: String(s.baseExcludedDays),
      insuranceSeniorityYrs: String(s.insuranceSeniorityYrs),
      paymentPct: s.paymentPct ? String(Math.round(s.paymentPct * 100)) : '', efssNumber: s.efssNumber ?? '',
    }
  }
  if (t.kind === 'vacation') {
    const v = t.entity
    return {
      ...EMPTY, startDate: v.startDate, endDate: v.endDate, notes: v.notes ?? '',
      vacationType: String(v.vacationType), calendarDays: String(v.calendarDays), workingDaysAbsent: String(v.workingDaysAbsent),
      baseAmount: v.baseAmount == null ? '' : fmtNum(v.baseAmount), baseDays: v.baseDays == null ? '' : String(v.baseDays),
      orderNumber: v.orderNumber ?? '',
    }
  }
  const tr = t.entity
  return {
    ...EMPTY, startDate: tr.startDate, endDate: tr.endDate, notes: tr.notes ?? '',
    workingDaysAbsent: String(tr.workingDaysAbsent), baseAmount: fmtNum(tr.baseAmount), baseWorkingDays: String(tr.baseWorkingDays),
    institutionName: tr.institutionName ?? '',
  }
}

function AbsenceDialog({ employeeId, editing, onClose }: {
  employeeId: number
  editing: EditTarget | null
  onClose: () => void
}) {
  const qc = useQueryClient()
  const [kind, setKind] = useState<Kind>(editing ? editing.kind : 'sick')
  const [form, setForm] = useState<FormState>(() => initForm(editing))
  const [error, setError] = useState<unknown>(null)

  const set = (k: keyof FormState) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm(f => ({ ...f, [k]: e.target.value }))

  const dec = (s: string) => parseDec(s) ?? 0
  const int = (s: string) => Math.round(parseDec(s) ?? 0)
  const vacType = Number(form.vacationType) as VacationType
  const unpaid = UNPAID_VACATION_TYPES.includes(vacType)

  const save = useMutation<unknown, unknown, void>({
    mutationFn: () => {
      if (kind === 'sick') {
        const body: CreateSickLeaveRequest = {
          startDate: form.startDate, endDate: form.endDate,
          daysTotal: int(form.daysTotal), baseAmount: dec(form.baseAmount),
          baseExcludedDays: int(form.baseExcludedDays), insuranceSeniorityYrs: int(form.insuranceSeniorityYrs),
          paymentPct: form.paymentPct.trim() === '' ? 0 : dec(form.paymentPct) / 100,
          efssNumber: form.efssNumber.trim() || null, notes: form.notes.trim() || null,
        }
        return editing ? updateSickLeave(employeeId, editing.entity.id, body) : createSickLeave(employeeId, body)
      }
      if (kind === 'vacation') {
        const body: CreateVacationRequest = {
          vacationType: vacType, startDate: form.startDate, endDate: form.endDate,
          calendarDays: int(form.calendarDays), workingDaysAbsent: int(form.workingDaysAbsent),
          baseAmount: unpaid ? null : dec(form.baseAmount), baseDays: unpaid ? null : int(form.baseDays),
          orderNumber: form.orderNumber.trim() || null, notes: form.notes.trim() || null,
        }
        return editing ? updateVacation(employeeId, editing.entity.id, body) : createVacation(employeeId, body)
      }
      const body: CreateTrainingLeaveRequest = {
        startDate: form.startDate, endDate: form.endDate,
        workingDaysAbsent: int(form.workingDaysAbsent), baseAmount: dec(form.baseAmount),
        baseWorkingDays: int(form.baseWorkingDays),
        institutionName: form.institutionName.trim() || null, notes: form.notes.trim() || null,
      }
      return editing ? updateTrainingLeave(employeeId, editing.entity.id, body) : createTrainingLeave(employeeId, body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keyFor(kind, employeeId) })
      onClose()
    },
    onError: setError,
  })

  // Мінімальна перевірка для кнопки — основну робить бек (повертає укр. повідомлення).
  const filled = (s: string) => s.trim() !== ''
  const canSave = filled(form.startDate) && filled(form.endDate) && (
    kind === 'sick' ? filled(form.daysTotal) && filled(form.baseAmount) && filled(form.insuranceSeniorityYrs) :
    kind === 'vacation' ? filled(form.calendarDays) && (unpaid || (filled(form.baseAmount) && filled(form.baseDays))) :
    filled(form.workingDaysAbsent) && filled(form.baseAmount) && filled(form.baseWorkingDays)
  )

  return (
    <Modal title={editing ? 'Редагувати відсутність' : 'Нова відсутність'}>
      {!editing && (
        <Field label="Тип відсутності">
          <select value={kind} onChange={e => setKind(e.target.value as Kind)} autoFocus>
            <option value="sick">🤒 Лікарняний</option>
            <option value="vacation">🏖 Відпустка</option>
            <option value="training">📚 Курси</option>
          </select>
        </Field>
      )}

      <div className="form-grid">
        <Field label="Дата початку">
          <input type="date" value={form.startDate} onChange={set('startDate')} />
        </Field>
        <Field label="Дата кінця">
          <input type="date" value={form.endDate} onChange={set('endDate')} />
        </Field>

        {kind === 'sick' && (
          <>
            <Field label="Днів хвороби" hint="перші 5 — школа, решта — ФСС">
              <input type="text" value={form.daysTotal} onChange={set('daysTotal')} />
            </Field>
            <Field label="База за 12 міс, грн" hint="повний заробіток за 12 місяців">
              <input type="text" value={form.baseAmount} onChange={set('baseAmount')} />
            </Field>
            <Field label="Виключені дні" hint="минулі лікарняні / декрет / без збереження">
              <input type="text" value={form.baseExcludedDays} onChange={set('baseExcludedDays')} />
            </Field>
            <Field label="Страховий стаж, років" hint="%: <3=50 · 3-5=60 · 5-8=70 · 8+=100">
              <input type="text" value={form.insuranceSeniorityYrs} onChange={set('insuranceSeniorityYrs')} />
            </Field>
            <Field label="Відсоток виплати, %" hint="порожньо = вивести зі стажу">
              <input type="text" value={form.paymentPct} onChange={set('paymentPct')} placeholder="авто" />
            </Field>
            <Field label="Номер е-лікарняного" hint="необовʼязково">
              <input type="text" value={form.efssNumber} onChange={set('efssNumber')} />
            </Field>
          </>
        )}

        {kind === 'vacation' && (
          <>
            <Field label="Тип відпустки">
              <select value={form.vacationType} onChange={set('vacationType')}>
                {([0, 1, 2, 3, 4] as VacationType[]).map(t => (
                  <option key={t} value={t}>{VACATION_TYPE_LABELS[t]}</option>
                ))}
              </select>
            </Field>
            <Field label="Календарних днів" hint="тривалість відпустки">
              <input type="text" value={form.calendarDays} onChange={set('calendarDays')} />
            </Field>
            <Field label="Робочих днів відсутності" hint="для табеля">
              <input type="text" value={form.workingDaysAbsent} onChange={set('workingDaysAbsent')} />
            </Field>
            {!unpaid && (
              <>
                <Field label="База за 12 міс, грн">
                  <input type="text" value={form.baseAmount} onChange={set('baseAmount')} />
                </Field>
                <Field label="Днів бази" hint="сума календарних днів 12 міс (≈365)">
                  <input type="text" value={form.baseDays} onChange={set('baseDays')} />
                </Field>
              </>
            )}
            <Field label="Наказ №" hint="необовʼязково">
              <input type="text" value={form.orderNumber} onChange={set('orderNumber')} />
            </Field>
          </>
        )}

        {kind === 'training' && (
          <>
            <Field label="Робочих днів на курсах">
              <input type="text" value={form.workingDaysAbsent} onChange={set('workingDaysAbsent')} />
            </Field>
            <Field label="База за 2 міс, грн" hint="виплати за 2 місяці до курсів">
              <input type="text" value={form.baseAmount} onChange={set('baseAmount')} />
            </Field>
            <Field label="Робочих днів бази" hint="робочі дні тих 2 місяців">
              <input type="text" value={form.baseWorkingDays} onChange={set('baseWorkingDays')} />
            </Field>
            <Field label="Заклад" hint="необовʼязково">
              <input type="text" value={form.institutionName} onChange={set('institutionName')} />
            </Field>
          </>
        )}
      </div>

      <Field label="Примітка">
        <input type="text" value={form.notes} onChange={set('notes')} />
      </Field>

      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => save.mutate()}
          disabled={!canSave || save.isPending}
        >
          {save.isPending ? 'Зберігаю…' : 'Зберегти'}
        </button>
      </div>
    </Modal>
  )
}

// ─── Підтвердження видалення ───

function ConfirmDelete({ employeeId, row, onClose }: {
  employeeId: number
  row: Row
  onClose: () => void
}) {
  const qc = useQueryClient()
  const [error, setError] = useState<unknown>(null)

  const del = useMutation({
    mutationFn: () => {
      if (row.kind === 'sick') return deleteSickLeave(employeeId, row.id)
      if (row.kind === 'vacation') return deleteVacation(employeeId, row.id)
      return deleteTrainingLeave(employeeId, row.id)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keyFor(row.kind, employeeId) })
      onClose()
    },
    onError: setError,
  })

  return (
    <Modal title="Видалити відсутність?">
      <p>
        {ICON[row.kind]} {KIND_LABEL[row.kind]} {fmtDate(row.start)} – {fmtDate(row.end)} буде видалено.
        Дію не можна скасувати.
      </p>
      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button type="button" className="btn btn-danger" onClick={() => del.mutate()} disabled={del.isPending}>
          Видалити
        </button>
      </div>
    </Modal>
  )
}
