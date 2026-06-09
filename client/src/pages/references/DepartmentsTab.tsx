import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useDepartments, keys } from '../../api/hooks'
import { createDepartment, deleteDepartment, updateDepartment } from '../../api/endpoints'
import { Loading, LoadError, ErrorNote, Modal, Field } from '../../components/ui'
import type { Department } from '../../api/types'

export function DepartmentsTab() {
  const { data, isPending, error } = useDepartments()
  const qc = useQueryClient()
  const [editing, setEditing] = useState<Department | 'new' | null>(null)
  const [deleting, setDeleting] = useState<Department | null>(null)
  const [actionError, setActionError] = useState<unknown>(null)

  const invalidate = () => qc.invalidateQueries({ queryKey: keys.departments })

  const remove = useMutation({
    mutationFn: (id: number) => deleteDepartment(id),
    onSuccess: () => {
      setDeleting(null)
      setActionError(null)
      invalidate()
    },
    onError: setActionError,
  })

  if (isPending) return <Loading />
  if (error) return <LoadError error={error} />

  return (
    <>
      <div className="row mb16">
        <span className="muted">Підрозділи групують посади у звітах і фільтрах.</span>
        <span className="spacer" />
        <button type="button" className="btn btn-primary" onClick={() => { setActionError(null); setEditing('new') }}>
          + Додати підрозділ
        </button>
      </div>
      <div className="table-wrap" style={{ maxWidth: 640 }}>
        <table>
          <thead>
            <tr><th>Підрозділ</th><th style={{ width: 180 }}></th></tr>
          </thead>
          <tbody>
            {data.map(d => (
              <tr key={d.id}>
                <td>{d.name}</td>
                <td style={{ textAlign: 'right' }}>
                  <button type="button" className="btn btn-sm" onClick={() => { setActionError(null); setEditing(d) }}>Редагувати</button>{' '}
                  <button type="button" className="btn btn-sm btn-danger" onClick={() => { setActionError(null); setDeleting(d) }}>Видалити</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {editing && (
        <DepartmentDialog
          department={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); invalidate() }}
        />
      )}
      {deleting && (
        <Modal title="Видалити підрозділ?">
          <p>«{deleting.name}» буде видалено. Якщо до підрозділу прив’язані посади — видалення неможливе.</p>
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

function DepartmentDialog({ department, onClose, onSaved }: {
  department: Department | null
  onClose: () => void
  onSaved: () => void
}) {
  const [name, setName] = useState(department?.name ?? '')
  const [error, setError] = useState<unknown>(null)

  const save = useMutation({
    mutationFn: () =>
      department ? updateDepartment(department.id, name.trim()) : createDepartment(name.trim()).then(() => undefined),
    onSuccess: onSaved,
    onError: setError,
  })

  return (
    <Modal title={department ? 'Редагувати підрозділ' : 'Новий підрозділ'}>
      <Field label="Назва">
        <input type="text" value={name} onChange={e => setName(e.target.value)} autoFocus />
      </Field>
      <ErrorNote error={error} />
      <div className="modal-actions">
        <button type="button" className="btn" onClick={onClose}>Скасувати</button>
        <button type="button" className="btn btn-primary" onClick={() => save.mutate()} disabled={!name.trim() || save.isPending}>
          Зберегти
        </button>
      </div>
    </Modal>
  )
}
