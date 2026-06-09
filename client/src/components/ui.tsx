import type { ReactNode } from 'react'

/**
 * Модальне вікно. Закриття — клік по фону або хрестик не робимо:
 * лише явні кнопки в actions, щоб випадковий клік не загубив введене.
 */
export function Modal({ title, children, wide }: { title: string; children: ReactNode; wide?: boolean }) {
  return (
    <div className="modal-backdrop">
      <div className={wide ? 'modal modal-wide' : 'modal'} role="dialog" aria-label={title}>
        <h2>{title}</h2>
        {children}
      </div>
    </div>
  )
}

export function ErrorNote({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof Error ? error.message : String(error)
  return <div className="note note-error mt8">{message}</div>
}

export function Loading() {
  return <div className="empty">Завантаження…</div>
}

export function LoadError({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Не вдалося завантажити дані'
  return <div className="note note-error">{message}</div>
}

export function StatusBadge({ status }: { status: 0 | 1 | 2 }) {
  const map = {
    0: ['badge badge-green', 'Активний'],
    1: ['badge badge-amber', 'У відпустці'],
    2: ['badge badge-gray', 'Звільнений'],
  } as const
  const [cls, label] = map[status]
  return <span className={cls}>{label}</span>
}

/** Поле форми: підпис зверху, будь-який інпут знизу. */
export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return (
    <div className="field">
      <label>{label}</label>
      {children}
      {hint && <span className="hint">{hint}</span>}
    </div>
  )
}
