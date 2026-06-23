// Типи — дзеркало DTO бекенда. Енуми приходять ЧИСЛАМИ
// (System.Text.Json без JsonStringEnumConverter), тому тут числові union-типи.

export type WorkerClass = 1 | 2 | 3 | 4
export const WorkerClass = {
  Pedagogical: 1,
  AdminPedagogical: 2,
  Specialist: 3,
  MOP: 4,
} as const

export const WORKER_CLASS_LABELS: Record<WorkerClass, string> = {
  1: 'Педагогічний (вчителі)',
  2: 'Адмін-педагогічний',
  3: 'Спеціаліст',
  4: 'МОП',
}

export const WORKER_CLASS_HINTS: Record<WorkerClass, string> = {
  1: 'Вчителі — погодинний оклад, №1749, вислуга, престижність',
  2: 'Директор, заступники, психолог — фіксований оклад, №1749',
  3: 'Бухгалтер, бібліотекар, медсестра — без №1749',
  4: 'Прибиральники, сторожі — без №1749 і вислуги',
}

// Дозволені діапазони тарифних розрядів per клас (EmployeeValidator.ValidateGradeForClass).
export const GRADE_RANGES: Record<WorkerClass, [number, number]> = {
  1: [10, 15],
  2: [8, 18],
  3: [4, 13],
  4: [1, 8],
}
export const GPD_GRADE_RANGE: [number, number] = [10, 14]
export const PKR_GRADE_RANGE: [number, number] = [10, 12]

export type EmployeeStatus = 0 | 1 | 2
export const EmployeeStatus = { Active: 0, OnLeave: 1, Dismissed: 2 } as const
export const STATUS_LABELS: Record<EmployeeStatus, string> = {
  0: 'Активний',
  1: 'У відпустці',
  2: 'Звільнений',
}

export type ClassGradeGroup = 0 | 1
export const CLASS_GRADE_GROUP_LABELS: Record<ClassGradeGroup, string> = {
  0: '1–4 класи',
  1: '5–11 класи',
}

export type CabinetType = 0 | 1 | 2
export const CABINET_TYPE_LABELS: Record<CabinetType, string> = {
  0: 'Звичайний кабінет',
  1: 'Кабінет музики / ІТ',
  2: 'Майстерня',
}

export type VacationType = 0 | 1 | 2 | 3 | 4
export const VacationType = { Annual: 0, Study: 1, Unpaid: 2, ChildCare: 3, Compensation: 4 } as const
export const VACATION_TYPE_LABELS: Record<VacationType, string> = {
  0: 'Щорічна',
  1: 'Навчальна',
  2: 'Без збереження зарплати',
  3: 'По догляду за дитиною',
  4: 'Компенсація за невикористану',
}
// Неоплачувані типи — база не потрібна, виплати нема (форма ховає поля бази).
export const UNPAID_VACATION_TYPES: VacationType[] = [2, 3]

// Звідки береться база середньоденної: Auto — рахується з підписаної історії, Manual — вводиться руками.
export type CalcMode = 0 | 1
export const CalcMode = { Auto: 0, Manual: 1 } as const

// ─── Довідники ───

export interface Department {
  id: number
  name: string
}

export interface Position {
  id: number
  name: string
  departmentId: number
  workerClass: WorkerClass
  isHourly: boolean
  excelAliases: string[]
  department: Department | null
}

export interface TariffGrade {
  id: number
  grade: number
  monthlyRate: number
  effectiveDate: string
}

export interface SystemParam {
  id: number
  key: string
  value: number
  effectiveDate: string
}

export interface WorkCalendarMonth {
  id: number
  year: number
  month: number
  workDays: number
}

export interface TitleType {
  id: number
  name: string
  workerClass: WorkerClass
  pct: number
}

export interface NotebookRate {
  id: number
  subjectKeyword: string
  pct: number
}

// ─── Працівники ───

export interface EmployeeSummary {
  id: number
  tabNumber: string
  fullName: string
  status: EmployeeStatus
  primaryPositionName: string | null
  primaryDepartmentName: string | null
  primaryWorkerClass: WorkerClass | null
  primaryTariffGrade: number | null
  primaryRateCount: number | null
  activePositionsCount: number
}

export interface EmployeeWorkload {
  employeePositionId: number
  hours1To4: number
  individualHours1To4: number
  hours5To9: number
  individualHours5To9: number
  hours10To11: number
  individualHours10To11: number
  notebookHours1To4: number
  notebookHours5To9: number
  notebookHours10To11: number
  inclusiveHours1To4: number
  inclusiveHours5To9: number
  inclusiveHours10To11: number
  notebookRateId: number | null
  additionalHours: number
}

