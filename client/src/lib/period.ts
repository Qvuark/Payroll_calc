// Період розрахунку (рік + місяць): спільний тип і поточний місяць.
// Винесено з MonthPicker, щоб компонент-файл експортував лише компонент (react-refresh).

export interface Period {
  year: number
  month: number
}

/** Поточний місяць — стартове значення для табеля/розрахунку. */
export function currentPeriod(): Period {
  const d = new Date()
  return { year: d.getFullYear(), month: d.getMonth() + 1 }
}
