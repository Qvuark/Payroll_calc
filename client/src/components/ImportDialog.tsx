import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { ImportReport } from '../api/types'
import { downloadFile } from '../api/client'
import { Modal, ErrorNote } from './ui'

interface Props {
  title: string
  /** Пояснення для бухгалтера: що зробить імпорт. */
  description: string
  templatePath: string
  templateName: string
  onImport: (file: File) => Promise<ImportReport>
  /** Query-ключі які треба перезавантажити після успішного імпорту. */
  invalidateKeys: readonly (readonly unknown[])[]
  onClose: () => void
}

/**
 * Універсальне вікно імпорту xlsx: скачати шаблон → обрати файл → імпорт → звіт.
 * Звіт показує created/updated/skipped і список помилок по рядках.
 */
export function ImportDialog({ title, description, templatePath, templateName, onImport, invalidateKeys, onClose }: Props) {
  const qc = useQueryClient()
  const [file, setFile] = useState<File | null>(null)
  const [busy, setBusy] = useState(false)
  const [report, setReport] = useState<ImportReport | null>(null)
  const [error, setError] = useState<unknown>(null)

  const run = async () => {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      const result = await onImport(file)
      setReport(result)
      for (const key of invalidateKeys) await qc.invalidateQueries({ queryKey: key })
    } catch (e) {
      setError(e)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal title={title} wide>
      {report === null ? (
        <>
          <p className="muted" style={{ marginTop: 0 }}>{description}</p>
          <div className="row mb16">
            <button
              type="button"
              className="btn"
              onClick={() => downloadFile(templatePath, templateName).catch(setError)}
            >
              ⬇ Скачати шаблон
            </button>
            <span className="hint">Заповніть шаблон у Excel і завантажте його сюди.</span>
          </div>
          <input
            type="file"
            accept=".xlsx"
            onChange={e => setFile(e.target.files?.[0] ?? null)}
          />
          <ErrorNote error={error} />
          <div className="modal-actions">
            <button type="button" className="btn" onClick={onClose} disabled={busy}>Скасувати</button>
            <button type="button" className="btn btn-primary" onClick={run} disabled={!file || busy}>
              {busy ? 'Імпортую…' : 'Імпортувати'}
            </button>
          </div>
        </>
      ) : (
        <>
          <div className="row mb16">
            <span className="badge badge-green">Створено: {report.created}</span>
            <span className="badge badge-blue">Оновлено: {report.updated}</span>
            {report.skipped > 0 && <span className="badge badge-amber">Пропущено: {report.skipped}</span>}
          </div>
          {report.errors.length > 0 ? (
            <div className="table-wrap" style={{ maxHeight: 320 }}>
              <table>
                <thead>
                  <tr><th>Рядок</th><th>Поле</th><th>Повідомлення</th></tr>
                </thead>
                <tbody>
                  {report.errors.map((e, i) => (
                    <tr key={i}>
                      <td>{e.row}</td>
                      <td>{e.field ?? '—'}</td>
                      <td className={e.severity === 1 ? 'muted' : ''} style={e.severity === 0 ? { color: 'var(--danger)' } : undefined}>
                        {e.message}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="note note-success">Усі рядки оброблено без помилок.</div>
          )}
          <div className="modal-actions">
            <button type="button" className="btn btn-primary" onClick={onClose}>Готово</button>
          </div>
        </>
      )}
    </Modal>
  )
}
