import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEmployee, useTariffGrades, useTitleTypes, usePositions, keys } from '../../api/hooks'
import { calculateOne, createEmployeePosition, dismissEmployee, updateEmployee } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote, Modal, Field, StatusBadge } from '../../components/ui'
import { CalcBreakdown } from '../../components/CalcBreakdown'
import { MonthPicker } from '../../components/MonthPicker'
import { currentPeriod, type Period } from '../../lib/period'
import { PositionCard } from './PositionCard'
import { AbsencesSection } from './AbsencesSection'
import { GRADE_RANGES, WORKER_CLASS_LABELS } from '../../api/types'
import type { CreatePositionRequest, EmployeeDetail, UpdateEmployeeRequest, WorkerClass } from '../../api/types'
import { fmtDate, fmtMoney, parseDec, todayIso } from '../../lib/format'

export function EmployeeCardPage() {
  const { id } = useParams()
  const employeeId = Number(id)
  const { data, isPending, error } = useEmployee(employeeId)
  const [addingPosition, setAddingPosition] = useState(false)
  const [dismissing, setDismissing] = useState(false)

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  // Активні ставки перші, головна — найперша.
  const positions = [...data.positions].sort((a, b) => {
    const aActive = a.dismissalDate === null ? 0 : 1
    const bActive = b.dismissalDate === null ? 0 : 1
    if (aActive !== bActive) return aActive - bActive
    return Number(b.isPrimary) - Number(a.isPrimary)
  })
  const hasActivePositions = data.positions.some(p => p.dismissalDate === null)
  // Чи є серед активних ставок головна — для відновлення ставки: якщо головної нема,
  // відновлена стає головною (тримаємо інваріант «рівно одна головна серед активних»).
  const hasActivePrimary = data.positions.some(p => p.dismissalDate === null && p.isPrimary)

  return (
    <>
      <div className="mb16">
        <Link to="/employees">← До списку працівників</Link>
      </div>
      <div className="page-header">
        <div>
          <h1>{data.fullName}</h1>
          <p>
            Таб. № {data.tabNumber} · ІПН {data.taxId} · Прийнято {fmtDate(data.hireDate)}
            {data.dismissalDate && ` · Звільнено ${fmtDate(data.dismissalDate)}`}
          </p>
        </div>
        <div className="row">
          <StatusBadge status={data.status} />
          {data.status !== 2 && (
            <button type="button" className="btn btn-danger" onClick={() => setDismissing(true)}>Звільнити</button>
          )}
        </div>
      </div>

      <PersonaCard employee={data} />

      <div className="row mt16 mb16">
        <h2>Ставки ({data.positions.filter(p => p.dismissalDate === null).length} активних)</h2>
        <span className="spacer" />
        <button type="button" className="btn btn-primary" onClick={() => setAddingPosition(true)}>+ Додати ставку</button>
      </div>
      {positions.length === 0 && (
        <div className="card empty">
          Ставок ще немає. Додайте посаду — без ставки зарплата не рахується.
        </div>
      )}
      {positions.map(p => (
        <PositionCard
          // Склад блоків у key: додавання/прибирання блока ремонтує картку ставки,
          // інакше локальний стан форм залишив би «привид» щойно прибраного блока.
          key={`${p.id}:${+!!p.workload}${+!!p.admin}${+!!p.gpd}${+!!p.pkr}${+!!p.nonPedagogical}`}
          employeeId={employeeId}
          position={p}
          hasActivePrimary={hasActivePrimary}
        />
      ))}

      <AbsencesSection employeeId={employeeId} />

      {positions.length > 0 && <SalaryPreview employeeId={employeeId} />}

      {addingPosition && (
        <AddPositionDialog
          employeeId={employeeId}
          suggestPrimary={!hasActivePositions}
          onClose={() => setAddingPosition(false)}
        />
      )}
      {dismissing && (
        <DismissDialog employee={data} onClose={() => setDismissing(false)} />
      )}
    </>
  )
}

// ─── Особисті дані ───