export interface EmployeeAdmin {
  employeePositionId: number
  hasClassMgmt: boolean
  classGradeGroup: ClassGradeGroup | null
  hasCabinet: boolean
  cabinetType: CabinetType | null
  hasGym: boolean
  hasShootingRange: boolean
  hasComputers: boolean
  hasExtracurricular: boolean
  hasWebsite: boolean
}

export interface EmployeeGpd {
  employeePositionId: number
  tariffGradeId: number
  gpdRate: number
}

export interface EmployeePkr {
  employeePositionId: number
  tariffGradeId: number
  pkrHours: number
}

export interface EmployeeNonPedagogical {
  employeePositionId: number
  hasDisinfectants: boolean
  hasNightShifts: boolean
  hasMentor: boolean
  mentorAmount: number
  hasLibraryMgmt: boolean
  libraryMgmtAmount: number
  hasTextbooks: boolean
  textbooksAmount: number
}

export interface EmployeePosition {
  id: number
  employeeId: number
  positionId: number
  positionName: string
  departmentName: string
  workerClass: WorkerClass
  tariffGradeId: number
  tariffGrade: number
  tariffMonthlyRate: number
  rateCount: number
  isPrimary: boolean
  hireDate: string
  dismissalDate: string | null
  positionStartDate: string | null
  effectiveFrom: string
  maintainsMilitaryRecords: boolean
  hasUnfavorable: boolean
  complexityBonusPct: number | null
  prestigeBonusPct: number | null
  titleTypeId: number | null
  titleTypeName: string | null
  directorPct: number | null
  workload: EmployeeWorkload | null
  admin: EmployeeAdmin | null
  gpd: EmployeeGpd | null
  pkr: EmployeePkr | null
  nonPedagogical: EmployeeNonPedagogical | null
}

export interface EmployeeDetail {
  id: number
  tabNumber: string
  fullName: string
  taxId: string
  hireDate: string
  dismissalDate: string | null
  education: string | null
  generalExperienceYears: number
  pedExperienceYears: number
  status: EmployeeStatus
  socialBenefitPct: number | null
  isHonored: boolean
  honoredAmount: number | null
  isUnionMember: boolean
  positions: EmployeePosition[]
}

// ─── Requests ───

export interface CreateEmployeeRequest {
  tabNumber: string
  fullName: string
  taxId: string
  hireDate: string
  education: string | null
  generalExperienceYears: number
  pedExperienceYears: number
  socialBenefitPct: number | null
  isHonored: boolean
  honoredAmount: number | null
  isUnionMember: boolean
}

export interface UpdateEmployeeRequest {
  fullName: string
  taxId: string
  dismissalDate: string | null
  education: string | null
  generalExperienceYears: number
  pedExperienceYears: number
  socialBenefitPct: number | null
  status: EmployeeStatus
  isHonored: boolean
  honoredAmount: number | null
  isUnionMember: boolean
}

export interface CreatePositionRequest {
  positionId: number
  tariffGradeId: number
  rateCount: number
  hireDate: string
  isPrimary: boolean
  maintainsMilitaryRecords: boolean
  hasUnfavorable: boolean
  complexityBonusPct: number | null
  prestigeBonusPct: number | null
  positionStartDate: string | null
  titleTypeId: number | null
  directorPct: number | null
}

export interface UpdatePositionRequest {
  tariffGradeId: number
  rateCount: number
  dismissalDate: string | null
  isPrimary: boolean
  maintainsMilitaryRecords: boolean
  hasUnfavorable: boolean
  positionStartDate: string | null
  titleTypeId: number | null
  complexityBonusPct: number | null
  prestigeBonusPct: number | null
  directorPct: number | null
}

export interface WorkloadRequest {
  hours1To4: number
  individualHours1To4: number
  hours5To9: number
  individualHours5To9: number
  hours10To11: number
  individualHours10To11: number
  notebookHours1To4: number
  notebookHours5To9: number
  notebookHours10To11: number
  inclusiveHours1To4: number
  inclusiveHours5To9: number
  inclusiveHours10To11: number
  notebookRateId: number | null
  additionalHours: number
}

export interface AdminRequest {
  hasClassMgmt: boolean
  classGradeGroup: ClassGradeGroup | null
  hasCabinet: boolean
  cabinetType: CabinetType | null
  hasGym: boolean
  hasShootingRange: boolean
  hasComputers: boolean
  hasExtracurricular: boolean
  hasWebsite: boolean
}

export interface GpdRequest {
  gpdRate: number
  tariffGradeId: number
}

export interface PkrRequest {
  pkrHours: number
  tariffGradeId: number
}

export interface NonPedagogicalRequest {
  hasDisinfectants: boolean
  hasNightShifts: boolean
  hasMentor: boolean
  mentorAmount: number
  hasLibraryMgmt: boolean
  libraryMgmtAmount: number
  hasTextbooks: boolean
  textbooksAmount: number
}

// ─── Відсутності (середньоденна) ───

