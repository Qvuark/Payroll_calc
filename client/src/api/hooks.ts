import { useQuery } from '@tanstack/react-query'
import * as ep from './endpoints'

// Спільні query-хуки для довідників і списків.
// Мутації живуть у компонентах (useMutation + invalidate по цих ключах).

export const keys = {
  departments: ['departments'] as const,
  positions: ['positions'] as const,
  tariffGrades: ['tariffGrades'] as const,
  systemParams: ['systemParams'] as const,
  workCalendar: (year: number) => ['workCalendar', year] as const,
  titleTypes: ['titleTypes'] as const,
  notebookRates: ['notebookRates'] as const,
  employees: ['employees'] as const,
  employee: (id: number) => ['employee', id] as const,
  timesheets: (year: number, month: number) => ['timesheets', year, month] as const,
}

export const useDepartments = () =>
  useQuery({ queryKey: keys.departments, queryFn: ep.getDepartments })

export const usePositions = () =>
  useQuery({ queryKey: keys.positions, queryFn: ep.getPositions })

export const useTariffGrades = () =>
  useQuery({ queryKey: keys.tariffGrades, queryFn: ep.getTariffGrades })

export const useSystemParams = () =>
  useQuery({ queryKey: keys.systemParams, queryFn: ep.getSystemParams })

export const useWorkCalendar = (year: number) =>
  useQuery({ queryKey: keys.workCalendar(year), queryFn: () => ep.getWorkCalendar(year) })

export const useTitleTypes = () =>
  useQuery({ queryKey: keys.titleTypes, queryFn: ep.getTitleTypes })

export const useNotebookRates = () =>
  useQuery({ queryKey: keys.notebookRates, queryFn: ep.getNotebookRates })

export const useEmployees = () =>
  useQuery({ queryKey: keys.employees, queryFn: ep.getEmployees })

export const useEmployee = (id: number) =>
  useQuery({ queryKey: keys.employee(id), queryFn: () => ep.getEmployee(id) })

export const useTimesheets = (year: number, month: number) =>
  useQuery({ queryKey: keys.timesheets(year, month), queryFn: () => ep.getTimesheets(year, month) })
