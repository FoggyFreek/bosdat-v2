import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { roomsApi, calendarApi, coursesApi, holidaysApi } from '@/services/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { Step3Summary } from './Step3Summary'
import { CalendarDayNavigation } from './CalendarDayNavigation'
import { CalendarDayGrid } from './CalendarDayGrid'
import { useCalendarGridItems } from '../hooks/useCalendarGridItems'
import { getWeekStart, formatDateForApi, calculateEndTime } from '@/lib/calendar-utils'
import { useToast } from '@/hooks/use-toast'
import type { Room } from '@/features/rooms/types'
import type { DayCalendar, Holiday } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'

interface Step3CalendarSlotSelectionProps {
  teacherId: string
  durationMinutes: number
}

export const Step3CalendarSlotSelection = ({
  teacherId,
  durationMinutes,
}: Step3CalendarSlotSelectionProps) => {
  const { formData, updateStep3 } = useEnrollmentForm()
  const { step1, step3 } = formData
  const { toast } = useToast()

  const [weekOffset, setWeekOffset] = useState(0)
  const [selectedDate, setSelectedDate] = useState(() => new Date())

  const weekStart = useMemo(() => {
    const baseDate = new Date()
    baseDate.setDate(baseDate.getDate() + weekOffset)
    return getWeekStart(baseDate)
  }, [weekOffset])

  // Fetch rooms
  const { data: rooms = [], isLoading: isLoadingRooms, error: roomsError } = useQuery<Room[]>({
    queryKey: ['rooms', { activeOnly: true }],
    queryFn: () => roomsApi.getAll({ activeOnly: true }),
  })

  // Fetch day calendar data
  const {
    data: dayCalendar,
    isLoading: isLoadingCalendar,
    error: calendarError,
  } = useQuery<DayCalendar>({
    queryKey: ['calendar', 'day', formatDateForApi(selectedDate), teacherId, step3.selectedRoomId],
    queryFn: () =>
      calendarApi.getDay({
        date: formatDateForApi(selectedDate),
        teacherId,
        roomId: step3.selectedRoomId || undefined,
      }),
    enabled: !!step3.selectedRoomId,
  })

  // Fetch courses
  const { data: courses = [], isLoading: isLoadingCourses } = useQuery<Course[]>({
    queryKey: ['courses', { dayOfWeek: selectedDate.getDay(), teacherId }],
    queryFn: () =>
      coursesApi.getAll({
        dayOfWeek: selectedDate.getDay(),
        teacherId,
      }),
    enabled: !step1.isTrial,
  })

  // Fetch holidays
  const { data: holidays = [] } = useQuery<Holiday[]>({
    queryKey: ['holidays'],
    queryFn: () => holidaysApi.getAll(),
  })

  // Transform data
  const gridItems = useCalendarGridItems({
    date: formatDateForApi(selectedDate),
    lessons: dayCalendar?.lessons || [],
    courses,
    isTrial: step1.isTrial,
  })

  const isHoliday = dayCalendar?.isHoliday || false
  const isLoading = isLoadingRooms || isLoadingCalendar || isLoadingCourses

  const handleWeekChange = (days: number) => {
    setWeekOffset(prev => prev + days)
  }

  const handleDateSelect = (date: Date) => {
    setSelectedDate(date)
    updateStep3({
      selectedDate: formatDateForApi(date),
      selectedDayOfWeek: date.getDay(),
      selectedStartTime: null,
      selectedEndTime: null,
    })
  }

  const handleTimeSelect = async (time: string) => {
    if (!step3.selectedRoomId) {
      toast({
        title: 'Room Required',
        description: 'Please select a room first.',
        variant: 'destructive',
      })
      return
    }

    const endTime = calculateEndTime(time, durationMinutes)

    // Check availability
    try {
      const availability = await calendarApi.checkAvailability({
        date: formatDateForApi(selectedDate),
        startTime: time,
        endTime,
        teacherId,
        roomId: step3.selectedRoomId,
      })

      if (!availability.isAvailable) {
        toast({
          title: 'Time Slot Unavailable',
          description: availability.conflicts?.[0]?.description || 'This time slot is not available.',
          variant: 'destructive',
        })
        return
      }

      // Update context with valid selection
      updateStep3({
        selectedStartTime: time,
        selectedEndTime: endTime,
      })

      toast({
        title: 'Time Slot Selected',
        description: `Selected ${time} - ${endTime}`,
      })
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to check availability. Please try again.',
        variant: 'destructive',
      })
    }
  }

  if (roomsError || calendarError) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-center">
          <p className="text-red-600 font-medium">Error loading data</p>
          <p className="text-sm text-slate-600 mt-2">
            {(roomsError as Error)?.message || (calendarError as Error)?.message}
          </p>
        </div>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <p className="text-slate-600">Loading calendar...</p>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-[calc(100vh-12rem)]">
      {/* Summary - Anchored at top */}
      <div className="sticky top-0 z-10 bg-white pb-4">
        <Step3Summary rooms={rooms} />
      </div>

      {/* Day Navigation - Anchored below summary */}
      <div className="sticky top-[calc(theme(spacing.0)+var(--summary-height,8rem))] z-10 bg-white pb-4">
        <CalendarDayNavigation
          weekStart={weekStart}
          selectedDate={selectedDate}
          holidays={holidays}
          onDateSelect={handleDateSelect}
          onWeekChange={handleWeekChange}
        />
      </div>

      {/* Calendar Grid - Scrollable */}
      <div className="flex-1 overflow-y-auto">
        <CalendarDayGrid
          items={gridItems}
          selectedTime={step3.selectedStartTime}
          isHoliday={isHoliday}
          durationMinutes={durationMinutes}
          onTimeSelect={handleTimeSelect}
        />
      </div>
    </div>
  )
}
