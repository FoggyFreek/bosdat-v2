import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Clock, Edit2, X, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { teachersApi } from '@/services/api'
import { useAuth } from '@/context/AuthContext'
import type { TeacherAvailability, UpdateTeacherAvailability } from '@/features/teachers/types'

interface TeacherAvailabilitySectionProps {
  teacherId: string
}

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
const DEFAULT_FROM_TIME = '09:00:00'
const DEFAULT_UNTIL_TIME = '22:00:00'
const UNAVAILABLE_TIME = '00:00:00'

// Display order: Monday-Sunday (1-6, 0)
const DISPLAY_ORDER = [1, 2, 3, 4, 5, 6, 0]

export function TeacherAvailabilitySection({ teacherId }: TeacherAvailabilitySectionProps) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [isEditing, setIsEditing] = useState(false)
  const [editedAvailability, setEditedAvailability] = useState<UpdateTeacherAvailability[]>([])

  const canEdit = user?.roles.includes('Admin') || user?.roles.includes('Teacher')

  const { data: availability = [], isLoading } = useQuery<TeacherAvailability[]>({
    queryKey: ['teacher', teacherId, 'availability'],
    queryFn: () => teachersApi.getAvailability(teacherId),
    enabled: !!teacherId,
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateTeacherAvailability[]) =>
      teachersApi.updateAvailability(teacherId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teacher', teacherId, 'availability'] })
      setIsEditing(false)
    },
  })

  const availabilityByDay = useMemo(() => {
    const map = new Map<number, TeacherAvailability>()
    availability.forEach((a) =>
      map.set(a.dayOfWeek, a))
    return map
  }, [availability])

  const editedByDay = useMemo(() => {
    const map = new Map<number, UpdateTeacherAvailability>()
    editedAvailability.forEach((a) => map.set(a.dayOfWeek, a))
    return map
  }, [editedAvailability])

  const startEditing = () => {
    // Initialize edit state with current availability or defaults for all 7 days
    const initial: UpdateTeacherAvailability[] = DISPLAY_ORDER.map((day) => {
      const existing = availabilityByDay.get(day)
      return {
        dayOfWeek: day,
        fromTime: existing?.fromTime ?? DEFAULT_FROM_TIME,
        untilTime: existing?.untilTime ?? DEFAULT_UNTIL_TIME,
      }
    })
    setEditedAvailability(initial)
    setIsEditing(true)
  }

  const cancelEditing = () => {
    setIsEditing(false)
    setEditedAvailability([])
  }

  const handleTimeChange = (dayOfWeek: number, field: 'fromTime' | 'untilTime', value: string) => {
    setEditedAvailability((prev) =>
      prev.map((a) => (a.dayOfWeek === dayOfWeek ? { ...a, [field]: value + ':00' } : a))
    )
  }

  const setDayUnavailable = (dayOfWeek: number) => {
    setEditedAvailability((prev) =>
      prev.map((a) =>
        a.dayOfWeek === dayOfWeek
          ? { ...a, fromTime: UNAVAILABLE_TIME, untilTime: UNAVAILABLE_TIME }
          : a
      )
    )
  }

  const setDayAvailable = (dayOfWeek: number) => {
    setEditedAvailability((prev) =>
      prev.map((a) =>
        a.dayOfWeek === dayOfWeek
          ? { ...a, fromTime: DEFAULT_FROM_TIME, untilTime: DEFAULT_UNTIL_TIME }
          : a
      )
    )
  }

  const handleSave = () => {
    updateMutation.mutate(editedAvailability)
  }

  const isTimeValid = (fromTime: string, untilTime: string): boolean => {
    // 00:00-00:00 is valid (unavailable)
    if (fromTime === UNAVAILABLE_TIME && untilTime === UNAVAILABLE_TIME) {
      return true
    }
    // Otherwise, end time must be at least 1 hour after start time
    const from = parseTimeToMinutes(fromTime)
    const until = parseTimeToMinutes(untilTime)
    return until >= from + 60
  }

  const hasValidationErrors = useMemo(() => {
    return editedAvailability.some((a) => !isTimeValid(a.fromTime, a.untilTime))
  }, [editedAvailability])

  const formatTime = (time: string): string => {
    // Format "HH:mm:ss" to "HH:mm"
    return time.slice(0, 5)
  }

  const isUnavailable = (fromTime: string, untilTime: string): boolean => {
    return fromTime === UNAVAILABLE_TIME && untilTime === UNAVAILABLE_TIME
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="h-5 w-5" />
            Availability
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-8">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2">
          <Clock className="h-5 w-5" />
          Availability
        </CardTitle>
        {canEdit && !isEditing && (
          <Button variant="outline" size="sm" onClick={startEditing}>
            <Edit2 className="h-4 w-4 mr-2" />
            Edit
          </Button>
        )}
        {isEditing && (
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={cancelEditing}>
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleSave}
              disabled={hasValidationErrors || updateMutation.isPending}
            >
              <Check className="h-4 w-4 mr-2" />
              {updateMutation.isPending ? 'Saving...' : 'Save'}
            </Button>
          </div>
        )}
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {DISPLAY_ORDER.map((day) => {
            const existing = availabilityByDay.get(day)
            const edited = editedByDay.get(day)

            if (isEditing && edited) {
              const unavailable = isUnavailable(edited.fromTime, edited.untilTime)
              const valid = isTimeValid(edited.fromTime, edited.untilTime)

              return (
                <div
                  key={day}
                  className={`flex items-center gap-4 p-3 rounded-lg border ${!valid ? 'border-destructive bg-destructive/5' : 'border-border'}`}
                >
                  <span className="w-24 font-medium">{DAY_NAMES[day]}</span>
                  {unavailable ? (
                    <div className="flex items-center gap-4 flex-1">
                      <span className="text-muted-foreground">Unavailable</span>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setDayAvailable(day)}
                        className="ml-auto"
                      >
                        Set Available
                      </Button>
                    </div>
                  ) : (
                    <div className="flex items-center gap-2 flex-1">
                      <Input
                        type="time"
                        value={formatTime(edited.fromTime)}
                        onChange={(e) => handleTimeChange(day, 'fromTime', e.target.value)}
                        className="w-32"
                      />
                      <span className="text-muted-foreground">to</span>
                      <Input
                        type="time"
                        value={formatTime(edited.untilTime)}
                        onChange={(e) => handleTimeChange(day, 'untilTime', e.target.value)}
                        className="w-32"
                      />
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setDayUnavailable(day)}
                        className="ml-auto text-muted-foreground hover:text-destructive"
                      >
                        Set Unavailable
                      </Button>
                    </div>
                  )}
                </div>
              )
            }

            // Display mode
            if (!existing) {
              return (
                <div key={day} className="flex items-center gap-4 p-3 rounded-lg bg-muted/50">
                  <span className="w-24 font-medium">{DAY_NAMES[day]}</span>
                  <span className="text-muted-foreground">Not set</span>
                </div>
              )
            }

            const unavailable = isUnavailable(existing.fromTime, existing.untilTime)

            return (
              <div
                key={day}
                className={`flex items-center gap-4 p-3 rounded-lg ${unavailable ? 'bg-muted/50' : 'bg-green-50 dark:bg-green-950/20'}`}
              >
                <span className="w-24 font-medium">{DAY_NAMES[day]}</span>
                {unavailable ? (
                  <span className="text-muted-foreground">Unavailable</span>
                ) : (
                  <span className="text-green-700 dark:text-green-400">
                    {formatTime(existing.fromTime)} - {formatTime(existing.untilTime)}
                  </span>
                )}
              </div>
            )
          })}
        </div>
        {isEditing && hasValidationErrors && (
          <p className="text-sm text-destructive mt-4">
            End time must be at least 1 hour after start time. Use &quot;Set Unavailable&quot; to mark a day as unavailable.
          </p>
        )}
      </CardContent>
    </Card>
  )
}

function parseTimeToMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number)
  return hours * 60 + minutes
}
