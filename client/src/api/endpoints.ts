import { api } from './client'
import type {
  AdminRequest,
  CalcResult,
  CreateEmployeeRequest,
  CreatePositionRequest,
  Department,
  EmployeeDetail,
  EmployeeSummary,
  GpdRequest,
  ImportReport,
  NonPedagogicalRequest,
  NotebookRate,
  PkrRequest,
  Position,
  SystemParam,
  TariffGrade,
  Timesheet,
  TimesheetRequest,
  TitleType,
  UpdateEmployeeRequest,
  UpdatePositionRequest,
  WorkCalendarMonth,
  WorkloadRequest,
} from './types'

// ─── Довідники ───

export const getDepartments = () => api<Department[]>('/departments')
export const createDepartment = (name: string) =>
  api<Department>('/departments', { method: 'POST', body: JSON.stringify({ name }) })
export const updateDepartment = (id: number, name: string) =>
  api<void>(`/departments/${id}`, { method: 'PUT', body: JSON.stringify({ name }) })
export const deleteDepartment = (id: number) =>
  api<void>(`/departments/${id}`, { method: 'DELETE' })

export const getPositions = () => api<Position[]>('/positions')
export const createPosition = (body: { name: string; departmentId: number; workerClass: number; isHourly: boolean }) =>
  api<Position>('/positions', { method: 'POST', body: JSON.stringify(body) })
export const updatePosition = (id: number, body: { name: string; departmentId: number; workerClass: number; isHourly: boolean }) =>
  api<void>(`/positions/${id}`, { method: 'PUT', body: JSON.stringify(body) })
export const deletePosition = (id: number) =>
  api<void>(`/positions/${id}`, { method: 'DELETE' })

export const getTariffGrades = () => api<TariffGrade[]>('/tariffgrades')
export const updateTariffGrade = (id: number, monthlyRate: number) =>
  api<void>(`/tariffgrades/${id}`, { method: 'PUT', body: JSON.stringify({ monthlyRate }) })

export const getSystemParams = () => api<SystemParam[]>('/systemparams')
export const updateSystemParam = (key: string, value: number) =>
  api<void>(`/systemparams/${encodeURIComponent(key)}`, { method: 'PUT', body: JSON.stringify({ value }) })

export const getWorkCalendar = (year: number) =>
  api<WorkCalendarMonth[]>(`/workcalendar?year=${year}`)
export const createCalendarYear = (year: number) =>
  api<WorkCalendarMonth[]>(`/workcalendar/${year}`, { method: 'POST' })
export const updateCalendarMonth = (year: number, month: number, workDays: number) =>
  api<void>(`/workcalendar/${year}/${month}`, { method: 'PUT', body: JSON.stringify({ workDays }) })

export const getTitleTypes = () => api<TitleType[]>('/titletypes')
export const getNotebookRates = () => api<NotebookRate[]>('/notebookrates')

// ─── Працівники ───

export const getEmployees = (includeDismissed = false) =>
  api<EmployeeSummary[]>(`/employees${includeDismissed ? '?includeDismissed=true' : ''}`)
export const getEmployee = (id: number) => api<EmployeeDetail>(`/employees/${id}`)
export const createEmployee = (body: CreateEmployeeRequest) =>
  api<EmployeeDetail>('/employees', { method: 'POST', body: JSON.stringify(body) })
export const updateEmployee = (id: number, body: UpdateEmployeeRequest) =>
  api<EmployeeDetail>(`/employees/${id}`, { method: 'PUT', body: JSON.stringify(body) })
export const dismissEmployee = (id: number) =>
  api<void>(`/employees/${id}`, { method: 'DELETE' })

// ─── Ставки працівника ───

const posBase = (employeeId: number) => `/employees/${employeeId}/positions`

export const createEmployeePosition = (employeeId: number, body: CreatePositionRequest) =>
  api<unknown>(posBase(employeeId), { method: 'POST', body: JSON.stringify(body) })
export const updateEmployeePosition = (employeeId: number, posId: number, body: UpdatePositionRequest) =>
  api<unknown>(`${posBase(employeeId)}/${posId}`, { method: 'PUT', body: JSON.stringify(body) })
export const dismissEmployeePosition = (employeeId: number, posId: number) =>
  api<void>(`${posBase(employeeId)}/${posId}`, { method: 'DELETE' })

export const putWorkload = (employeeId: number, posId: number, body: WorkloadRequest) =>
  api<unknown>(`${posBase(employeeId)}/${posId}/workload`, { method: 'PUT', body: JSON.stringify(body) })
export const putAdmin = (employeeId: number, posId: number, body: AdminRequest) =>
  api<unknown>(`${posBase(employeeId)}/${posId}/admin`, { method: 'PUT', body: JSON.stringify(body) })
export const putGpd = (employeeId: number, posId: number, body: GpdRequest) =>
  api<unknown>(`${posBase(employeeId)}/${posId}/gpd`, { method: 'PUT', body: JSON.stringify(body) })
export const putPkr = (employeeId: number, posId: number, body: PkrRequest) =>
  api<unknown>(`${posBase(employeeId)}/${posId}/pkr`, { method: 'PUT', body: JSON.stringify(body) })
export const putNonPedagogical = (employeeId: number, posId: number, body: NonPedagogicalRequest) =>
  api<unknown>(`${posBase(employeeId)}/${posId}/nonpedagogical`, { method: 'PUT', body: JSON.stringify(body) })

export type BlockKind = 'workload' | 'admin' | 'gpd' | 'pkr' | 'nonpedagogical'
export const deleteBlock = (employeeId: number, posId: number, kind: BlockKind) =>
  api<void>(`${posBase(employeeId)}/${posId}/${kind}`, { method: 'DELETE' })

// ─── Табель ───

export const getTimesheets = (year: number, month: number) =>
  api<Timesheet[]>(`/timesheets?year=${year}&month=${month}`)
export const upsertTimesheet = (body: TimesheetRequest) =>
  api<Timesheet>('/timesheets', { method: 'PUT', body: JSON.stringify(body) })

// ─── Розрахунок ───

export const calculateAll = (year: number, month: number) =>
  api<CalcResult[]>(`/calculations/all?year=${year}&month=${month}`, { method: 'POST' })

// Рахує одного працівника за місяць (POST зберігає чернетку в Calculation). 404 — працівника немає.
export const calculateOne = (employeeId: number, year: number, month: number) =>
  api<CalcResult>(`/calculations?employeeId=${employeeId}&year=${year}&month=${month}`, { method: 'POST' })

// ─── Імпорт ───

function postFile(path: string, file: File): Promise<ImportReport> {
  const form = new FormData()
  form.append('file', file)
  return api<ImportReport>(path, { method: 'POST', body: form })
}

export const importStaff = (file: File) => postFile('/import/staff', file)
export const importTeachers = (file: File) => postFile('/import/teachers', file)
export const importTimesheet = (file: File, year: number, month: number) =>
  postFile(`/import/timesheet?year=${year}&month=${month}`, file)
