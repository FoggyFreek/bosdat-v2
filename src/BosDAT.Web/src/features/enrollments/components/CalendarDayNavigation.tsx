import { Button } from '@/components/ui/button'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { getWeekDays, formatDateForApi } from '@/lib/calendar-utils'
import type { Holiday } from '@/features/schedule/types'

interface CalendarDayNavigationProps {
  weekStart: Date
  selectedDate: Date
  holidays: Holiday[]
  onDateSelect: (date: Date) => void
  onWeekChange: (days: number) => void
}

const DAY_NAMES = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

const isHoliday = (date: Date, holidays: Holiday[]): boolean => {
  const dateStr = formatDateForApi(date)
  return holidays.some((holiday) => {
    return dateStr >= holiday.startDate && dateStr <= holiday.endDate
  })
}

export const CalendarDayNavigation = ({
  weekStart,
  selectedDate,
  holidays,
  onDateSelect,
  onWeekChange,
}: CalendarDayNavigationProps) => {
  const weekDays = getWeekDays(weekStart)
  const selectedDateStr = formatDateForApi(selectedDate)

  const handleDayClick = (date: Date) => {
    onDateSelect(date)
  }

  const handleDayKeyDown = (e: React.KeyboardEvent, date: Date) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      onDateSelect(date)
    }
  }

  return (
    <div className="flex items-center justify-between gap-2">
      <Button
        variant="outline"
        size="icon"
        onClick={() => onWeekChange(-7)}
        aria-label="Previous week"
      >
        <ChevronLeft className="h-4 w-4" />
      </Button>

      <div className="flex flex-1 gap-1">
        {weekDays.map((day, index) => {
          const dateStr = formatDateForApi(day)
          const isSelected = dateStr === selectedDateStr
          const hasHoliday = isHoliday(day, holidays)

          return (
            <button
              key={dateStr}
              onClick={() => handleDayClick(day)}
              onKeyDown={(e) => handleDayKeyDown(e, day)}
              className={`
                flex flex-col items-center justify-center
                px-3 py-2 rounded-md
                transition-colors
                hover:bg-slate-100
                focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2
                ${isSelected ? 'bg-primary text-primary-foreground hover:bg-primary/90' : 'bg-white border'}
                relative
                flex-1
              `}
              aria-label={`${DAY_NAMES[index]} ${day.getDate()}`}
            >
              <span className="text-xs font-medium">{DAY_NAMES[index]}</span>
              <span className="text-lg font-semibold">{day.getDate()}</span>
              {hasHoliday && (
                <div className="absolute top-1 right-1 w-2 h-2 bg-amber-500 rounded-full" />
              )}
            </button>
          )
        })}
      </div>

      <Button
        variant="outline"
        size="icon"
        onClick={() => onWeekChange(7)}
        aria-label="Next week"
      >
        <ChevronRight className="h-4 w-4" />
      </Button>
    </div>
  )
}