export interface SickLeave {
  id: number
  employeeId: number
  baseCalculationMode: CalcMode
  startDate: string
  endDate: string
  daysTotal: number
  daysEmployer: number
  daysFss: number
  workingDaysAbsent: number
  insuranceSeniorityYrs: number
  paymentPct: number
  baseAmount: number
  baseExcludedDays: number
  baseDays: number
  averageDaily: number
  amountEmployer: number
  amountFss: number
  totalAmount: number
  overrideAmountEmployer: number | null
  overrideAmountFss: number | null
  efssNumber: string | null
  notes: string | null
  createdAt: string
}

export interface Vacation {
  id: number
  employeeId: number
  baseCalculationMode: CalcMode
  vacationType: VacationType
  startDate: string
  endDate: string
  calendarDays: number
  workingDaysAbsent: number
  baseAmount: number | null
  baseDays: number | null
  averageDaily: number | null
  totalAmount: number | null
  overrideTotalAmount: number | null
  isCarryOver: boolean
  orderNumber: string | null
  notes: string | null
  createdAt: string
}

export interface TrainingLeave {
  id: number
  employeeId: number
  baseCalculationMode: CalcMode
  startDate: string
  endDate: string
  workingDaysAbsent: number
  baseAmount: number
  baseWorkingDays: number
  averageDaily: number
  totalAmount: number
  overrideTotalAmount: number | null
  institutionName: string | null
  notes: string | null
  createdAt: string
}

export interface CreateSickLeaveRequest {
  baseCalculationMode: CalcMode
  startDate: string
  endDate: string
  daysTotal: number
  workingDaysAbsent: number
  baseAmount: number
  baseExcludedDays: number
  insuranceSeniorityYrs: number
  paymentPct: number
  overrideAmountEmployer: number | null
  overrideAmountFss: number | null
  efssNumber: string | null
  notes: string | null
}

export interface CreateVacationRequest {
  baseCalculationMode: CalcMode
  vacationType: VacationType
  startDate: string
  endDate: string
  calendarDays: number
  workingDaysAbsent: number
  baseAmount: number | null
  baseDays: number | null
  overrideTotalAmount: number | null
  orderNumber: string | null
  notes: string | null
}

export interface CreateTrainingLeaveRequest {
  baseCalculationMode: CalcMode
  startDate: string
  endDate: string
  workingDaysAbsent: number
  baseAmount: number
  baseWorkingDays: number
  overrideTotalAmount: number | null
  institutionName: string | null
  notes: string | null
}

// ─── Табель ───

export interface Timesheet {
  id: number
  employeeId: number
  tabNumber: string
  fullName: string
  year: number
  month: number
  workedDays: number
  workedHours: number
  nightHours: number
  replacementHours: number
  holidayAmount: number
  advance: number
  enforcementOrders: number
  annualBonus: number
  bonus: number
  recalculation: number
  physEducation: number
  downtime: number
  indexation: number
  unfavorableManual: number
}

export interface TimesheetRequest {
  employeeId: number
  year: number
  month: number
  workedDays: number
  workedHours: number
  nightHours: number
  replacementHours: number
  holidayAmount: number
  advance: number
  enforcementOrders: number
  annualBonus: number
  bonus: number
  recalculation: number
  physEducation: number
  downtime: number
  indexation: number
  unfavorableManual: number
}

// ─── Розрахунок ───

export interface CalcComponent {
  name: string
  amount: number
  formula: string
  sourceClass: WorkerClass | null
}

export interface PositionCalcInfo {
  positionName: string
  workerClass: WorkerClass
  rateCount: number
}

export interface CalcResult {
  employeeId: number
  fullName: string
  taxId: string
  year: number
  month: number
  normDays: number
  workedDays: number
  positions: PositionCalcInfo[]
  earnings: CalcComponent[]
  gross: number
  deductions: CalcComponent[]
  totalWithheld: number
  netPay: number
  paramsSnapshot: Record<string, number>
}

// Стан підпису місяця: усього збережених розрахунків і скільки підписано.
export interface MonthSignStatus {
  total: number
  signed: number
}

// Прев'ю авто-бази: чи вистачає підписаної історії для події і скільки нарахувань вона дає.
export interface AvgBasePreview {
  signedMonths: number
  requiredMonths: number
  enough: boolean
  amount: number
}

// Правило «що входить у базу середньоденної»: виплата + 4 галочки на кожен випадок.
export interface AvgSalaryInclusionRule {
  id: number
  fieldKey: string
  label: string
  includeSick: boolean
  includeVacation: boolean
  includeTraining: boolean
  includeCompensation: boolean
}

// ─── Імпорт ───

export interface ParserError {
  row: number
  field: string | null
  message: string
  severity: 0 | 1 // 0 = Error, 1 = Warning
}

export interface ImportReport {
  created: number
  updated: number
  skipped: number
  errors: ParserError[]
}
