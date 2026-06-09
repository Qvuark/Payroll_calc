import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useDepartments, usePositions, keys } from '../../api/hooks'
import { createPosition, deletePosition, updatePosition } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote, Modal, Field } from '../../components/ui'
import { WORKER_CLASS_LABELS, WORKER_CLASS_HINTS } from '../../api/types'
import type { Position, WorkerClass } from '../../api/types'

export function PositionsTab() {
  const positions = usePositions()
  const departments = useDepartments()
  const qc = useQueryClient()
  const [filterDept, setFilterDept] = useState(0)
  const [filterClass, setFilterClass] = useState(0)
  const [editing, setEditing] = useState<Position | 'new' | null>(null)
  const [deleting, setDeleting] = useState<Position | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)

  const invalidate = () => qc.invalidateQueries({ queryKey: keys.positions })

  const remove = useMutation({
    mutationFn: (id: number) => deletePosition(id),
    onSuccess: () => {
      setDeleting(null)
      setActionError(null)
      invalidate()
    },
    onError: setActionError,
  })

  if (positions.isPending || departments.isPending) return <Loading />
  if (positions.error) return <LoadError error={positions.error} />
  if (departments.error) return <LoadError error={departments.error} />

  const rows = positions.data.filter(p =>
    (filterDept === 0 || p.departmentId === filterDept) &&
    (filterClass === 0 || p.workerClass === filterClass),
  )

  return (
    <>
      <div className="row mb16">
        <select style={{ width: 220 }} value={filterDept} onChange={e => setFilterDept(Number(e.target.value))}>
          <option value={0}>Всі підрозділи</option>
          {departments.data.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
        </select>
        <select style={{ width: 220 }} value={filterClass} onChange={e => setFilterClass(Number(e.target.value))}>
          <option value={0}>Всі класи</option>
          {([1, 2, 3, 4] as const).map(c => <option key={c} value={c}>Клас {c} — {WORKER_CLASS_LABELS[c]}</option>)}
        </select>
        <span className="spacer" />
        <button type="button" className="btn btn-primary" onClick={() => { setActionError(null); setEditing('new') }}>
          + Додати посаду
        </button>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Посада</th>
              <th>Підрозділ</th>
              <th>Клас</th>
              <th>Оплата</th>
              <th style={{ width: 180 }}></th>
            </tr>
          </thead>
          <tbody>
            {rows.map(p => (
              <tr key={p.id}>
                <td><strong>{p.name}</strong></td>
                <td>{p.department?.name ?? '—'}</td>
                <td>{p.workerClass} — {WORKER_CLASS_LABELS[p.workerClass]}</td>
                <td>{p.isHourly ? <span className="badge badge-blue">погодинна</span> : <span className="muted">за днями</span>}</td>
                <td style={{ textAlign: 'right' }}>
                  <button type="button" className="btn btn-sm" onClick={() => { setActionError(null); setEditing(p) }}>Редагувати</button>{' '}
                  <button type="button" className="btn btn-sm btn-danger" onClick={() => { setActionError(null); setDeleting(p) }}>Видалити</button>
                </td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr><td colSpan={5} className="empty">Нічого не знайдено</td></tr>
            )}
          </tbody>
        </table>
      </div>
      {editing && (
        <PositionDialog
          position={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); invalidate() }}
        />
      )}
      {deleting && (
        <Modal title="Видалити посаду?">
          <p>«{deleting.name}» буде видалено. Якщо на посаду призначені працівники — видалення неможливе.</p>
          <ErrorNote error={actionError} />
          <div className="modal-actions">
            <button type="button" className="btn" onClick={() => setDeleting(null)}>Скасувати</button>
            <button type="button" className="btn btn-danger" onClick={() => remove.mutate(deleting.id)} disabled={remove.isPending}>
              Видалити
            </button>
          </div>
        </Modal>
      )}
    </>
  )
}

function PositionDialog({ position, onClose, onSaved }: {
  position: Position | null
  onClose: () => void
  onSaved: () => void
}) {
  const departments = useDepartments()
  const [name, setName] = useState(position?.name ?? '')
  const [departmentId, setDepartmentId] = useState(position?.departmentId ?? 0)
  const [workerClass, setWorkerClass] = useState<WorkerClass>(position?.workerClass ?? 1)
  const [isHourly, setIsHourly] = useState(position?.isHourly ?? false)
  const [error, setError] = useState<unknown>(null)

  const body = { name: name.trim(), departmentId, workerClass, isHourly }
  const save = useMutation({
    mutationFn: () =>
      position ? updatePosition(position.id, body) : createPosition(body).then(() => undefined),
    onSuccess: onSaved,
    onError: setError,
  })

  return (
    <Modal title={position ? 'Редагувати посаду' : 'Нова посада'}>
      <div className="form-grid">
        <Field label="Назва посади">
          <input type="text" value={name} onChange={e => setName(e.target.value)} autoFocus />
        </Field>
        <Field label="Підрозділ">
          <select value={departmentId} onChange={e => setDepartmentId(Number(e.target.value))}>
            <option value={0} disabled>Оберіть…</option>
            {departments.data?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
        </Field>
      </div>
      <Field label="Клас працівника" hint={WORKER_CLASS_HINTS[workerClass]}>
        <select value={workerClass} onChange={e => setWorkerClass(Number(e.target.value) as WorkerClass)}>
          {([1, 2, 3, 4] as const).map(c => (
            <option key={c} value={c}>Клас {c} — {WORKER_CLASS_LABELS[c]}</option>
          ))}
        </select>
      </Field>
      <label className="check mt16">
        <input type="checkbox" checked={isHourly} onChange={e => setIsHourly(e.target.checked)} />
        Погодинна оплата (сторож): оклад = тариф / норма годин × відпрацьовані години
      </label>
      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => save.mutate()}
          disabled={!name.trim() || departmentId === 0 || save.isPending}
        >
          Зберегти
        </button>
      </div>
    </Modal>
  )
}
