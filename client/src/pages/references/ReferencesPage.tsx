import { useState } from 'react'
import { SystemParamsTab } from './SystemParamsTab'
import { TariffGradesTab } from './TariffGradesTab'
import { CalendarTab } from './CalendarTab'
import { DepartmentsTab } from './DepartmentsTab'
import { PositionsTab } from './PositionsTab'
import { InclusionRulesTab } from './InclusionRulesTab'

const TABS = [
  { id: 'params', label: 'Параметри системи' },
  { id: 'grades', label: 'Тарифна сітка' },
  { id: 'calendar', label: 'Виробничий календар' },
  { id: 'departments', label: 'Підрозділи' },
  { id: 'positions', label: 'Посади' },
  { id: 'inclusion', label: 'База середньоденної' },
] as const

type TabId = (typeof TABS)[number]['id']

export function ReferencesPage() {
  const [tab, setTab] = useState<TabId>('params')
  return (
    <>
      <div className="page-header">
        <div>
          <h1>Довідники</h1>
          <p>Ставки, розряди, календар і структура школи. Заповнюється один раз, далі — точкові правки.</p>
        </div>
      </div>
      <div className="tabs">
        {TABS.map(t => (
          <button
            key={t.id}
            type="button"
            className={tab === t.id ? 'tab active' : 'tab'}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>
      {tab === 'params' && <SystemParamsTab />}
      {tab === 'grades' && <TariffGradesTab />}
      {tab === 'calendar' && <CalendarTab />}
      {tab === 'departments' && <DepartmentsTab />}
      {tab === 'positions' && <PositionsTab />}
      {tab === 'inclusion' && <InclusionRulesTab />}
    </>
  )
}
