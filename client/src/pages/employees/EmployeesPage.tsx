import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEmployees, keys } from '../../api/hooks'
import { createEmployee, importStaff, importTeachers } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote, Modal, Field, StatusBadge } from '../../components/ui'
import { ImportDialog } from '../../components/ImportDialog'
import { WORKER_CLASS_LABELS } from '../../api/types'
import type { CreateEmployeeRequest } from '../../api/types'
import { parseDec, todayIso } from '../../lib/format'

type ImportKind = 'teachers' | 'staff' | null

export function EmployeesPage() {
  const { data, isPending, error } = useEmployees()
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [filterClass, setFilterClass] = useState(0)
  const [creating, setCreating] = useState(false)
  const [importKind, setImportKind] = useState<ImportKind>(null)

  const rows = useMemo(() => {
    if (!data) return []
    const q = search.trim().toLowerCase()
    return data.filter(e =>
      (q === '' || e.fullName.toLowerCase().includes(q) || e.tabNumber.toLowerCase().includes(q)) &&
      (filterClass === 0 || e.primaryWorkerClass === filterClass),
    )
  }, [data, search, filterClass])

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Працівники</h1>
          <p>{data.length} активних. Натисніть на рядок, щоб відкрити картку.</p>
        </div>
        <div className="row">
          <button type="button" className="btn" onClick={() => setImportKind('teachers')}>⬆ Імпорт вчителів</button>
          <button type="button" className="btn" onClick={() => setImportKind('staff')}>⬆ Імпорт інших</button>
          <button type="button" className="btn btn-primary" onClick={() => setCreating(true)}>+ Додати працівника</button>
        </div>
      </div>

      <div className="row mb16">
        <input
          type="text"
          className="search-input"
          placeholder="Пошук: ПІБ або табельний номер"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
        <select style={{ width: 240 }} value={filterClass} onChange={e => setFilterClass(Number(e.target.value))}>
          <option value={0}>Всі класи</option>
          {([1, 2, 3, 4] as const).map(c => <option key={c} value={c}>Клас {c} — {WORKER_CLASS_LABELS[c]}</option>)}
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>ПІБ</th>
              <th>Таб. №</th>
              <th>Посада (головна)</th>
              <th>Клас</th>
              <th className="num">Розряд</th>
              <th className="num">Ставок</th>
              <th>Статус</th>
            </tr>
          </thead>
          <tbody>
            {rows.map(e => (
              <tr key={e.id} className="clickable" onClick={() => navigate(`/employees/${e.id}`)}>
                <td><strong>{e.fullName}</strong></td>
                <td className="muted">{e.tabNumber}</td>
                <td>{e.primaryPositionName ?? <span className="muted">без ставки</span>}</td>
                <td>{e.primaryWorkerClass ? e.primaryWorkerClass : '—'}</td>
                <td className="num">{e.primaryTariffGrade ?? '—'}</td>
                <td className="num">{e.activePositionsCount}</td>
                <td><StatusBadge status={e.status} /></td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr><td colSpan={7} className="empty">Нікого не знайдено</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateEmployeeDialog onClose={() => setCreating(false)} />}
      {importKind === 'teachers' && (
        <ImportDialog
          title="Імпорт вчителів з Excel"
          description="Створює або оновлює картки вчителів (Клас 1): навантаження, зошити, надбавки. Рядки з помилками пропускаються — програма покаже список."
          templatePath="/import/templates/teachers"
          templateName="teachers_template.xlsx"
          onImport={importTeachers}
          invalidateKeys={[keys.employees]}
          onClose={() => setImportKind(null)}
        />
      )}
      {importKind === 'staff' && (
        <ImportDialog
          title="Імпорт працівників (не вчителі)"
          description="Створює або оновлює картки адміністрації, спеціалістів і МОП (Класи 2–4): ставки, ГПД/ПКР, непедагогічні надбавки."
          templatePath="/import/templates/staff"
          templateName="staff_template.xlsx"
          onImport={importStaff}
          invalidateKeys={[keys.employees]}
          onClose={() => setImportKind(null)}
        />
      )}
    </>
  )
}

function CreateEmployeeDialog({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const navigate = useNavigate()
  const [form, setForm] = useState({
    tabNumber: '',
    fullName: '',
    taxId: '',
    hireDate: todayIso(),
    education: '',
    generalExperienceYears: '0',
    pedExperienceYears: '0',
  })
  const [error, setError] = useState<unknown>(null)

  const save = useMutation({
    mutationFn: () => {
      const body: CreateEmployeeRequest = {
        tabNumber: form.tabNumber.trim(),
        fullName: form.fullName.trim(),
        taxId: form.taxId.trim(),
        hireDate: form.hireDate,
        education: form.education.trim() || null,
        generalExperienceYears: parseDec(form.generalExperienceYears) ?? 0,
        pedExperienceYears: parseDec(form.pedExperienceYears) ?? 0,
        socialBenefitPct: null,
        isHonored: false,
        honoredAmount: null,
      }
      return createEmployee(body)
    },
    onSuccess: created => {
      qc.invalidateQueries({ queryKey: keys.employees })
      // Одразу в картку — далі додаються ставки.
      navigate(`/employees/${created.id}`)
    },
    onError: setError,
  })

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [key]: e.target.value }))

  const taxIdOk = /^\d{10}$/.test(form.taxId.trim())
  const canSave = form.tabNumber.trim() !== '' && form.fullName.trim() !== '' && taxIdOk

  return (
    <Modal title="Новий працівник">
      <p className="hint" style={{ marginTop: 0 }}>
        Спочатку особисті дані. Ставки (посада, розряд, навантаження) додаються у картці після збереження.
      </p>
      <div className="form-grid">
        <Field label="ПІБ повністю">
          <input type="text" value={form.fullName} onChange={set('fullName')} autoFocus />
        </Field>
        <Field label="Табельний номер">
          <input type="text" value={form.tabNumber} onChange={set('tabNumber')} />
        </Field>
        <Field label="ІПН (10 цифр)" hint={form.taxId && !taxIdOk ? 'ІПН має містити рівно 10 цифр' : undefined}>
          <input type="text" value={form.taxId} onChange={set('taxId')} maxLength={10} />
        </Field>
        <Field label="Дата прийняття у школу">
          <input type="date" value={form.hireDate} onChange={set('hireDate')} />
        </Field>
        <Field label="Освіта">
          <input type="text" value={form.education} onChange={set('education')} placeholder="Вища педагогічна" />
        </Field>
        <Field label="Загальний стаж, років">
          <input type="text" value={form.generalExperienceYears} onChange={set('generalExperienceYears')} />
        </Field>
        <Field label="Педагогічний стаж, років">
          <input type="text" value={form.pedExperienceYears} onChange={set('pedExperienceYears')} />
        </Field>
      </div>
      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button type="button" className="btn btn-primary" onClick={() => save.mutate()} disabled={!canSave || save.isPending}>
          Створити
        </button>
      </div>
    </Modal>
  )
}
