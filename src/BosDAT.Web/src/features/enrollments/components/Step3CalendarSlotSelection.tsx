import { useState, useMemo, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { roomsApi, calendarApi, coursesApi } from '@/services/api'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { Step3Summary } from './Step3Summary'
import { CalendarComponent } from '@/components/calendar/CalendarComponent'
import { useCalendarEvents } from '../hooks/useCalendarEvents'
import { getWeekStart, getWeekDays, formatDateForApi, calculateEndTime } from '@/lib/calendar-utils'
import { useToast } from '@/hooks/use-toast'
import { createEnrollmentColorScheme, formatTimeSlotToTime } from '../utils/calendarAdapter'
import type { Room } from '@/features/rooms/types'
import type { WeekCalendar } from '@/features/schedule/types'
import type { Course } from '@/features/courses/types'
import type { CalendarEvent, TimeSlot } from '@/components/calendar/types'

interface Step3CalendarSlotSelectionProps {
  teacherId: string
  durationMinutes: number
}

export const Step3CalendarSlotSelection = ({
  teacherId,
  durationMinutes,
}: Step3CalendarSlotSelectionProps) => {
  const { formData, updateStep3 } = useEnrollmentForm()
  const { step1, step2, step3 } = formData
  const { toast } = useToast()

  const [selectedDate, setSelectedDate] = useState<Date>(() => {
    if (step1.startDate) {
      return new Date(step1.startDate)
    }
    return new Date()
  })

  const [placeholderEvent, setPlaceholderEvent] = useState<CalendarEvent | null>(null)

  const weekStart = useMemo(() => {
    return getWeekStart(selectedDate)
  }, [selectedDate])

  const weekDays = useMemo(() => getWeekDays(weekStart), [weekStart])

  const colorScheme = useMemo(() => createEnrollmentColorScheme(), [])

  // Fetch rooms
  const { data: rooms = [], isLoading: isLoadingRooms, error: roomsError } = useQuery<Room[]>({
    queryKey: ['rooms', { activeOnly: true }],
    queryFn: () => roomsApi.getAll({ activeOnly: true }),
  })

  // Fetch week calendar data - loads all events initially, filters by room when selected
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

  const allEvents = useMemo(() => {
    if (placeholderEvent) {
      return [...events, placeholderEvent]
    }
    return events
  }, [events, placeholderEvent])


  const isLoading = isLoadingRooms || isLoadingCalendar || isLoadingCourses

  const handleWeekChange = (days: number) => {
    const newDate = new Date(selectedDate)
    newDate.setDate(newDate.getDate() + days)
    const newWeekStart = getWeekStart(newDate)
    setSelectedDate(newWeekStart)
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

  const createPlaceholderEvent = useCallback(
    (date: Date, startTime: string, endTime: string): CalendarEvent => {
      const dateStr = formatDateForApi(date)
      return {
        startDateTime: `${dateStr}T${startTime}:00`,
        endDateTime: `${dateStr}T${endTime}:00`,
        title: 'Selected Slot',
        frequency: step1.recurrence === 'Biweekly' ? 'bi-weekly' : 'weekly',
        eventType: 'placeholder',
        attendees: step2.students.map(s => s.studentName),
      }
    },
    [step1.recurrence, step2.students]
  )


  // When a time is selected, check availability and update context
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

        const placeholder = createPlaceholderEvent(targetDate, time, endTime)
        setPlaceholderEvent(placeholder)

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
    [step3.selectedRoomId, createPlaceholderEvent, durationMinutes, selectedDate, teacherId, updateStep3, toast]
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
    <div className="grid grid-cols-1 lg:grid-cols-[400px_1fr] gap-6 h-[calc(100vh-28rem)]">
      {/* Left side: Summary and Room Selection */}
      <div className="lg:overflow-y-auto lg:pr-2">
        <Step3Summary rooms={rooms} />
      </div>

      {/* Right side: Calendar - takes remaining space on desktop, full width on mobile */}
      <div className="overflow-hidden">
        <CalendarComponent
          title={`Week of ${formatDateForApi(weekStart)}`}
          events={allEvents}
          dates={weekDays}
          dayStartTime={8}
          dayEndTime={23}
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