function PersonaCard({ employee }: { employee: EmployeeDetail }) {
  const qc = useQueryClient()
  const [form, setForm] = useState({
    fullName: employee.fullName,
    taxId: employee.taxId,
    education: employee.education ?? '',
    generalExperienceYears: String(employee.generalExperienceYears),
    pedExperienceYears: String(employee.pedExperienceYears),
    socialBenefitPct: employee.socialBenefitPct === null ? '' : String(employee.socialBenefitPct).replace('.', ','),
    onLeave: employee.status === 1,
    isHonored: employee.isHonored ?? false,
    honoredAmount: employee.honoredAmount == null ? '' : String(employee.honoredAmount).replace('.', ','),
  })
  const [error, setError] = useState<unknown>(null)
  const [saved, setSaved] = useState(false)

  // Тіло запиту з полів форми; статус і дату звільнення передаємо явно
  // (бек вимагає: Active → DismissalDate null; Dismissed → дата обовʼязкова).
  const body = (status: 0 | 1 | 2, dismissalDate: string | null): UpdateEmployeeRequest => ({
    fullName: form.fullName.trim(),
    taxId: form.taxId.trim(),
    dismissalDate,
    education: form.education.trim() || null,
    generalExperienceYears: parseDec(form.generalExperienceYears) ?? 0,
    pedExperienceYears: parseDec(form.pedExperienceYears) ?? 0,
    socialBenefitPct: parseDec(form.socialBenefitPct),
    status,
    isHonored: form.isHonored,
    honoredAmount: form.isHonored ? parseDec(form.honoredAmount) : null,
  })

  const onSaved = () => {
    setError(null)
    setSaved(true)
    setTimeout(() => setSaved(false), 2500)
    qc.invalidateQueries({ queryKey: keys.employee(employee.id) })
    qc.invalidateQueries({ queryKey: keys.employees })
  }

  const save = useMutation({
    mutationFn: () => updateEmployee(employee.id, body(employee.status === 2 ? 2 : form.onLeave ? 1 : 0, employee.dismissalDate)),
    onSuccess: onSaved,
    onError: setError,
  })

  // Відновлення помилково звільненого: статус → Активний, дата звільнення → null.
  // Закриті ставки лишаються закритими — їх відновлюють окремо в картці ставки.
  const reactivate = useMutation({
    mutationFn: () => updateEmployee(employee.id, body(0, null)),
    onSuccess: onSaved,
    onError: setError,
  })

  type TextKey = 'fullName' | 'taxId' | 'education' | 'generalExperienceYears' | 'pedExperienceYears' | 'socialBenefitPct'
  const set = (key: TextKey) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [key]: e.target.value }))

  return (
    <div className="card">
      <div className="card-title">
        <h2>Особисті дані</h2>
        <div className="row">
          {saved && <span className="badge badge-green">Збережено</span>}
          {employee.status === 2 && (
            <button type="button" className="btn btn-sm" onClick={() => reactivate.mutate()} disabled={reactivate.isPending}>
              Відновити працівника
            </button>
          )}
          <button type="button" className="btn btn-primary btn-sm" onClick={() => save.mutate()} disabled={save.isPending}>
            Зберегти
          </button>
        </div>
      </div>
      <div className="form-grid">
        <Field label="ПІБ повністю">
          <input type="text" value={form.fullName} onChange={set('fullName')} />
        </Field>
        <Field label="ІПН (10 цифр)">
          <input type="text" value={form.taxId} onChange={set('taxId')} maxLength={10} />
        </Field>
        <Field label="Освіта">
          <input type="text" value={form.education} onChange={set('education')} />
        </Field>
        <Field label="Загальний стаж, років" hint="на початок року — для лікарняних">
          <input type="text" value={form.generalExperienceYears} onChange={set('generalExperienceYears')} />
        </Field>
        <Field label="Педагогічний стаж, років" hint="визначає % вислуги (10/20/30)">
          <input type="text" value={form.pedExperienceYears} onChange={set('pedExperienceYears')} />
        </Field>
        <Field label="Податкова соц. пільга" hint="порожньо = пільги немає">
          <input type="text" value={form.socialBenefitPct} onChange={set('socialBenefitPct')} />
        </Field>
      </div>
      <div className="row mt16">
        {employee.status !== 2 && (
          <label className="check">
            <input type="checkbox" checked={form.onLeave} onChange={e => setForm(f => ({ ...f, onLeave: e.target.checked }))} />
            У відпустці (декрет / навчальна)
          </label>
        )}
        <label className="check">
          <input type="checkbox" checked={form.isHonored} onChange={e => setForm(f => ({ ...f, isHonored: e.target.checked }))} />
          Заслужений вчитель / працівник освіти
        </label>
        {form.isHonored && (
          <input
            type="text"
            style={{ width: 140 }}
            placeholder="Сума, грн"
            value={form.honoredAmount}
            onChange={e => setForm(f => ({ ...f, honoredAmount: e.target.value }))}
          />
        )}
      </div>
      <ErrorNote error={error} />
    </div>
  )
}

