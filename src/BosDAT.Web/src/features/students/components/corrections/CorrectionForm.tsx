import { memo } from 'react'
import { Pencil, Calculator } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type {
  LedgerEntryType,
  StudentEnrollment,
  EnrollmentPricing,
} from '@/features/students/types'
import { PricingBreakdown } from './PricingBreakdown'

type CalculationMethod = 'manual' | 'course-based'

const VALID_ENTRY_TYPES = ['Credit', 'Debit'] as const
function isLedgerEntryType(value: string): value is LedgerEntryType {
  return VALID_ENTRY_TYPES.includes(value as LedgerEntryType)
}

interface CorrectionFormProps {
  // Form state
  readonly description: string
  readonly amount: string
  readonly entryType: LedgerEntryType
  readonly calculationMethod: CalculationMethod
  readonly selectedCourseId: string
  readonly numberOfOccurrences: string
  // Data
  readonly activeEnrollments: readonly StudentEnrollment[]
  readonly enrollmentPricing?: Readonly<EnrollmentPricing>
  readonly calculatedAmount: number
  readonly isPricingLoading: boolean
  readonly isSubmitting: boolean
  // Handlers
  readonly onDescriptionChange: (value: string) => void
  readonly onAmountChange: (value: string) => void
  readonly onEntryTypeChange: (value: LedgerEntryType) => void
  readonly onCalculationMethodChange: (method: CalculationMethod) => void
  readonly onCourseChange: (courseId: string) => void
  readonly onOccurrencesChange: (value: string) => void
  readonly onSubmit: (e: React.FormEvent) => void
  readonly onCancel: () => void
  readonly isFormValid: boolean
}

export function CorrectionForm({
  description,
  amount,
  entryType,
  calculationMethod,
  selectedCourseId,
  numberOfOccurrences,
  activeEnrollments,
  enrollmentPricing,
  calculatedAmount,
  isPricingLoading,
  isSubmitting,
  onDescriptionChange,
  onAmountChange,
  onEntryTypeChange,
  onCalculationMethodChange,
  onCourseChange,
  onOccurrencesChange,
  onSubmit,
  onCancel,
  isFormValid,
}: CorrectionFormProps) {
  return (
    <form onSubmit={onSubmit} className="mb-6 p-4 bg-muted/50 rounded-lg space-y-4">
      <CalculationMethodToggle
        method={calculationMethod}
        onChange={onCalculationMethodChange}
      />

      {calculationMethod === 'manual' ? (
        <ManualEntryFields
          description={description}
          amount={amount}
          entryType={entryType}
          onDescriptionChange={onDescriptionChange}
          onAmountChange={onAmountChange}
          onEntryTypeChange={onEntryTypeChange}
        />
      ) : (
        <CourseBasedFields
          description={description}
          entryType={entryType}
          selectedCourseId={selectedCourseId}
          numberOfOccurrences={numberOfOccurrences}
          activeEnrollments={activeEnrollments}
          enrollmentPricing={enrollmentPricing}
          calculatedAmount={calculatedAmount}
          isPricingLoading={isPricingLoading}
          onDescriptionChange={onDescriptionChange}
          onEntryTypeChange={onEntryTypeChange}
          onCourseChange={onCourseChange}
          onOccurrencesChange={onOccurrencesChange}
        />
      )}

      <div className="flex gap-2">
        <Button type="submit" disabled={isSubmitting || !isFormValid}>
          {isSubmitting ? 'Adding...' : 'Add Entry'}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}

interface CalculationMethodToggleProps {
  readonly method: CalculationMethod
  readonly onChange: (method: CalculationMethod) => void
}

const CalculationMethodToggle = memo(function CalculationMethodToggle({
  method,
  onChange,
}: CalculationMethodToggleProps) {
  return (
    <div className="space-y-2">
      <Label>Calculation Method</Label>
      <div className="flex items-center rounded-md border w-fit">
        <Button
          type="button"
          variant={method === 'manual' ? 'default' : 'ghost'}
          size="sm"
          className="rounded-r-none"
          onClick={() => onChange('manual')}
        >
          <Pencil className="h-4 w-4 mr-2" />
          Manual Entry
        </Button>
        <Button
          type="button"
          variant={method === 'course-based' ? 'default' : 'ghost'}
          size="sm"
          className="rounded-l-none"
          onClick={() => onChange('course-based')}
        >
          <Calculator className="h-4 w-4 mr-2" />
          Course-Based
        </Button>
      </div>
    </div>
  )
})

interface ManualEntryFieldsProps {
  readonly description: string
  readonly amount: string
  readonly entryType: LedgerEntryType
  readonly onDescriptionChange: (value: string) => void
  readonly onAmountChange: (value: string) => void
  readonly onEntryTypeChange: (value: LedgerEntryType) => void
}

const ManualEntryFields = memo(function ManualEntryFields({
  description,
  amount,
  entryType,
  onDescriptionChange,
  onAmountChange,
  onEntryTypeChange,
}: ManualEntryFieldsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Input
          id="description"
          value={description}
          onChange={(e) => onDescriptionChange(e.target.value)}
          placeholder="e.g., Lesson credit"
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="amount">Amount</Label>
        <Input
          id="amount"
          type="number"
          step="0.01"
          min="0.01"
          value={amount}
          onChange={(e) => onAmountChange(e.target.value)}
          placeholder="0.00"
          required
        />
      </div>
      <EntryTypeSelect value={entryType} onChange={onEntryTypeChange} />
    </div>
  )
})

