import { Navigate, NavLink, Route, Routes } from 'react-router-dom'
import { ReferencesPage } from './pages/references/ReferencesPage'
import { EmployeesPage } from './pages/employees/EmployeesPage'
import { EmployeeCardPage } from './pages/employees/EmployeeCardPage'
import { TimesheetPage } from './pages/timesheet/TimesheetPage'
import { CalculationsPage } from './pages/calculations/CalculationsPage'
import { DocumentsPage } from './pages/documents/DocumentsPage'

const NAV = [
  { to: '/references', label: 'Довідники', icon: BookIcon },
  { to: '/employees', label: 'Працівники', icon: PeopleIcon },
  { to: '/timesheet', label: 'Табель', icon: CalendarIcon },
  { to: '/calculations', label: 'Розрахунок', icon: CalcIcon },
  { to: '/documents', label: 'Документи', icon: DocIcon },
]

export default function App() {
  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          PayrollCalc
          <small>розрахунок зарплати</small>
        </div>
        {NAV.map(item => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) => (isActive ? 'nav-item active' : 'nav-item')}
          >
            <item.icon />
            <span>{item.label}</span>
          </NavLink>
        ))}
      </aside>
      <main className="main">
        <div className="page">
          <Routes>
            <Route path="/" element={<Navigate to="/employees" replace />} />
            <Route path="/references" element={<ReferencesPage />} />
            <Route path="/employees" element={<EmployeesPage />} />
            <Route path="/employees/:id" element={<EmployeeCardPage />} />
            <Route path="/timesheet" element={<TimesheetPage />} />
            <Route path="/calculations" element={<CalculationsPage />} />
            <Route path="/documents" element={<DocumentsPage />} />
          </Routes>
        </div>
      </main>
    </div>
  )
}

// Іконки — мінімальні inline SVG, 18px, stroke поточним кольором.

function iconProps() {
  return {
    width: 18,
    height: 18,
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.8,
    strokeLinecap: 'round',
    strokeLinejoin: 'round',
  } as const
}

function BookIcon() {
  return (
    <svg {...iconProps()}>
      <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
      <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
    </svg>
  )
}

function PeopleIcon() {
  return (
    <svg {...iconProps()}>
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
      <circle cx="9" cy="7" r="4" />
      <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
      <path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  )
}

function CalendarIcon() {
  return (
    <svg {...iconProps()}>
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <line x1="16" y1="2" x2="16" y2="6" />
      <line x1="8" y1="2" x2="8" y2="6" />
      <line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  )
}

function CalcIcon() {
  return (
    <svg {...iconProps()}>
      <rect x="4" y="2" width="16" height="20" rx="2" />
      <line x1="8" y1="6" x2="16" y2="6" />
      <line x1="8" y1="11" x2="8" y2="11.01" />
      <line x1="12" y1="11" x2="12" y2="11.01" />
      <line x1="16" y1="11" x2="16" y2="11.01" />
      <line x1="8" y1="15" x2="8" y2="15.01" />
      <line x1="12" y1="15" x2="12" y2="15.01" />
      <line x1="16" y1="15" x2="16" y2="18" />
      <line x1="8" y1="18" x2="8" y2="18.01" />
      <line x1="12" y1="18" x2="12" y2="18.01" />
    </svg>
  )
}

function DocIcon() {
  return (
    <svg {...iconProps()}>
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
      <line x1="16" y1="13" x2="8" y2="13" />
      <line x1="16" y1="17" x2="8" y2="17" />
    </svg>
  )
}
