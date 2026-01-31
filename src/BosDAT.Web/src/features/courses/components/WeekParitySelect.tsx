import { AlertCircle } from 'lucide-react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import type { WeekParity } from '../types'

interface WeekParitySelectProps {
  value: WeekParity
  onChange: (value: WeekParity) => void
  disabled?: boolean
  helperText?: string
  has53WeekYearWarning?: boolean
}

const WEEK_PARITY_OPTIONS = [
  {
    value: 'All' as const,
    label: 'All Weeks (Every Week)',
    description: 'Course occurs every week',
  },
  {
    value: 'Odd' as const,
    label: 'Odd Weeks Only (1, 3, 5...)',
    description: 'Course occurs in odd ISO weeks',
  },
  {
    value: 'Even' as const,
    label: 'Even Weeks Only (2, 4, 6...)',
    description: 'Course occurs in even ISO weeks',
  },
] as const

export function WeekParitySelect({
  value,
  onChange,
  disabled = false,
  helperText,
  has53WeekYearWarning = false,
}: WeekParitySelectProps) {
  const selectedOption = WEEK_PARITY_OPTIONS.find((opt) => opt.value === value)

  return (
    <div className="space-y-2">
      <Select value={value} onValueChange={onChange} disabled={disabled}>
        <SelectTrigger>
          <SelectValue>{selectedOption?.label ?? 'Select week parity'}</SelectValue>
        </SelectTrigger>
        <SelectContent>
          {WEEK_PARITY_OPTIONS.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {helperText && <p className="text-sm text-muted-foreground">{helperText}</p>}

      {has53WeekYearWarning && value !== 'All' && (
        <Alert variant="default" className="border-amber-200 bg-amber-50">
          <AlertCircle className="h-4 w-4 text-amber-600" />
          <AlertDescription className="text-sm text-amber-900">
            <strong>Note:</strong> This year has 53 ISO weeks. Week 53 and Week 1 of next year
            will both be {value} weeks, creating a 7-day gap instead of the usual 14 days between
            lessons.
          </AlertDescription>
        </Alert>
      )}
    </div>
  )
}
