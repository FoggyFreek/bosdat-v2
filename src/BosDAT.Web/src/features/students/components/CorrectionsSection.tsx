import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Undo2, Calculator, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { studentLedgerApi, enrollmentsApi } from '@/services/api'
import type {
  StudentLedgerEntry,
  LedgerEntryType,
  StudentEnrollment,
  EnrollmentPricing,
} from '@/features/students/types'
import { formatDate, cn } from '@/lib/utils'

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

  // Fetch student enrollments for course dropdown
  const { data: enrollments = [] } = useQuery<StudentEnrollment[]>({
    queryKey: ['enrollments', 'student', studentId],
    queryFn: () => enrollmentsApi.getByStudent(studentId),
    enabled: showAddForm && calculationMethod === 'course-based',
  })

  // Fetch pricing when course selected
  const { data: enrollmentPricing, isLoading: isPricingLoading } = useQuery<EnrollmentPricing>({
    queryKey: ['enrollment-pricing', studentId, selectedCourseId],
    queryFn: () => enrollmentsApi.getEnrollmentPricing(studentId, selectedCourseId),
    enabled: !!selectedCourseId && calculationMethod === 'course-based',
  })

  // Filter entries by status
  const filteredEntries = useMemo(() => {
    if (statusFilter === 'active') {
      return entries.filter((e) => e.status === 'Open' || e.status === 'PartiallyApplied')
    }
    return entries
  }, [entries, statusFilter])

  // Calculate amount for course-based method
  const calculatedAmount = useMemo(() => {
    if (calculationMethod === 'course-based' && enrollmentPricing && numberOfOccurrences) {
      const occurrences = parseInt(numberOfOccurrences, 10)
      if (!isNaN(occurrences) && occurrences > 0) {
        return enrollmentPricing.pricePerLesson * occurrences
      }
    }
    return 0
  }, [calculationMethod, enrollmentPricing, numberOfOccurrences])

  // Get active enrollments only (Active or Trail status)
  const activeEnrollments = useMemo(() => {
    return enrollments.filter((e) => e.status === 'Active' || e.status === 'Trail')
  }, [enrollments])

  const createMutation = useMutation({
    mutationFn: () => {
      const finalAmount =
        calculationMethod === 'course-based' ? calculatedAmount : parseFloat(amount)

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
      calculationMethod === 'course-based' ? calculatedAmount : parseFloat(amount)

    if (description && finalAmount > 0) {
      createMutation.mutate()
    }
  }

  const handleReverse = () => {
    if (reverseDialog.entryId && reverseReason.trim()) {
      reverseMutation.mutate({ id: reverseDialog.entryId, reason: reverseReason })
    }
  }

  const isFormValid = () => {
    if (!description) return false
    if (calculationMethod === 'manual') {
      return amount && parseFloat(amount) > 0
    }
    return selectedCourseId && numberOfOccurrences && parseInt(numberOfOccurrences, 10) > 0
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Corrections</h2>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Ledger Entries</CardTitle>
          <div className="flex items-center gap-4">
            {/* Status Filter Toggle */}
            <div className="flex items-center rounded-md border">
              <Button
                type="button"
                variant={statusFilter === 'active' ? 'default' : 'ghost'}
                size="sm"
                className="rounded-r-none"
                onClick={() => setStatusFilter('active')}
              >
                Active
              </Button>
              <Button
                type="button"
                variant={statusFilter === 'all' ? 'default' : 'ghost'}
                size="sm"
                className="rounded-l-none"
                onClick={() => setStatusFilter('all')}
              >
                All
              </Button>
            </div>
            <Button size="sm" onClick={() => setShowAddForm(!showAddForm)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Correction
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {showAddForm && (
            <form onSubmit={handleSubmit} className="mb-6 p-4 bg-muted/50 rounded-lg space-y-4">
              {/* Calculation Method Toggle */}
              <div className="space-y-2">
                <Label>Calculation Method</Label>
                <div className="flex items-center rounded-md border w-fit">
                  <Button
                    type="button"
                    variant={calculationMethod === 'manual' ? 'default' : 'ghost'}
                    size="sm"
                    className="rounded-r-none"
                    onClick={() => {
                      setCalculationMethod('manual')
                      setSelectedCourseId('')
                      setNumberOfOccurrences('')
                    }}
                  >
                    <Pencil className="h-4 w-4 mr-2" />
                    Manual Entry
                  </Button>
                  <Button
                    type="button"
                    variant={calculationMethod === 'course-based' ? 'default' : 'ghost'}
                    size="sm"
                    className="rounded-l-none"
                    onClick={() => {
                      setCalculationMethod('course-based')
                      setAmount('')
                    }}
                  >
                    <Calculator className="h-4 w-4 mr-2" />
                    Course-Based
                  </Button>
                </div>
              </div>

              {calculationMethod === 'manual' ? (
                /* Manual Entry Fields */
                <div className="grid gap-4 md:grid-cols-3">
                  <div className="space-y-2">
                    <Label htmlFor="description">Description</Label>
                    <Input
                      id="description"
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
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
                      onChange={(e) => setAmount(e.target.value)}
                      placeholder="0.00"
                      required
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="entryType">Type</Label>
                    <Select
                      value={entryType}
                      onValueChange={(v) => setEntryType(v as LedgerEntryType)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Credit">Credit</SelectItem>
                        <SelectItem value="Debit">Debit</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              ) : (
                /* Course-Based Fields */
                <div className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="description-course">Description</Label>
                      <Input
                        id="description-course"
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="e.g., Missed lessons credit"
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="entryType-course">Type</Label>
                      <Select
                        value={entryType}
                        onValueChange={(v) => setEntryType(v as LedgerEntryType)}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Credit">Credit</SelectItem>
                          <SelectItem value="Debit">Debit</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="courseEnrollment">Course Enrollment</Label>
                      <Select value={selectedCourseId} onValueChange={setSelectedCourseId}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select enrollment..." />
                        </SelectTrigger>
                        <SelectContent>
                          {activeEnrollments.map((enrollment) => (
                            <SelectItem key={enrollment.id} value={enrollment.courseId}>
                              {enrollment.instrumentName} - {enrollment.courseTypeName} (
                              {enrollment.teacherName})
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
                        onChange={(e) => setNumberOfOccurrences(e.target.value)}
                        placeholder="Enter number of lessons"
                        disabled={!selectedCourseId}
                      />
                    </div>
                  </div>

                  {/* Pricing Breakdown Card */}
                  {selectedCourseId && (
                    <div className="mt-4">
                      {isPricingLoading ? (
                        <div className="flex items-center justify-center py-4">
                          <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                        </div>
                      ) : enrollmentPricing ? (
                        <Card className="bg-background">
                          <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium">Pricing Breakdown</CardTitle>
                          </CardHeader>
                          <CardContent className="space-y-2 text-sm">
                            <div className="flex justify-between">
                              <span className="text-muted-foreground">Course:</span>
                              <span>{enrollmentPricing.courseName}</span>
                            </div>
                            <div className="flex justify-between">
                              <span className="text-muted-foreground">
                                Base Price ({enrollmentPricing.isChildPricing ? 'Child' : 'Adult'}):
                              </span>
                              <span>€{enrollmentPricing.applicableBasePrice.toFixed(2)}</span>
                            </div>
                            {enrollmentPricing.discountPercent > 0 && (
                              <div className="flex justify-between">
                                <span className="text-muted-foreground">Discount:</span>
                                <span className="text-green-600">
                                  -{enrollmentPricing.discountPercent}% (€
                                  {enrollmentPricing.discountAmount.toFixed(2)})
                                </span>
                              </div>
                            )}
                            <div className="flex justify-between font-medium border-t pt-2">
                              <span>Price per Lesson:</span>
                              <span>€{enrollmentPricing.pricePerLesson.toFixed(2)}</span>
                            </div>
                            {numberOfOccurrences && parseInt(numberOfOccurrences, 10) > 0 && (
                              <div className="flex justify-between font-bold text-base border-t pt-2">
                                <span>
                                  Total ({numberOfOccurrences}{' '}
                                  {parseInt(numberOfOccurrences, 10) === 1 ? 'lesson' : 'lessons'}):
                                </span>
                                <span className="text-primary">€{calculatedAmount.toFixed(2)}</span>
                              </div>
                            )}
                          </CardContent>
                        </Card>
                      ) : (
                        <p className="text-sm text-muted-foreground">
                          No pricing information available for this enrollment.
                        </p>
                      )}
                    </div>
                  )}
                </div>
              )}

              <div className="flex gap-2">
                <Button type="submit" disabled={createMutation.isPending || !isFormValid()}>
                  {createMutation.isPending ? 'Adding...' : 'Add Entry'}
                </Button>
                <Button type="button" variant="outline" onClick={resetForm}>
                  Cancel
                </Button>
              </div>
            </form>
          )}

          {filteredEntries.length === 0 ? (
            <p className="text-muted-foreground">
              {statusFilter === 'active' ? 'No active corrections' : 'No corrections yet'}
            </p>
          ) : (
            <div className="divide-y">
              {filteredEntries.map((entry) => (
                <div key={entry.id} className="flex items-center justify-between py-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-sm text-muted-foreground">
                        {entry.correctionRefName}
                      </span>
                      <span
                        className={cn(
                          'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                          entry.entryType === 'Credit' && 'bg-green-100 text-green-800',
                          entry.entryType === 'Debit' && 'bg-red-100 text-red-800'
                        )}
                      >
                        {entry.entryType}
                      </span>
                      <span
                        className={cn(
                          'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                          entry.status === 'Open' && 'bg-blue-100 text-blue-800',
                          entry.status === 'Applied' && 'bg-gray-100 text-gray-800',
                          entry.status === 'PartiallyApplied' && 'bg-yellow-100 text-yellow-800',
                          entry.status === 'Reversed' && 'bg-red-100 text-red-800'
                        )}
                      >
                        {entry.status}
                      </span>
                    </div>
                    <p className="font-medium">{entry.description}</p>
                    <p className="text-sm text-muted-foreground">
                      {formatDate(entry.createdAt)} by {entry.createdByName}
                    </p>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p
                        className={cn(
                          'font-medium',
                          entry.entryType === 'Credit' && 'text-green-600',
                          entry.entryType === 'Debit' && 'text-red-600'
                        )}
                      >
                        {entry.entryType === 'Credit' ? '+' : '-'}€{entry.amount.toFixed(2)}
                      </p>
                      {entry.remainingAmount !== entry.amount && (
                        <p className="text-xs text-muted-foreground">
                          Remaining: €{entry.remainingAmount.toFixed(2)}
                        </p>
                      )}
                    </div>
                    {entry.status === 'Open' && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-orange-600 hover:text-orange-700 hover:bg-orange-50"
                        onClick={() => setReverseDialog({ open: true, entryId: entry.id })}
                      >
                        <Undo2 className="h-4 w-4 mr-1" />
                        Reverse
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={reverseDialog.open} onOpenChange={(open) => setReverseDialog({ open })}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reverse Entry</DialogTitle>
            <DialogDescription>
              This will create an offsetting entry. Please provide a reason for the reversal.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="reverseReason">Reason</Label>
              <Input
                id="reverseReason"
                value={reverseReason}
                onChange={(e) => setReverseReason(e.target.value)}
                placeholder="e.g., Entered in error"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setReverseDialog({ open: false })}>
              Cancel
            </Button>
            <Button
              onClick={handleReverse}
              disabled={!reverseReason.trim() || reverseMutation.isPending}
            >
              {reverseMutation.isPending ? 'Reversing...' : 'Reverse Entry'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
