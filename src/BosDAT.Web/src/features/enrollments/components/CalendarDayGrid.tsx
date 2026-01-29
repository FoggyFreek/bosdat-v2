import { useEffect, useRef } from 'react'
import { CalendarTimeSlot } from './CalendarTimeSlot'
import { timeToMinutes, minutesToTime } from '@/lib/calendar-utils'
import type { CalendarGridItem } from '../types'

interface CalendarDayGridProps {
  items: CalendarGridItem[]
  selectedTime: string | null
  isHoliday: boolean
  durationMinutes: number
  onTimeSelect: (time: string) => void
}

const START_TIME = '08:00'
const END_TIME = '23:00'
const INTERVAL_MINUTES = 10

const generateTimeSlots = (): string[] => {
  const slots: string[] = []
  const startMinutes = timeToMinutes(START_TIME)
  const endMinutes = timeToMinutes(END_TIME)

  for (let minutes = startMinutes; minutes < endMinutes; minutes += INTERVAL_MINUTES) {
    slots.push(minutesToTime(minutes))
  }

  return slots
}

const isSlotAvailable = (
  slotTime: string,
  durationMinutes: number,
  items: CalendarGridItem[],
  isHoliday: boolean
): boolean => {
  if (isHoliday) return false

  const slotStart = timeToMinutes(slotTime)
  const slotEnd = slotStart + durationMinutes

  // Check if slot overlaps with any existing item
  for (const item of items) {
    const itemStart = timeToMinutes(item.startTime)
    const itemEnd = timeToMinutes(item.endTime)

    // Check for overlap: slot starts before item ends AND slot ends after item starts
    if (slotStart < itemEnd && slotEnd > itemStart) {
      return false
    }
  }

  return true
}

const getItemAtTime = (time: string, items: CalendarGridItem[]): CalendarGridItem | undefined => {
  return items.find(item => item.startTime === time)
}

export const CalendarDayGrid = ({
  items,
  selectedTime,
  isHoliday,
  durationMinutes,
  onTimeSelect,
}: CalendarDayGridProps) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const timeSlots = generateTimeSlots()

  // Scroll to current time on mount
  useEffect(() => {
    if (containerRef.current) {
      const now = new Date()
      const currentTime = `${String(now.getHours()).padStart(2, '0')}:${String(Math.floor(now.getMinutes() / 10) * 10).padStart(2, '0')}`
      const currentSlotIndex = timeSlots.indexOf(currentTime)

      if (currentSlotIndex >= 0) {
        const slotHeight = 60 // min-h-[60px]
        const scrollPosition = currentSlotIndex * slotHeight - 200 // Offset to show some slots above

        containerRef.current.scrollTop = Math.max(0, scrollPosition)
      }
    }
  }, [timeSlots]) // Empty dependency array - only run on mount

  const handleTimeSelect = (time: string) => {
    const item = getItemAtTime(time, items)
    if (!item && isSlotAvailable(time, durationMinutes, items, isHoliday)) {
      onTimeSelect(time)
    }
  }

  return (
    <div ref={containerRef} className="overflow-y-auto h-[600px] space-y-0.5 pr-2">
      {timeSlots.map((time) => {
        const item = getItemAtTime(time, items)
        const isSelected = selectedTime === time
        const isSelectable = isSlotAvailable(time, durationMinutes, items, isHoliday)

        return (
          <CalendarTimeSlot
            key={time}
            time={time}
            item={item}
            isSelected={isSelected}
            isSelectable={isSelectable}
            isHoliday={isHoliday}
            durationMinutes={durationMinutes}
            onSelect={handleTimeSelect}
          />
        )
      })}
    </div>
  )
}
