import { Badge } from '@/components/ui/badge'
import { getInitials } from '@/lib/string-utils'
import type { CalendarGridItem } from '../types'

interface CalendarTimeSlotProps {
  time: string
  item?: CalendarGridItem
  isSelected?: boolean
  isSelectable?: boolean
  isHoliday?: boolean
  durationMinutes?: number
  onSelect: (time: string) => void
}

const COLORS = {
  Individual: { base: 'bg-blue-100 border-blue-200', future: 'bg-blue-50 border-blue-100' },
  Group: { base: 'bg-green-100 border-green-200', future: 'bg-green-50 border-green-100' },
  Workshop: { base: 'bg-amber-100 border-amber-200', future: 'bg-amber-50 border-amber-100' },
  Trail: { base: 'bg-purple-100 border-purple-200', future: 'bg-purple-50 border-purple-100' },
}

export const CalendarTimeSlot = ({
  time,
  item,
  isSelected = false,
  isSelectable = false,
  isHoliday = false,
  onSelect,
}: CalendarTimeSlotProps) => {
  const handleClick = () => {
    if (!item && isSelectable) {
      onSelect(time)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!item && isSelectable && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault()
      onSelect(time)
    }
  }

  // Empty slot
  if (!item) {
    const baseClasses = `
      w-full min-h-[60px] px-2 py-1
      border rounded
      text-xs text-slate-500
      transition-colors
      focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-1
      ${isHoliday ? 'bg-amber-50 border-amber-200' : 'bg-white border-slate-200'}
      ${isSelectable ? 'cursor-pointer hover:bg-slate-100' : 'cursor-not-allowed'}
      ${isSelected ? 'ring-2 ring-primary ring-offset-1' : ''}
    `

    return (
      <button
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        className={baseClasses}
        disabled={!isSelectable}
        aria-label={`Empty slot at ${time}`}
      >
        <span>{time}</span>
      </button>
    )
  }

  // Occupied slot
  const colorClasses = item.isFuture
    ? COLORS[item.courseType].future
    : COLORS[item.courseType].base

  const visibleStudents = item.studentNames.slice(0, 3)
  const remainingCount = item.studentNames.length - 3

  return (
    <button
      className={`
        w-full min-h-[60px] px-2 py-1.5
        border rounded
        text-left
        transition-all
        focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-1
        ${colorClasses}
        ${isSelected ? 'ring-2 ring-primary ring-offset-1' : ''}
        cursor-default
      `}
      aria-label={`Occupied slot: ${item.title} at ${time}`}
      tabIndex={0}
    >
      <div className="space-y-1">
        <p className="text-xs font-medium text-slate-900 line-clamp-1">{item.title}</p>

        <div className="flex flex-wrap gap-1">
          {visibleStudents.map((studentName) => (
            <Badge
              key={studentName}
              variant="secondary"
              className="flex items-center justify-center h-5 w-5 rounded-full text-[10px] font-semibold p-0"
            >
              {getInitials(studentName)}
            </Badge>
          ))}
          {remainingCount > 0 && (
            <Badge
              variant="secondary"
              className="flex items-center justify-center h-5 px-1.5 rounded-full text-[10px] font-semibold"
            >
              +{remainingCount}
            </Badge>
          )}
        </div>

        {item.frequency && (
          <Badge variant="outline" className="text-[10px] py-0 h-4">
            {item.frequency}
          </Badge>
        )}
      </div>
    </button>
  )
}