// ─── Нова ставка ───

function AddPositionDialog({ employeeId, suggestPrimary, onClose }: {
  employeeId: number
  suggestPrimary: boolean
  onClose: () => void
}) {
  const qc = useQueryClient()
  const positions = usePositions()
  const grades = useTariffGrades()
  const titles = useTitleTypes()
  const [positionId, setPositionId] = useState(0)
  const [tariffGradeId, setTariffGradeId] = useState(0)
  const [rateCount, setRateCount] = useState('1')
  const [hireDate, setHireDate] = useState(todayIso())
  const [isPrimary, setIsPrimary] = useState(suggestPrimary)
  const [titleTypeId, setTitleTypeId] = useState(0)
  const [error, setError] = useState<unknown>(null)

  const selectedPosition = positions.data?.find(p => p.id === positionId) ?? null
  const workerClass: WorkerClass | null = selectedPosition?.workerClass ?? null
  // Розряди обмежені діапазоном класу посади — недозволені навіть не показуємо.
  const gradeRange = workerClass ? GRADE_RANGES[workerClass] : null
  const allowedGrades = (grades.data ?? []).filter(
    g => gradeRange !== null && g.grade >= gradeRange[0] && g.grade <= gradeRange[1],
  )
  const allowedTitles = (titles.data ?? []).filter(t => workerClass !== null && t.workerClass === workerClass)

  const save = useMutation({
    mutationFn: () => {
      const body: CreatePositionRequest = {
        positionId,
        tariffGradeId,
        rateCount: parseDec(rateCount) ?? 1,
        hireDate,
        isPrimary,
        maintainsMilitaryRecords: false,
        hasUnfavorable: false,
        complexityBonusPct: null,
        prestigeBonusPct: null,
        positionStartDate: null,
        titleTypeId: titleTypeId || null,
      }
      return createEmployeePosition(employeeId, body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.employee(employeeId) })
      qc.invalidateQueries({ queryKey: keys.employees })
      onClose()
    },
    onError: setError,
  })

  return (
    <Modal title="Нова ставка">
      <div className="form-grid">
        <Field label="Посада" hint={workerClass ? `Клас ${workerClass} — ${WORKER_CLASS_LABELS[workerClass]}` : undefined}>
          <select
            value={positionId}
            onChange={e => { setPositionId(Number(e.target.value)); setTariffGradeId(0); setTitleTypeId(0) }}
            autoFocus
          >
            <option value={0} disabled>Оберіть посаду…</option>
            {positions.data?.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </Field>
        <Field
          label="Тарифний розряд"
          hint={gradeRange ? `Дозволено ${gradeRange[0]}–${gradeRange[1]} для цього класу` : 'Спершу оберіть посаду'}
        >
          <select value={tariffGradeId} onChange={e => setTariffGradeId(Number(e.target.value))} disabled={!workerClass}>
            <option value={0} disabled>Оберіть розряд…</option>
            {allowedGrades.map(g => (
              <option key={g.id} value={g.id}>{g.grade} — {g.monthlyRate.toLocaleString('uk-UA')} грн</option>
            ))}
          </select>
        </Field>
        <Field label="Кількість ставок" hint="0,5 = півставки; 1 = повна; 1,5 = півтори">
          <input type="text" value={rateCount} onChange={e => setRateCount(e.target.value)} />
        </Field>
        <Field label="Дата прийняття на посаду">
          <input type="date" value={hireDate} onChange={e => setHireDate(e.target.value)} />
        </Field>
        {allowedTitles.length > 0 && (
          <Field label="Звання">
            <select value={titleTypeId} onChange={e => setTitleTypeId(Number(e.target.value))}>
              <option value={0}>Без звання</option>
              {allowedTitles.map(t => (
                <option key={t.id} value={t.id}>{t.name} (+{Math.round(t.pct * 100)}%)</option>
              ))}
            </select>
          </Field>
        )}
      </div>
      <label className="check mt16">
        <input type="checkbox" checked={isPrimary} onChange={e => setIsPrimary(e.target.checked)} />
        Головна ставка (показується у списках і на розрахунковому листі)
      </label>
      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => save.mutate()}
          disabled={positionId === 0 || tariffGradeId === 0 || save.isPending}
        >
          Додати
        </button>
      </div>
    </Modal>
  )
}

