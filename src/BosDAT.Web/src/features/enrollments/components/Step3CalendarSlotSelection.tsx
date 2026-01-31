import { useState, useMemo, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { roomsApi, calendarApi, coursesApi } from '@/services/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { Step3Summary } from './Step3Summary'
import CalendarComponent from '@/features/calendar/CalendarComponent'
import { useCalendarEvents } from '../hooks/useCalendarEvents'
import { getWeekStart, getWeekDays, formatDateForApi, calculateEndTime } from '@/lib/calendar-utils'
import { useToast } from '@/hooks/use-toast'
import { createEnrollmentColorScheme, formatTimeSlotToTime } from '../utils/calendarAdapter'
import type { Room } from '@/features/rooms/types'
import type { WeekCalendar } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'
import type { TimeSlot } from '@/features/calendar/types'

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

  const weekDays = useMemo(() => getWeekDays(weekStart), [weekStart])

  const colorScheme = useMemo(() => createEnrollmentColorScheme(), [])

  // Fetch rooms
  const { data: rooms = [], isLoading: isLoadingRooms, error: roomsError } = useQuery<Room[]>({
    queryKey: ['rooms', { activeOnly: true }],
    queryFn: () => roomsApi.getAll({ activeOnly: true }),
  })

  // Fetch week calendar data
  const {
    data: weekCalendar,
    isLoading: isLoadingCalendar,
    error: calendarError,
  } = useQuery<WeekCalendar>({
    queryKey: ['calendar', 'week', formatDateForApi(weekStart), teacherId, step3.selectedRoomId],
    queryFn: () =>
      calendarApi.getWeek({
        date: formatDateForApi(weekStart),
        teacherId,
        roomId: step3.selectedRoomId || undefined,
      }),
    enabled: !!step3.selectedRoomId,
  })

  // Fetch all courses for the teacher (for the entire week)
  const { data: courses = [], isLoading: isLoadingCourses } = useQuery<Course[]>({
    queryKey: ['courses', { teacherId }],
    queryFn: () =>
      coursesApi.getAll({
        teacherId,
      }),
    enabled: !step1.isTrial,
  })

  // Transform API data directly to calendar events
  const events = useCalendarEvents({
    weekStart,
    lessons: weekCalendar?.lessons || [],
    courses,
    holidays: weekCalendar?.holidays || [],
    isTrial: step1.isTrial,
  })

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

  const handleTimeSelect = useCallback(
    async (time: string, date?: Date) => {
      if (!step3.selectedRoomId) {
        toast({
          title: 'Room Required',
          description: 'Please select a room first.',
          variant: 'destructive',
        })
        return
      }

      // Use provided date or fall back to selectedDate
      const targetDate = date || selectedDate
      const endTime = calculateEndTime(time, durationMinutes)

      // Check availability
      try {
        const availability = await calendarApi.checkAvailability({
          date: formatDateForApi(targetDate),
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

        // Update context with valid selection including the date
        updateStep3({
          selectedDate: formatDateForApi(targetDate),
          selectedDayOfWeek: targetDate.getDay(),
          selectedStartTime: time,
          selectedEndTime: endTime,
        })

        // Update local state to reflect the selected date
        setSelectedDate(targetDate)

        toast({
          title: 'Time Slot Selected',
          description: `Selected ${formatDateForApi(targetDate)} ${time} - ${endTime}`,
        })
      } catch (error) {
        console.error('Failed to check availability:', error)
        toast({
          title: 'Error',
          description: 'Failed to check availability. Please try again.',
          variant: 'destructive',
        })
      }
    },
    [step3.selectedRoomId, durationMinutes, selectedDate, teacherId, updateStep3, toast]
  )

  const handleTimeslotClick = useCallback(
    (timeslot: TimeSlot) => {
      const time = formatTimeSlotToTime(timeslot)
      // Pass both time and date from the timeslot, overriding any previously selected date
      handleTimeSelect(time, timeslot.date)
    },
    [handleTimeSelect]
  )

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

      {/* Calendar Grid - Scrollable */}
      <div className="flex-1 overflow-hidden">
        <CalendarComponent
          title={`Week of ${formatDateForApi(weekStart)}`}
          events={events}
          dates={weekDays}
          daystartTime={8}
          dayendTime={23}
          hourHeight={80}
          colorScheme={colorScheme}
          onNavigatePrevious={() => handleWeekChange(-7)}
          onNavigateNext={() => handleWeekChange(7)}
          onTimeslotClick={handleTimeslotClick}
          onDateSelect={handleDateSelect}
          highlightedDate={selectedDate}
        />
      </div>
    </div>
  )
}
