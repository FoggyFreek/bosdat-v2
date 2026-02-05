import { memo } from 'react'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { SummaryCard } from '@/components/SummaryCard'
import { getInitials } from '@/lib/string-utils'
import { useEnrollmentForm } from '../context/EnrollmentFormContext'
import { Step2Summary } from './Step2Summary'
import type { Room } from '@/features/rooms/types'

interface Step3SummaryProps {
  readonly rooms: readonly Room[]
}

export const Step3Summary = memo(function Step3Summary({ rooms }: Step3SummaryProps) {
  const { formData, updateStep3 } = useEnrollmentForm()
  const { step2, step3 } = formData
  const students = step2.students || []

  const handleRoomChange = (value: string) => {
    updateStep3({ selectedRoomId: Number.parseInt(value, 10) })
  }

  return (
    <div className="space-y-4">
      <Step2Summary />

      <SummaryCard title="Selected Students">
        <div className="flex flex-wrap gap-2">
          {students.map((student) => (
            <Badge
              key={student.studentId}
              variant="secondary"
              className="flex items-center justify-center h-8 w-8 rounded-full text-xs font-semibold"
            >
              {getInitials(student.studentName)}
            </Badge>
          ))}
        </div>
      </SummaryCard>

      <div className="rounded-lg border bg-muted/50 p-4">
        <h3 className="font-medium mb-3 text-sm">Room Selection</h3>
        <Select
          value={step3.selectedRoomId?.toString() || ''}
          onValueChange={handleRoomChange}
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select a room" />
          </SelectTrigger>
          <SelectContent>
            {(rooms || []).map((room) => (
              <SelectItem key={room.id} value={room.id.toString()}>
                {room.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </div>
  )
})