// ─── Звільнення ───

function DismissDialog({ employee, onClose }: { employee: EmployeeDetail; onClose: () => void }) {
  const qc = useQueryClient()
  const navigate = useNavigate()
  const [error, setError] = useState<unknown>(null)

  const dismiss = useMutation({
    mutationFn: () => dismissEmployee(employee.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.employees })
      qc.invalidateQueries({ queryKey: keys.employee(employee.id) })
      navigate('/employees')
    },
    onError: setError,
  })

  return (
    <Modal title="Звільнити працівника?">
      <p>
        {employee.fullName} отримає статус «Звільнений», усі активні ставки буде закрито.
        Дані залишаться в архіві — фізично нічого не видаляється.
      </p>
      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button type="button" className="btn btn-danger" onClick={() => dismiss.mutate()} disabled={dismiss.isPending}>
          Звільнити
        </button>
      </div>
    </Modal>
  )
}

// ─── Превʼю зарплати ───

/**
 * Розрахунок зарплати однієї людини прямо в картці: обрати місяць → «Розрахувати» →
 * нараховано/утримано/на руки + покомпонентний розклад. Швидкий зворотний звʼязок:
 * правиш ставку — одразу бачиш ефект, без прогону всіх 74 на сторінці розрахунку.
 */
function SalaryPreview({ employeeId }: { employeeId: number }) {
  const [period, setPeriod] = useState<Period>(currentPeriod)
  const calc = useMutation({
    mutationFn: () => calculateOne(employeeId, period.year, period.month),
  })
  const r = calc.data

  return (
    <div className="card mt16">
      <div className="card-title">
        <h2>Зарплата за місяць</h2>
        <span className="muted">Превʼю однієї людини — без прогону всіх</span>
      </div>
      <div className="row mb16">
        <MonthPicker value={period} onChange={p => { setPeriod(p); calc.reset() }} />
        <button type="button" className="btn btn-primary btn-sm" onClick={() => calc.mutate()} disabled={calc.isPending}>
          {calc.isPending ? 'Рахую…' : 'Розрахувати'}
        </button>
        {r && <span className="muted">Днів: {r.workedDays} / {r.normDays}</span>}
      </div>
      <ErrorNote error={calc.error} />
      {r && (
        <>
          <div className="row mb16" style={{ gap: 32 }}>
            <Stat label="Нараховано" value={fmtMoney(r.gross)} />
            <Stat label="Утримано" value={fmtMoney(r.totalWithheld)} />
            <Stat label="До виплати" value={fmtMoney(r.netPay)} strong />
          </div>
          <CalcBreakdown earnings={r.earnings} deductions={r.deductions} />
        </>
      )}
    </div>
  )
}

function Stat({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div>
      <div className="hint">{label}</div>
      <div style={{ fontSize: 18, fontWeight: strong ? 700 : 500 }}>{value}</div>
    </div>
  )
}