interface CourseBasedFieldsProps {
  readonly description: string
  readonly entryType: LedgerEntryType
  readonly selectedCourseId: string
  readonly numberOfOccurrences: string
  readonly activeEnrollments: readonly StudentEnrollment[]
  readonly enrollmentPricing?: Readonly<EnrollmentPricing>
  readonly calculatedAmount: number
  readonly isPricingLoading: boolean
  readonly onDescriptionChange: (value: string) => void
  readonly onEntryTypeChange: (value: LedgerEntryType) => void
  readonly onCourseChange: (courseId: string) => void
  readonly onOccurrencesChange: (value: string) => void
}

const CourseBasedFields = memo(function CourseBasedFields({
  description,
  entryType,
  selectedCourseId,
  numberOfOccurrences,
  activeEnrollments,
  enrollmentPricing,
  calculatedAmount,
  isPricingLoading,
  onDescriptionChange,
  onEntryTypeChange,
  onCourseChange,
  onOccurrencesChange,
}: CourseBasedFieldsProps) {
  return (
    <div className="space-y-4">
      <div className="grid gap-4 md:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="description-course">Description</Label>
          <Input
            id="description-course"
            value={description}
            onChange={(e) => onDescriptionChange(e.target.value)}
            placeholder="e.g., Missed lessons credit"
            required
          />
        </div>
        <EntryTypeSelect value={entryType} onChange={onEntryTypeChange} id="entryType-course" />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="courseEnrollment">Course Enrollment</Label>
          <Select value={selectedCourseId} onValueChange={onCourseChange}>
            <SelectTrigger>
              <SelectValue placeholder="Select enrollment..." />
            </SelectTrigger>
            <SelectContent>
              {activeEnrollments.map((enrollment) => (
                <SelectItem key={enrollment.id} value={enrollment.courseId}>
                  {enrollment.instrumentName} - {enrollment.courseTypeName} ({enrollment.teacherName})
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="numberOfOccurrences">Number of Lessons</Label>
          <Input
            id="numberOfOccurrences"
            type="number"
            min="1"
            step="1"
            value={numberOfOccurrences}
            onChange={(e) => onOccurrencesChange(e.target.value)}
            placeholder="Enter number of lessons"
            disabled={!selectedCourseId}
          />
        </div>
      </div>

      {selectedCourseId && (
        <PricingSection
          isPricingLoading={isPricingLoading}
          enrollmentPricing={enrollmentPricing}
          numberOfOccurrences={numberOfOccurrences}
          calculatedAmount={calculatedAmount}
        />
      )}
    </div>
  )
})

interface EntryTypeSelectProps {
  readonly value: LedgerEntryType
  readonly onChange: (value: LedgerEntryType) => void
  readonly id?: string
}

const EntryTypeSelect = memo(function EntryTypeSelect({
  value,
  onChange,
  id = 'entryType',
}: EntryTypeSelectProps) {
  const handleValueChange = (v: string) => {
    if (isLedgerEntryType(v)) {
      onChange(v)
    }
  }

  return (
    <div className="space-y-2">
      <Label htmlFor={id}>Type</Label>
      <Select value={value} onValueChange={handleValueChange}>
        <SelectTrigger>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="Credit">Credit</SelectItem>
          <SelectItem value="Debit">Debit</SelectItem>
        </SelectContent>
      </Select>
    </div>
  )
})

interface PricingSectionProps {
  readonly isPricingLoading: boolean
  readonly enrollmentPricing?: Readonly<EnrollmentPricing>
  readonly numberOfOccurrences: string
  readonly calculatedAmount: number
}

const PricingSection = memo(function PricingSection({
  isPricingLoading,
  enrollmentPricing,
  numberOfOccurrences,
  calculatedAmount,
}: PricingSectionProps) {
  if (isPricingLoading) {
    return (
      <div className="mt-4 flex items-center justify-center py-4">
        <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!enrollmentPricing) {
    return (
      <p className="mt-4 text-sm text-muted-foreground">
        No pricing information available for this enrollment.
      </p>
    )
  }

  return (
    <div className="mt-4">
      <PricingBreakdown
        pricing={enrollmentPricing}
        numberOfOccurrences={numberOfOccurrences}
        calculatedAmount={calculatedAmount}
      />
    </div>
  )
})
