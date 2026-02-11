import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { CalendarComponent } from '@/components/calendar/CalendarComponent'
import type { CalendarEvent } from '@/components/calendar/types'
import { formatDate } from '@/lib/datetime-helpers'
import type { useScheduleCalendarData } from '@/features/lessons/hooks/useScheduleCalendarData'

type ScheduleCalendar = ReturnType<typeof useScheduleCalendarData>

interface LessonSchedulerLayoutProps {
  title: string
  backTo: string
  summaryCard: ReactNode
  calendar: ScheduleCalendar
  allEvents: CalendarEvent[]
  slotLabel: string
  submitLabel: string
  pendingLabel: string
  isPending: boolean
  onSubmit: () => void
}

export function LessonSchedulerLayout({
  title,
  backTo,
  summaryCard,
  calendar,
  allEvents,
  slotLabel,
  submitLabel,
  pendingLabel,
  isPending,
  onSubmit,
}: LessonSchedulerLayoutProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to={backTo}>
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <h1 className="text-2xl font-bold">{title}</h1>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[320px_1fr] gap-4">
        <div className="space-y-4">
          {summaryCard}

          <div className="rounded-lg border bg-muted/50 p-4 space-y-3">
            <h3 className="font-medium text-sm">Filters</h3>
            <div className="space-y-2">
              <label className="text-xs text-muted-foreground">Teacher</label>
              <Select value={calendar.filterTeacher} onValueChange={calendar.setFilterTeacher}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="All teachers" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All teachers</SelectItem>
                  {calendar.teachers.map((teacher) => (
                    <SelectItem key={teacher.id} value={teacher.id}>
                      {teacher.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <label className="text-xs text-muted-foreground">Room</label>
              <Select value={calendar.filterRoom} onValueChange={calendar.setFilterRoom}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="All rooms" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All rooms</SelectItem>
                  {calendar.rooms.map((room) => (
                    <SelectItem key={room.id} value={room.id.toString()}>
                      {room.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {calendar.selectedSlot && (
            <div className="rounded-lg border border-primary bg-primary/5 p-4">
              <h3 className="font-medium text-sm mb-2">{slotLabel}</h3>
              <div className="grid gap-1 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Date:</span>
                  <span className="font-medium">{formatDate(calendar.selectedSlot.date)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Time:</span>
                  <span className="font-medium">
                    {calendar.selectedSlot.startTime} – {calendar.selectedSlot.endTime}
                  </span>
                </div>
              </div>
            </div>
          )}

          <Button
            className="w-full"
            disabled={!calendar.selectedSlot || isPending}
            onClick={onSubmit}
          >
            {isPending ? pendingLabel : submitLabel}
          </Button>
        </div>

        <div className="min-h-[600px]">
          {calendar.isLoading && (
            <div className="flex items-center justify-center py-16">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          )}
          {!calendar.isLoading && (
            <CalendarComponent
              title="Schedule"
              events={allEvents}
              dates={calendar.weekDays}
              colorScheme={calendar.colorScheme}
              onNavigatePrevious={calendar.goToPreviousWeek}
              onNavigateNext={calendar.goToNextWeek}
              onTimeslotClick={calendar.handleTimeslotClick}
              availability={calendar.availability}
              highlightedDate={calendar.highlightedDate}
              dayStartTime={8}
              dayEndTime={22}
              hourHeight={80}
            />
          )}
        </div>
      </div>
    </div>
  )
}
