import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { studentLedgerApi } from '@/features/students/api'
import { enrollmentsApi } from '@/features/enrollments/api'
import type {
  StudentLedgerEntry,
  LedgerEntryType,
  LedgerApplication,
  StudentEnrollment,
  EnrollmentPricing,
} from '@/features/students/types'
import { CorrectionForm, LedgerEntryRow, ReverseDialog, DecoupleDialog } from './corrections'

interface CorrectionsSectionProps {
  studentId: string
}

type CalculationMethod = 'manual' | 'course-based'
type StatusFilter = 'active' | 'all'

export function CorrectionsSection({ studentId }: CorrectionsSectionProps) {
  const queryClient = useQueryClient()
  const [showAddForm, setShowAddForm] = useState(false)
  const [reverseDialog, setReverseDialog] = useState<{ open: boolean; entryId?: string }>({
    open: false,
  })
  const [reverseReason, setReverseReason] = useState('')
  const [decoupleDialog, setDecoupleDialog] = useState<{ open: boolean; application: LedgerApplication | null }>({
    open: false,
    application: null,
  })
  const [decoupleReason, setDecoupleReason] = useState('')

  // Filter state
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('active')

  // Calculation method state
  const [calculationMethod, setCalculationMethod] = useState<CalculationMethod>('manual')
  const [selectedCourseId, setSelectedCourseId] = useState('')
  const [numberOfOccurrences, setNumberOfOccurrences] = useState('')

  // Form state
  const [description, setDescription] = useState('')
  const [amount, setAmount] = useState('')
  const [entryType, setEntryType] = useState<LedgerEntryType>('Credit')

  const { data: entries = [], isLoading } = useQuery<StudentLedgerEntry[]>({
    queryKey: ['student-ledger', studentId],
    queryFn: () => studentLedgerApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  const { data: enrollments = [] } = useQuery<StudentEnrollment[]>({
    queryKey: ['enrollments', 'student', studentId],
    queryFn: () => enrollmentsApi.getByStudent(studentId),
    enabled: showAddForm && calculationMethod === 'course-based',
  })

  const { data: enrollmentPricing, isLoading: isPricingLoading } = useQuery<EnrollmentPricing>({
    queryKey: ['enrollment-pricing', studentId, selectedCourseId],
    queryFn: () => enrollmentsApi.getEnrollmentPricing(studentId, selectedCourseId),
    enabled: !!selectedCourseId && calculationMethod === 'course-based',
  })

  const filteredEntries = useMemo(() => {
    if (statusFilter === 'active') {
      return entries.filter((e) => e.status === 'Open' || e.status === 'PartiallyApplied')
    }
    return entries
  }, [entries, statusFilter])

  const calculatedAmount = useMemo(() => {
    if (calculationMethod !== 'course-based' || !enrollmentPricing || !numberOfOccurrences) {
      return 0
    }
    const occurrences = Number.parseInt(numberOfOccurrences, 10)
    return !Number.isNaN(occurrences) && occurrences > 0 ? enrollmentPricing.pricePerLesson * occurrences : 0
  }, [calculationMethod, enrollmentPricing, numberOfOccurrences])

  const activeEnrollments = useMemo(() => {
    return enrollments.filter((e) => e.status === 'Active' || e.status === 'Trail')
  }, [enrollments])

  const createMutation = useMutation({
    mutationFn: () => {
      const finalAmount =
        calculationMethod === 'course-based' ? calculatedAmount : Number.parseFloat(amount)
      return studentLedgerApi.create({
        studentId,
        description,
        amount: finalAmount,
        entryType,
        courseId: calculationMethod === 'course-based' ? selectedCourseId : undefined,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['student-ledger', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-ledger-summary', studentId] })
      resetForm()
    },
  })

  const reverseMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      studentLedgerApi.reverse(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['student-ledger', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-ledger-summary', studentId] })
      setReverseDialog({ open: false })
      setReverseReason('')
    },
  })

  const decoupleMutation = useMutation({
    mutationFn: ({ applicationId, reason }: { applicationId: string; reason: string }) =>
      studentLedgerApi.decouple(applicationId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['student-ledger', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-ledger-summary', studentId] })
      setDecoupleDialog({ open: false, application: null })
      setDecoupleReason('')
    },
  })

  const resetForm = () => {
    setShowAddForm(false)
    setDescription('')
    setAmount('')
    setEntryType('Credit')
    setCalculationMethod('manual')
    setSelectedCourseId('')
    setNumberOfOccurrences('')
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const finalAmount =
      calculationMethod === 'course-based' ? calculatedAmount : Number.parseFloat(amount)
    if (description && finalAmount > 0) {
      createMutation.mutate()
    }
  }

  const handleReverse = () => {
    if (reverseDialog.entryId && reverseReason.trim()) {
      reverseMutation.mutate({ id: reverseDialog.entryId, reason: reverseReason })
    }
  }

  const handleDecouple = () => {
    if (decoupleDialog.application && decoupleReason.trim()) {
      decoupleMutation.mutate({ applicationId: decoupleDialog.application.id, reason: decoupleReason })
    }
  }

  const handleCalculationMethodChange = (method: CalculationMethod) => {
    setCalculationMethod(method)
    if (method === 'manual') {
      setSelectedCourseId('')
      setNumberOfOccurrences('')
    } else {
      setAmount('')
    }
  }

  const isFormValid = (): boolean => {
    if (!description) return false
    if (calculationMethod === 'manual') {
      return !!amount && Number.parseFloat(amount) > 0
    }
    return !!selectedCourseId && !!numberOfOccurrences && Number.parseInt(numberOfOccurrences, 10) > 0
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  const emptyMessage = statusFilter === 'active' ? 'No active corrections' : 'No corrections yet'

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Corrections</h2>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Ledger Entries</CardTitle>
          <div className="flex items-center gap-4">
            <StatusFilterToggle value={statusFilter} onChange={setStatusFilter} />
            <Button size="sm" onClick={() => setShowAddForm(!showAddForm)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Correction
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {showAddForm && (
            <CorrectionForm
              description={description}
              amount={amount}
              entryType={entryType}
              calculationMethod={calculationMethod}
              selectedCourseId={selectedCourseId}
              numberOfOccurrences={numberOfOccurrences}
              activeEnrollments={activeEnrollments}
              enrollmentPricing={enrollmentPricing}
              calculatedAmount={calculatedAmount}
              isPricingLoading={isPricingLoading}
              isSubmitting={createMutation.isPending}
              onDescriptionChange={setDescription}
              onAmountChange={setAmount}
              onEntryTypeChange={setEntryType}
              onCalculationMethodChange={handleCalculationMethodChange}
              onCourseChange={setSelectedCourseId}
              onOccurrencesChange={setNumberOfOccurrences}
              onSubmit={handleSubmit}
              onCancel={resetForm}
              isFormValid={isFormValid()}
            />
          )}

          {filteredEntries.length === 0 ? (
            <p className="text-muted-foreground">{emptyMessage}</p>
          ) : (
            <div className="divide-y">
              {filteredEntries.map((entry) => (
                <LedgerEntryRow
                  key={entry.id}
                  entry={entry}
                  onReverse={(entryId) => setReverseDialog({ open: true, entryId })}
                  onDecouple={(application) => setDecoupleDialog({ open: true, application })}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <ReverseDialog
        open={reverseDialog.open}
        reason={reverseReason}
        isPending={reverseMutation.isPending}
        onOpenChange={(open) => setReverseDialog({ open })}
        onReasonChange={setReverseReason}
        onConfirm={handleReverse}
      />

      <DecoupleDialog
        open={decoupleDialog.open}
        application={decoupleDialog.application}
        reason={decoupleReason}
        isPending={decoupleMutation.isPending}
        onOpenChange={(open) => setDecoupleDialog({ open, application: open ? decoupleDialog.application : null })}
        onReasonChange={setDecoupleReason}
        onConfirm={handleDecouple}
      />
    </div>
  )
}

interface StatusFilterToggleProps {
  value: StatusFilter
  onChange: (value: StatusFilter) => void
}

function StatusFilterToggle({ value, onChange }: StatusFilterToggleProps) {
  return (
    <div className="flex items-center rounded-md border">
      <Button
        type="button"
        variant={value === 'active' ? 'default' : 'ghost'}
        size="sm"
        className="rounded-r-none"
        onClick={() => onChange('active')}
      >
        Active
      </Button>
      <Button
        type="button"
        variant={value === 'all' ? 'default' : 'ghost'}
        size="sm"
        className="rounded-l-none"
        onClick={() => onChange('all')}
      >
        All
      </Button>
    </div>
  )
}
